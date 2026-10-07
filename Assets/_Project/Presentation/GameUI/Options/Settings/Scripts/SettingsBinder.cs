using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Settings
{
    public sealed class SettingsBinder : UIBinder
    {
        public VisualElement Root { get; }

        // Header
        public VisualElement Header { get; }
        public Label TitleLabel { get; }
        public Label SubtitleLabel { get; }
        public Button BackButton { get; }

        // Search
        public VisualElement SearchGroup { get; }
        public TextField SearchField { get; }
        public Button SearchClearButton { get; }

        // Navigation
        public ScrollView NavigationScroll { get; }
        public VisualElement Navigation { get; }

        // Content
        public Label CategoryTitleLabel { get; }
        public Label CategoryDescriptionLabel { get; }
        public ScrollView ContentScroll { get; }
        public VisualElement Content { get; }

        // Footer
        public Label StatusLabel { get; }
        public Button ResetButton { get; }
        public Button ApplyButton { get; }
        public Button DoneButton { get; }

        public SettingsBinder(VisualElement documentRoot)
        {
            if (documentRoot == null)
            {
                Debug.LogError(
                    "[SettingsBinder] Document root is null.");

                IsValid = false;
                return;
            }

            Root = Bind<VisualElement>(
                documentRoot, "SettingsShellRoot");

            // UIBinder.Bind calls root.Q, so stop if the root is missing.
            if (Root == null)
                return;

            Header = Bind<VisualElement>(Root, "ShellHeader");
            TitleLabel = Bind<Label>(Root, "ShellTitle");
            SubtitleLabel = Bind<Label>(Root, "ShellSubtitle");
            BackButton = Bind<Button>(Root, "ShellBackButton");

            SearchGroup = Bind<VisualElement>(Root, "ShellSearchGroup");
            SearchField = Bind<TextField>(Root, "ShellSearchField");
            SearchClearButton = Bind<Button>(
                Root, "ShellSearchClearButton");

            NavigationScroll = Bind<ScrollView>(
                Root, "ShellNavigationScrollView");
            Navigation = Bind<VisualElement>(Root, "ShellNavigation");

            CategoryTitleLabel = Bind<Label>(
                Root, "ShellCategoryTitle");
            CategoryDescriptionLabel = Bind<Label>(
                Root, "ShellCategoryDescription");
            ContentScroll = Bind<ScrollView>(
                Root, "ShellContentScrollView");
            Content = Bind<VisualElement>(Root, "ShellContent");

            StatusLabel = Bind<Label>(Root, "ShellStatusLabel");
            ResetButton = Bind<Button>(Root, "ShellResetButton");
            ApplyButton = Bind<Button>(Root, "ShellApplyButton");
            DoneButton = Bind<Button>(Root, "ShellDoneButton");
        }
    }
}