using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class WhiteboardView : IDisposable
{
    private readonly WhiteboardBinder binder;
    private readonly WhiteboardViewBuilder builder;

    private readonly WhiteboardPaintElement grid;
    private readonly WhiteboardPaintElement frames;
    private readonly WhiteboardPaintElement connectors;
    private readonly WhiteboardPaintElement objects;
    private readonly WhiteboardPaintElement drawings;
    private readonly WhiteboardPaintElement selection;

    private WhiteboardViewState state;
    private VisualElement popup;

    public bool SettingsVisible { get; private set; }

    public WhiteboardView(
        WhiteboardBinder binder,
        WhiteboardViewBuilder builder)
    {
        this.binder = binder;
        this.builder = builder;

        grid = AddPainter(binder.GridLayer, PaintGrid);
        frames = AddPainter(binder.FrameLayer,
            p => PaintItems(p, WhiteboardItemKind.Frame));

        connectors = AddPainter(binder.ConnectorLayer,
            PaintConnections);

        objects = AddPainter(binder.ObjectLayer, PaintObjects);

        drawings = AddPainter(binder.DrawingLayer,
            p => PaintItems(p, WhiteboardItemKind.Stroke));

        selection = AddPainter(binder.SelectionLayer, PaintSelection);

        binder.BoardViewport.focusable = true;
        binder.BoardViewport.style.backgroundColor = Color.black;

        binder.BoardTitleField.isDelayed = true;

        binder["ExportButton"].SetEnabled(false);
        binder["ExportButton"].tooltip =
            "Disk export is outside the demo scope.";

        binder["MoreToolsButton"].SetEnabled(false);
        binder["MoreToolsButton"].tooltip =
            "Images and snips are outside the demo scope.";

        binder["CustomBackgroundButton"].SetEnabled(false);
        binder["CustomBackgroundButton"].tooltip =
            "Use a background preset in this demo.";

        SetSettingsVisible(false);
        binder.ContextToolbar.AddToClassList("wb-hidden");
    }

    public void Render(WhiteboardViewState next)
    {
        state = next;

        binder.BoardViewport.style.backgroundColor =
            state.Document.Background;

        binder.BoardTitleField.SetValueWithoutNotify(
            state.Document.Title);

        binder.SaveStatusLabel.text = "Session only";
        binder["ZoomValueButton"].text =
            $"{Mathf.RoundToInt(state.Zoom * 100f)}%";

        binder.ToolHintLabel.text = state.Hint;

        binder.ShowGridToggle.SetValueWithoutNotify(
            state.Document.ShowGrid);

        binder.SnapToGridToggle.SetValueWithoutNotify(
            state.Document.SnapToGrid);

        binder["UndoButton"].SetEnabled(state.CanUndo);
        binder["RedoButton"].SetEnabled(state.CanRedo);

        SetTool("NavigationToolButton",
            state.Tool == WhiteboardTool.Select ||
            state.Tool == WhiteboardTool.Pan);

        SetTool("StickyNoteToolButton",
            state.Tool == WhiteboardTool.StickyNote);

        SetTool("TextToolButton",
            state.Tool == WhiteboardTool.Text);

        SetTool("ShapesAndLinesToolButton",
            state.Tool == WhiteboardTool.Rectangle ||
            state.Tool == WhiteboardTool.Ellipse ||
            state.Tool == WhiteboardTool.Line ||
            state.Tool == WhiteboardTool.Connector);

        SetTool("DrawToolButton",
            state.Tool == WhiteboardTool.Draw);

        SetTool("FrameToolButton",
            state.Tool == WhiteboardTool.Frame);

        SetTool("StickerToolButton",
            state.Tool == WhiteboardTool.Sticker);

        binder["NavigationToolButton"].EnableInClassList(
            "wb-navigation--pan",
            state.Tool == WhiteboardTool.Pan);

        SetBackgroundSwatch("BackgroundDarkButton",
            new Color(0.075f, 0.085f, 0.11f));

        SetBackgroundSwatch("BackgroundSlateButton",
            new Color(0.15f, 0.18f, 0.23f));

        SetBackgroundSwatch("BackgroundBlueButton",
            new Color(0.06f, 0.13f, 0.23f));

        SetBackgroundSwatch("BackgroundGreenButton",
            new Color(0.06f, 0.17f, 0.13f));

        // Keep painters, replace only generated object labels.
        ClearLabels(binder.FrameLayer, frames);
        ClearLabels(binder.ObjectLayer, objects);

        foreach (WhiteboardItem item in state.Document.Items)
        {
            if (item.IsConnection ||
                item.Kind == WhiteboardItemKind.Stroke)
            {
                continue;
            }

            VisualElement parent = item.Kind == WhiteboardItemKind.Frame
                ? binder.FrameLayer
                : binder.ObjectLayer;

            parent.Add(builder.BuildLabel(item, state));
        }

        grid.MarkDirtyRepaint();
        frames.MarkDirtyRepaint();
        connectors.MarkDirtyRepaint();
        objects.MarkDirtyRepaint();
        drawings.MarkDirtyRepaint();
        selection.MarkDirtyRepaint();
    }

    public void SetSettingsVisible(bool visible)
    {
        SettingsVisible = visible;

        binder.BoardSettingsPanel.EnableInClassList(
            "wb-hidden", !visible);
    }

    public void ShowPopup(
        VisualElement anchor,
        IEnumerable<WhiteboardMenuEntry> entries)
    {
        HidePopup();

        popup = builder.BuildMenu(entries, HidePopup);

        Vector2 point = binder.Workspace.WorldToLocal(
            new Vector2(
                anchor.worldBound.xMin,
                anchor.worldBound.yMax));

        float width = binder.Workspace.contentRect.width;
        float height = binder.Workspace.contentRect.height;

        popup.style.position = Position.Absolute;
        popup.style.left = Mathf.Clamp(
            point.x, 0f, Mathf.Max(0f, width - 240f));

        popup.style.top = Mathf.Clamp(
            point.y, 0f, Mathf.Max(0f, height - 340f));

        binder.PopupLayer.Add(popup);
    }

    public bool IsInsidePopup(VisualElement target)
    {
        return popup != null &&
               target != null &&
               (target == popup || popup.Contains(target));
    }

    public void HidePopup()
    {
        popup?.RemoveFromHierarchy();
        popup = null;
    }

    public void Dispose()
    {
        HidePopup();

        binder.GridLayer.Clear();
        binder.FrameLayer.Clear();
        binder.ConnectorLayer.Clear();
        binder.ObjectLayer.Clear();
        binder.DrawingLayer.Clear();
        binder.SelectionLayer.Clear();
        binder.ContextToolbar.Clear();
    }

    private void SetTool(string button, bool selected)
    {
        binder[button].EnableInClassList(
            "wb-tool--selected", selected);
    }

    private void SetBackgroundSwatch(string button, Color color)
    {
        binder[button].style.backgroundColor = color;

        binder[button].EnableInClassList(
            "wb-swatch-selected",
            state.Document.Background == color);
    }

    private void PaintGrid(Painter2D painter)
    {
        if (state == null || !state.Document.ShowGrid)
            return;

        Rect viewport = binder.BoardViewport.contentRect;
        float spacing = Mathf.Max(1f, state.Document.GridSize) * state.Zoom;

        // Keep the grid readable when zoomed out.
        while (spacing < 14f)
            spacing *= 2f;

        float offsetX = Mathf.Repeat(state.Pan.x, spacing);
        float offsetY = Mathf.Repeat(state.Pan.y, spacing);

        painter.strokeColor = new Color(1f, 1f, 1f, 0.075f);
        painter.lineWidth = 1f;
        painter.BeginPath();

        for (float x = offsetX; x < viewport.width; x += spacing)
        {
            painter.MoveTo(new Vector2(x, 0f));
            painter.LineTo(new Vector2(x, viewport.height));
        }

        for (float y = offsetY; y < viewport.height; y += spacing)
        {
            painter.MoveTo(new Vector2(0f, y));
            painter.LineTo(new Vector2(viewport.width, y));
        }

        painter.Stroke();
    }

    private void PaintItems(Painter2D painter, WhiteboardItemKind kind)
    {
        if (state == null)
            return;

        foreach (WhiteboardItem item in state.Document.Items)
        {
            if (item.Kind == kind)
                PaintItem(painter, item);
        }
    }

    private void PaintObjects(Painter2D painter)
    {
        if (state == null)
            return;

        foreach (WhiteboardItem item in state.Document.Items)
        {
            if (item.Kind != WhiteboardItemKind.Frame &&
                item.Kind != WhiteboardItemKind.Stroke &&
                !item.IsConnection)
            {
                PaintItem(painter, item);
            }
        }
    }

    private void PaintConnections(Painter2D painter)
    {
        if (state == null)
            return;

        foreach (WhiteboardItem item in state.Document.Items)
        {
            if (!item.IsConnection)
                continue;

            Vector2 start = state.Screen(
                state.Document.Resolve(item.Start));

            Vector2 end = state.Screen(
                state.Document.Resolve(item.End));

            painter.strokeColor = item.Color;
            painter.lineWidth = Mathf.Max(1f, item.LineWidth * state.Zoom);
            painter.BeginPath();
            painter.MoveTo(start);
            painter.LineTo(end);
            painter.Stroke();

            if (item.Kind != WhiteboardItemKind.Connector)
                continue;

            Vector2 direction = (end - start).normalized;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            float length = 12f * state.Zoom;

            painter.BeginPath();
            painter.MoveTo(end - direction * length + normal * length * 0.5f);
            painter.LineTo(end);
            painter.LineTo(end - direction * length - normal * length * 0.5f);
            painter.Stroke();
        }
    }

    private void PaintItem(Painter2D painter, WhiteboardItem item)
    {
        if (item.Kind == WhiteboardItemKind.Stroke)
        {
            if (item.Points.Count < 2)
                return;

            painter.strokeColor = item.Color;
            painter.lineWidth = Mathf.Max(1f, item.LineWidth * state.Zoom);
            painter.BeginPath();

            painter.MoveTo(state.Screen(
                item.Position + item.Points[0]));

            for (int i = 1; i < item.Points.Count; i++)
            {
                painter.LineTo(state.Screen(
                    item.Position + item.Points[i]));
            }

            painter.Stroke();
            return;
        }

        if (item.Kind == WhiteboardItemKind.Text ||
            item.Kind == WhiteboardItemKind.Sticker)
        {
            return;
        }

        Rect bounds = state.Screen(item.Bounds);

        if (item.Kind == WhiteboardItemKind.Ellipse)
            EllipsePath(painter, bounds);
        else
            RectPath(painter, bounds);

        if (item.Kind == WhiteboardItemKind.Frame)
        {
            painter.strokeColor = item.Color;
            painter.lineWidth = 2f;
            painter.Stroke();
        }
        else
        {
            painter.fillColor = item.Color;
            painter.Fill();
        }
    }

    private void PaintSelection(Painter2D painter)
    {
        if (state == null)
            return;

        WhiteboardItem selected = state.Document.Find(state.SelectedId);

        if (selected == null)
            return;

        Rect bounds = state.Screen(state.Document.GetBounds(selected));

        bounds.xMin -= 5f;
        bounds.yMin -= 5f;
        bounds.xMax += 5f;
        bounds.yMax += 5f;

        painter.strokeColor = new Color(0.3f, 0.7f, 1f);
        painter.lineWidth = 2f;

        RectPath(painter, bounds);
        painter.Stroke();
    }

    private static void RectPath(Painter2D painter, Rect rect)
    {
        painter.BeginPath();
        painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
        painter.LineTo(new Vector2(rect.xMax, rect.yMin));
        painter.LineTo(new Vector2(rect.xMax, rect.yMax));
        painter.LineTo(new Vector2(rect.xMin, rect.yMax));
        painter.ClosePath();
    }

    private static void EllipsePath(Painter2D painter, Rect rect)
    {
        const int segments = 48;

        painter.BeginPath();

        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;

            Vector2 point = rect.center + new Vector2(
                Mathf.Cos(angle) * rect.width * 0.5f,
                Mathf.Sin(angle) * rect.height * 0.5f);

            if (i == 0)
                painter.MoveTo(point);
            else
                painter.LineTo(point);
        }

        painter.ClosePath();
    }

    private static WhiteboardPaintElement AddPainter(
        VisualElement parent,
        Action<Painter2D> paint)
    {
        WhiteboardPaintElement element =
            new WhiteboardPaintElement(paint);

        parent.Add(element);
        return element;
    }

    private static void ClearLabels(
        VisualElement parent,
        VisualElement painter)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            if (parent[i] != painter)
                parent.RemoveAt(i);
        }
    }
}

internal sealed class WhiteboardPaintElement : VisualElement
{
    public WhiteboardPaintElement(Action<Painter2D> paint)
    {
        pickingMode = PickingMode.Ignore;

        style.position = Position.Absolute;
        style.left = 0f;
        style.top = 0f;
        style.right = 0f;
        style.bottom = 0f;

        generateVisualContent += context => paint(context.painter2D);
    }
}