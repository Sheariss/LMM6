using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerBinder : UIBinder
    {
        public VisualElement WindowRoot { get; }
        public VisualElement WindowHeader { get; }
        public VisualElement Root { get; }
        public VisualElement MenuLayer { get; }

        public VisualElement TabBar { get; }
        public VisualElement TabsContainer { get; }
        public Button NewTabButton { get; }

        public VisualElement NavigationBar { get; }
        public Button BackButton { get; }
        public Button ForwardButton { get; }
        public Button UpButton { get; }
        public Button RefreshButton { get; }
        public VisualElement BreadcrumbContainer { get; }
        public VisualElement AddressBar { get; }
        public VisualElement PathIcon { get; }
        public TextField AddressField { get; }
        public Label AddressError { get; }

        public TextField SearchField { get; }
        public Label SearchPlaceholder { get; }

        public VisualElement CommandBar { get; }
        public Button NewButton { get; }
        public Button CutButton { get; }
        public Button CopyButton { get; }
        public Button PasteButton { get; }
        public Button RenameButton { get; }
        public Button ShareButton { get; }
        public Button DeleteButton { get; }
        public Button SortButton { get; }
        public Button ViewButton { get; }
        public Button MoreButton { get; }

        public ScrollView SidebarScrollView { get; }
        public Button HomeButton { get; }
        public Button RecentButton { get; }
        public Button StarredButton { get; }
        public Button DesktopButton { get; }
        public Button DocumentsButton { get; }
        public Button PicturesButton { get; }
        public Button DownloadsButton { get; }

        public Label DirectoryTitle { get; }
        public Label DirectorySubtitle { get; }
        public Label SortStatus { get; }

        public ScrollView ContentScrollView { get; }
        public VisualElement ContentContainer { get; }
        public VisualElement EmptyState { get; }

        public VisualElement StatusBar { get; }
        public Label ItemCount { get; }
        public Label SelectionStatus { get; }

        public FileExplorerBinder(VisualElement root)
        {
            VisualElement window = root;

            while (window != null && window.name != "WindowRoot")
                window = window.parent;

            if (window == null)
            {
                throw new InvalidOperationException(
                    "File Explorer could not find its enclosing WindowRoot.");
            }

            WindowRoot = window;
            WindowHeader = Bind<VisualElement>(window, "WindowHeader");
            MenuLayer = Bind<VisualElement>(window, "FE-MenuLayer");

            Root = Bind<VisualElement>(root, "FileExplorerRoot");

            TabBar = Bind<VisualElement>(window, "FE-TabBar");
            TabsContainer = Bind<VisualElement>(window, "FE-TabsContainer");
            NewTabButton = Bind<Button>(window, "FE-NewTabButton");

            NavigationBar = Bind<VisualElement>(root, "FE-NavigationBar");
            BackButton = Bind<Button>(root, "FE-BackButton");
            ForwardButton = Bind<Button>(root, "FE-ForwardButton");
            UpButton = Bind<Button>(root, "FE-UpButton");
            RefreshButton = Bind<Button>(root, "FE-RefreshButton");
            BreadcrumbContainer = Bind<VisualElement>(root, "FE-BreadcrumbContainer");
            AddressBar = Bind<VisualElement>(root, "FE-AddressBar");
            PathIcon = Bind<VisualElement>(root, "FE-PathIcon");
            AddressField = Bind<TextField>(root, "FE-AddressField");
            AddressError = Bind<Label>(root, "FE-AddressError");
            SearchField = Bind<TextField>(root, "FE-SearchField");
            SearchPlaceholder = Bind<Label>(root, "FE-SearchPlaceholder");

            CommandBar = Bind<VisualElement>(root, "FE-CommandBar");
            NewButton = Bind<Button>(root, "FE-NewButton");
            CutButton = Bind<Button>(root, "FE-CutButton");
            CopyButton = Bind<Button>(root, "FE-CopyButton");
            PasteButton = Bind<Button>(root, "FE-PasteButton");
            RenameButton = Bind<Button>(root, "FE-RenameButton");
            ShareButton = Bind<Button>(root, "FE-ShareButton");
            DeleteButton = Bind<Button>(root, "FE-DeleteButton");
            SortButton = Bind<Button>(root, "FE-SortButton");
            ViewButton = Bind<Button>(root, "FE-ViewButton");
            MoreButton = Bind<Button>(root, "FE-MoreButton");

            SidebarScrollView = Bind<ScrollView>(root, "FE-SidebarScrollView");
            HomeButton = Bind<Button>(root, "FE-HomeButton");
            RecentButton = Bind<Button>(root, "FE-RecentButton");
            StarredButton = Bind<Button>(root, "FE-StarredButton");
            DesktopButton = Bind<Button>(root, "FE-DesktopButton");
            DocumentsButton = Bind<Button>(root, "FE-DocumentsButton");
            PicturesButton = Bind<Button>(root, "FE-PicturesButton");
            DownloadsButton = Bind<Button>(root, "FE-DownloadsButton");

            DirectoryTitle = Bind<Label>(root, "FE-DirectoryTitle");
            DirectorySubtitle = Bind<Label>(root, "FE-DirectorySubtitle");
            SortStatus = Bind<Label>(root, "FE-SortStatus");

            ContentScrollView = Bind<ScrollView>(root, "FE-ContentScrollView");
            ContentContainer = Bind<VisualElement>(root, "FE-ContentContainer");
            EmptyState = Bind<VisualElement>(root, "FE-EmptyState");

            StatusBar = Bind<VisualElement>(root, "FE-StatusBar");
            ItemCount = Bind<Label>(root, "FE-ItemCount");
            SelectionStatus = Bind<Label>(root, "FE-SelectionStatus");
        }
    }
}