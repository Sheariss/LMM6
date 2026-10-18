using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class StartMenuBinder : UIBinder
    {
        public VisualElement Root { get; }
        public TextField SearchField { get; }

        public ScrollView HomePanel { get; }
        public VisualElement PinnedGrid { get; }
        public Label PinnedEmpty { get; }
        public Button PinnedToggle { get; }
        public Label PinnedToggleText { get; }
        public Image PinnedToggleIcon { get; }

        public VisualElement RecentGrid { get; }
        public Label RecentEmpty { get; }
        public Button RecentShowAllButton { get; }

        public Button ViewButton { get; }
        public Label ViewValue { get; }
        public VisualElement CategoryGrid { get; }
        public VisualElement AllAppsGrid { get; }
        public VisualElement AlphabeticalList { get; }
        public Label AppsEmpty { get; }

        public VisualElement RecentPanel { get; }
        public Button RecentBackButton { get; }
        public ScrollView RecentScrollView { get; }
        public VisualElement RecentList { get; }
        public Label RecentPanelEmpty { get; }

        public VisualElement SearchPanel { get; }
        public ScrollView SearchScrollView { get; }
        public VisualElement SearchResults { get; }
        public Label SearchEmpty { get; }

        public Image ProfileImage { get; }
        public Label ProfileName { get; }

        public VisualElement CategoryPopup { get; }
        public Label CategoryPopupTitle { get; }
        public ScrollView CategoryPopupScroll { get; }
        public VisualElement CategoryPopupGrid { get; }

        public VisualElement ViewPopup { get; }
        public Button CategoryViewButton { get; }
        public Button GridViewButton { get; }
        public Button ListViewButton { get; }

        public Button PowerButton { get; }
        public VisualElement PowerPopup { get; }
        public Button LockButton { get; }
        public Button SleepButton { get; }
        public Button ShutdownButton { get; }
        public Button RestartButton { get; }

        public StartMenuBinder(VisualElement host)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));

            Root = Bind<VisualElement>(host, "StartMenuRoot");

            if (Root == null)
            {
                throw new InvalidOperationException(
                    "The Start menu template has no StartMenuRoot.");
            }

            SearchField = Bind<TextField>(
                Root, "StartMenuSearchField");

            HomePanel = Bind<ScrollView>(
                Root, "StartMenuHomePanel");

            PinnedGrid = Bind<VisualElement>(
                Root, "StartMenuPinnedGrid");

            PinnedEmpty = Bind<Label>(
                Root, "StartMenuPinnedEmpty");

            PinnedToggle = Bind<Button>(
                Root, "StartMenuPinnedToggle");

            PinnedToggleText = Bind<Label>(
                Root, "StartMenuPinnedToggleText");

            PinnedToggleIcon = Bind<Image>(
                Root, "StartMenuPinnedToggleIcon");

            RecentGrid = Bind<VisualElement>(
                Root, "StartMenuRecentGrid");

            RecentEmpty = Bind<Label>(
                Root, "StartMenuRecentEmpty");

            RecentShowAllButton = Bind<Button>(
                Root, "StartMenuRecentShowAllButton");

            ViewButton = Bind<Button>(
                Root, "StartMenuViewButton");

            ViewValue = Bind<Label>(
                Root, "StartMenuViewValue");

            CategoryGrid = Bind<VisualElement>(
                Root, "StartMenuCategoryGrid");

            AllAppsGrid = Bind<VisualElement>(
                Root, "StartMenuAllAppsGrid");

            AlphabeticalList = Bind<VisualElement>(
                Root, "StartMenuAlphabeticalList");

            AppsEmpty = Bind<Label>(
                Root, "StartMenuAppsEmpty");

            RecentPanel = Bind<VisualElement>(
                Root, "StartMenuRecentPanel");

            RecentBackButton = Bind<Button>(
                Root, "StartMenuRecentBackButton");

            RecentScrollView = Bind<ScrollView>(
                Root, "StartMenuRecentScrollView");

            RecentList = Bind<VisualElement>(
                Root, "StartMenuRecentList");

            RecentPanelEmpty = Bind<Label>(
                Root, "StartMenuRecentPanelEmpty");

            SearchPanel = Bind<VisualElement>(
                Root, "StartMenuSearchPanel");

            SearchScrollView = Bind<ScrollView>(
                Root, "StartMenuSearchScrollView");

            SearchResults = Bind<VisualElement>(
                Root, "StartMenuSearchResults");

            SearchEmpty = Bind<Label>(
                Root, "StartMenuSearchEmpty");

            ProfileImage = Bind<Image>(
                Root, "StartMenuProfileImage");

            ProfileName = Bind<Label>(
                Root, "StartMenuProfileName");

            CategoryPopup = Bind<VisualElement>(
                Root, "StartMenuCategoryPopup");

            CategoryPopupTitle = Bind<Label>(
                Root, "StartMenuCategoryPopupTitle");

            CategoryPopupScroll = Bind<ScrollView>(
                Root, "StartMenuCategoryPopupScroll");

            CategoryPopupGrid = Bind<VisualElement>(
                Root, "StartMenuCategoryPopupGrid");

            ViewPopup = Bind<VisualElement>(
                Root, "StartMenuViewPopup");

            CategoryViewButton = Bind<Button>(
                Root, "StartMenuCategoryViewButton");

            GridViewButton = Bind<Button>(
                Root, "StartMenuGridViewButton");

            ListViewButton = Bind<Button>(
                Root, "StartMenuListViewButton");

            PowerButton = Bind<Button>(
                Root, "StartMenuPowerButton");

            PowerPopup = Bind<VisualElement>(
                Root, "StartMenuPowerPopup");

            LockButton = Bind<Button>(
                Root, "StartMenuLockButton");

            SleepButton = Bind<Button>(
                Root, "StartMenuSleepButton");

            ShutdownButton = Bind<Button>(
                Root, "StartMenuShutdownButton");

            RestartButton = Bind<Button>(
                Root, "StartMenuRestartButton");

            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "The Start menu template is missing required elements. " +
                    "Use the latest StartMenu.uxml.");
            }
        }
    }
}