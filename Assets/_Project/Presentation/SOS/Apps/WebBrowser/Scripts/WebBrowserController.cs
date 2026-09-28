using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.WebBrowser
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class WebBrowserController : MonoBehaviour
    {
        private UIDocument uiDocument;

        private WebBrowserBinder binder;
        private WebBrowserView view;
        private WebBrowserViewBuilder viewBuilder;
        private WebBrowserEngine engine;

        private void Start()
        {
            uiDocument = GetComponent<UIDocument>();

            binder = new WebBrowserBinder(
                uiDocument.rootVisualElement);

            engine = new WebBrowserEngine();
            view = new WebBrowserView(binder);
            viewBuilder = new WebBrowserViewBuilder();

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
            if (binder.BrowserBackButton != null)
                binder.BrowserBackButton.clicked += OnBackPressed;

            if (binder.BrowserForwardButton != null)
                binder.BrowserForwardButton.clicked += OnForwardPressed;

            if (binder.BrowserReloadButton != null)
                binder.BrowserReloadButton.clicked += OnReloadPressed;

            if (binder.BrowserBookmarkButton != null)
                binder.BrowserBookmarkButton.clicked += OnBookmarkPressed;

            if (binder.BrowserAddressField != null)
                binder.BrowserAddressField.RegisterCallback<KeyDownEvent>(
                    OnAddressFieldKeyDown);

            if (binder.BookmarkAshfordHerald != null)
                binder.BookmarkAshfordHerald.clicked += OnAshfordHeraldPressed;

            if (binder.BookmarkAshfordPD != null)
                binder.BookmarkAshfordPD.clicked += OnAshfordPDPressed;

            if (binder.BookmarkTown != null)
                binder.BookmarkTown.clicked += OnTownPressed;
        }

        private void UnregisterCallbacks()
        {
            if (binder == null)
                return;

            if (binder.BrowserBackButton != null)
                binder.BrowserBackButton.clicked -= OnBackPressed;

            if (binder.BrowserForwardButton != null)
                binder.BrowserForwardButton.clicked -= OnForwardPressed;

            if (binder.BrowserReloadButton != null)
                binder.BrowserReloadButton.clicked -= OnReloadPressed;

            if (binder.BrowserBookmarkButton != null)
                binder.BrowserBookmarkButton.clicked -= OnBookmarkPressed;

            if (binder.BrowserAddressField != null)
                binder.BrowserAddressField.UnregisterCallback<KeyDownEvent>(
                    OnAddressFieldKeyDown);

            if (binder.BookmarkAshfordHerald != null)
                binder.BookmarkAshfordHerald.clicked -= OnAshfordHeraldPressed;

            if (binder.BookmarkAshfordPD != null)
                binder.BookmarkAshfordPD.clicked -= OnAshfordPDPressed;

            if (binder.BookmarkTown != null)
                binder.BookmarkTown.clicked -= OnTownPressed;
        }

        // -------------------- NAVIGATION --------------------
        private void OnBackPressed()
        {
            engine.GoBack();

            LoadCurrentPage();
        }

        private void OnForwardPressed()
        {
            engine.GoForward();

            LoadCurrentPage();
        }

        private void OnReloadPressed()
        {
            engine.Reload();

            LoadCurrentPage();
        }

        private void OnAddressFieldKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
                return;

            Navigate(binder.BrowserAddressField.value);

            evt.StopPropagation();
        }

        private void Navigate(string address)
        {
            engine.Navigate(address);

            LoadCurrentPage();
        }

        // -------------------- BOOKMARK --------------------
        private void OnBookmarkPressed()
        {
            engine.ToggleBookmark();

            Render();
        }

        private void OnAshfordHeraldPressed()
        {
            Navigate("ashfordherald.com");
        }

        private void OnAshfordPDPressed()
        {
            Navigate("ashfordpd.gov");
        }

        private void OnTownPressed()
        {
            Navigate("ashfordmn.gov");
        }

        // -------------------- PAGE --------------------
        private void LoadCurrentPage()
        {
            view.ClearPage();

            string address = engine.CurrentAddress;

            if (string.IsNullOrWhiteSpace(address))
            {
                Render();
                return;
            }

            VisualElement page = CreatePlaceholderPage(address);

            view.SetPageContent(page);

            Render();
        }

        private VisualElement CreatePlaceholderPage(string address)
        {
            VisualElement root = new VisualElement();

            root.AddToClassList("browser-placeholder-page");

            Label title = new Label(address);

            title.AddToClassList("browser-placeholder-title");

            Label description = new Label(
                "Page content will be loaded here.");

            description.AddToClassList(
                "browser-placeholder-description");

            root.Add(title);
            root.Add(description);

            return root;
        }

        // -------------------- RENDER --------------------
        private void Render()
        {
            WebBrowserViewState state =
                viewBuilder.Build(engine);

            view.Render(state);
        }
    }
}