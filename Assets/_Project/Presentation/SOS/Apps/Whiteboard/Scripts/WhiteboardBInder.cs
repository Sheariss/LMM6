using System;
using System.Collections.Generic;
using Atlas.Utils;
using UnityEngine.UIElements;

public sealed class WhiteboardBinder : UIBinder
{
    public VisualElement Root { get; }

    public VisualElement Workspace { get; }
    public VisualElement BoardViewport { get; }
    public VisualElement BoardTransform { get; }

    public VisualElement GridLayer { get; }
    public VisualElement FrameLayer { get; }
    public VisualElement ConnectorLayer { get; }
    public VisualElement ObjectLayer { get; }
    public VisualElement DrawingLayer { get; }
    public VisualElement SelectionLayer { get; }

    public VisualElement ContextToolbar { get; }
    public VisualElement BoardSettingsPanel { get; }
    public VisualElement PopupLayer { get; }

    public TextField BoardTitleField { get; }
    public Label SaveStatusLabel { get; }
    public Label ToolHintLabel { get; }

    public Toggle ShowGridToggle { get; }
    public Toggle SnapToGridToggle { get; }

    public Dictionary<string, Button> Buttons { get; } =
        new Dictionary<string, Button>();

    public Button this[string name] => Buttons[name];

    public WhiteboardBinder(VisualElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        Root = root.name == "WhiteboardRoot"
            ? root
            : Bind<VisualElement>(root, "WhiteboardRoot");

        if (Root == null)
            return;

        Workspace = Element("Workspace");
        BoardViewport = Element("BoardViewport");
        BoardTransform = Element("BoardTransform");

        GridLayer = Element("GridLayer");
        FrameLayer = Element("FrameLayer");
        ConnectorLayer = Element("ConnectorLayer");
        ObjectLayer = Element("ObjectLayer");
        DrawingLayer = Element("DrawingLayer");
        SelectionLayer = Element("SelectionLayer");

        ContextToolbar = Element("ContextToolbar");
        BoardSettingsPanel = Element("BoardSettingsPanel");
        PopupLayer = Element("PopupLayer");

        BoardTitleField = Bind<TextField>(Root, "BoardTitleField");
        SaveStatusLabel = Bind<Label>(Root, "SaveStatusLabel");
        ToolHintLabel = Bind<Label>(Root, "ToolHintLabel");

        ShowGridToggle = Bind<Toggle>(Root, "ShowGridToggle");
        SnapToGridToggle = Bind<Toggle>(Root, "SnapToGridToggle");

        string[] names =
        {
            "FileMenuButton",
            "EditMenuButton",
            "ViewMenuButton",
            "BoardSettingsButton",
            "ExportButton",
            "NavigationToolButton",
            "StickyNoteToolButton",
            "TextToolButton",
            "ShapesAndLinesToolButton",
            "DrawToolButton",
            "FrameToolButton",
            "StickerToolButton",
            "MoreToolsButton",
            "UndoButton",
            "RedoButton",
            "FramesButton",
            "HelpButton",
            "ZoomOutButton",
            "ZoomValueButton",
            "ZoomInButton",
            "FitToContentButton",
            "CloseBoardSettingsButton",
            "BackgroundDarkButton",
            "BackgroundSlateButton",
            "BackgroundBlueButton",
            "BackgroundGreenButton",
            "CustomBackgroundButton"
        };

        foreach (string name in names)
            Buttons.Add(name, Bind<Button>(Root, name));
    }

    private VisualElement Element(string name)
    {
        return Bind<VisualElement>(Root, name);
    }
}