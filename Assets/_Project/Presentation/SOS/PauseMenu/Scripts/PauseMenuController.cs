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
                Debug.LogError("[SaveSlotMenuController] GameStateManager instance was not found.");
                return false;
            }
            return true;
        }

        //
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