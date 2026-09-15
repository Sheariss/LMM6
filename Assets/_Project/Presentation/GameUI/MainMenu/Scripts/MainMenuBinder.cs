using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.MainMenu
{
    public sealed class MainMenuBinder
    {
        // -------------------- UI ELEMENTS --------------------
        public Button NewGameButton { get; }
        public Button ContinueButton { get; }
        public Button SettingsButton { get; }
        public Button AccessibilityButton { get; }
        public Button CreditsButton { get; }
        public Button ExitButton { get; }

        // -------------------- UI BINDER --------------------

        public MainMenuBinder(VisualElement root)
        {
            NewGameButton = root.Q<Button>("MM-NewGameButton");
            ContinueButton = root.Q<Button>("MM-ContinueButton");
            SettingsButton = root.Q<Button>("MM-SettingsButton");
            AccessibilityButton = root.Q<Button>("MM-AccessibilityButton");
            CreditsButton = root.Q<Button>("MM-CreditsButton");
            ExitButton = root.Q<Button>("MM-ExitButton");
        }

        // -------------------- ACTION BINDER --------------------
        public void BindActions(
            Action onNewGamePressed,
            Action onContinuePressed,
            Action onSettingsPressed,
            Action onAccessibilityPressed,
            Action onCreditsPressed,
            Action onExitPressed)
        {
            NewGameButton.clicked += onNewGamePressed;
            ContinueButton.clicked += onContinuePressed;
            SettingsButton.clicked += onSettingsPressed;
            AccessibilityButton.clicked += onAccessibilityPressed;
            CreditsButton.clicked += onCreditsPressed;
            ExitButton.clicked += onExitPressed;
        }
    }
}