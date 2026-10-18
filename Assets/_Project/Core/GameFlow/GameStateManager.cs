using System;
using Atlas.Core.SceneManagement;
using Atlas.Development;
using UnityEngine;

namespace Atlas.Core.GameState
{
    public class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }

        public GameFlowState FlowState { get; private set; }

        public GameplayMode GameplayMode { get; private set; }

        public event Action<GameFlowState, GameFlowState>
            FlowStateChanged;

        public event Action<GameplayMode, GameplayMode>
            GameplayModeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(GameStateManager)}] Duplicate instance " +
                    "detected. Destroying duplicate."
                );

                Destroy(gameObject);
                return;
            }

            Instance = this;

            FlowState = GameFlowState.Booting;
            GameplayMode = GameplayMode.None;

            DontDestroyOnLoad(gameObject);
        }

        private void SetFlowState(GameFlowState state)
        {
            if (FlowState == state)
                return;

            GameFlowState previousState = FlowState;
            FlowState = state;

            FlowStateChanged?.Invoke(previousState, FlowState);
        }

        private void SetGameplayMode(GameplayMode mode)
        {
            if (FlowState != GameFlowState.Playing)
            {
                Debug.LogWarning(
                    $"[{nameof(GameStateManager)}] Cannot set Gameplay " +
                    $"Mode to {mode} while Flow State is {FlowState}."
                );

                return;
            }

            if (GameplayMode == mode)
                return;

            GameplayMode previousMode = GameplayMode;
            GameplayMode = mode;

            GameplayModeChanged?.Invoke(previousMode, GameplayMode);
        }

        public void ResetGameplayMode()
        {
            if (GameplayMode == GameplayMode.None)
                return;

            GameplayMode previousMode = GameplayMode;
            GameplayMode = GameplayMode.None;

            GameplayModeChanged?.Invoke(previousMode, GameplayMode);
        }

        private bool IsTransitionAllowed(
            GameFlowState current,
            GameFlowState target)
        {
            return current switch
            {
                GameFlowState.Booting =>
                    target == GameFlowState.Splash ||
                    target == GameFlowState.Credits,

                GameFlowState.Splash =>
                    target == GameFlowState.MainMenu,

                GameFlowState.MainMenu =>
                    target == GameFlowState.Loading ||
                    target == GameFlowState.Credits,

                GameFlowState.Loading =>
                    target == GameFlowState.Playing ||
                    target == GameFlowState.MainMenu,

                GameFlowState.Playing =>
                    target == GameFlowState.Playing ||
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

        private bool TryToEnterState(
            GameFlowState target,
            GameScene scene,
            bool resetGameplayMode = false)
        {
            if (!IsTransitionAllowed(FlowState, target))
            {
                LogInvalidTransition(target);
                return false;
            }

            if (resetGameplayMode)
                ResetGameplayMode();

            SetFlowState(target);
            RequestSceneTransition(scene);

            return true;
        }

        private bool TryTransitionTo(GameFlowState target)
        {
            if (!IsTransitionAllowed(FlowState, target))
            {
                LogInvalidTransition(target);
                return false;
            }

            SetFlowState(target);
            return true;
        }

        private void LogInvalidTransition(GameFlowState targetState)
        {
            Debug.LogWarning(
                $"[{nameof(GameStateManager)}] Invalid Game Flow " +
                $"transition: {FlowState} -> {targetState}."
            );
        }

        private void RequestSceneTransition(GameScene scene)
        {
            SceneLoadManager.Instance.LoadScene(scene);
        }

        public bool TryEnterDevelopmentStartPoint(
            DevelopmentStartPoint startPoint)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!DevelopmentContext.IsEnabled ||
                DevelopmentContext.StartPoint != startPoint ||
                FlowState != GameFlowState.Booting)
            {
                return false;
            }

            GameFlowState targetState;
            GameScene targetScene;

            switch (startPoint)
            {
                case DevelopmentStartPoint.MainMenu:
                    targetState = GameFlowState.MainMenu;
                    targetScene = GameScene.MainMenu;
                    break;

                case DevelopmentStartPoint.NewGame:
                    targetState = GameFlowState.Loading;
                    targetScene = GameScene.Loading;
                    break;

                case DevelopmentStartPoint.SOS:
                case DevelopmentStartPoint.GuestDesktop:
                    targetState = GameFlowState.Playing;
                    targetScene = GameScene.SOS;
                    break;

                case DevelopmentStartPoint.Credits:
                    targetState = GameFlowState.Credits;
                    targetScene = GameScene.Credits;
                    break;

                default:
                    return false;
            }

            if (SceneLoadManager.Instance == null)
            {
                Debug.LogError(
                    "[GameStateManager] Cannot start development flow: " +
                    "SceneLoadManager was not found."
                );

                return false;
            }

            ResetGameplayMode();
            SetFlowState(targetState);
            RequestSceneTransition(targetScene);

            return true;
#else
            return false;
#endif
        }

        public void EnterSplash()
        {
            TryToEnterState(
                GameFlowState.Splash,
                GameScene.Splash,
                true
            );
        }

        public void EnterMainMenu()
        {
            TryToEnterState(
                GameFlowState.MainMenu,
                GameScene.MainMenu,
                true
            );
        }

        public void EnterLoading()
        {
            TryToEnterState(
                GameFlowState.Loading,
                GameScene.Loading
            );
        }

        public void EnterPlaying()
        {
            TryToEnterState(
                GameFlowState.Playing,
                GameScene.SOS
            );
        }

        public void EnterCredits()
        {
            TryToEnterState(
                GameFlowState.Credits,
                GameScene.Credits,
                true
            );
        }

        public void StartNewGame(int slot)
        {
            EnterLoading();
        }

        public void LoadGame(int slotId)
        {
            EnterLoading();

            // TODO: SaveManager loads and restores slot.
            EnterPlaying();
        }

        public void LoadGame()
        {
            TryTransitionTo(GameFlowState.Loading);

            // TODO: LoadingPanelController handles the loading panel
            // while the active scene remains loaded.
        }

        public void PauseGame()
        {
            TryTransitionTo(GameFlowState.Paused);
        }

        public void ResumeGame()
        {
            TryTransitionTo(GameFlowState.Playing);
        }

        public void EndGame()
        {
            TryTransitionTo(GameFlowState.GameOver);
            ResetGameplayMode();
        }
    }
}