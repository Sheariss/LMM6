using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.WebBrowser
{
    public sealed class WebBrowserViewState
    {
        public string TabTitle { get; set; } = "New Tab";
        public bool CanCloseTab { get; set; } = true;

        public bool CanGoBack { get; set; }
        public bool CanGoForward { get; set; }
        public bool IsLoading { get; set; }

        public string Address { get; set; } = "";
        public bool IsBookmarked { get; set; }

        public bool ShowBookmarksBar { get; set; } = true;
        public bool ShowBookmarksOverflow { get; set; }

        public bool ShowPage { get; set; } = true;
    }

    public sealed class WebBrowserView
    {
        private readonly WebBrowserBinder binder;

        public WebBrowserView(WebBrowserBinder binder)
        {
            this.binder = binder;
        }

        public void Render(WebBrowserViewState state)
        {
            if (state == null)
                return;

            SetTabTitle(state.TabTitle);
            SetTabCloseVisible(state.CanCloseTab);
            SetNavigationState(
                state.CanGoBack,
                state.CanGoForward,
                state.IsLoading);
            SetAddress(state.Address);
            SetBookmarked(state.IsBookmarked);
            SetBookmarksBarVisible(state.ShowBookmarksBar);
            SetBookmarksOverflowVisible(state.ShowBookmarksOverflow);
            SetPageVisible(state.ShowPage);
        }

        public void SetTabTitle(string title)
        {
            if (binder.BrowserTabTitle == null)
                return;

            binder.BrowserTabTitle.text =
                string.IsNullOrWhiteSpace(title)
                    ? "New Tab"
                    : title;
        }

        public void SetTabCloseVisible(bool visible)
        {
            SetVisible(binder.BrowserTabCloseButton, visible);
        }

        public void SetNavigationState(
            bool canGoBack,
            bool canGoForward,
            bool isLoading)
        {
            if (binder.BrowserBackButton != null)
                binder.BrowserBackButton.SetEnabled(canGoBack);

            if (binder.BrowserForwardButton != null)
                binder.BrowserForwardButton.SetEnabled(canGoForward);

            if (binder.BrowserReloadButton != null)
                binder.BrowserReloadButton.SetEnabled(!isLoading);
        }

        public void SetAddress(string address)
        {
            if (binder.BrowserAddressField == null)
                return;

            binder.BrowserAddressField.SetValueWithoutNotify(
                address ?? "");
        }

        public void SetBookmarked(bool bookmarked)
        {
            if (binder.BrowserBookmarkButton == null)
                return;

            binder.BrowserBookmarkButton.EnableInClassList(
                "browser-address-button--bookmarked",
                bookmarked);
        }

        public void SetBookmarksBarVisible(bool visible)
        {
            SetVisible(binder.BrowserBookmarksBar, visible);
        }

        public void SetBookmarksOverflowVisible(bool visible)
        {
            SetVisible(
                binder.BrowserBookmarksOverflowButton,
                visible);
        }

        public void SetPageVisible(bool visible)
        {
            SetVisible(binder.BrowserPageViewport, visible);
        }

        public void ClearPage()
        {
            if (binder.BrowserPageScrollView == null)
                return;

            binder.BrowserPageScrollView.Clear();
        }

        public void SetPageContent(VisualElement content)
        {
            if (binder.BrowserPageScrollView == null)
                return;

            binder.BrowserPageScrollView.Clear();

            if (content != null)
                binder.BrowserPageScrollView.Add(content);
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
    }
}