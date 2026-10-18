using System.Collections;
using Atlas.Core.GameState;
using Atlas.Core.Persistence;
using Atlas.Core.SceneManagement;
using Atlas.Development;
using Atlas.Presentation.SOS.Desktop;
using Atlas.SOS.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Atlas.Core
{
    public sealed class BootstrapManager : MonoBehaviour
    {
        [Header("Development")]
        [SerializeField]
        private DevelopmentSettings developmentSettings;

        [Header("Core Runtime Managers")]
        [SerializeField]
        private GameStateManager gameStateManager;

        [SerializeField]
        private SceneLoadManager sceneLoadManager;

        [SerializeField]
        private SaveManager saveManager;

        public static BootstrapManager Instance { get; private set; }

        public bool IsInitialized { get; private set; }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool waitingForGuestScene;
#endif

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
                return;

            if (!VerifyCoreRuntime())
            {
                Debug.LogError(
                    $"[{nameof(BootstrapManager)}] Core Runtime validation " +
                    "failed. Application flow will not begin."
                );

                return;
            }

            // Resolve the verified runtime instances.
            gameStateManager = GameStateManager.Instance;
            sceneLoadManager = SceneLoadManager.Instance;
            saveManager = SaveManager.Instance;

            DevelopmentContext.Initialize(developmentSettings);

            IsInitialized = true;

            Debug.Log(
                $"[{nameof(BootstrapManager)}] " +
                "Core Runtime initialized successfully."
            );

            StartGameFlow();
        }

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

            return isValid;
        }

        private bool VerifyRequiredManager(
            Object manager,
            string managerName)
        {
            if (manager != null)
                return true;

            Debug.LogError(
                $"[{nameof(BootstrapManager)}] Required Core Runtime " +
                $"manager '{managerName}' was not found."
            );

            return false;
        }

        private void StartGameFlow()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DevelopmentContext.IsEnabled &&
                DevelopmentContext.StartPoint !=
                    DevelopmentStartPoint.Normal)
            {
                bool startAtGuestDesktop =
                    DevelopmentContext.StartPoint ==
                    DevelopmentStartPoint.GuestDesktop;

                if (startAtGuestDesktop)
                {
                    waitingForGuestScene = true;

                    // Unity invokes sceneLoaded after OnEnable and before Start.
                    SceneManager.sceneLoaded += OnDevelopmentSceneLoaded;
                    sceneLoadManager.SceneLoadFailed +=
                        OnDevelopmentSceneLoadFailed;
                }

                if (gameStateManager.TryEnterDevelopmentStartPoint(
                        DevelopmentContext.StartPoint))
                {
                    return;
                }

                StopWaitingForGuestScene();
            }
#endif

            gameStateManager.EnterSplash();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnDevelopmentSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            if (!waitingForGuestScene ||
                scene.name != SceneDefinitions.GetSceneName(GameScene.SOS))
            {
                return;
            }

            StopWaitingForGuestScene();

            DesktopManager desktop =
                FindSceneComponent<DesktopManager>(scene);

            AuthenticationManager authentication =
                AuthenticationManager.Instance;

            if (desktop == null || !desktop.isActiveAndEnabled)
            {
                Debug.LogError(
                    "[BootstrapManager] Guest startup failed: " +
                    "an active DesktopManager was not found in SOS.",
                    this
                );

                return;
            }

            if (authentication == null)
            {
                Debug.LogError(
                    "[BootstrapManager] Guest startup failed: " +
                    "AuthenticationManager was not found.",
                    this
                );

                return;
            }

            AuthenticationResult result =
                authentication.TrySignIn("guest", string.Empty);

            if (result != AuthenticationResult.Success)
            {
                Debug.LogError(
                    "[BootstrapManager] Guest startup failed: " +
                    $"guest sign-in returned {result}.",
                    this
                );

                return;
            }

            HideStartupScreens(scene);

            StartCoroutine(
                PrepareGuestDesktop(desktop, authentication)
            );
        }

        private IEnumerator PrepareGuestDesktop(
            DesktopManager desktop,
            AuthenticationManager authentication)
        {
            // Let LoginScreenController and the other scene components
            // initialize and subscribe to the desktop events.
            yield return null;

            if (desktop == null ||
                !desktop.isActiveAndEnabled ||
                authentication == null ||
                !authentication.IsSignedIn ||
                authentication.CurrentUser.UserId != "guest")
            {
                Debug.LogError(
                    "[BootstrapManager] Guest desktop startup was interrupted.",
                    this
                );

                yield break;
            }

            // The existing LoginScreenController receives DesktopReady,
            // reveals the desktop, and keeps the login screen hidden.
            desktop.LoadDesktop(
                authentication.CurrentUser,
                skipWelcome: true
            );
        }

        private static T FindSceneComponent<T>(Scene scene)
            where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>();

                if (component != null)
                    return component;
            }

            return null;
        }

        private static void HideStartupScreens(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                UIDocument[] documents =
                    root.GetComponentsInChildren<UIDocument>(true);

                foreach (UIDocument document in documents)
                {
                    VisualElement documentRoot =
                        document.rootVisualElement;

                    if (documentRoot == null)
                        continue;

                    HideElement(documentRoot, "LockScreenRoot");
                    HideElement(documentRoot, "LoginScreenRoot");
                    HideElement(documentRoot, "BootScreenRoot");
                }
            }
        }

        private static void HideElement(
            VisualElement documentRoot,
            string elementName)
        {
            VisualElement element =
                documentRoot.Q<VisualElement>(elementName);

            if (element != null)
                element.style.display = DisplayStyle.None;
        }

        private void OnDevelopmentSceneLoadFailed(
            GameScene scene,
            string message)
        {
            if (!waitingForGuestScene || scene != GameScene.SOS)
                return;

            StopWaitingForGuestScene();

            Debug.LogError(
                $"[BootstrapManager] Guest scene loading failed: {message}",
                this
            );
        }

        private void StopWaitingForGuestScene()
        {
            waitingForGuestScene = false;

            SceneManager.sceneLoaded -= OnDevelopmentSceneLoaded;

            if (sceneLoadManager != null)
            {
                sceneLoadManager.SceneLoadFailed -=
                    OnDevelopmentSceneLoadFailed;
            }
        }
#endif

        private void OnDestroy()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            StopWaitingForGuestScene();
#endif

            if (Instance == this)
                Instance = null;
        }
    }
}