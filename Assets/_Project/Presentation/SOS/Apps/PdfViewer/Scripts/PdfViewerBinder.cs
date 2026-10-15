using System;
using UnityEngine.UIElements;

public sealed class PdfViewerBinder
{
    public VisualElement Root { get; }
    public VisualElement Header { get; }
    public VisualElement Toolbar { get; }

    public Label WindowTitle { get; }
    public Label DocumentTitle { get; }

    public IntegerField PageNumberField { get; }
    public Label PageCountLabel { get; }
    public FloatField ZoomField { get; }
    public DropdownField FitDropdown { get; }

    public VisualElement SearchBar { get; }
    public TextField SearchField { get; }
    public Label SearchResultLabel { get; }

    public VisualElement Body { get; }
    public VisualElement Sidebar { get; }
    public VisualElement SidebarResizeHandle { get; }
    public Label SidebarTitle { get; }
    public ScrollView ThumbnailsPanel { get; }
    public VisualElement ThumbnailItems { get; }
    public VisualElement OutlinePanel { get; }
    public TreeView DocumentOutline { get; }
    public Label OutlineEmptyLabel { get; }

    public VisualElement DocumentViewport { get; }
    public ScrollView DocumentScrollView { get; }
    public VisualElement DocumentPages { get; }
    public Label EmptyDocumentLabel { get; }

    public VisualElement MoreOptionsMenu { get; }
    public VisualElement PresentationControls { get; }

    public PdfViewerBinder(VisualElement root)
    {
        Root = FindWindowRoot(root);

        Header = Get<VisualElement>("WindowHeader");
        Toolbar = Get<VisualElement>("PdfToolbar");

        WindowTitle = Get<Label>("WindowTitle");
        DocumentTitle = Get<Label>("DocumentTitle");

        PageNumberField = Get<IntegerField>("PageNumberField");
        PageCountLabel = Get<Label>("PageCountLabel");
        ZoomField = Get<FloatField>("ZoomField");
        FitDropdown = Get<DropdownField>("FitDropdown");

        SearchBar = Get<VisualElement>("SearchBar");
        SearchField = Get<TextField>("SearchField");
        SearchResultLabel = Get<Label>("SearchResultLabel");

        Body = Get<VisualElement>("ViewerBody");
        Sidebar = Get<VisualElement>("Sidebar");
        SidebarResizeHandle =
            Get<VisualElement>("SidebarResizeHandle");

        SidebarTitle = Get<Label>("SidebarTitle");
        ThumbnailsPanel = Get<ScrollView>("ThumbnailsPanel");
        ThumbnailItems = Get<VisualElement>("ThumbnailItems");
        OutlinePanel = Get<VisualElement>("OutlinePanel");
        DocumentOutline = Get<TreeView>("DocumentOutline");
        OutlineEmptyLabel = Get<Label>("OutlineEmptyLabel");

        DocumentViewport = Get<VisualElement>("DocumentViewport");
        DocumentScrollView =
            Get<ScrollView>("DocumentScrollView");

        DocumentPages = Get<VisualElement>("DocumentPages");
        EmptyDocumentLabel = Get<Label>("EmptyDocumentLabel");

        MoreOptionsMenu = Get<VisualElement>("MoreOptionsMenu");
        PresentationControls =
            Get<VisualElement>("PresentationControls");
    }

    public Button Button(string name)
    {
        return Get<Button>(name);
    }

    public T Get<T>(string name) where T : VisualElement
    {
        T element = Root.Q<T>(name);

        if (element == null)
        {
            throw new InvalidOperationException(
                $"PDF Viewer is missing {typeof(T).Name} '{name}'.");
        }

        return element;
    }

    private static VisualElement FindWindowRoot(VisualElement root)
    {
        if (root == null)
            throw new ArgumentNullException(nameof(root));

        for (VisualElement current = root;
             current != null;
             current = current.parent)
        {
            if (current.name == "WindowRoot")
                return current;
        }

        VisualElement window = root.Q("WindowRoot");

        if (window != null)
            return window;

        throw new InvalidOperationException(
            "PDF Viewer could not find WindowRoot.");
    }
}