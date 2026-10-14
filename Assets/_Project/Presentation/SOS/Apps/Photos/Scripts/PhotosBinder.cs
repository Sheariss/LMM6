using System;
using Atlas.Utils;
using UnityEngine.UIElements;

public sealed class PhotosBinder : UIBinder
{
    public VisualElement Root { get; }
    public VisualElement PhotoViewport { get; }

    public Image PhotoImage { get; }
    public Label PhotoNameLabel { get; }
    public Label CounterLabel { get; }
    public Button PreviousButton { get; }
    public Button NextButton { get; }

    public Button EditButton { get; }
    public Button RotateButton { get; }
    public Button DeleteButton { get; }
    public Button ShareButton { get; }
    public Button MoreButton { get; }
    public Button OpenWithButton { get; }

    public VisualElement FilmstripContainer { get; }
    public ScrollView FilmstripScrollView { get; }
    public VisualElement FilmstripItems { get; }
    public Button FilmstripButton { get; }

    public Label FileTypeLabel { get; }
    public Label DimensionsLabel { get; }
    public Label FileSizeLabel { get; }

    public Button InfoButton { get; }
    public Button ExtractTextButton { get; }
    public Button ActualSizeButton { get; }
    public DropdownField ZoomDropdown { get; }
    public Button ZoomOutButton { get; }
    public Slider ZoomSlider { get; }
    public Button ZoomInButton { get; }
    public Button FullscreenButton { get; }

    public VisualElement MoreMenu { get; }
    public Button SaveAsButton { get; }
    public Button CopyButton { get; }
    public Button ResizeButton { get; }
    public Button SetAsButton { get; }
    public Button SettingsButton { get; }

    public VisualElement OpenWithMenu { get; }
    public VisualElement OpenWithItems { get; }
    public Button ChooseAnotherAppButton { get; }

    public VisualElement SetAsMenu { get; }
    public Button SetAsBackgroundButton { get; }
    public Button SetAsLockScreenButton { get; }

    public PhotosBinder(VisualElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        Root = FindWindowRoot(root);

        PhotoViewport = Bind<VisualElement>(Root, "PhotoViewport");

        PhotoImage = Bind<Image>(Root, "photo-image");
        PhotoNameLabel = Bind<Label>(Root, "photo-name-label");
        CounterLabel = Bind<Label>(Root, "counter-label");
        PreviousButton = Bind<Button>(Root, "previous-button");
        NextButton = Bind<Button>(Root, "next-button");

        EditButton = Bind<Button>(Root, "EditButton");
        RotateButton = Bind<Button>(Root, "RotateButton");
        DeleteButton = Bind<Button>(Root, "DeleteButton");
        ShareButton = Bind<Button>(Root, "ShareButton");
        MoreButton = Bind<Button>(Root, "MoreButton");
        OpenWithButton = Bind<Button>(Root, "OpenWithButton");

        FilmstripContainer = Bind<VisualElement>(
            Root,
            "FilmstripContainer");

        FilmstripScrollView = Bind<ScrollView>(
            Root,
            "FilmstripScrollView");

        FilmstripItems = Bind<VisualElement>(Root, "FilmstripItems");
        FilmstripButton = Bind<Button>(Root, "FilmstripButton");

        FileTypeLabel = Bind<Label>(Root, "FileTypeLabel");
        DimensionsLabel = Bind<Label>(Root, "DimensionsLabel");
        FileSizeLabel = Bind<Label>(Root, "FileSizeLabel");

        InfoButton = Bind<Button>(Root, "InfoButton");
        ExtractTextButton = Bind<Button>(Root, "ExtractTextButton");
        ActualSizeButton = Bind<Button>(Root, "ActualSizeButton");
        ZoomDropdown = Bind<DropdownField>(Root, "ZoomDropdown");
        ZoomOutButton = Bind<Button>(Root, "ZoomOutButton");
        ZoomSlider = Bind<Slider>(Root, "ZoomSlider");
        ZoomInButton = Bind<Button>(Root, "ZoomInButton");
        FullscreenButton = Bind<Button>(Root, "FullscreenButton");

        MoreMenu = Bind<VisualElement>(Root, "MoreMenu");
        SaveAsButton = Bind<Button>(Root, "SaveAsButton");
        CopyButton = Bind<Button>(Root, "CopyButton");
        ResizeButton = Bind<Button>(Root, "ResizeButton");
        SetAsButton = Bind<Button>(Root, "SetAsButton");
        SettingsButton = Bind<Button>(Root, "SettingsButton");

        OpenWithMenu = Bind<VisualElement>(Root, "OpenWithMenu");
        OpenWithItems = Bind<VisualElement>(Root, "OpenWithItems");

        ChooseAnotherAppButton = Bind<Button>(
            Root,
            "ChooseAnotherAppButton");

        SetAsMenu = Bind<VisualElement>(Root, "SetAsMenu");

        SetAsBackgroundButton = Bind<Button>(
            Root,
            "SetAsBackgroundButton");

        SetAsLockScreenButton = Bind<Button>(
            Root,
            "SetAsLockScreenButton");

        // Bind() logs missing elements and sets inherited IsValid.
        // PhotosController checks IsValid before creating the view.
    }

    private static VisualElement FindWindowRoot(VisualElement root)
    {
        // Handles WindowRoot itself or content inside the window.
        for (VisualElement current = root;
             current != null;
             current = current.parent)
        {
            if (current.name == "WindowRoot")
                return current;
        }

        // Also handles a document root or cloned TemplateContainer.
        VisualElement window = root.Q<VisualElement>("WindowRoot");

        if (window != null)
            return window;

        throw new InvalidOperationException(
            "PhotosBinder could not find WindowRoot.");
    }
}