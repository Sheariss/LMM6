using Atlas.Core.SceneManagement;
using System;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Atlas.Core.GameState
{
    /// <summary>
    /// Maintains the current IIS game flow state and gameplay mode, coordinates high-level application flow.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        // Singleton Instance
        public static GameStateManager Instance { get; private set; }

        //Getters
        // Internal State primitives
        public GameFlowState FlowState { get; private set; }
        public GameplayMode GameplayMode { get; private set; }

        /// <summary>
        /// Events raised after the Game Flow State or Gameplay Mode changes.
        /// </summary>
        public event Action<GameFlowState, GameFlowState> FlowStateChanged;
        public event Action<GameplayMode, GameplayMode> GameplayModeChanged;

        // Awaken Manager check for duplicate instances
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(GameStateManager)}] Duplicate instance detected. " +
                    "Destroying duplicate."
                );

                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Initialize Game State primitives.
            FlowState = GameFlowState.Booting;
            GameplayMode = GameplayMode.None;

            DontDestroyOnLoad(gameObject);
        }

        // -------------------- PRIVATE PRIMITIVE OPERATIONS --------------------
        // Setters
        /// <summary>
        /// Changes the current high-level application flow state and gameplay mode.
        /// </summary>
        ///  <remarks>
        ///  Gameplay Mode may only be modified while gameplay is active.
        ///  </remarks>
        private void SetFlowState(GameFlowState state)
        {
            if (FlowState == state)
            {
                return;
            }

            GameFlowState previousState = FlowState;
            FlowState = state;

            FlowStateChanged?.Invoke(previousState, FlowState);
        }
        private void SetGameplayMode(GameplayMode mode)
        {
            if (FlowState != GameFlowState.Playing)
            {
                Debug.LogWarning(
                    $"[{nameof(GameStateManager)}] Cannot set Gameplay Mode " +
                    $"to {mode} while Flow State is {FlowState}."
                );

                return;
            }

            if (GameplayMode == mode)
            {
                return;
            }

            GameplayMode previousMode = GameplayMode;
            GameplayMode = mode;

            GameplayModeChanged?.Invoke(previousMode, GameplayMode);
        }

        // Reset Primitives
        // Question should we not have a seperate ResetGameState or such?
        public void ResetGameplayMode()
        {
            if (GameplayMode == GameplayMode.None)
            {
                return;
            }

            GameplayMode previousMode = GameplayMode;
            GameplayMode = GameplayMode.None;

            GameplayModeChanged?.Invoke(previousMode, GameplayMode);
        }

        // -------------------- INTERNAL OPERATIONS --------------------

        // Transition Permission Matrix
        private bool IsTransitionAllowed(
            GameFlowState current,
            GameFlowState target)
        {
            return current switch
            {
                GameFlowState.Booting =>
                    target == GameFlowState.Splash,

                GameFlowState.Splash =>
                    target == GameFlowState.MainMenu,

                GameFlowState.MainMenu =>
                    target == GameFlowState.Loading ||
                    target == GameFlowState.Credits,

                GameFlowState.Loading =>
                    target == GameFlowState.Playing ||
                    target == GameFlowState.MainMenu,

                GameFlowState.Playing =>
                    target == GameFlowState.Paused ||
                    target == GameFlowState.Loading ||
                    target == GameFlowState.GameOver ||
                    target == GameFlowState.MainMenu,

                GameFlowState.Paused =>
                    target == GameFlowState.Playing ||
                    target == GameFlowState.Loading ||
                    target == GameFlowState.MainMenu,

                GameFlowState.GameOver =>
                    target == GameFlowState.MainMenu ||
                    target == GameFlowState.Credits,

                GameFlowState.Credits =>
                    target == GameFlowState.MainMenu,

                _ => false
            };
        }

        private bool TryToEnterState(GameFlowState target, GameScene scene)
        {
            if (!IsTransitionAllowed(FlowState, target))
            {
                LogInvalidTransition(target);
                return  false;
            }

            SetFlowState(target);
            RequestSceneTransition(scene);
            return true;
        }

        private bool TryTransitionTo(GameFlowState target)
        {
            if (!IsTransitionAllowed(FlowState, target))
            {
                Debug.LogWarning(
                    $"[{nameof(GameStateManager)}] Invalid flow transition: " +
                    $"{FlowState} -> {target}."
                );

                return false;
            }

            SetFlowState(target);
            return true;
        }

        private void LogInvalidTransition(GameFlowState targetState)
        {
            Debug.LogWarning(
                $"[{nameof(GameStateManager)}] Invalid Game Flow transition: " +
                $"{FlowState} -> {targetState}."
            );
        }

        /// <summary>
        /// Delegates scene loading to SceneLoadManager.
        /// </summary>
        /// <param name="scene"></param>
        private void RequestSceneTransition(GameScene scene)
        {
            SceneLoadManager.Instance.LoadScene(scene);
        }

        // -------------------- PUBLIC OPERATIONS --------------------
        /// <summary>
        /// Transitions the application to the specified Game Flow State and requests
        /// the corresponding scene when applicable.
        /// </summary>
        /// <param name="currentState">
        /// The current Game Flow State used to validate whether the requested transition is permitted.
        /// </param>


        // -------------------- Scene-Backed State Entry --------------------
        // Enter[GameFlowState] Generic State Change + Scene Load
        // Validates the requested transition, updates the Game Flow State,
        // resets gameplay context when applicable, and requests the corresponding scene.

        public void EnterSplash()
        {
            // Any valid source -> Splash State + Splash Scene
            // Request Splash Load from SceneLoadManager
            ResetGameplayMode();
            TryToEnterState(GameFlowState.Splash, GameScene.Splash);
        }

        public void EnterMainMenu()
        {
            // Any valid source -> Main Menu State + Main Menu Scene
            ResetGameplayMode();
            TryToEnterState(GameFlowState.MainMenu, GameScene.MainMenu);
        }

        public void EnterLoading()
        {
            TryToEnterState(GameFlowState.Loading, GameScene.Loading);
        }

        public void EnterPlaying()
        {
            TryToEnterState(GameFlowState.Playing, GameScene.SOS);
        }

        public void EnterCredits()
        {
            // Any valid source -> Credits State + Credits Scene
            ResetGameplayMode();
            TryToEnterState(GameFlowState.Credits, GameScene.Credits);
        }







        // -------------------- Specialized operations State Entry --------------------
        public void StartNewGame(int slot)
        {
            EnterLoading();

            // Call the SaveManager.StartNewSaveFile or something

            EnterPlaying();
        }

        // Continuing from an existing save file
        public void LoadGame(int slotId)
        {
            EnterLoading();

            // SaveManager loads and restores slot.

            EnterPlaying();
        }


        // -------------------- Presentation Coupled State Entry --------------------
        public void LoadGame()
        {
            // Any valid source -> Loading Process (AKA NO SCENE CHANGE)
            if (!IsTransitionAllowed(FlowState, GameFlowState.Loading))
            {

                return;
            }

            SetFlowState(GameFlowState.Loading);

            // Active Scene stays loaded
            // LoadingPanelController populates, redraws, shows, updates and hides the panel.
        }

        public void PauseGame()
        {
            // Playing -> Paused


            // ENSURE THAT CAN ONLY BE ACTIVATED WHILE PLAYING
            // PRESERVE GAMEPLAY MODE
            // have to decide if the pause menu only exisits inthe SOS scene (perhaps credits has a similar "emu ,or they can jsut be skipps or fast fowarded"
            // Actual visibility must be handed by the pause menu controller
            // so call something like PauseMenuCoontroller.ShowPauseMenu

            if (!IsTransitionAllowed(
                FlowState,
                GameFlowState.Paused))
            {
                LogInvalidTransition(GameFlowState.Paused);
                return;
            }

            SetFlowState(GameFlowState.Paused);

            // PauseMenuController reacts to the state change
            // or is invoked through the presentation system.
        }

        public void ResumeGame()
        {
            if (!IsTransitionAllowed(
                FlowState,
                GameFlowState.Playing))
            {
                LogInvalidTransition(GameFlowState.Playing);
                return;
            }

            SetFlowState(GameFlowState.Playing);

            // SOS scene remains loaded.
            // PauseMenuController hides the panel.
        }

        public void EndGame()
        {
            // Playing -> GameOver
            if (!IsTransitionAllowed(
                FlowState,
                GameFlowState.GameOver))
            {
                LogInvalidTransition(GameFlowState.GameOver);
                return;
            }

            SetFlowState(GameFlowState.GameOver);

            // SOS scene remains loaded.
            // GameOverPanelController shows the panel.
        }


    }
}
