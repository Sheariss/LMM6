using Atlas.Core.GameState;
using Atlas.Core.Persistence;
using Atlas.Core.SceneManagement;
using Atlas.Development;
using UnityEngine;
using UnityEngine.AdaptivePerformance;

namespace Atlas.Core
{
    public sealed class BootstrapManager : MonoBehaviour
    {
        [Header("Development")]
        [SerializeField]
        private DevelopmentSettings developmentSettings;

        // -------------------- CORE RUNTIME MANAGERS --------------------
        [SerializeField] private GameStateManager gameStateManager;
        [SerializeField] private SceneLoadManager sceneLoadManager;
        [SerializeField] private SaveManager saveManager;
        // TODO: Add rest of the managers
        //
        // [SerializeField] private AccessibilityManager accessibilityManager;
        // [SerializeField] private AudioManager audioManager;
        // [SerializeField] private InputManager inputManager;
        // [SerializeField] private NavigationManager navigationManager;
        // [SerializeField] private ProgressionManager progressionManager;
        // [SerializeField] private SettingsManager settingsManager;
        // [SerializeField] private ShortcutManager shortcutManager;
        // [SerializeField] private ThemeManager themeManager;


        public static BootstrapManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[{nameof(BootstrapManager)}] Duplicate instance detected. " +
                    "Destroying duplicate."
                );

                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (IsInitialized)
            {
                return;
            }

            if (!VerifyCoreRuntime())
            {
                Debug.LogError(
                    $"[{nameof(BootstrapManager)}] Core Runtime validation failed. " +
                    "Application flow will not begin."
                );

                return;
            }

            IsInitialized = true;

            Debug.Log(
                $"[{nameof(BootstrapManager)}] Core Runtime initialized successfully."
            );

            DevelopmentContext.Initialize(
                developmentSettings
            );

            StartGameFlow();
        }

        // -------------------- RUNTIME VERIFICATION --------------------
        private bool VerifyCoreRuntime()
        {

            bool isValid = true;

            isValid &= VerifyRequiredManager(
                GameStateManager.Instance,
                nameof(GameStateManager)
            );

            isValid &= VerifyRequiredManager(
                SaveManager.Instance,
                nameof(SaveManager)
            );

            isValid &= VerifyRequiredManager(
                SceneLoadManager.Instance,
                nameof(SceneLoadManager)
            );

            // TODO: Add verification for the rest of the managers

            return isValid;
        }

        private bool VerifyRequiredManager(
            Object manager,
            string managerName
        )
        {
            if (manager != null)
            {
                return true;
            }

            Debug.LogError(
                $"[{nameof(BootstrapManager)}] Required Core Runtime manager " +
                $"'{managerName}' was not found."
            );

            return false;
        }

        private void StartGameFlow()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

            if (DevelopmentContext.IsEnabled)
            {
                switch (DevelopmentContext.StartPoint)
                {
                    case DevelopmentStartPoint.MainMenu:
                        gameStateManager.EnterMainMenu();
                        return;

                    case DevelopmentStartPoint.NewGame:
                        gameStateManager.StartNewGame(1);
                        return;

                    case DevelopmentStartPoint.SOS:
                        //StartDevelopmentSOS();
                        return;
                }
            }

#endif

            gameStateManager.EnterSplash();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
