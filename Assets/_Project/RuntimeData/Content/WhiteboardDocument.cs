using System;
using System.Collections.Generic;
using UnityEngine;

public enum WhiteboardItemKind
{
    StickyNote,
    Text,
    Rectangle,
    Ellipse,
    Sticker,
    Frame,
    Stroke,
    Line,
    Connector
}

public enum WhiteboardTool
{
    Select,
    Pan,
    StickyNote,
    Text,
    Rectangle,
    Ellipse,
    Sticker,
    Frame,
    Draw,
    Line,
    Connector
}

[Serializable]
public sealed class WhiteboardEndpoint
{
    public string ItemId;

    // Normalized position within an attached item's bounds.
    public Vector2 Anchor = new Vector2(0.5f, 0.5f);

    // Used when the endpoint is not attached to an item.
    public Vector2 Position;
}

[Serializable]
public sealed class WhiteboardItem
{
    public string Id = Guid.NewGuid().ToString("N");
    public WhiteboardItemKind Kind;

    public Vector2 Position;
    public Vector2 Size = new Vector2(200f, 150f);

    public string Text = "";
    public Color Color = new Color(1f, 0.87f, 0.48f);
    public Color TextColor = new Color(0.12f, 0.13f, 0.16f);
    public float FontSize = 20f;
    public float LineWidth = 3f;

    // Stroke points are local to Position.
    public List<Vector2> Points = new List<Vector2>();

    public WhiteboardEndpoint Start = new WhiteboardEndpoint();
    public WhiteboardEndpoint End = new WhiteboardEndpoint();

    public bool IsConnection =>
        Kind == WhiteboardItemKind.Line ||
        Kind == WhiteboardItemKind.Connector;

    public bool CanAttach =>
        !IsConnection &&
        Kind != WhiteboardItemKind.Stroke &&
        Kind != WhiteboardItemKind.Frame;

    public bool CanResize =>
        !IsConnection &&
        Kind != WhiteboardItemKind.Stroke;

    public Rect Bounds => new Rect(Position, Size);
}

[Serializable]
public sealed class WhiteboardDocument
{
    public int Version = 1;
    public string Id = Guid.NewGuid().ToString("N");
    public string Title = "Untitled board";

    public Color Background = new Color(0.075f, 0.085f, 0.11f);
    public bool ShowGrid = true;
    public bool SnapToGrid;
    public float GridSize = 24f;

    public List<WhiteboardItem> Items = new List<WhiteboardItem>();

    public WhiteboardDocument Clone()
    {
        return JsonUtility.FromJson<WhiteboardDocument>(
            JsonUtility.ToJson(this));
    }

    public WhiteboardItem Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        return Items.Find(item => item.Id == id);
    }

    public Vector2 Resolve(WhiteboardEndpoint endpoint)
    {
        WhiteboardItem item = Find(endpoint.ItemId);

        if (item == null)
            return endpoint.Position;

        return item.Position + Vector2.Scale(item.Size, endpoint.Anchor);
    }

    public Rect GetBounds(WhiteboardItem item)
    {
        if (!item.IsConnection)
            return item.Bounds;

        Vector2 start = Resolve(item.Start);
        Vector2 end = Resolve(item.End);

        return Rect.MinMaxRect(
            Mathf.Min(start.x, end.x),
            Mathf.Min(start.y, end.y),
            Mathf.Max(start.x, end.x),
            Mathf.Max(start.y, end.y));
    }
}