using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Core.Persistence;
using Atlas.Core.GameState;

namespace Atlas.Presentation.GameUI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------

        [SerializeField]
        private UIDocument uiDoc;

        // -------------------- RUNTIME DEPENDENCIES --------------------

        private SaveManager saveManager;
        private ProgressionManager progressionManager;
        private GameStateManager gameStateManager;

        // -------------------- HELPERS --------------------

        private MainMenuBinder binder;
        private MainMenuView view;
        private MainMenuViewBuilder builder;

        // -------------------- LIFECYCLE --------------------

        private void Awake()
        {
            ResolveDependencies();

            binder =
                new MainMenuBinder(
                    uiDoc.rootVisualElement
                );

            view =
                new MainMenuView(binder);

            builder =
                new MainMenuViewBuilder(
                    saveManager,
                    progressionManager
                );

            BindActions();
        }

        private void OnEnable()
        {
            RefreshView();
        }

        // -------------------- DEPENDENCIES --------------------
        private void ResolveDependencies()
        {
            saveManager = SaveManager.Instance;

            //progressionManager = ProgressionManager.Instance;

            gameStateManager = GameStateManager.Instance;

            if (saveManager == null)
            {
                Debug.LogError("[MainMenuController] SaveManager instance was not found.");
            }

            if (progressionManager == null)
            {
                Debug.LogError("[MainMenuController] ProgressionManager instance was not found.");
            }

            if (gameStateManager == null)
            {
                Debug.LogError("[MainMenuController] GameStateManager instance was not found.");
            }
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
            MainMenuView.ViewState state = builder.Build();
            view.Apply(state);
        }

        // -------------------- ACTIONS --------------------

        private void OnNewGamePressed()
        {
            // saveSlotMenuController.ShowForNewGame();
        }

        private void OnContinuePressed()
        {
            // saveSlotMenuController.ShowExistingSaves();
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