using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------

        [SerializeField]
        private UIDocument uiDoc;

        // -------------------- RUNTIME DEPENDENCIES --------------------

        [SerializeField]
        private SaveManager saveManager;

        [SerializeField]
        private ProgressionManager progressionManager;

        // -------------------- HELPERS --------------------

        private MainMenuBinder binder;
        private MainMenuView view;
        private MainMenuViewBuilder builder;

        // -------------------- LIFECYCLE --------------------

        private void Awake()
        {
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
            MainMenuView.ViewState state =
                builder.Build();

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
            GameStateManager.EnterCredits();
        }

        private void OnExitPressed()
        {
            Application.Quit();
        }
    }
}