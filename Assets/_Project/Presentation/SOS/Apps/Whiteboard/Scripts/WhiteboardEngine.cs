using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WhiteboardEngine
{
    public const float MinimumZoom = 0.2f;
    public const float MaximumZoom = 4f;

    private const int HistoryLimit = 80;

    private readonly List<string> undo = new List<string>();
    private readonly List<string> redo = new List<string>();

    private string editSnapshot;

    public WhiteboardDocument Document { get; private set; }

    public string SelectedId { get; private set; }
    public WhiteboardItem Selected => Document.Find(SelectedId);

    public WhiteboardTool Tool { get; set; } = WhiteboardTool.Select;

    public Vector2 Pan { get; private set; } = new Vector2(120f, 90f);
    public float Zoom { get; private set; } = 1f;

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public bool IsEditing => editSnapshot != null;

    public WhiteboardEngine(WhiteboardDocument document)
    {
        Document = document ??
            throw new ArgumentNullException(nameof(document));
    }

    public Vector2 ToBoard(Vector2 viewportPoint)
    {
        return (viewportPoint - Pan) / Zoom;
    }

    public Vector2 ToViewport(Vector2 boardPoint)
    {
        return boardPoint * Zoom + Pan;
    }

    public Vector2 Snap(Vector2 point)
    {
        if (!Document.SnapToGrid)
            return point;

        float grid = Mathf.Max(1f, Document.GridSize);

        return new Vector2(
            Mathf.Round(point.x / grid) * grid,
            Mathf.Round(point.y / grid) * grid);
    }

    public void Select(string id)
    {
        SelectedId = Document.Find(id) != null ? id : null;
    }

    public void SetPan(Vector2 pan)
    {
        Pan = pan;
    }

    public void SetZoom(float zoom, Vector2 viewportAnchor)
    {
        if (float.IsNaN(zoom) || float.IsInfinity(zoom))
            return;

        Vector2 boardAnchor = ToBoard(viewportAnchor);

        Zoom = Mathf.Clamp(zoom, MinimumZoom, MaximumZoom);
        Pan = viewportAnchor - boardAnchor * Zoom;
    }

    public void Fit(Vector2 viewportSize)
    {
        if (Document.Items.Count == 0 ||
            viewportSize.x <= 0f ||
            viewportSize.y <= 0f)
        {
            return;
        }

        Rect bounds = Document.GetBounds(Document.Items[0]);

        foreach (WhiteboardItem item in Document.Items)
            bounds = Union(bounds, Document.GetBounds(item));

        Fit(bounds, viewportSize);
    }

    public void Fit(Rect bounds, Vector2 viewportSize)
    {
        if (viewportSize.x <= 0f || viewportSize.y <= 0f)
            return;

        float availableWidth = Mathf.Max(1f, viewportSize.x - 180f);
        float availableHeight = Mathf.Max(1f, viewportSize.y - 140f);

        Zoom = Mathf.Clamp(
            Mathf.Min(
                availableWidth / Mathf.Max(1f, bounds.width),
                availableHeight / Mathf.Max(1f, bounds.height)),
            MinimumZoom,
            MaximumZoom);

        // Leave room for the left tool rail.
        Vector2 center = viewportSize * 0.5f + new Vector2(30f, 0f);
        Pan = center - bounds.center * Zoom;
    }

    public void BeginEdit()
    {
        if (IsEditing)
            throw new InvalidOperationException(
                "Commit or cancel the current edit first.");

        editSnapshot = Snapshot();
    }

    public bool CommitEdit()
    {
        if (!IsEditing)
            return false;

        string before = editSnapshot;
        editSnapshot = null;

        if (before == Snapshot())
            return false;

        Push(undo, before);
        redo.Clear();
        return true;
    }

    public void CancelEdit()
    {
        if (!IsEditing)
            return;

        string before = editSnapshot;
        editSnapshot = null;
        Restore(before);
    }

    public void Undo()
    {
        CancelEdit();

        if (!CanUndo)
            return;

        Push(redo, Snapshot());
        Restore(Pop(undo));
    }

    public void Redo()
    {
        CancelEdit();

        if (!CanRedo)
            return;

        Push(undo, Snapshot());
        Restore(Pop(redo));
    }

    public WhiteboardItem Create(
        WhiteboardItemKind kind,
        Vector2 position)
    {
        WhiteboardItem item = new WhiteboardItem
        {
            Kind = kind,
            Position = Snap(position)
        };

        switch (kind)
        {
            case WhiteboardItemKind.StickyNote:
                item.Text = "New note";
                break;

            case WhiteboardItemKind.Text:
                item.Text = "Add your text";
                item.Size = new Vector2(280f, 90f);
                item.Color = Color.clear;
                item.TextColor = Color.white;
                break;

            case WhiteboardItemKind.Rectangle:
            case WhiteboardItemKind.Ellipse:
                item.Text = "Shape";
                item.Size = new Vector2(180f, 120f);
                item.Color = new Color(0.45f, 0.73f, 1f);
                break;

            case WhiteboardItemKind.Frame:
                item.Text = "New frame";
                item.Size = new Vector2(620f, 380f);
                item.Color = new Color(0.55f, 0.62f, 0.73f);
                item.TextColor = Color.white;
                break;

            case WhiteboardItemKind.Sticker:
                item.Text = "★";
                item.Size = new Vector2(90f, 90f);
                item.FontSize = 58f;
                item.Color = Color.clear;
                item.TextColor = new Color(1f, 0.83f, 0.3f);
                break;

            case WhiteboardItemKind.Stroke:
                item.Color = Color.white;
                item.Size = Vector2.zero;
                break;

            case WhiteboardItemKind.Line:
            case WhiteboardItemKind.Connector:
                item.Color = new Color(0.65f, 0.8f, 1f);
                item.Start.Position = item.Position;
                item.End.Position = item.Position;
                break;
        }

        return item;
    }

    public void Add(WhiteboardItem item)
    {
        Change(() =>
        {
            Document.Items.Add(item);
            SelectedId = item.Id;
        });
    }

    public void MoveSelected(Vector2 position)
    {
        WhiteboardItem item = Selected;

        if (item == null || item.IsConnection)
            return;

        Change(() => item.Position = Snap(position));
    }

    public void ResizeSelected(Vector2 size)
    {
        WhiteboardItem item = Selected;

        if (item == null || !item.CanResize)
            return;

        Change(() => item.Size = new Vector2(
            Mathf.Clamp(size.x, 60f, 4000f),
            Mathf.Clamp(size.y, 40f, 4000f)));
    }

    public void SetSelectedText(string text)
    {
        WhiteboardItem item = Selected;

        if (item == null || item.IsConnection ||
            item.Kind == WhiteboardItemKind.Stroke)
        {
            return;
        }

        Change(() => item.Text = text ?? "");
    }

    public void SetSelectedColor(Color color)
    {
        WhiteboardItem item = Selected;

        if (item == null)
            return;

        Change(() =>
        {
            if (item.Kind == WhiteboardItemKind.Text ||
                item.Kind == WhiteboardItemKind.Sticker)
            {
                item.TextColor = color;
            }
            else
            {
                item.Color = color;
            }
        });
    }

    public void DeleteSelected()
    {
        WhiteboardItem item = Selected;

        if (item == null)
            return;

        Change(() =>
        {
            // Preserve attached endpoints as free endpoints.
            foreach (WhiteboardItem connection in Document.Items)
            {
                if (!connection.IsConnection)
                    continue;

                Detach(connection.Start, item.Id);
                Detach(connection.End, item.Id);
            }

            Document.Items.Remove(item);
            SelectedId = null;
        });
    }

    public void Rename(string title)
    {
        string value = string.IsNullOrWhiteSpace(title)
            ? "Untitled board"
            : title.Trim();

        if (value.Length > 80)
            value = value.Substring(0, 80);

        Change(() => Document.Title = value);
    }

    public void SetBackground(Color color)
    {
        Change(() => Document.Background = color);
    }

    public void SetShowGrid(bool enabled)
    {
        Change(() => Document.ShowGrid = enabled);
    }

    public void SetSnapToGrid(bool enabled)
    {
        Change(() => Document.SnapToGrid = enabled);
    }

    public void AppendStrokePoint(WhiteboardItem stroke, Vector2 point)
    {
        if (!IsEditing || stroke.Kind != WhiteboardItemKind.Stroke)
            return;

        Vector2 local = point - stroke.Position;

        if (stroke.Points.Count > 0 &&
            Vector2.Distance(
                stroke.Points[stroke.Points.Count - 1],
                local) < 2f / Zoom)
        {
            return;
        }

        stroke.Points.Add(local);

        Vector2 minimum = Vector2.zero;
        Vector2 maximum = Vector2.zero;

        foreach (Vector2 sample in stroke.Points)
        {
            minimum = Vector2.Min(minimum, sample);
            maximum = Vector2.Max(maximum, sample);
        }

        // Normalize so Position is always the stroke's upper-left corner.
        if (minimum != Vector2.zero)
        {
            stroke.Position += minimum;

            for (int i = 0; i < stroke.Points.Count; i++)
                stroke.Points[i] -= minimum;

            maximum -= minimum;
        }

        stroke.Size = Vector2.Max(maximum, Vector2.one);
    }

    public WhiteboardItem HitTest(Vector2 point)
    {
        // Frames are behind other content.
        for (int i = Document.Items.Count - 1; i >= 0; i--)
        {
            WhiteboardItem item = Document.Items[i];

            if (item.Kind != WhiteboardItemKind.Frame &&
                Hit(item, point))
            {
                return item;
            }
        }

        for (int i = Document.Items.Count - 1; i >= 0; i--)
        {
            WhiteboardItem item = Document.Items[i];

            if (item.Kind == WhiteboardItemKind.Frame &&
                Hit(item, point))
            {
                return item;
            }
        }

        return null;
    }

    private bool Hit(WhiteboardItem item, Vector2 point)
    {
        float tolerance = 8f / Zoom;

        if (item.IsConnection)
        {
            return DistanceToSegment(
                point,
                Document.Resolve(item.Start),
                Document.Resolve(item.End)) <= tolerance;
        }

        if (item.Kind == WhiteboardItemKind.Stroke)
        {
            for (int i = 1; i < item.Points.Count; i++)
            {
                if (DistanceToSegment(
                    point,
                    item.Position + item.Points[i - 1],
                    item.Position + item.Points[i]) <= tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        Rect bounds = item.Bounds;

        if (!bounds.Contains(point))
            return false;

        if (item.Kind == WhiteboardItemKind.Ellipse)
        {
            Vector2 normalized = point - bounds.center;
            normalized.x /= bounds.width * 0.5f;
            normalized.y /= bounds.height * 0.5f;
            return normalized.sqrMagnitude <= 1f;
        }

        if (item.Kind != WhiteboardItemKind.Frame)
            return true;

        // Let clicks inside frames reach the canvas.
        return point.y <= bounds.yMin + 32f ||
               point.x <= bounds.xMin + tolerance ||
               point.x >= bounds.xMax - tolerance ||
               point.y >= bounds.yMax - tolerance;
    }

    private void Change(Action action)
    {
        bool ownsEdit = !IsEditing;

        if (ownsEdit)
            BeginEdit();

        action();

        if (ownsEdit)
            CommitEdit();
    }

    private void Detach(WhiteboardEndpoint endpoint, string itemId)
    {
        if (endpoint.ItemId != itemId)
            return;

        endpoint.Position = Document.Resolve(endpoint);
        endpoint.ItemId = null;
    }

    private string Snapshot()
    {
        return JsonUtility.ToJson(Document);
    }

    private void Restore(string snapshot)
    {
        Document = JsonUtility.FromJson<WhiteboardDocument>(snapshot);

        if (Document.Find(SelectedId) == null)
            SelectedId = null;
    }

    private static void Push(List<string> history, string snapshot)
    {
        history.Add(snapshot);

        if (history.Count > HistoryLimit)
            history.RemoveAt(0);
    }

    private static string Pop(List<string> history)
    {
        int index = history.Count - 1;
        string value = history[index];
        history.RemoveAt(index);
        return value;
    }

    private static Rect Union(Rect a, Rect b)
    {
        return Rect.MinMaxRect(
            Mathf.Min(a.xMin, b.xMin),
            Mathf.Min(a.yMin, b.yMin),
            Mathf.Max(a.xMax, b.xMax),
            Mathf.Max(a.yMax, b.yMax));
    }

    private static float DistanceToSegment(
        Vector2 point,
        Vector2 start,
        Vector2 end)
    {
        Vector2 segment = end - start;

        float t = segment.sqrMagnitude > 0f
            ? Mathf.Clamp01(
                Vector2.Dot(point - start, segment) /
                segment.sqrMagnitude)
            : 0f;

        return Vector2.Distance(point, start + segment * t);
    }
}