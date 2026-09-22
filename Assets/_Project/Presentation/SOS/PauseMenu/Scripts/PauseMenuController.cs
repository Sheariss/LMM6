using Atlas.Core.GameState;
using Atlas.Presentation.GameUI.SecurityScreen;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.PauseMenu
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        private UIDocument uiDoc;

        // -------------------- HELPERS --------------------
        private PauseMenuBinder binder;

        private bool isPaused = false;

        // -------------------- RUNTIME DEPENDENCIES --------------------
        private GameStateManager gameStateManager;

        // -------------------- LIFECYCLE --------------------
        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[PauseMenuController] UIDocument root was not found.");
                return;
            }

            if (!ResolveDependencies())
            {
                return;
            }

            binder = new PauseMenuBinder(uiDoc.rootVisualElement);
            if (!binder.IsValid)
            {
                Debug.LogError("[PauseMenuController] UI bindings are invalid.");
                return;
            }

            RegisterCallbacks();

            // TODO: Add ActionBinding and Callbacks
            Hide();
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePauseMenu();
            }
        }

        // -------------------- DEPENDENCIES --------------------
        private bool ResolveDependencies()
        {
            gameStateManager = GameStateManager.Instance;

            if (gameStateManager == null)
            {
                Debug.LogError("[PauseMenuController] GameStateManager instance was not found.");
                return false;
            }
            return true;
        }

        // -------------------- CALLBACKS --------------------
        private void RegisterCallbacks()
        {
            binder.ResumeButton.clicked += OnResumePressed;
            binder.SaveButton.clicked += OnSavePressed;
            binder.LoadButton.clicked += OnLoadPressed;
            binder.SettingsButton.clicked += OnSettingsPressed;
            binder.AccessibilityButton.clicked += OnAccessibilityPressed;
            binder.HelpButton.clicked += OnHelpPressed;
            binder.MainMenuButton.clicked += OnMainMenuPressed;
        }

        private void UnregisterCallbacks()
        {
            if (binder == null)
                return;

            binder.ResumeButton.clicked -= OnResumePressed;
            binder.SaveButton.clicked -= OnSavePressed;
            binder.LoadButton.clicked -= OnLoadPressed;
            binder.SettingsButton.clicked -= OnSettingsPressed;
            binder.AccessibilityButton.clicked -= OnAccessibilityPressed;
            binder.HelpButton.clicked -= OnHelpPressed;
            binder.MainMenuButton.clicked -= OnMainMenuPressed;
        }


        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        // -------------------- BUTTON EVENTS --------------------
        private void OnResumePressed()
        {
            Debug.Log("[PauseMenuController] Resume pressed.");

            Hide();

            gameStateManager.ResumeGame();
        }

        private void OnSavePressed()
        {
            Debug.Log("[PauseMenuController] Save pressed.");

            // TODO: Call the SaveManager to actually save the game
        }

        private void OnLoadPressed()
        {
            Debug.Log("[PauseMenuController] Load pressed.");

            gameStateManager.EnterMainMenu();
            // TODO: SkipMainMenu to jsut show save slots.
        }

        private void OnSettingsPressed()
        {
            Debug.Log("[PauseMenuController] Settings pressed.");

            // TODO: Show settings via MainMenu Controller or SettingsController
        }

        private void OnAccessibilityPressed()
        {
            Debug.Log("[PauseMenuController] Accessibility pressed.");

            // TODO: Show Accessibility settings
        }

        private void OnHelpPressed()
        {
            Debug.Log("[PauseMenuController] Help pressed.");

            // TODO: Help/Accessibility panel shown
        }

        private void OnMainMenuPressed()
        {
            Debug.Log("[PauseMenuController] Main Menu pressed.");

            gameStateManager.EnterMainMenu();
        }

        // -------------------- PANEL VISIBILITY --------------------
        private void TogglePauseMenu()
        {
            if (isPaused)
                Hide();
            else
                Show();
        }

        private void Show()
        {
            isPaused = true;

            binder.Root.style.display = DisplayStyle.Flex;

            gameStateManager.PauseGame();

            Time.timeScale = 0f;
        }

        private void Hide()
        {
            isPaused = false;

            binder.Root.style.display = DisplayStyle.None;

            gameStateManager.ResumeGame();

            Time.timeScale = 1f;
        }


    }
}