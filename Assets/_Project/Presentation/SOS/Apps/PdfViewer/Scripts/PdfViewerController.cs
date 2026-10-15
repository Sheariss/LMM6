using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using Atlas.Presentation.SOS.Windows;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PdfViewerController :
    AppContentController,
    IFileContentReceiver
{
    [Header("File Content")]
    [SerializeField] private PdfContentCatalog pdfContentCatalog;

    private PdfViewerBinder binder;
    private PdfViewerEngine engine;
    private PdfViewerView view;

    private WindowController window;
    private IVisualElementScheduledItem layoutRefresh;

    private bool refreshing;
    private bool fullscreen;
    private bool presentation;
    private bool restoreWindowOnExit;

    private PdfViewerView.Anchor presentationAnchor;

    private int panPointer = -1;
    private int panButton = -1;
    private Vector2 panStart;
    private Vector2 panOffset;

    private int resizePointer = -1;
    private float resizeStartX;
    private float resizeStartWidth;
    private float sidebarWidth = 260f;

    public void ConfigureWindow(WindowController controller)
    {
        window = controller;
        Refresh();
    }

    protected override void OnInitialize()
    {
        binder = new PdfViewerBinder(InstanceRoot);
        engine = new PdfViewerEngine();
        view = new PdfViewerView(binder, NavigateToPage);

        binder.Root.focusable = true;
        binder.SidebarResizeHandle.focusable = true;
        binder.Sidebar.style.width = sidebarWidth;

        binder.FitDropdown.choices = new List<string>
        {
            "Automatic",
            "Fit page",
            "Fit width",
            "Actual size"
        };

        RegisterCallbacks();
        Refresh();
    }

    protected override void OnShutdown()
    {
        layoutRefresh?.Pause();
        layoutRefresh = null;

        EndPan();
        EndResize();

        if (fullscreen)
            ExitFullscreen();

        if (binder != null)
        {
            binder.DocumentOutline.selectedIndicesChanged -=
                OnOutlineSelectionChanged;

            binder.DocumentScrollView.verticalScroller.valueChanged -=
                OnDocumentScrolled;
        }

        view?.Dispose();

        view = null;
        engine = null;
        binder = null;
        window = null;
    }

    public bool TryOpenFile(
        FileOpenRequest request,
        out string error)
    {
        error = null;

        if (!IsInitialized ||
            engine == null ||
            view == null ||
            binder == null)
        {
            error = "PDF Viewer is not initialized.";
            return false;
        }

        if (request?.File == null)
        {
            error = "No PDF file was provided.";
            return false;
        }

        FileSystemFileDefinition file = request.File;

        if (file.FileType != FileSystemFileType.PDF)
        {
            error = "PDF Viewer can only open PDF files.";
            return false;
        }

        if (file.Hidden || string.IsNullOrWhiteSpace(file.Id))
        {
            error = "The selected PDF file is invalid or hidden.";
            return false;
        }

        if (pdfContentCatalog == null)
        {
            error = "PDF Viewer has no PdfContentCatalog assigned.";
            return false;
        }

        if (!pdfContentCatalog.TryResolve(
            file.ContentId,
            out PdfContentEntry document,
            out error))
        {
            return false;
        }

        EndPan();
        EndResize();

        // Preserve an existing document if validation fails above.
        if (fullscreen)
            ExitFullscreen();

        engine.LoadDocument(document, file.Name);
        engine.SetZoomMode(PdfZoomMode.FitWidth);
        engine.HandToolSelected = true;

        Refresh();
        view.RequestPageJump(0);

        return true;
    }

    private void RegisterCallbacks()
    {
        Click("PreviousPageButton",
            () => NavigateToPage(engine.CurrentIndex - 1));

        Click("NextPageButton",
            () => NavigateToPage(engine.CurrentIndex + 1));

        Click("FirstPageButton", () => NavigateToPage(0));
        Click("LastPageButton",
            () => NavigateToPage(engine.PageCount - 1));

        Click("CurrentPageButton",
            () => NavigateToPage(engine.CurrentIndex));

        Click("SidebarToggleButton", () =>
            ChangeLayout(() =>
                engine.SidebarVisible = !engine.SidebarVisible));

        Click("CloseSidebarButton", () =>
            ChangeLayout(() => engine.SidebarVisible = false));

        Click("ThumbnailsTabButton", () =>
        {
            engine.OutlineSelected = false;
            Refresh();
        });

        Click("OutlineTabButton", () =>
        {
            engine.OutlineSelected = true;
            Refresh();
        });

        Click("ZoomOutButton",
            () => ChangeZoom(-PdfViewerEngine.ZoomStep));

        Click("ZoomInButton",
            () => ChangeZoom(PdfViewerEngine.ZoomStep));

        Click("SearchButton", OpenSearch);
        Click("CloseSearchButton", CloseSearch);

        Click("PreviousMatchButton", () => MoveSearchMatch(-1));
        Click("NextMatchButton", () => MoveSearchMatch(1));

        Click("MoreOptionsButton", () =>
        {
            engine.MoreMenuVisible = !engine.MoreMenuVisible;
            Refresh();
        });

        Click("RotateClockwiseButton", () => Rotate(90));
        Click("RotateCounterclockwiseButton", () => Rotate(-90));

        Click("HandToolButton", () =>
        {
            EndPan();
            engine.HandToolSelected = !engine.HandToolSelected;
            engine.MoreMenuVisible = false;
            Refresh();
        });

        Click("ViewerFullscreenButton", () =>
        {
            if (fullscreen)
                ExitFullscreen();
            else
                EnterFullscreen(false);
        });

        Click("PresentationModeButton",
            () => EnterFullscreen(true));

        Click("ExitPresentationButton", ExitFullscreen);

        Click("PresentationPreviousButton",
            () => NavigateToPage(engine.CurrentIndex - 1));

        Click("PresentationNextButton",
            () => NavigateToPage(engine.CurrentIndex + 1));

        BindCallback<ChangeEvent<int>>(
            binder.PageNumberField,
            OnPageNumberChanged);

        BindCallback<ChangeEvent<float>>(
            binder.ZoomField,
            OnZoomFieldChanged);

        BindCallback<ChangeEvent<string>>(
            binder.FitDropdown,
            OnFitChanged);

        BindCallback<ChangeEvent<string>>(
            binder.SearchField,
            OnSearchChanged);

        BindCallback<KeyDownEvent>(
            binder.Root,
            OnKeyDown,
            TrickleDown.TrickleDown);

        BindCallback<PointerDownEvent>(
            binder.Root,
            OnRootPointerDown,
            TrickleDown.TrickleDown);

        BindCallback<GeometryChangedEvent>(
            binder.DocumentScrollView.contentViewport,
            OnViewportChanged);

        BindCallback<GeometryChangedEvent>(
            binder.Body,
            _ => ClampSidebarWidth());

        BindCallback<WheelEvent>(
            binder.DocumentViewport,
            OnWheel,
            TrickleDown.TrickleDown);

        BindCallback<PointerDownEvent>(
            binder.DocumentViewport,
            OnPanDown,
            TrickleDown.TrickleDown);

        BindCallback<PointerMoveEvent>(
            binder.DocumentViewport,
            OnPanMove,
            TrickleDown.TrickleDown);

        BindCallback<PointerUpEvent>(
            binder.DocumentViewport,
            OnPanUp,
            TrickleDown.TrickleDown);

        BindCallback<PointerCancelEvent>(
            binder.DocumentViewport,
            _ => EndPan());

        BindCallback<PointerCaptureOutEvent>(
            binder.DocumentViewport,
            evt =>
            {
                if (evt.pointerId == panPointer)
                {
                    panPointer = -1;
                    panButton = -1;
                    binder.DocumentViewport.RemoveFromClassList(
                        "pdf-dragging");
                }
            });

        BindCallback<PointerDownEvent>(
            binder.SidebarResizeHandle,
            OnResizeDown);

        BindCallback<PointerMoveEvent>(
            binder.SidebarResizeHandle,
            OnResizeMove);

        BindCallback<PointerUpEvent>(
            binder.SidebarResizeHandle,
            evt =>
            {
                if (evt.pointerId != resizePointer ||
                    evt.button != 0)
                {
                    return;
                }

                EndResize();
                evt.StopPropagation();
            });

        BindCallback<PointerCancelEvent>(
            binder.SidebarResizeHandle,
            _ => EndResize());

        BindCallback<PointerCaptureOutEvent>(
            binder.SidebarResizeHandle,
            evt =>
            {
                if (evt.pointerId == resizePointer)
                    resizePointer = -1;
            });

        BindCallback<KeyDownEvent>(
            binder.SidebarResizeHandle,
            OnResizeKeyDown);

        BindCallback<DetachFromPanelEvent>(
            binder.Root,
            _ =>
            {
                EndPan();
                EndResize();
                view.CancelPositioning();
                layoutRefresh?.Pause();
            });

        binder.DocumentOutline.selectedIndicesChanged +=
            OnOutlineSelectionChanged;

        binder.DocumentScrollView.verticalScroller.valueChanged +=
            OnDocumentScrolled;
    }

    private void Click(string name, Action action)
    {
        BindClick(binder.Button(name), action);
    }

    private bool CanFullscreen =>
        window != null &&
        binder != null &&
        binder.Button("WindowMaximizeButton").enabledSelf;

    private void Refresh()
    {
        if (refreshing || view == null || engine == null)
            return;

        refreshing = true;

        try
        {
            view.Render(
                engine,
                fullscreen,
                presentation,
                CanFullscreen);
        }
        finally
        {
            refreshing = false;
        }
    }

    private void NavigateToPage(int index)
    {
        if (!engine.SelectPage(index))
            return;

        EndPan();
        layoutRefresh?.Pause();

        engine.MoreMenuVisible = false;

        Refresh();
        view.RequestPageJump(index);
    }

    private void OnPageNumberChanged(ChangeEvent<int> evt)
    {
        if (!engine.HasDocument)
            return;

        int number = Mathf.Clamp(evt.newValue, 1, engine.PageCount);

        binder.PageNumberField.SetValueWithoutNotify(number);
        NavigateToPage(number - 1);
    }

    private void ChangeZoom(
        float delta,
        Vector2? worldPoint = null)
    {
        if (!engine.HasDocument || presentation)
            return;

        float percentage = view.CurrentScale * 100f + delta;

        SetZoom(percentage, worldPoint);
    }

    private void SetZoom(
        float percentage,
        Vector2? worldPoint = null)
    {
        if (!engine.HasDocument || presentation)
            return;

        if (float.IsNaN(percentage) ||
            float.IsInfinity(percentage))
        {
            binder.ZoomField.SetValueWithoutNotify(
                view.CurrentScale * 100f);

            return;
        }

        EndPan();
        layoutRefresh?.Pause();

        PdfViewerView.Anchor anchor =
            view.CaptureAnchor(worldPoint);

        engine.SetZoom(percentage);

        Refresh();

        binder.ZoomField.SetValueWithoutNotify(
            engine.ZoomPercent);

        view.RestoreAnchor(anchor);
    }

    private void OnZoomFieldChanged(ChangeEvent<float> evt)
    {
        SetZoom(evt.newValue);
    }

    private void OnFitChanged(ChangeEvent<string> evt)
    {
        if (!engine.HasDocument || presentation)
            return;

        ChangeLayout(() =>
        {
            switch (evt.newValue)
            {
                case "Fit page":
                    engine.SetZoomMode(PdfZoomMode.FitPage);
                    break;

                case "Fit width":
                    engine.SetZoomMode(PdfZoomMode.FitWidth);
                    break;

                case "Automatic":
                    engine.SetZoomMode(PdfZoomMode.Automatic);
                    break;

                default:
                    engine.SetZoom(100f);
                    break;
            }
        });
    }

    private void ChangeLayout(Action change)
    {
        EndPan();
        layoutRefresh?.Pause();

        PdfViewerView.Anchor anchor = view.CaptureAnchor();

        change();
        Refresh();

        if (engine.HasDocument)
            view.RestoreAnchor(anchor);
    }

    private void Rotate(int degrees)
    {
        if (!engine.HasDocument)
            return;

        EndPan();
        layoutRefresh?.Pause();

        engine.Rotate(degrees);
        engine.MoreMenuVisible = false;

        Refresh();
        view.RequestPageJump(engine.CurrentIndex);
    }

    private void OpenSearch()
    {
        if (!engine.HasDocument || presentation)
            return;

        ChangeLayout(() =>
        {
            engine.SearchVisible = true;
            engine.MoreMenuVisible = false;
        });

        binder.SearchField.Focus();
        binder.SearchField.SelectAll();
    }

    private void CloseSearch()
    {
        ChangeLayout(() => engine.SearchVisible = false);
        binder.Root.Focus();
    }

    private void OnSearchChanged(ChangeEvent<string> evt)
    {
        engine.Search(evt.newValue);

        layoutRefresh?.Pause();
        Refresh();

        if (engine.SearchMatches.Count > 0)
            view.RequestPageJump(engine.CurrentIndex);
    }

    private void MoveSearchMatch(int direction)
    {
        if (!engine.MoveSearchMatch(direction))
            return;

        layoutRefresh?.Pause();
        Refresh();
        view.RequestPageJump(engine.CurrentIndex);
    }

    private void OnOutlineSelectionChanged(
        IEnumerable<int> indices)
    {
        if (view == null || view.IsRebuilding)
            return;

        foreach (int index in indices)
        {
            if (index < 0)
                continue;

            PdfOutlineContent item = binder.DocumentOutline
                .GetItemDataForIndex<PdfOutlineContent>(index);

            if (item != null)
                NavigateToPage(item.PageNumber - 1);

            break;
        }
    }

    private void OnDocumentScrolled(float value)
    {
        if (refreshing ||
            view == null ||
            view.IsRebuilding ||
            view.IsPositioning ||
            view.HasPendingJump ||
            !engine.HasDocument ||
            presentation)
        {
            return;
        }

        int index = view.DetectCurrentPage();

        if (index < 0 || index == engine.CurrentIndex)
            return;

        engine.SelectPage(index);
        Refresh();
    }

    private void OnViewportChanged(GeometryChangedEvent evt)
    {
        if (engine == null || !engine.HasDocument)
            return;

        bool hadPendingPosition = view.HasPendingJump;

        PdfViewerView.Anchor anchor = view.CaptureAnchor();

        layoutRefresh?.Pause();

        layoutRefresh = binder.Root.schedule.Execute(() =>
        {
            if (view == null || engine == null)
                return;

            Refresh();

            // Do not overwrite a navigation or zoom request.
            if (!hadPendingPosition && !view.HasPendingJump)
                view.RestoreAnchor(anchor);
        });
    }

    private void OnWheel(WheelEvent evt)
    {
        if (presentation)
        {
            evt.StopImmediatePropagation();
            return;
        }

        if (!evt.ctrlKey ||
            !engine.HasDocument ||
            Mathf.Approximately(evt.delta.y, 0f))
        {
            return;
        }

        ChangeZoom(
            evt.delta.y < 0f
                ? PdfViewerEngine.ZoomStep
                : -PdfViewerEngine.ZoomStep,
            (Vector2)evt.mousePosition);

        evt.StopImmediatePropagation();
    }

    private void OnRootPointerDown(PointerDownEvent evt)
    {
        if (!engine.MoreMenuVisible)
            return;

        VisualElement target = evt.target as VisualElement;

        if (Within(target, binder.MoreOptionsMenu) ||
            Within(target, binder.Button("MoreOptionsButton")))
        {
            return;
        }

        engine.MoreMenuVisible = false;
        Refresh();
    }

    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Escape)
        {
            if (panPointer >= 0)
                EndPan();
            else if (resizePointer >= 0)
                EndResize();
            else if (engine.MoreMenuVisible)
            {
                engine.MoreMenuVisible = false;
                Refresh();
            }
            else if (engine.SearchVisible && !presentation)
                CloseSearch();
            else if (fullscreen)
                ExitFullscreen();
            else
                return;

            evt.StopImmediatePropagation();
            return;
        }

        if (evt.ctrlKey && evt.keyCode == KeyCode.F)
        {
            OpenSearch();
            evt.StopImmediatePropagation();
            return;
        }

        VisualElement target = evt.target as VisualElement;

        bool enter =
            evt.keyCode == KeyCode.Return ||
            evt.keyCode == KeyCode.KeypadEnter;

        if (enter && Within(target, binder.SearchField))
        {
            MoveSearchMatch(evt.shiftKey ? -1 : 1);
            evt.StopImmediatePropagation();
            return;
        }

        if (!engine.HasDocument || IsEditingField(target))
            return;

        if (Within(target, binder.DocumentOutline) ||
            Within(target, binder.SidebarResizeHandle))
        {
            return;
        }

        if (evt.keyCode == KeyCode.F11)
        {
            if (fullscreen)
                ExitFullscreen();
            else
                EnterFullscreen(false);

            evt.StopImmediatePropagation();
            return;
        }

        switch (evt.keyCode)
        {
            case KeyCode.PageUp:
                NavigateToPage(engine.CurrentIndex - 1);
                break;

            case KeyCode.PageDown:
                NavigateToPage(engine.CurrentIndex + 1);
                break;

            case KeyCode.Home:
                NavigateToPage(0);
                break;

            case KeyCode.End:
                NavigateToPage(engine.PageCount - 1);
                break;

            case KeyCode.LeftArrow:
            case KeyCode.UpArrow:
                if (!presentation)
                    return;

                NavigateToPage(engine.CurrentIndex - 1);
                break;

            case KeyCode.RightArrow:
            case KeyCode.DownArrow:
            case KeyCode.Space:
                if (!presentation)
                    return;

                NavigateToPage(engine.CurrentIndex + 1);
                break;

            default:
                return;
        }

        evt.StopImmediatePropagation();
    }

    private void EnterFullscreen(bool usePresentation)
    {
        if (!engine.HasDocument || !CanFullscreen)
            return;

        EndPan();
        EndResize();
        layoutRefresh?.Pause();

        PdfViewerView.Anchor anchor = view.CaptureAnchor();

        if (!fullscreen)
        {
            restoreWindowOnExit =
                window.State != WindowState.Maximized;

            window.Maximize();

            if (window.State != WindowState.Maximized)
                return;
        }

        if (usePresentation && !presentation)
            presentationAnchor = anchor;

        fullscreen = true;
        presentation = usePresentation;
        engine.MoreMenuVisible = false;

        Refresh();

        if (presentation)
            view.RequestPageJump(engine.CurrentIndex);
        else
            view.RestoreAnchor(anchor);

        binder.Root.Focus();
    }

    private void ExitFullscreen()
    {
        if (!fullscreen)
            return;

        EndPan();
        layoutRefresh?.Pause();

        bool wasPresentation = presentation;

        PdfViewerView.Anchor anchor = wasPresentation
            ? presentationAnchor
            : view.CaptureAnchor();

        fullscreen = false;
        presentation = false;

        if (restoreWindowOnExit &&
            window != null &&
            window.State == WindowState.Maximized)
        {
            window.Restore();
        }

        restoreWindowOnExit = false;

        Refresh();

        if (engine.HasDocument)
        {
            if (!wasPresentation ||
                anchor.Page == engine.CurrentIndex)
            {
                view.RestoreAnchor(anchor);
            }
            else
            {
                view.RequestPageJump(engine.CurrentIndex);
            }
        }

        binder.Root.Focus();
    }

    private void OnPanDown(PointerDownEvent evt)
    {
        bool allowed =
            evt.button == 2 ||
            (evt.button == 0 && engine.HandToolSelected);

        if (!allowed ||
            presentation ||
            evt.pointerType != UnityEngine.UIElements.PointerType.mouse ||
            panPointer >= 0 ||
            !view.CanPan ||
            view.HasPendingJump)
        {
            return;
        }

        for (VisualElement current = evt.target as VisualElement;
             current != null && current != binder.DocumentViewport;
             current = current.parent)
        {
            if (current is Scroller ||
                current is Slider ||
                current is Button)
            {
                return;
            }
        }

        panPointer = evt.pointerId;
        panButton = evt.button;

        panStart = binder.DocumentViewport.WorldToLocal(
            (Vector2)evt.position);

        panOffset = view.ScrollOffset;

        binder.DocumentViewport.CapturePointer(panPointer);
        binder.DocumentViewport.AddToClassList("pdf-dragging");

        evt.StopImmediatePropagation();
    }

    private void OnPanMove(PointerMoveEvent evt)
    {
        if (panPointer < 0 || evt.pointerId != panPointer)
            return;

        Vector2 pointer = binder.DocumentViewport.WorldToLocal(
            (Vector2)evt.position);

        view.SetScrollOffset(
            panOffset - (pointer - panStart));

        evt.StopImmediatePropagation();
    }

    private void OnPanUp(PointerUpEvent evt)
    {
        if (evt.pointerId != panPointer ||
            evt.button != panButton)
        {
            return;
        }

        EndPan();
        evt.StopImmediatePropagation();
    }

    private void EndPan()
    {
        int pointer = panPointer;

        panPointer = -1;
        panButton = -1;

        if (binder == null)
            return;

        binder.DocumentViewport.RemoveFromClassList("pdf-dragging");

        if (pointer >= 0 &&
            binder.DocumentViewport.HasPointerCapture(pointer))
        {
            binder.DocumentViewport.ReleasePointer(pointer);
        }
    }

    private void OnResizeDown(PointerDownEvent evt)
    {
        if (evt.button != 0 ||
            evt.pointerType != UnityEngine.UIElements.PointerType.mouse ||
            presentation ||
            resizePointer >= 0)
        {
            return;
        }

        EndPan();

        resizePointer = evt.pointerId;

        resizeStartX = binder.Body.WorldToLocal(
            (Vector2)evt.position).x;

        resizeStartWidth = sidebarWidth;

        binder.SidebarResizeHandle.CapturePointer(resizePointer);
        binder.SidebarResizeHandle.Focus();

        evt.StopPropagation();
    }

    private void OnResizeMove(PointerMoveEvent evt)
    {
        if (resizePointer < 0 ||
            evt.pointerId != resizePointer)
        {
            return;
        }

        float x = binder.Body.WorldToLocal(
            (Vector2)evt.position).x;

        SetSidebarWidth(resizeStartWidth + x - resizeStartX);

        evt.StopPropagation();
    }

    private void OnResizeKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.LeftArrow)
            SetSidebarWidth(sidebarWidth - 16f);
        else if (evt.keyCode == KeyCode.RightArrow)
            SetSidebarWidth(sidebarWidth + 16f);
        else
            return;

        evt.StopPropagation();
    }

    private void SetSidebarWidth(float width)
    {
        float bodyWidth = binder.Body.resolvedStyle.width;

        float maximum = float.IsNaN(bodyWidth)
            ? 400f
            : Mathf.Min(400f, Mathf.Max(200f, bodyWidth - 360f));

        sidebarWidth = Mathf.Clamp(width, 200f, maximum);
        binder.Sidebar.style.width = sidebarWidth;
    }

    private void ClampSidebarWidth()
    {
        SetSidebarWidth(sidebarWidth);
    }

    private void EndResize()
    {
        int pointer = resizePointer;
        resizePointer = -1;

        if (binder != null &&
            pointer >= 0 &&
            binder.SidebarResizeHandle.HasPointerCapture(pointer))
        {
            binder.SidebarResizeHandle.ReleasePointer(pointer);
        }
    }

    private static bool Within(
        VisualElement target,
        VisualElement container)
    {
        return target != null &&
               (target == container || container.Contains(target));
    }

    private static bool IsEditingField(VisualElement target)
    {
        for (VisualElement current = target;
             current != null;
             current = current.parent)
        {
            if (current is TextField ||
                current is IntegerField ||
                current is FloatField ||
                current is DropdownField)
            {
                return true;
            }
        }

        return false;
    }
}