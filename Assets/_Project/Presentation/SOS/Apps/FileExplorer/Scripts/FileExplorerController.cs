using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.FileExplorer
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class FileExplorerController : MonoBehaviour
    {
        [Header("File System")]
        [SerializeField]
        private FileSystemLibrary fileSystemLibrary;

        [SerializeField]
        private string userId = "guest";

        private UIDocument uiDocument;

        private FileExplorerBinder binder;
        private FileExplorerView view;
        private FileExplorerViewBuilder viewBuilder;
        private FileExplorerEngine engine;

        private void Start()
        {
            uiDocument =
                GetComponentInParent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError(
                    $"{nameof(FileExplorerController)} could not find a UIDocument.");

                return;
            }

            binder =
                new FileExplorerBinder(
                    uiDocument.rootVisualElement);

            engine =
                new FileExplorerEngine(
                    fileSystemLibrary,
                    userId);

            view =
                new FileExplorerView(
                    binder);

            viewBuilder =
                new FileExplorerViewBuilder();

            RegisterCallbacks();

            Render();
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        // -------------------- CALLBACKS --------------------
        private void RegisterCallbacks()
        {
            binder.NewTabButton.clicked += OnNewTabPressed;

            binder.BackButton.clicked += OnBackPressed;
            binder.ForwardButton.clicked += OnForwardPressed;
            binder.UpButton.clicked += OnUpPressed;

            binder.SearchField.RegisterValueChangedCallback(
                OnSearchChanged);

            binder.NewButton.clicked += OnNewPressed;
            binder.SortButton.clicked += OnSortPressed;
            binder.ViewButton.clicked += OnViewPressed;
            binder.FilterButton.clicked += OnFilterPressed;
            binder.RefreshButton.clicked += OnRefreshPressed;

            binder.HomeButton.clicked += OnHomePressed;
            binder.RecentButton.clicked += OnRecentPressed;
            binder.StarredButton.clicked += OnStarredPressed;

            binder.DesktopButton.clicked += OnDesktopPressed;
            binder.DocumentsButton.clicked += OnDocumentsPressed;
            binder.PicturesButton.clicked += OnPicturesPressed;
            binder.DownloadsButton.clicked += OnDownloadsPressed;

            binder.ContentScrollView.RegisterCallback<ClickEvent>(OnContentBackgroundPressed);
        }

        private void UnregisterCallbacks()
        {
            if (binder == null || !binder.IsValid)
            {
                return;
            }

            binder.NewTabButton.clicked -= OnNewTabPressed;

            binder.BackButton.clicked -= OnBackPressed;
            binder.ForwardButton.clicked -= OnForwardPressed;
            binder.UpButton.clicked -= OnUpPressed;

            binder.SearchField.UnregisterValueChangedCallback(OnSearchChanged);

            binder.NewButton.clicked -= OnNewPressed;
            binder.SortButton.clicked -= OnSortPressed;
            binder.ViewButton.clicked -= OnViewPressed;
            binder.FilterButton.clicked -= OnFilterPressed;
            binder.RefreshButton.clicked -= OnRefreshPressed;

            binder.HomeButton.clicked -= OnHomePressed;
            binder.RecentButton.clicked -= OnRecentPressed;
            binder.StarredButton.clicked -= OnStarredPressed;

            binder.DesktopButton.clicked -= OnDesktopPressed;
            binder.DocumentsButton.clicked -= OnDocumentsPressed;
            binder.PicturesButton.clicked -= OnPicturesPressed;
            binder.DownloadsButton.clicked -= OnDownloadsPressed;

            binder.ContentScrollView.UnregisterCallback<ClickEvent>(OnContentBackgroundPressed);
        }

        // -------------------- TABS --------------------
        private void OnNewTabPressed()
        {
            engine.OpenTab();

            Render();
        }

        private void OnTabPressed(
            int tabId)
        {
            engine.SelectTab(
                tabId);

            Render();
        }

        private void OnTabClosePressed(
            int tabId)
        {
            engine.CloseTab(
                tabId);

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

        private void OnBreadcrumbPressed(
            string path)
        {
            engine.Navigate(
                path);

            Render();
        }

        // -------------------- SEARCH --------------------
        private void OnSearchChanged(
            ChangeEvent<string> evt)
        {
            engine.SetSearchQuery(
                evt.newValue);

            Render();
        }

        // -------------------- SIDEBAR --------------------
        private void OnHomePressed()
        {
            NavigateTo(
                FileExplorerLocation.Home);
        }

        private void OnDesktopPressed()
        {
            NavigateTo(
                FileExplorerLocation.Desktop);
        }

        private void OnDocumentsPressed()
        {
            NavigateTo(
                FileExplorerLocation.Documents);
        }

        private void OnPicturesPressed()
        {
            NavigateTo(
                FileExplorerLocation.Pictures);
        }

        private void OnDownloadsPressed()
        {
            NavigateTo(
                FileExplorerLocation.Downloads);
        }

        private void NavigateTo(
            FileExplorerLocation location)
        {
            engine.NavigateToLocation(
                location);

            Render();
        }

        private void OnRecentPressed()
        {
            Debug.Log(
                "Recent view not implemented yet.");
        }

        private void OnStarredPressed()
        {
            Debug.Log(
                "Starred view not implemented yet.");
        }

        // -------------------- COMMAND BAR --------------------
        private void OnNewPressed()
        {
            Debug.Log(
                "New item menu not implemented yet.");
        }

        private void OnSortPressed()
        {
            CycleSortMode();

            Render();
        }

        private void CycleSortMode()
        {
            FileExplorerTab tab =
                engine.ActiveTab;

            if (tab == null)
                return;

            FileExplorerSortMode next =
                tab.SortMode switch
                {
                    FileExplorerSortMode.Name =>
                        FileExplorerSortMode.DateModified,

                    FileExplorerSortMode.DateModified =>
                        FileExplorerSortMode.Type,

                    FileExplorerSortMode.Type =>
                        FileExplorerSortMode.Size,

                    _ =>
                        FileExplorerSortMode.Name
                };

            engine.SetSortMode(
                next);
        }

        private void OnViewPressed()
        {
            engine.CycleViewMode();

            Render();
        }

        private void OnFilterPressed()
        {
            Debug.Log(
                "Filter menu not implemented yet.");
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
            {
                engine.ToggleItemSelection(
                    item.Id);
            }
            else
            {
                engine.SelectItem(
                    item.Id);
            }

            Render();
        }

        private void OnItemOpened(
            FileExplorerItem item)
        {
            if (item == null)
                return;

            if (item.IsFolder)
            {
                engine.OpenItem(
                    item);

                Render();

                return;
            }

            OpenFile(
                item);
        }

        private void OpenFile(
            FileExplorerItem item)
        {
            Debug.Log(
                $"Open file: {item.Path}");

            // Later:
            //
            // DesktopManager.Instance.OpenFile(item);
            //
            // or route using item.FileDefinition.FileType /
            // item.FileDefinition.ContentId.
        }

        private void OnContentBackgroundPressed(
            ClickEvent evt)
        {
            if (evt.target !=
                    binder.ContentScrollView &&
                evt.target !=
                    binder.ContentContainer)
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
                viewBuilder.BuildState(
                    engine);

            view.Render(
                state);

            VisualElement tabs =
                viewBuilder.BuildTabs(
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

            view.SetTabs(
                tabs);

            view.SetBreadcrumbs(
                breadcrumbs);

            view.SetContent(
                directory);
        }
    }
}