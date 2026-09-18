using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Core.Persistence;
using Atlas.Core.GameState;
using System;
using Atlas.Presentation.GameUI.SaveSlotMenu;

namespace Atlas.Presentation.GameUI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------

        private UIDocument uiDoc;

        // -------------------- RUNTIME DEPENDENCIES --------------------

        private SaveManager saveManager;
        // private ProgressionManager progressionManager;
        private GameStateManager gameStateManager;

        // -------------------- MENU CONTROLLERS --------------------

        [SerializeField]
        private SaveSlotMenuController saveSlotMenuController;



        // -------------------- HELPERS --------------------

        private MainMenuBinder binder;
        private MainMenuView view;
        private MainMenuViewBuilder builder;

        // -------------------- LIFECYCLE --------------------

        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError(
                    "[MainMenuController] UIDocument was not found in parent hierarchy."
                );

                return;
            }

            if (!ResolveDependencies())
            {
                return;
            }

            binder =
                new MainMenuBinder(
                    uiDoc.rootVisualElement
                );

            view =
                new MainMenuView(binder);

            builder =
                new MainMenuViewBuilder(
                    saveManager//,
                    //progressionManager
                );

            BindActions();

            RefreshView();
        }

        // -------------------- DEPENDENCIES --------------------
        private bool ResolveDependencies()
        {
            gameStateManager = GameStateManager.Instance;
            saveManager = SaveManager.Instance;
            // progressionManager = ProgressionManager.Instance;

            bool valid = true;

            if (gameStateManager == null)
            {
                Debug.LogError(
                    "[MainMenuController] GameStateManager instance was not found."
                );

                valid = false;
            }

            if (saveManager == null)
            {
                Debug.LogError(
                    "[MainMenuController] SaveManager instance was not found."
                );

                valid = false;
            }

            /*
            if (progressionManager == null)
            {
                Debug.LogError(
                    "[MainMenuController] ProgressionManager instance was not found."
                );

                valid = false;
            }*/

            if (saveSlotMenuController == null)
            {
                Debug.LogError(
                    "[MainMenuController] SaveSlotMenuController reference was not assigned."
                );

                valid = false;
            }


            return valid;
        }

        // -------------------- ACTION BINDING --------------------

        private void BindActions()
        {
            binder.BindActions(
                OnNewGamePressed,
                OnContinuePressed,
                OnSettingsPressed,
                OnAccessibilityPressed,
                OnCreditsPressed,
                OnExitPressed
            );
        }

        // -------------------- VIEW --------------------

        public void RefreshView()
        {
            if (builder == null || view == null)
            {
                return;
            }

            MainMenuView.ViewState state = builder.Build();

            view.Apply(state);
        }

        // -------------------- ACTIONS --------------------

        private void OnNewGamePressed()
        {
            Debug.Log("[MainMenuController] OnNewGamePressed called.");
            saveSlotMenuController.ShowForNewGame();

        }

        private void OnContinuePressed()
        {
            saveSlotMenuController.ShowForContinue();
        }

        private void OnSettingsPressed()
        {
            // settingsMenuController.Show();
        }

        private void OnAccessibilityPressed()
        {
            // accessibilityMenuController.Show();
        }

        private void OnCreditsPressed()
        {
            gameStateManager.EnterCredits();
        }

        private void OnExitPressed()
        {
            Application.Quit();
        }
    }
}