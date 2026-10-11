using System;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerController : AppContentController
    {
        [Header("File System")]
        [SerializeField] private FileSystemLibrary fileSystemLibrary;

        [SerializeField] private string userId = "guest";

        private FileExplorerBinder binder;
        private FileExplorerView view;
        private FileExplorerViewBuilder viewBuilder;
        private FileExplorerEngine engine;

        // -------------------- INITIALIZATION --------------------

        protected override void OnInitialize()
        {
            if (fileSystemLibrary == null)
            {
                throw new InvalidOperationException(
                    "FileExplorerController requires a FileSystemLibrary.");
            }

            binder = new FileExplorerBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "File Explorer UI is missing required elements.");
            }

            engine = new FileExplorerEngine(
                fileSystemLibrary,
                userId);

            view = new FileExplorerView(binder);
            viewBuilder = new FileExplorerViewBuilder();

            RegisterCallbacks();
            Render();
        }

        protected override void OnShutdown()
        {
            // The base class removes callbacks registered through
            // BindClick and BindCallback before this method runs.
            binder = null;
            view = null;
            viewBuilder = null;
            engine = null;
        }

        // -------------------- CALLBACKS --------------------

        private void RegisterCallbacks()
        {
            BindClick(binder.NewTabButton, OnNewTabPressed);

            BindClick(binder.BackButton, OnBackPressed);
            BindClick(binder.ForwardButton, OnForwardPressed);
            BindClick(binder.UpButton, OnUpPressed);

            BindCallback<ChangeEvent<string>>(
                binder.SearchField,
                OnSearchChanged);

            BindClick(binder.NewButton, OnNewPressed);
            BindClick(binder.SortButton, OnSortPressed);
            BindClick(binder.ViewButton, OnViewPressed);
            BindClick(binder.FilterButton, OnFilterPressed);
            BindClick(binder.RefreshButton, OnRefreshPressed);

            BindClick(binder.HomeButton, OnHomePressed);
            BindClick(binder.RecentButton, OnRecentPressed);
            BindClick(binder.StarredButton, OnStarredPressed);

            BindClick(binder.DesktopButton, OnDesktopPressed);
            BindClick(binder.DocumentsButton, OnDocumentsPressed);
            BindClick(binder.PicturesButton, OnPicturesPressed);
            BindClick(binder.DownloadsButton, OnDownloadsPressed);

            BindCallback<ClickEvent>(
                binder.ContentScrollView,
                OnContentBackgroundPressed);
        }

        // -------------------- TABS --------------------

        private void OnNewTabPressed()
        {
            engine.OpenTab();
            Render();
        }

        private void OnTabPressed(int tabId)
        {
            engine.SelectTab(tabId);
            Render();
        }

        private void OnTabClosePressed(int tabId)
        {
            engine.CloseTab(tabId);
            Render();
        }

        // -------------------- NAVIGATION --------------------

        private void OnBackPressed()
        {
            engine.GoBack();
            Render();
        }

        private void OnForwardPressed()
        {
            engine.GoForward();
            Render();
        }

        private void OnUpPressed()
        {
            engine.GoUp();
            Render();
        }

        private void OnBreadcrumbPressed(string path)
        {
            engine.Navigate(path);
            Render();
        }

        // -------------------- SEARCH --------------------

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            engine.SetSearchQuery(evt.newValue);
            Render();
        }

        // -------------------- SIDEBAR --------------------

        private void OnHomePressed()
        {
            NavigateTo(FileExplorerLocation.Home);
        }

        private void OnDesktopPressed()
        {
            NavigateTo(FileExplorerLocation.Desktop);
        }

        private void OnDocumentsPressed()
        {
            NavigateTo(FileExplorerLocation.Documents);
        }

        private void OnPicturesPressed()
        {
            NavigateTo(FileExplorerLocation.Pictures);
        }

        private void OnDownloadsPressed()
        {
            NavigateTo(FileExplorerLocation.Downloads);
        }

        private void NavigateTo(FileExplorerLocation location)
        {
            engine.NavigateToLocation(location);
            Render();
        }

        private void OnRecentPressed()
        {
            Debug.Log("Recent view not implemented yet.");
        }

        private void OnStarredPressed()
        {
            Debug.Log("Starred view not implemented yet.");
        }

        // -------------------- COMMAND BAR --------------------

        private void OnNewPressed()
        {
            Debug.Log("New item menu not implemented yet.");
        }

        private void OnSortPressed()
        {
            CycleSortMode();
            Render();
        }

        private void CycleSortMode()
        {
            FileExplorerTab tab = engine.ActiveTab;

            if (tab == null)
                return;

            FileExplorerSortMode next = tab.SortMode switch
            {
                FileExplorerSortMode.Name =>
                    FileExplorerSortMode.DateModified,

                FileExplorerSortMode.DateModified =>
                    FileExplorerSortMode.Type,

                FileExplorerSortMode.Type =>
                    FileExplorerSortMode.Size,

                _ => FileExplorerSortMode.Name
            };

            engine.SetSortMode(next);
        }

        private void OnViewPressed()
        {
            engine.CycleViewMode();
            Render();
        }

        private void OnFilterPressed()
        {
            Debug.Log("Filter menu not implemented yet.");
        }

        private void OnRefreshPressed()
        {
            Render();
        }

        // -------------------- ITEMS --------------------

        private void OnItemPressed(
            FileExplorerItem item,
            bool additive)
        {
            if (item == null)
                return;

            if (additive)
                engine.ToggleItemSelection(item.Id);
            else
                engine.SelectItem(item.Id);

            Render();
        }

        private void OnItemOpened(FileExplorerItem item)
        {
            if (item == null)
                return;

            if (item.IsFolder)
            {
                engine.OpenItem(item);
                Render();
                return;
            }

            OpenFile(item);
        }

        private void OpenFile(FileExplorerItem item)
        {
            Debug.Log($"Open file: {item.Path}");

            // Route file opening through AppController later.
        }

        private void OnContentBackgroundPressed(ClickEvent evt)
        {
            if (evt.target != binder.ContentScrollView &&
                evt.target != binder.ContentContainer)
            {
                return;
            }

            engine.ClearSelection();
            Render();
        }

        // -------------------- RENDER --------------------

        private void Render()
        {
            if (engine == null ||
                view == null ||
                viewBuilder == null)
            {
                return;
            }

            FileExplorerViewState state =
                viewBuilder.BuildState(engine);

            view.Render(state);

            VisualElement tabs = viewBuilder.BuildTabs(
                engine,
                OnTabPressed,
                OnTabClosePressed);

            VisualElement breadcrumbs =
                viewBuilder.BuildBreadcrumbs(
                    engine,
                    OnBreadcrumbPressed);

            VisualElement directory =
                viewBuilder.BuildDirectory(
                    engine,
                    OnItemPressed,
                    OnItemOpened);

            view.SetTabs(tabs);
            view.SetBreadcrumbs(breadcrumbs);
            view.SetContent(directory);
        }
    }
}