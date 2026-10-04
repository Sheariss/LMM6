using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.Options
{
    public sealed class OptionsShellBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Label TitleLabel { get; }
        public Label DescriptionLabel { get; }
        public VisualElement NavigationContainer { get; }
        public VisualElement ContentContainer { get; }
        public Label SectionTitleLabel { get; }
        public Label SectionDescriptionLabel { get; }
        public VisualElement OptionsContainer { get; }
        public Button BackButton { get; }
        public Button ResetButton { get; }
        public Button ApplyButton { get; }

        public OptionsShellBinder(VisualElement root)
        {
            Root = Bind<VisualElement>(root, "OptionsShellRoot");
            TitleLabel = Bind<Label>(root, "OptionsShellTitle");
            DescriptionLabel = Bind<Label>(root, "OptionsShellDescription");
            NavigationContainer = Bind<VisualElement>(root, "OptionsNavigation");
            ContentContainer = Bind<VisualElement>(root, "OptionsContent");
            SectionTitleLabel = Bind<Label>(root, "OptionsSectionTitle");
            SectionDescriptionLabel = Bind<Label>(root, "OptionsSectionDescription");
            OptionsContainer = Bind<VisualElement>(root, "OptionsContainer");
            BackButton = Bind<Button>(root, "OptionsBackButton");
            ResetButton = Bind<Button>(root, "OptionsResetButton");
            ApplyButton = Bind<Button>(root, "OptionsApplyButton");
        }
    }
}