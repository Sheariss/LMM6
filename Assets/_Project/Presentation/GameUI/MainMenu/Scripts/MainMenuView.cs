using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.MainMenu
{
    public sealed class MainMenuView
    {
        private readonly MainMenuBinder binder;

        public MainMenuView(MainMenuBinder binder)
        {
            this.binder = binder;
        }

        // -------------------- VIEW --------------------
        public void Apply(ViewState state)
        {
            int btnNumber = 1;

            ApplyButton(binder.NewGameButton, true, "Initialize New Session", ref btnNumber);
            ApplyButton(binder.ContinueButton, state.ContinueVisible, "Resume Previous Session", ref btnNumber);
            ApplyButton(binder.SettingsButton, true, "System Configuration", ref btnNumber);
            ApplyButton(binder.AccessibilityButton, true, "Accessibility", ref btnNumber);
            ApplyButton(binder.CreditsButton, state.CreditsVisible, "View System Contributors", ref btnNumber);
            ApplyButton(binder.ExitButton, true, "Exit Workstation", ref btnNumber);
        }

        // -------------------- BUTTON VIEW --------------------
        private static void ApplyButton(Button button, bool visible, string description, ref int btnNumber)
        {
            button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (!visible)
            {
                return;
            }
            button.text = $"{btnNumber}) {description}";
            btnNumber++;
        }

        // -------------------- VIEW STATE --------------------
        public sealed class ViewState
        {
            public bool ContinueVisible { get; init; }
            public bool CreditsVisible { get; init; }
        }
    }
}