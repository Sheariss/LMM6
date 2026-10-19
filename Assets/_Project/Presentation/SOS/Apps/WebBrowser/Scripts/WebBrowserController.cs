using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.WebBrowser
{
    public sealed class WebBrowserController : AppContentController
    {
        private WebBrowserBinder binder;
        private WebBrowserView view;
        private WebBrowserViewBuilder viewBuilder;
        private WebBrowserEngine engine;

        protected override void OnInitialize()
        {
            binder = new WebBrowserBinder(InstanceRoot);

            engine = new WebBrowserEngine();
            view = new WebBrowserView(binder);
            viewBuilder = new WebBrowserViewBuilder();

            RegisterCallbacks();
            Render();
        }

        protected override void OnShutdown()
        {
            binder = null;
            view = null;
            viewBuilder = null;
            engine = null;
        }

        private void RegisterCallbacks()
        {
            if (binder.BrowserBackButton != null)
                BindClick(binder.BrowserBackButton, OnBackPressed);

            if (binder.BrowserForwardButton != null)
                BindClick(binder.BrowserForwardButton, OnForwardPressed);

            if (binder.BrowserReloadButton != null)
                BindClick(binder.BrowserReloadButton, OnReloadPressed);

            if (binder.BrowserBookmarkButton != null)
                BindClick(binder.BrowserBookmarkButton, OnBookmarkPressed);

            if (binder.BrowserAddressField != null)
            {
                BindCallback<KeyDownEvent>(
                    binder.BrowserAddressField,
                    OnAddressFieldKeyDown);
            }

            if (binder.BookmarkAshfordHerald != null)
                BindClick(binder.BookmarkAshfordHerald, OnAshfordHeraldPressed);

            if (binder.BookmarkAshfordPD != null)
                BindClick(binder.BookmarkAshfordPD, OnAshfordPDPressed);

            if (binder.BookmarkTown != null)
                BindClick(binder.BookmarkTown, OnTownPressed);
        }

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
            {
                return;
            }

            Navigate(binder.BrowserAddressField.value);
            evt.StopPropagation();
        }

        private void Navigate(string address)
        {
            engine.Navigate(address);
            LoadCurrentPage();
        }

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

        private void Render()
        {
            WebBrowserViewState state = viewBuilder.Build(engine);
            view.Render(state);

            NotifyPreviewChanged(
                string.IsNullOrWhiteSpace(engine.CurrentAddress)
                    ? "Web Browser"
                    : engine.CurrentAddress);
        }
    }
}