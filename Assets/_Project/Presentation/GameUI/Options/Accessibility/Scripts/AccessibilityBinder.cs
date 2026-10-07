using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Accessibility
{
    public sealed class AccessibilityBinder : UIBinder
    {
        public VisualElement Root { get; }

        public Label TitleLabel { get; }
        public Label SubtitleLabel { get; }
        public Button BackButton { get; }

        public TextField SearchField { get; }
        public Button SearchClearButton { get; }

        public ScrollView NavigationScroll { get; }
        public VisualElement Navigation { get; }

        public Label CategoryTitleLabel { get; }
        public Label CategoryDescriptionLabel { get; }
        public ScrollView ContentScroll { get; }
        public VisualElement Content { get; }

        public Label StatusLabel { get; }
        public Button ResetButton { get; }
        public Button ApplyButton { get; }
        public Button DoneButton { get; }

        public AccessibilityBinder(VisualElement documentRoot)
        {
            if (documentRoot == null)
            {
                Debug.LogError("[AccessibilityBinder] Document root is null.");
                IsValid = false;
                return;
            }

            Root = Bind<VisualElement>(documentRoot, "SettingsShellRoot");

            if (Root == null)
                return;

            TitleLabel = Bind<Label>(Root, "ShellTitle");
            SubtitleLabel = Bind<Label>(Root, "ShellSubtitle");
            BackButton = Bind<Button>(Root, "ShellBackButton");

            SearchField = Bind<TextField>(Root, "ShellSearchField");
            SearchClearButton = Bind<Button>(Root, "ShellSearchClearButton");

            NavigationScroll = Bind<ScrollView>(
                Root, "ShellNavigationScrollView");
            Navigation = Bind<VisualElement>(Root, "ShellNavigation");

            CategoryTitleLabel = Bind<Label>(Root, "ShellCategoryTitle");
            CategoryDescriptionLabel = Bind<Label>(
                Root, "ShellCategoryDescription");

            ContentScroll = Bind<ScrollView>(Root, "ShellContentScrollView");
            Content = Bind<VisualElement>(Root, "ShellContent");

            StatusLabel = Bind<Label>(Root, "ShellStatusLabel");
            ResetButton = Bind<Button>(Root, "ShellResetButton");
            ApplyButton = Bind<Button>(Root, "ShellApplyButton");
            DoneButton = Bind<Button>(Root, "ShellDoneButton");
        }
    }
}