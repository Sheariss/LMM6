using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerViewState
    {
        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public bool CanGoUp { get; set; }

        public string DirectoryTitle { get; set; } = "";
        public string DirectorySubtitle { get; set; } = "";
        public string SortStatus { get; set; } = "";

        public string SearchText { get; set; } = "";

        public FileExplorerLocation SelectedLocation { get; set; }

        public FileExplorerViewMode ViewMode { get; set; } =
            FileExplorerViewMode.Grid;

        public bool IsEmpty { get; set; }
        public int ItemCount { get; set; }
        public int SelectedItemCount { get; set; }

        public bool CanCreateNew { get; set; } = true;
        public bool CanSort { get; set; } = true;
        public bool CanChangeView { get; set; } = true;
        public bool CanFilter { get; set; } = true;
        public bool CanRefresh { get; set; } = true;
    }

    public sealed class FileExplorerView
    {
        private readonly FileExplorerBinder binder;

        public FileExplorerView(
            FileExplorerBinder binder)
        {
            this.binder = binder;
        }

        public void Render(
            FileExplorerViewState state)
        {
            if (state == null)
                return;

            SetNavigationState(
                state.CanGoBack,
                state.CanGoForward,
                state.CanGoUp);

            SetDirectory(
                state.DirectoryTitle,
                state.DirectorySubtitle);

            SetSortStatus(
                state.SortStatus);

            SetSearchText(
                state.SearchText);

            SetSidebarSelection(
                state.SelectedLocation);

            SetViewMode(
                state.ViewMode);

            SetEmptyState(
                state.IsEmpty);

            SetItemCount(
                state.ItemCount);

            SetSelectionCount(
                state.SelectedItemCount);

            SetCommandState(
                state.CanCreateNew,
                state.CanSort,
                state.CanChangeView,
                state.CanFilter,
                state.CanRefresh);
        }

        public void SetNavigationState(
            bool canGoBack,
            bool canGoForward,
            bool canGoUp)
        {
            if (binder.BackButton != null)
                binder.BackButton.SetEnabled(canGoBack);

            if (binder.ForwardButton != null)
                binder.ForwardButton.SetEnabled(canGoForward);

            if (binder.UpButton != null)
                binder.UpButton.SetEnabled(canGoUp);
        }

        public void SetDirectory(
            string title,
            string subtitle)
        {
            if (binder.DirectoryTitle != null)
            {
                binder.DirectoryTitle.text =
                    string.IsNullOrWhiteSpace(title)
                        ? "Files"
                        : title;
            }

            if (binder.DirectorySubtitle != null)
            {
                binder.DirectorySubtitle.text =
                    subtitle ?? "";
            }
        }

        public void SetSortStatus(
            string text)
        {
            if (binder.SortStatus == null)
                return;

            binder.SortStatus.text =
                text ?? "";
        }

        public void SetSearchText(
            string text)
        {
            if (binder.SearchField == null)
                return;

            binder.SearchField.SetValueWithoutNotify(
                text ?? "");
        }

        public void SetSidebarSelection(
            FileExplorerLocation location)
        {
            SetSidebarButtonSelected(
                binder.HomeButton,
                location == FileExplorerLocation.Home);

            SetSidebarButtonSelected(
                binder.RecentButton,
                location == FileExplorerLocation.Recent);

            SetSidebarButtonSelected(
                binder.StarredButton,
                location == FileExplorerLocation.Starred);

            SetSidebarButtonSelected(
                binder.DesktopButton,
                location == FileExplorerLocation.Desktop);

            SetSidebarButtonSelected(
                binder.DocumentsButton,
                location == FileExplorerLocation.Documents);

            SetSidebarButtonSelected(
                binder.PicturesButton,
                location == FileExplorerLocation.Pictures);

            SetSidebarButtonSelected(
                binder.DownloadsButton,
                location == FileExplorerLocation.Downloads);
        }

        public void SetViewMode(
            FileExplorerViewMode mode)
        {
            if (binder.ContentContainer == null)
                return;

            binder.ContentContainer.EnableInClassList(
                "fe-content--large-grid",
                mode == FileExplorerViewMode.LargeGrid);

            binder.ContentContainer.EnableInClassList(
                "fe-content--grid",
                mode == FileExplorerViewMode.Grid);

            binder.ContentContainer.EnableInClassList(
                "fe-content--list",
                mode == FileExplorerViewMode.List);

            binder.ContentContainer.EnableInClassList(
                "fe-content--details",
                mode == FileExplorerViewMode.Details);
        }

        public void SetEmptyState(
            bool isEmpty)
        {
            SetVisible(
                binder.ContentScrollView,
                !isEmpty);

            SetVisible(
                binder.EmptyState,
                isEmpty);
        }

        public void SetItemCount(
            int count)
        {
            if (binder.ItemCount == null)
                return;

            binder.ItemCount.text =
                $"{count} {(count == 1 ? "item" : "items")}";
        }

        public void SetSelectionCount(
            int count)
        {
            if (binder.SelectionStatus == null)
                return;

            binder.SelectionStatus.text =
                count > 0
                    ? $"{count} selected"
                    : "";
        }

        public void SetContent(
            VisualElement content)
        {
            if (binder.ContentContainer == null)
                return;

            binder.ContentContainer.Clear();

            if (content == null)
                return;

            MoveChildren(
                content,
                binder.ContentContainer);
        }

        public void SetTabs(
            VisualElement content)
        {
            if (binder.TabsContainer == null)
                return;

            binder.TabsContainer.Clear();

            if (content == null)
                return;

            MoveChildren(
                content,
                binder.TabsContainer);
        }

        public void SetBreadcrumbs(
            VisualElement content)
        {
            if (binder.BreadcrumbContainer == null)
                return;

            binder.BreadcrumbContainer.Clear();

            if (content == null)
                return;

            MoveChildren(
                content,
                binder.BreadcrumbContainer);
        }

        public void ScrollContentToTop()
        {
            if (binder.ContentScrollView == null)
                return;

            binder.ContentScrollView.scrollOffset =
                Vector2.zero;
        }

        public void SetCommandState(
            bool canCreateNew,
            bool canSort,
            bool canChangeView,
            bool canFilter,
            bool canRefresh)
        {
            if (binder.NewButton != null)
                binder.NewButton.SetEnabled(canCreateNew);

            if (binder.SortButton != null)
                binder.SortButton.SetEnabled(canSort);

            if (binder.ViewButton != null)
                binder.ViewButton.SetEnabled(canChangeView);

            if (binder.FilterButton != null)
                binder.FilterButton.SetEnabled(canFilter);

            if (binder.RefreshButton != null)
                binder.RefreshButton.SetEnabled(canRefresh);
        }

        private static void SetSidebarButtonSelected(
            Button button,
            bool selected)
        {
            if (button == null)
                return;

            button.EnableInClassList(
                "fe-sidebar-item--active",
                selected);
        }

        private static void SetVisible(
            VisualElement element,
            bool visible)
        {
            if (element == null)
                return;

            element.style.display =
                visible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        private static void MoveChildren(
            VisualElement source,
            VisualElement destination)
        {
            if (source == null ||
                destination == null)
            {
                return;
            }

            while (source.childCount > 0)
            {
                destination.Add(
                    source.ElementAt(0));
            }
        }
    }
}