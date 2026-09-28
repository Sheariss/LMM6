using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.WebBrowser
{
    public sealed class WebBrowserBinder : UIBinder
    {
        // -------------------- TAB BAR --------------------
        public VisualElement BrowserTabBar { get; }
        public VisualElement BrowserTabsContainer { get; }
        public VisualElement BrowserTab { get; }
        public VisualElement BrowserTabFavicon { get; }
        public Label BrowserTabTitle { get; }
        public Button BrowserTabCloseButton { get; }
        public Button BrowserAddTabButton { get; }
        public VisualElement BrowserTabBarSpacer { get; }

        // -------------------- NAVIGATION --------------------
        public VisualElement BrowserNavigationBar { get; }
        public Button BrowserBackButton { get; }
        public Button BrowserForwardButton { get; }
        public Button BrowserReloadButton { get; }

        // -------------------- ADDRESS BAR --------------------
        public VisualElement BrowserAddressBar { get; }
        public VisualElement BrowserSiteIcon { get; }
        public TextField BrowserAddressField { get; }
        public Button BrowserBookmarkButton { get; }
        public Button BrowserMenuButton { get; }

        // -------------------- BOOKMARKS --------------------
        public VisualElement BrowserBookmarksBar { get; }
        public VisualElement BrowserBookmarksContainer { get; }
        public Button BookmarkAshfordHerald { get; }
        public Button BookmarkAshfordPD { get; }
        public Button BookmarkTown { get; }
        public Button BrowserBookmarksOverflowButton { get; }

        // -------------------- PAGE VIEWPORT --------------------
        public VisualElement BrowserPageViewport { get; }
        public VisualElement BrowserPageContainer { get; }
        public ScrollView BrowserPageScrollView { get; }

        // -------------------- FOOTER --------------------
        public VisualElement WindowFooter { get; }

        public WebBrowserBinder(VisualElement root)
        {

            // Tab Bar
            BrowserTabBar = Bind<VisualElement>(root, "BrowserTabBar");
            BrowserTabsContainer = Bind<VisualElement>(root, "BrowserTabsContainer");
            BrowserTab = Bind<VisualElement>(root, "BrowserTab");
            BrowserTabFavicon = Bind<VisualElement>(root, "BrowserTabFavicon");
            BrowserTabTitle = Bind<Label>(root, "BrowserTabTitle");
            BrowserTabCloseButton = Bind<Button>(root, "BrowserTabCloseButton");
            BrowserAddTabButton = Bind<Button>(root, "BrowserAddTabButton");
            BrowserTabBarSpacer = Bind<VisualElement>(root, "BrowserTabBarSpacer");

            // Navigation
            BrowserNavigationBar = Bind<VisualElement>(root, "BrowserNavigationBar");
            BrowserBackButton = Bind<Button>(root, "BrowserBackButton");
            BrowserForwardButton = Bind<Button>(root, "BrowserForwardButton");
            BrowserReloadButton = Bind<Button>(root, "BrowserReloadButton");

            // Address Bar
            BrowserAddressBar = Bind<VisualElement>(root, "BrowserAddressBar");
            BrowserSiteIcon = Bind<VisualElement>(root, "BrowserSiteIcon");
            BrowserAddressField = Bind<TextField>(root, "BrowserAddressField");
            BrowserBookmarkButton = Bind<Button>(root, "BrowserBookmarkButton");
            BrowserMenuButton = Bind<Button>(root, "BrowserMenuButton");

            // Bookmarks
            BrowserBookmarksBar = Bind<VisualElement>(root, "BrowserBookmarksBar");
            BrowserBookmarksContainer = Bind<VisualElement>(root, "BrowserBookmarksContainer");
            BookmarkAshfordHerald = Bind<Button>(root, "BookmarkAshfordHerald");
            BookmarkAshfordPD = Bind<Button>(root, "BookmarkAshfordPD");
            BookmarkTown = Bind<Button>(root, "BookmarkTown");
            BrowserBookmarksOverflowButton = Bind<Button>(root, "BrowserBookmarksOverflowButton");

            // Page Viewport
            BrowserPageViewport = Bind<VisualElement>(root, "BrowserPageViewport");
            BrowserPageContainer = Bind<VisualElement>(root, "BrowserPageContainer");
            BrowserPageScrollView = Bind<ScrollView>(root, "BrowserPageScrollView");
        }
    }
}