using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class WhiteboardController :
    AppContentController,
    IFileContentReceiver
{
    [SerializeField]
    private WhiteboardContentCatalog contentCatalog;

    private static readonly Dictionary<string, WhiteboardEngine> Sessions =
        new Dictionary<string, WhiteboardEngine>();

    private static string lastSessionKey;

    private WhiteboardBinder binder;
    private WhiteboardViewBuilder builder;
    private WhiteboardView view;
    private WhiteboardEngine engine;

    private string contextItemId;
    private string connectorSourceId;

    private bool spaceHeld;
    private int pointerId = -1;
    private int gestureButton;

    private Gesture gesture;
    private Vector2 pointerStart;
    private Vector2 panStart;
    private Vector2 itemStart;
    private WhiteboardItem gestureItem;

    private enum Gesture
    {
        None,
        Pan,
        Move,
        Draw,
        Line
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessions()
    {
        Sessions.Clear();
        lastSessionKey = null;
    }

    protected override void OnInitialize()
    {
        binder = new WhiteboardBinder(InstanceRoot);

        if (!binder.IsValid)
        {
            throw new InvalidOperationException(
                "Whiteboard UI is missing required elements.");
        }

        builder = new WhiteboardViewBuilder();
        view = new WhiteboardView(binder, builder);

        RegisterCallbacks();

        if (lastSessionKey != null &&
            Sessions.TryGetValue(lastSessionKey, out WhiteboardEngine previous))
        {
            engine = previous;
        }
        else
        {
            OpenStarter(WhiteboardContentCatalog.StartHereId);
        }

        Refresh(true);
    }

    protected override void OnShutdown()
    {
        CancelGesture();
        view?.Dispose();

        binder = null;
        builder = null;
        view = null;
        engine = null;

        connectorSourceId = null;
        contextItemId = null;
        spaceHeld = false;
    }

    public bool TryOpenFile(
        FileOpenRequest request,
        out string error)
    {
        error = null;

        if (!IsInitialized || binder == null)
        {
            error = "Whiteboard is not initialized.";
            return false;
        }

        if (request?.File == null ||
            request.File.Hidden ||
            string.IsNullOrWhiteSpace(request.File.Id) ||
            string.IsNullOrWhiteSpace(request.UserId))
        {
            error = "The whiteboard file request is invalid.";
            return false;
        }

        // The enum integration is shown after these seven scripts.
        if (!(request.File.Name ?? "").EndsWith(
            ".whiteboard",
            StringComparison.OrdinalIgnoreCase))
        {
            error = "Whiteboard can only open .whiteboard files.";
            return false;
        }

        string key =
            "file:" + request.UserId.Length + ":" +
            request.UserId + ":" + request.File.Id;

        if (!Sessions.TryGetValue(key, out WhiteboardEngine session))
        {
            if (!TryResolve(
                request.ContentId,
                out WhiteboardDocument document,
                out error))
            {
                return false;
            }

            // Authored files are opened as editable session copies.
            session = new WhiteboardEngine(document);
            Sessions.Add(key, session);
        }

        UseSession(key, session);
        return true;
    }

    private void RegisterCallbacks()
    {
        Click("NavigationToolButton", () =>
            SetTool(engine.Tool == WhiteboardTool.Select
                ? WhiteboardTool.Pan
                : WhiteboardTool.Select));

        Click("StickyNoteToolButton",
            () => SetTool(WhiteboardTool.StickyNote));

        Click("TextToolButton",
            () => SetTool(WhiteboardTool.Text));

        Click("DrawToolButton",
            () => SetTool(WhiteboardTool.Draw));

        Click("FrameToolButton",
            () => SetTool(WhiteboardTool.Frame));

        Click("StickerToolButton",
            () => SetTool(WhiteboardTool.Sticker));

        Click("ShapesAndLinesToolButton", ShowShapes);
        Click("FileMenuButton", ShowFileMenu);
        Click("EditMenuButton", ShowEditMenu);
        Click("ViewMenuButton", ShowViewMenu);
        Click("FramesButton", ShowFrames);

        Click("HelpButton",
            () => OpenStarter(WhiteboardContentCatalog.TutorialId));

        Click("UndoButton", () => Change(engine.Undo));
        Click("RedoButton", () => Change(engine.Redo));

        Click("ZoomOutButton", () => ZoomBy(1f / 1.2f));
        Click("ZoomInButton", () => ZoomBy(1.2f));
        Click("ZoomValueButton", ShowZoomMenu);
        Click("FitToContentButton", FitContent);

        Click("BoardSettingsButton", () =>
            view.SetSettingsVisible(!view.SettingsVisible));

        Click("CloseBoardSettingsButton",
            () => view.SetSettingsVisible(false));

        Background("BackgroundDarkButton",
            new Color(0.075f, 0.085f, 0.11f));

        Background("BackgroundSlateButton",
            new Color(0.15f, 0.18f, 0.23f));

        Background("BackgroundBlueButton",
            new Color(0.06f, 0.13f, 0.23f));

        Background("BackgroundGreenButton",
            new Color(0.06f, 0.17f, 0.13f));

        BindCallback<ChangeEvent<string>>(
            binder.BoardTitleField,
            evt => Change(() => engine.Rename(evt.newValue)));

        BindCallback<ChangeEvent<bool>>(
            binder.ShowGridToggle,
            evt => Change(() => engine.SetShowGrid(evt.newValue)));

        BindCallback<ChangeEvent<bool>>(
            binder.SnapToGridToggle,
            evt => Change(() => engine.SetSnapToGrid(evt.newValue)));

        BindCallback<PointerDownEvent>(
            binder.Root, OnRootPointerDown, TrickleDown.TrickleDown);

        BindCallback<PointerDownEvent>(
            binder.BoardViewport, OnPointerDown);

        BindCallback<PointerMoveEvent>(
            binder.BoardViewport, OnPointerMove);

        BindCallback<PointerUpEvent>(
            binder.BoardViewport, OnPointerUp);

        BindCallback<PointerCancelEvent>(
            binder.BoardViewport, evt =>
            {
                if (evt.pointerId == pointerId)
                {
                    CancelGesture();
                    Refresh(true);
                }
            });

        BindCallback<PointerCaptureOutEvent>(
            binder.BoardViewport, evt =>
            {
                if (evt.pointerId == pointerId)
                {
                    CancelGesture();
                    Refresh(true);
                }
            });

        BindCallback<WheelEvent>(
            binder.BoardViewport, OnWheel);

        BindCallback<KeyDownEvent>(binder.Root, OnKeyDown);
        BindCallback<KeyUpEvent>(binder.Root, OnKeyUp);

        BindCallback<FocusOutEvent>(binder.Root, evt =>
        {
            VisualElement next = evt.relatedTarget as VisualElement;

            if (next != null &&
                (next == binder.Root || binder.Root.Contains(next)))
            {
                return;
            }

            spaceHeld = false;
            CancelGesture();
            Refresh(true);
        });

        BindCallback<DetachFromPanelEvent>(
            binder.Root, evt =>
            {
                spaceHeld = false;
                CancelGesture();
            });
    }

    private void Click(string name, Action action)
    {
        BindClick(binder[name], action);
    }

    private void Background(string name, Color color)
    {
        Click(name, () => Change(() => engine.SetBackground(color)));
    }

    private bool TryResolve(
        string contentId,
        out WhiteboardDocument document,
        out string error)
    {
        if (contentCatalog != null)
            return contentCatalog.TryResolve(contentId, out document, out error);

        return WhiteboardContentCatalog.TryCreateBuiltIn(
            contentId, out document, out error);
    }

    private void OpenStarter(string contentId)
    {
        string key = "starter:" + contentId;

        if (!Sessions.TryGetValue(key, out WhiteboardEngine session))
        {
            if (!TryResolve(
                contentId,
                out WhiteboardDocument document,
                out string error))
            {
                Debug.LogWarning(error, this);
                return;
            }

            session = new WhiteboardEngine(document);
            Sessions.Add(key, session);
        }

        UseSession(key, session);
    }

    private void NewBoard()
    {
        WhiteboardDocument document = new WhiteboardDocument();
        string key = "new:" + document.Id;

        WhiteboardEngine session = new WhiteboardEngine(document);
        Sessions.Add(key, session);

        UseSession(key, session);
    }

    private void UseSession(string key, WhiteboardEngine session)
    {
        CancelGesture();

        engine = session;
        lastSessionKey = key;

        connectorSourceId = null;
        contextItemId = null;

        view.HidePopup();
        Refresh(true);
    }

    private void SetTool(WhiteboardTool tool)
    {
        CancelGesture();
        connectorSourceId = null;
        engine.Tool = tool;

        view.HidePopup();
        Refresh();
        binder.BoardViewport.Focus();
    }

    private void Change(Action action)
    {
        CancelGesture();
        action();
        Refresh(true);
    }

    private void Refresh(bool rebuildContext = false)
    {
        if (engine == null || view == null)
            return;

        string hint = connectorSourceId != null
            ? "Now click the destination object · Esc to cancel"
            : null;

        view.Render(builder.Build(engine, hint));

        WhiteboardItem item = engine.Selected;

        if (rebuildContext || contextItemId != item?.Id)
        {
            contextItemId = item?.Id;

            binder.ContextToolbar.EnableInClassList(
                "wb-hidden", item == null);

            if (item != null)
            {
                builder.BuildContextToolbar(
                    binder.ContextToolbar,
                    item,
                    text => Change(() => engine.SetSelectedText(text)),
                    size => Change(() => engine.ResizeSelected(size)),
                    color => Change(() => engine.SetSelectedColor(color)),
                    () => Change(engine.DeleteSelected));
            }
            else
            {
                binder.ContextToolbar.Clear();
            }
        }
    }

    private void OnRootPointerDown(PointerDownEvent evt)
    {
        VisualElement target = evt.target as VisualElement;

        if (!view.IsInsidePopup(target))
            view.HidePopup();
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (gesture != Gesture.None)
            return;

        bool pan = evt.button == 2 ||
                   (evt.button == 0 &&
                    (spaceHeld || engine.Tool == WhiteboardTool.Pan));

        if (!pan && evt.button != 0)
            return;

        Vector2 local = Local(evt.position);
        Vector2 board = engine.ToBoard(local);

        binder.BoardViewport.Focus();

        if (pan)
        {
            pointerStart = local;
            panStart = engine.Pan;
            BeginGesture(Gesture.Pan, evt);
        }
        else
        {
            switch (engine.Tool)
            {
                case WhiteboardTool.Select:
                    {
                        WhiteboardItem hit = engine.HitTest(board);
                        engine.Select(hit?.Id);

                        if (hit != null && !hit.IsConnection)
                        {
                            pointerStart = board;
                            itemStart = hit.Position;

                            engine.BeginEdit();
                            BeginGesture(Gesture.Move, evt);
                        }

                        Refresh(true);
                        break;
                    }

                case WhiteboardTool.Draw:
                    {
                        engine.BeginEdit();

                        gestureItem = engine.Create(
                            WhiteboardItemKind.Stroke, board);

                        engine.Add(gestureItem);
                        engine.AppendStrokePoint(gestureItem, board);

                        BeginGesture(Gesture.Draw, evt);
                        Refresh(true);
                        break;
                    }

                case WhiteboardTool.Line:
                    {
                        engine.BeginEdit();

                        gestureItem = engine.Create(
                            WhiteboardItemKind.Line, board);

                        engine.Add(gestureItem);
                        BeginGesture(Gesture.Line, evt);
                        Refresh(true);
                        break;
                    }

                case WhiteboardTool.Connector:
                    ConnectAt(board);
                    break;

                default:
                    AddAt(board);
                    break;
            }
        }

        evt.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (evt.pointerId != pointerId)
            return;

        Vector2 local = Local(evt.position);
        Vector2 board = engine.ToBoard(local);

        switch (gesture)
        {
            case Gesture.Pan:
                engine.SetPan(panStart + local - pointerStart);
                break;

            case Gesture.Move:
                engine.MoveSelected(itemStart + board - pointerStart);
                break;

            case Gesture.Draw:
                engine.AppendStrokePoint(gestureItem, board);
                break;

            case Gesture.Line:
                gestureItem.End.Position = engine.Snap(board);
                break;

            default:
                return;
        }

        Refresh();
        evt.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.pointerId != pointerId ||
            evt.button != gestureButton)
        {
            return;
        }

        Vector2 board = engine.ToBoard(Local(evt.position));

        if (gesture == Gesture.Draw)
            engine.AppendStrokePoint(gestureItem, board);

        if (gesture == Gesture.Line)
            gestureItem.End.Position = engine.Snap(board);

        bool invalidStroke =
            gesture == Gesture.Draw &&
            gestureItem.Points.Count < 2;

        bool invalidLine =
            gesture == Gesture.Line &&
            Vector2.Distance(
                gestureItem.Start.Position,
                gestureItem.End.Position) < 2f;

        if (invalidStroke || invalidLine)
            engine.CancelEdit();
        else if (engine.IsEditing)
            engine.CommitEdit();

        ReleaseGesture();
        Refresh(true);
        evt.StopPropagation();
    }

    private void BeginGesture(Gesture next, PointerDownEvent evt)
    {
        gesture = next;
        pointerId = evt.pointerId;
        gestureButton = evt.button;
        binder.BoardViewport.CapturePointer(pointerId);
    }

    private void CancelGesture()
    {
        if (gesture == Gesture.None)
            return;

        if (engine != null)
        {
            if (gesture == Gesture.Pan)
                engine.SetPan(panStart);
            else
                engine.CancelEdit();
        }

        ReleaseGesture();
    }

    private void ReleaseGesture()
    {
        int captured = pointerId;

        pointerId = -1;
        gesture = Gesture.None;
        gestureItem = null;

        if (binder != null &&
            captured >= 0 &&
            binder.BoardViewport.HasPointerCapture(captured))
        {
            binder.BoardViewport.ReleasePointer(captured);
        }
    }

    private void AddAt(Vector2 board)
    {
        WhiteboardItemKind kind;

        switch (engine.Tool)
        {
            case WhiteboardTool.StickyNote:
                kind = WhiteboardItemKind.StickyNote;
                break;

            case WhiteboardTool.Text:
                kind = WhiteboardItemKind.Text;
                break;

            case WhiteboardTool.Rectangle:
                kind = WhiteboardItemKind.Rectangle;
                break;

            case WhiteboardTool.Ellipse:
                kind = WhiteboardItemKind.Ellipse;
                break;

            case WhiteboardTool.Frame:
                kind = WhiteboardItemKind.Frame;
                break;

            case WhiteboardTool.Sticker:
                kind = WhiteboardItemKind.Sticker;
                break;

            default:
                return;
        }

        engine.Add(engine.Create(kind, board));

        // Return to selection so the new object can be moved or edited.
        engine.Tool = WhiteboardTool.Select;
        Refresh(true);
    }

    private void ConnectAt(Vector2 board)
    {
        WhiteboardItem hit = engine.HitTest(board);

        if (hit == null || !hit.CanAttach)
            return;

        WhiteboardItem source =
            engine.Document.Find(connectorSourceId);

        if (source == null)
        {
            connectorSourceId = hit.Id;
            engine.Select(hit.Id);
            Refresh(true);
            return;
        }

        if (source.Id == hit.Id)
            return;

        // Attach to opposite horizontal edges.
        bool destinationOnRight =
            hit.Bounds.center.x >= source.Bounds.center.x;

        WhiteboardItem connector = engine.Create(
            WhiteboardItemKind.Connector, Vector2.zero);

        connector.Start.ItemId = source.Id;
        connector.Start.Anchor = new Vector2(
            destinationOnRight ? 1f : 0f, 0.5f);

        connector.End.ItemId = hit.Id;
        connector.End.Anchor = new Vector2(
            destinationOnRight ? 0f : 1f, 0.5f);

        engine.Add(connector);
        connectorSourceId = null;
        Refresh(true);
    }

    private void OnWheel(WheelEvent evt)
    {
        if (gesture != Gesture.None)
            return;

        float factor = Mathf.Exp(-evt.delta.y * 0.08f);

        engine.SetZoom(
            engine.Zoom * factor,
            Local(evt.mousePosition));

        Refresh();
        evt.StopPropagation();
        evt.PreventDefault();
    }

    private void ZoomBy(float factor)
    {
        CancelGesture();

        engine.SetZoom(
            engine.Zoom * factor,
            binder.BoardViewport.contentRect.size * 0.5f);

        Refresh();
    }

    private void FitContent()
    {
        CancelGesture();
        engine.Fit(binder.BoardViewport.contentRect.size);
        Refresh();
    }

    private Vector2 Local(Vector3 position)
    {
        return binder.BoardViewport.WorldToLocal(
            new Vector2(position.x, position.y));
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (IsEditingField(evt.target as VisualElement))
            return;

        bool command = evt.ctrlKey || evt.commandKey;

        if (evt.keyCode == KeyCode.Escape)
        {
            CancelGesture();
            connectorSourceId = null;
            engine.Tool = WhiteboardTool.Select;
            engine.Select(null);

            view.HidePopup();
            view.SetSettingsVisible(false);
            Refresh(true);
        }
        else if (command && evt.keyCode == KeyCode.Z)
        {
            if (evt.shiftKey)
                Change(engine.Redo);
            else
                Change(engine.Undo);
        }
        else if (command && evt.keyCode == KeyCode.Y)
        {
            Change(engine.Redo);
        }
        else if (command || evt.altKey)
        {
            return;
        }
        else
        {
            switch (evt.keyCode)
            {
                case KeyCode.Space:
                    spaceHeld = true;
                    break;

                case KeyCode.V:
                    SetTool(WhiteboardTool.Select);
                    break;

                case KeyCode.H:
                    SetTool(WhiteboardTool.Pan);
                    break;

                case KeyCode.N:
                    SetTool(WhiteboardTool.StickyNote);
                    break;

                case KeyCode.T:
                    SetTool(WhiteboardTool.Text);
                    break;

                case KeyCode.P:
                    SetTool(WhiteboardTool.Draw);
                    break;

                case KeyCode.F:
                    SetTool(WhiteboardTool.Frame);
                    break;

                case KeyCode.Delete:
                case KeyCode.Backspace:
                    Change(engine.DeleteSelected);
                    break;

                default:
                    return;
            }
        }

        evt.StopPropagation();
        evt.PreventDefault();
    }

    private void OnKeyUp(KeyUpEvent evt)
    {
        if (evt.keyCode == KeyCode.Space)
            spaceHeld = false;
    }

    private static bool IsEditingField(VisualElement target)
    {
        for (VisualElement current = target;
             current != null;
             current = current.parent)
        {
            if (current is TextField ||
                current is FloatField ||
                current is IntegerField)
            {
                return true;
            }
        }

        return false;
    }

    private void ShowShapes()
    {
        Show("ShapesAndLinesToolButton",
            new WhiteboardMenuEntry("Rectangle",
                () => SetTool(WhiteboardTool.Rectangle)),
            new WhiteboardMenuEntry("Ellipse",
                () => SetTool(WhiteboardTool.Ellipse)),
            new WhiteboardMenuEntry("Line",
                () => SetTool(WhiteboardTool.Line)),
            new WhiteboardMenuEntry("Connector",
                () => SetTool(WhiteboardTool.Connector)));
    }

    private void ShowFileMenu()
    {
        List<WhiteboardMenuEntry> entries =
            new List<WhiteboardMenuEntry>
            {
                new WhiteboardMenuEntry("New board", NewBoard),
                new WhiteboardMenuEntry("Start Here.whiteboard",
                    () => OpenStarter(
                        WhiteboardContentCatalog.StartHereId)),
                new WhiteboardMenuEntry("Whiteboard Tutorial.whiteboard",
                    () => OpenStarter(
                        WhiteboardContentCatalog.TutorialId))
            };

        foreach (KeyValuePair<string, WhiteboardEngine> pair in Sessions)
        {
            string key = pair.Key;
            WhiteboardEngine session = pair.Value;

            entries.Add(new WhiteboardMenuEntry(
                "Open: " + session.Document.Title,
                () => UseSession(key, session)));
        }

        view.ShowPopup(binder["FileMenuButton"], entries);
    }

    private void ShowEditMenu()
    {
        Show("EditMenuButton",
            new WhiteboardMenuEntry("Undo",
                () => Change(engine.Undo), engine.CanUndo),
            new WhiteboardMenuEntry("Redo",
                () => Change(engine.Redo), engine.CanRedo),
            new WhiteboardMenuEntry("Delete selection",
                () => Change(engine.DeleteSelected),
                engine.Selected != null));
    }

    private void ShowViewMenu()
    {
        Show("ViewMenuButton",
            new WhiteboardMenuEntry("Fit board", FitContent),
            new WhiteboardMenuEntry("Actual size",
                () =>
                {
                    engine.SetZoom(
                        1f,
                        binder.BoardViewport.contentRect.size * 0.5f);
                    Refresh();
                }),
            new WhiteboardMenuEntry(
                engine.Document.ShowGrid ? "Hide grid" : "Show grid",
                () => Change(() =>
                    engine.SetShowGrid(!engine.Document.ShowGrid))),
            new WhiteboardMenuEntry(
                engine.Document.SnapToGrid
                    ? "Disable snapping"
                    : "Enable snapping",
                () => Change(() =>
                    engine.SetSnapToGrid(!engine.Document.SnapToGrid))));
    }

    private void ShowZoomMenu()
    {
        List<WhiteboardMenuEntry> entries =
            new List<WhiteboardMenuEntry>();

        foreach (int percentage in new[] { 25, 50, 75, 100, 150, 200, 400 })
        {
            int captured = percentage;

            entries.Add(new WhiteboardMenuEntry(
                captured + "%",
                () =>
                {
                    engine.SetZoom(
                        captured / 100f,
                        binder.BoardViewport.contentRect.size * 0.5f);
                    Refresh();
                }));
        }

        entries.Add(new WhiteboardMenuEntry("Fit board", FitContent));
        view.ShowPopup(binder["ZoomValueButton"], entries);
    }

    private void ShowFrames()
    {
        List<WhiteboardMenuEntry> entries =
            new List<WhiteboardMenuEntry>();

        foreach (WhiteboardItem item in engine.Document.Items)
        {
            if (item.Kind != WhiteboardItemKind.Frame)
                continue;

            string id = item.Id;

            entries.Add(new WhiteboardMenuEntry(
                string.IsNullOrWhiteSpace(item.Text)
                    ? "Untitled frame"
                    : item.Text,
                () =>
                {
                    WhiteboardItem frame = engine.Document.Find(id);

                    if (frame == null)
                        return;

                    engine.Fit(
                        frame.Bounds,
                        binder.BoardViewport.contentRect.size);

                    engine.Select(id);
                    Refresh(true);
                }));
        }

        if (entries.Count == 0)
        {
            entries.Add(new WhiteboardMenuEntry(
                "No frames yet", null, false));
        }

        view.ShowPopup(binder["FramesButton"], entries);
    }

    private void Show(
        string anchor,
        params WhiteboardMenuEntry[] entries)
    {
        view.ShowPopup(binder[anchor], entries);
    }
}