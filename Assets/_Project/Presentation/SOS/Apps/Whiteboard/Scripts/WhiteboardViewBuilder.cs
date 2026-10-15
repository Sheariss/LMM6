using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class WhiteboardViewState
{
    public WhiteboardDocument Document;
    public WhiteboardTool Tool;
    public string SelectedId;
    public Vector2 Pan;
    public float Zoom;
    public bool CanUndo;
    public bool CanRedo;
    public string Hint;

    public Vector2 Screen(Vector2 point)
    {
        return point * Zoom + Pan;
    }

    public Rect Screen(Rect rect)
    {
        return new Rect(Screen(rect.position), rect.size * Zoom);
    }
}

public sealed class WhiteboardMenuEntry
{
    public string Text;
    public Action Action;
    public bool Enabled;

    public WhiteboardMenuEntry(
        string text,
        Action action,
        bool enabled = true)
    {
        Text = text;
        Action = action;
        Enabled = enabled;
    }
}

public sealed class WhiteboardViewBuilder
{
    public WhiteboardViewState Build(
        WhiteboardEngine engine,
        string hintOverride = null)
    {
        return new WhiteboardViewState
        {
            Document = engine.Document,
            Tool = engine.Tool,
            SelectedId = engine.SelectedId,
            Pan = engine.Pan,
            Zoom = engine.Zoom,
            CanUndo = engine.CanUndo,
            CanRedo = engine.CanRedo,
            Hint = hintOverride ?? Hint(engine.Tool)
        };
    }

    public VisualElement BuildLabel(
        WhiteboardItem item,
        WhiteboardViewState state)
    {
        Rect bounds = state.Screen(item.Bounds);

        VisualElement root = new VisualElement
        {
            pickingMode = PickingMode.Ignore
        };

        Place(root, bounds);

        Label label = new Label(item.Text)
        {
            pickingMode = PickingMode.Ignore
        };

        label.style.position = Position.Absolute;
        label.style.left = 10f * state.Zoom;
        label.style.right = 10f * state.Zoom;
        label.style.top = 8f * state.Zoom;
        label.style.bottom = 8f * state.Zoom;

        label.style.marginLeft = 0f;
        label.style.marginRight = 0f;
        label.style.marginTop = 0f;
        label.style.marginBottom = 0f;

        label.style.fontSize = item.FontSize * state.Zoom;
        label.style.color = item.TextColor;
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.overflow = Overflow.Hidden;

        if (item.Kind == WhiteboardItemKind.Frame)
        {
            label.style.height = 28f * state.Zoom;
            label.style.bottom = StyleKeyword.Auto;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
        }
        else if (item.Kind == WhiteboardItemKind.Rectangle ||
                 item.Kind == WhiteboardItemKind.Ellipse ||
                 item.Kind == WhiteboardItemKind.Sticker)
        {
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
        }

        root.Add(label);
        return root;
    }

    public VisualElement BuildMenu(
        IEnumerable<WhiteboardMenuEntry> entries,
        Action close)
    {
        ScrollView menu = new ScrollView(
            ScrollViewMode.Vertical);

        menu.style.width = 240f;
        menu.style.maxHeight = 330f;
        menu.style.backgroundColor = new Color(0.13f, 0.15f, 0.2f);
        menu.style.paddingLeft = 6f;
        menu.style.paddingRight = 6f;
        menu.style.paddingTop = 6f;
        menu.style.paddingBottom = 6f;

        foreach (WhiteboardMenuEntry entry in entries)
        {
            WhiteboardMenuEntry captured = entry;

            Button button = new Button(() =>
            {
                close();
                captured.Action?.Invoke();
            })
            {
                text = entry.Text
            };

            button.AddToClassList("wb-text-button");
            button.style.height = 34f;
            button.style.flexShrink = 0f;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.SetEnabled(entry.Enabled);

            menu.Add(button);
        }

        return menu;
    }

    public void BuildContextToolbar(
        VisualElement toolbar,
        WhiteboardItem item,
        Action<string> setText,
        Action<Vector2> resize,
        Action<Color> setColor,
        Action delete)
    {
        toolbar.Clear();

        // Stack controls to avoid covering the whole board.
        toolbar.style.flexDirection = FlexDirection.Column;
        toolbar.style.alignItems = Align.Stretch;
        toolbar.style.width = 260f;

        if (!item.IsConnection &&
            item.Kind != WhiteboardItemKind.Stroke)
        {
            TextField text = new TextField
            {
                multiline = true,
                isDelayed = true,
                maxLength = 2000
            };

            text.style.height = 80f;
            text.SetValueWithoutNotify(item.Text);
            text.RegisterValueChangedCallback(
                evt => setText(evt.newValue));

            toolbar.Add(text);
        }

        if (item.CanResize)
        {
            FloatField width = new FloatField("Width")
            {
                isDelayed = true
            };

            FloatField height = new FloatField("Height")
            {
                isDelayed = true
            };

            width.SetValueWithoutNotify(item.Size.x);
            height.SetValueWithoutNotify(item.Size.y);

            width.RegisterValueChangedCallback(evt =>
            {
                resize(new Vector2(evt.newValue, height.value));
            });

            height.RegisterValueChangedCallback(evt =>
            {
                resize(new Vector2(width.value, evt.newValue));
            });

            toolbar.Add(width);
            toolbar.Add(height);
        }

        VisualElement colors = new VisualElement();
        colors.style.flexDirection = FlexDirection.Row;

        Color[] palette =
        {
            new Color(1f, 0.87f, 0.48f),
            new Color(0.65f, 0.82f, 1f),
            new Color(0.6f, 0.88f, 0.75f),
            new Color(1f, 0.65f, 0.76f),
            Color.white
        };

        foreach (Color color in palette)
        {
            Color captured = color;

            Button swatch = new Button(() => setColor(captured));
            swatch.style.width = 32f;
            swatch.style.height = 28f;
            swatch.style.marginRight = 6f;
            swatch.style.backgroundColor = color;

            colors.Add(swatch);
        }

        toolbar.Add(colors);

        Button remove = new Button(delete)
        {
            text = "Delete selection"
        };

        remove.AddToClassList("wb-text-button");
        toolbar.Add(remove);
    }

    public static void Place(VisualElement element, Rect rect)
    {
        element.style.position = Position.Absolute;
        element.style.left = rect.x;
        element.style.top = rect.y;
        element.style.width = rect.width;
        element.style.height = rect.height;
    }

    private static string Hint(WhiteboardTool tool)
    {
        switch (tool)
        {
            case WhiteboardTool.Select:
                return "Select and drag objects · Hold Space to pan";

            case WhiteboardTool.Pan:
                return "Drag to pan · Scroll to zoom";

            case WhiteboardTool.Draw:
                return "Drag to draw · Esc to select";

            case WhiteboardTool.Line:
                return "Drag to create a line";

            case WhiteboardTool.Connector:
                return "Click a source object, then a destination";

            default:
                return $"Click the board to add: {tool}";
        }
    }
}