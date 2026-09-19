using Atlas.Core.SceneManagement;
using System;
using UnityEngine;

namespace Atlas.Core.GameState
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        // Internal State primitives
        public GameFlowState FlowState { get; private set; }
        public GameplayMode GameplayMode { get; private set; }

        // Events
        public event Action<GameFlowState, GameFlowState> FlowStateChanged;
        public event Action<GameplayMode, GameplayMode> GameplayModeChanged;

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
        private bool IsTransitionAllowed( GameFlowState current, GameFlowState target)
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

        private bool TryToEnterState(GameFlowState target, GameScene scene, bool resetGameplayMode = false)
        {
            if (!IsTransitionAllowed(FlowState, target))
            {
                LogInvalidTransition(target);
                return  false;
            }

            if (resetGameplayMode)
            {
                ResetGameplayMode();
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

        private void RequestSceneTransition(GameScene scene)
        {
            SceneLoadManager.Instance.LoadScene(scene);
        }

        // -------------------- PUBLIC OPERATIONS --------------------
        // -------------------- Scene-Backed State Entry --------------------
        public void EnterSplash()
        {
            TryToEnterState(GameFlowState.Splash, GameScene.Splash, true);
        }

        public void EnterMainMenu()
        {
            TryToEnterState(GameFlowState.MainMenu, GameScene.MainMenu, true);
        }

        public void EnterLoading()
        {
            TryToEnterState(GameFlowState.Loading, GameScene.Loading, false);
        }

        public void EnterPlaying()
        {
            TryToEnterState(GameFlowState.Playing, GameScene.SOS, false);
        }

        public void EnterCredits()
        {
            TryToEnterState(GameFlowState.Credits, GameScene.Credits, true);
        }

        // -------------------- Specialized operations State Entry --------------------
        public void StartNewGame(int slot)
        {
            EnterLoading();
            // TODO: Call the SaveManager.StartNewSaveFile or something
            EnterPlaying();
        }

        public void LoadGame(int slotId)
        {
            EnterLoading();
            // TODO: SaveManager loads and restores slot.
            EnterPlaying();
        }

        // -------------------- Presentation Coupled State Entry --------------------
        public void LoadGame()
        {
            TryTransitionTo(GameFlowState.Loading);
            // TO DO: Active Scene stays loaded > LoadingPanelController populates, redraws, shows, updates and hides the panel.
        }

        public void PauseGame()
        {
            TryTransitionTo(GameFlowState.Paused);
            // TO DO: PauseMenuController reacts to the state change or is invoked through the presentation system.
        }

        public void ResumeGame()
        {
            TryTransitionTo(GameFlowState.Playing);
            // TODO: SOS scene remains loaded. > PauseMenuController hides the panel.
        }

        public void EndGame()
        {
            TryTransitionTo(GameFlowState.GameOver);
            ResetGameplayMode();
            // TODO: SOS scene remains loaded > GameOverPanelController shows the panel.
        }
    }
}