using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerBinder : UIBinder
    {
        public VisualElement Root { get; }

        // -------------------- TABS --------------------
        public VisualElement TabBar { get; }
        public VisualElement TabsContainer { get; }
        public Button NewTabButton { get; }

        // -------------------- NAVIGATION --------------------
        public VisualElement NavigationBar { get; }
        public Button BackButton { get; }
        public Button ForwardButton { get; }
        public Button UpButton { get; }
        public VisualElement BreadcrumbContainer { get; }
        public TextField SearchField { get; }

        // -------------------- COMMANDS --------------------
        public VisualElement CommandBar { get; }
        public Button NewButton { get; }
        public Button SortButton { get; }
        public Button ViewButton { get; }
        public Button FilterButton { get; }
        public Button RefreshButton { get; }

        // -------------------- SIDEBAR --------------------
        public ScrollView SidebarScrollView { get; }
        public Button HomeButton { get; }
        public Button RecentButton { get; }
        public Button StarredButton { get; }
        public Button DesktopButton { get; }
        public Button DocumentsButton { get; }
        public Button PicturesButton { get; }
        public Button DownloadsButton { get; }

        // -------------------- DIRECTORY --------------------
        public Label DirectoryTitle { get; }
        public Label DirectorySubtitle { get; }
        public Label SortStatus { get; }

        // -------------------- CONTENT --------------------
        public ScrollView ContentScrollView { get; }
        public VisualElement ContentContainer { get; }
        public VisualElement EmptyState { get; }

        // -------------------- STATUS --------------------
        public VisualElement StatusBar { get; }
        public Label ItemCount { get; }
        public Label SelectionStatus { get; }

        public FileExplorerBinder(
            VisualElement root)
        {
            Root =
                Bind<VisualElement>(
                    root,
                    "FileExplorerRoot");

            TabBar =
                Bind<VisualElement>(
                    root,
                    "FE-TabBar");

            TabsContainer =
                Bind<VisualElement>(
                    root,
                    "FE-TabsContainer");

            NewTabButton =
                Bind<Button>(
                    root,
                    "FE-NewTabButton");

            NavigationBar =
                Bind<VisualElement>(
                    root,
                    "FE-NavigationBar");

            BackButton =
                Bind<Button>(
                    root,
                    "FE-BackButton");

            ForwardButton =
                Bind<Button>(
                    root,
                    "FE-ForwardButton");

            UpButton =
                Bind<Button>(
                    root,
                    "FE-UpButton");

            BreadcrumbContainer =
                Bind<VisualElement>(
                    root,
                    "FE-BreadcrumbContainer");

            SearchField =
                Bind<TextField>(
                    root,
                    "FE-SearchField");

            CommandBar =
                Bind<VisualElement>(
                    root,
                    "FE-CommandBar");

            NewButton =
                Bind<Button>(
                    root,
                    "FE-NewButton");

            SortButton =
                Bind<Button>(
                    root,
                    "FE-SortButton");

            ViewButton =
                Bind<Button>(
                    root,
                    "FE-ViewButton");

            FilterButton =
                Bind<Button>(
                    root,
                    "FE-FilterButton");

            RefreshButton =
                Bind<Button>(
                    root,
                    "FE-RefreshButton");

            SidebarScrollView =
                Bind<ScrollView>(
                    root,
                    "FE-SidebarScrollView");

            HomeButton =
                Bind<Button>(
                    root,
                    "FE-HomeButton");

            RecentButton =
                Bind<Button>(
                    root,
                    "FE-RecentButton");

            StarredButton =
                Bind<Button>(
                    root,
                    "FE-StarredButton");

            DesktopButton =
                Bind<Button>(
                    root,
                    "FE-DesktopButton");

            DocumentsButton =
                Bind<Button>(
                    root,
                    "FE-DocumentsButton");

            PicturesButton =
                Bind<Button>(
                    root,
                    "FE-PicturesButton");

            DownloadsButton =
                Bind<Button>(
                    root,
                    "FE-DownloadsButton");

            DirectoryTitle =
                Bind<Label>(
                    root,
                    "FE-DirectoryTitle");

            DirectorySubtitle =
                Bind<Label>(
                    root,
                    "FE-DirectorySubtitle");

            SortStatus =
                Bind<Label>(
                    root,
                    "FE-SortStatus");

            ContentScrollView =
                Bind<ScrollView>(
                    root,
                    "FE-ContentScrollView");

            ContentContainer =
                Bind<VisualElement>(
                    root,
                    "FE-ContentContainer");

            EmptyState =
                Bind<VisualElement>(
                    root,
                    "FE-EmptyState");

            StatusBar =
                Bind<VisualElement>(
                    root,
                    "FE-StatusBar");

            ItemCount =
                Bind<Label>(
                    root,
                    "FE-ItemCount");

            SelectionStatus =
                Bind<Label>(
                    root,
                    "FE-SelectionStatus");
        }
    }
}