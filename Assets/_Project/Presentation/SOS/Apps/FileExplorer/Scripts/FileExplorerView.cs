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
        public bool CanRefresh { get; set; } = true;

        // Retained for compatibility with the existing view builder.
        public bool CanFilter { get; set; } = true;
    }

    public sealed class FileExplorerView
    {
        private readonly FileExplorerBinder binder;

        public FileExplorerView(FileExplorerBinder binder)
        {
            this.binder = binder;
        }

        public void Render(FileExplorerViewState state)
        {
            if (state == null)
                return;

            SetNavigationState(
                state.CanGoBack,
                state.CanGoForward,
                state.CanGoUp);

            SetDirectory(state.DirectoryTitle, state.DirectorySubtitle);
            SetSortStatus(state.SortStatus);
            SetSearchText(state.SearchText);
            SetSidebarSelection(state.SelectedLocation);
            SetViewMode(state.ViewMode);
            SetEmptyState(state.IsEmpty);
            SetItemCount(state.ItemCount);
            SetSelectionCount(state.SelectedItemCount);

            SetCommandState(
                state.CanCreateNew,
                state.CanSort,
                state.CanChangeView,
                state.CanFilter,
                state.CanRefresh);
        }

        public void SetNavigationState(bool back, bool forward, bool up)
        {
            binder.BackButton.SetEnabled(back);
            binder.ForwardButton.SetEnabled(forward);
            binder.UpButton.SetEnabled(up);
        }

        public void SetDirectory(string title, string subtitle)
        {
            string directory = string.IsNullOrWhiteSpace(title)
                ? "Files"
                : title;

            binder.DirectoryTitle.text = directory;
            binder.DirectorySubtitle.text = subtitle ?? "";

            binder.SearchPlaceholder.text = $"Search {directory}";
            binder.SearchField.tooltip = $"Search {directory}";
        }

        public void SetSortStatus(string text)
        {
            binder.SortStatus.text = text ?? "";
        }

        public void SetSearchText(string text)
        {
            string query = text ?? "";

            binder.SearchField.SetValueWithoutNotify(query);
            SetVisible(binder.SearchPlaceholder, query.Length == 0);
        }

        public void SetSidebarSelection(FileExplorerLocation location)
        {
            SetSidebarButtonSelected(
                binder.HomeButton, location == FileExplorerLocation.Home);
            SetSidebarButtonSelected(
                binder.RecentButton, location == FileExplorerLocation.Recent);
            SetSidebarButtonSelected(
                binder.StarredButton, location == FileExplorerLocation.Starred);
            SetSidebarButtonSelected(
                binder.DesktopButton, location == FileExplorerLocation.Desktop);
            SetSidebarButtonSelected(
                binder.DocumentsButton, location == FileExplorerLocation.Documents);
            SetSidebarButtonSelected(
                binder.PicturesButton, location == FileExplorerLocation.Pictures);
            SetSidebarButtonSelected(
                binder.DownloadsButton, location == FileExplorerLocation.Downloads);
        }

        public void SetViewMode(FileExplorerViewMode mode)
        {
            binder.ContentContainer.EnableInClassList(
                "fe-content--large-grid", mode == FileExplorerViewMode.LargeGrid);
            binder.ContentContainer.EnableInClassList(
                "fe-content--grid", mode == FileExplorerViewMode.Grid);
            binder.ContentContainer.EnableInClassList(
                "fe-content--list", mode == FileExplorerViewMode.List);
            binder.ContentContainer.EnableInClassList(
                "fe-content--details", mode == FileExplorerViewMode.Details);
        }

        public void SetEmptyState(bool empty)
        {
            SetVisible(binder.ContentScrollView, !empty);
            SetVisible(binder.EmptyState, empty);
        }

        public void SetItemCount(int count)
        {
            binder.ItemCount.text =
                $"{count} {(count == 1 ? "item" : "items")}";
        }

        public void SetSelectionCount(int count)
        {
            binder.SelectionStatus.text = count > 0
                ? $"{count} selected"
                : "";
        }

        public void SetCommandState(
            bool canCreateNew,
            bool canSort,
            bool canChangeView,
            bool canFilter,
            bool canRefresh)
        {
            binder.NewButton.SetEnabled(canCreateNew);
            binder.SortButton.SetEnabled(canSort);
            binder.ViewButton.SetEnabled(canChangeView);
            binder.RefreshButton.SetEnabled(canRefresh);
        }

        public void SetContent(VisualElement content)
        {
            ReplaceChildren(binder.ContentContainer, content);
        }

        public void SetTabs(VisualElement content)
        {
            ReplaceChildren(binder.TabsContainer, content);
        }

        public void SetBreadcrumbs(VisualElement content)
        {
            ReplaceChildren(binder.BreadcrumbContainer, content);
        }

        public void ScrollContentToTop()
        {
            binder.ContentScrollView.scrollOffset = Vector2.zero;
        }

        private static void SetSidebarButtonSelected(Button button, bool selected)
        {
            button.EnableInClassList("fe-sidebar-item--active", selected);
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            element.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private static void ReplaceChildren(
            VisualElement destination,
            VisualElement source)
        {
            destination.Clear();

            if (source == null)
                return;

            while (source.childCount > 0)
                destination.Add(source.ElementAt(0));
        }
    }
}