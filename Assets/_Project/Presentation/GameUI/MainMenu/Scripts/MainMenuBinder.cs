using System;
using UnityEngine;
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

        // -------------------- STATE --------------------

        public bool IsValid { get; }

        // -------------------- UI BINDER --------------------
        public MainMenuBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError(
                    "[MainMenuBinder] Root VisualElement is null."
                );

                IsValid = false;
                return;
            }

            NewGameButton = root.Q<Button>("MM-NewGameButton");
            ContinueButton = root.Q<Button>("MM-LoadGameButton");
            SettingsButton = root.Q<Button>("MM-GameSettingsButton");
            AccessibilityButton = root.Q<Button>("MM-GameAccessibilityButton");
            CreditsButton = root.Q<Button>("MM-GameCreditsButton");
            ExitButton = root.Q<Button>("MM-ExitGameButton");

            IsValid = VerifyBindings();
        }

        // -------------------- VERIFICATION --------------------
        private bool VerifyBindings()
        {
            bool isValid = true;

            if (NewGameButton == null)
            {
                Debug.LogError("[MainMenuBinder] Missing required element: 'MM-NewGameButton'.");
                isValid = false;
            }

            if (ContinueButton == null)
            {
                Debug.LogError("[MainMenuBinder] Missing required element: 'MM-ContinueButton'.");
                isValid = false;
            }

            if (SettingsButton == null)
            {
                Debug.LogError("[MainMenuBinder] Missing required element: 'MM-SettingsButton'.");
                isValid = false;
            }

            if (AccessibilityButton == null)
            {
                Debug.LogError("[MainMenuBinder] Missing required element: 'MM-AccessibilityButton'.");
                isValid = false;
            }

            if (CreditsButton == null)
            {
                Debug.LogError("[MainMenuBinder] Missing required element: 'MM-CreditsButton'.");
                isValid = false;
            }

            if (ExitButton == null)
            {
                Debug.LogError("[MainMenuBinder] Missing required element: 'MM-ExitButton'.");
                isValid = false;
            }

            if (isValid)
            {
                Debug.Log("[MainMenuBinder] All required UI bindings verified.");
            }

            return isValid;
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