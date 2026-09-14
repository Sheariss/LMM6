using UnityEngine;

namespace Atlas.Core
{

    /// <summary>
    /// Persistent entry point for the IIS Core Runtime.
    /// </summary>
    /// <remarks>
    /// Responsible for coordinating validation of the configured
    /// Core Runtime before normal application flow begins.
    /// </remarks>
    public sealed class BootstrapManager : MonoBehaviour
    {
        public static BootstrapManager Instance { get; private set; }

        /// <summary>
        /// Indicates whether Bootstrap initialization has completed successfully.
        /// </summary>
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

        /// <summary>
        /// Initializes the Bootstrap Manager and it verifies the Core Runtime.
        /// </summary>
        /// <remarks>
        /// Verification of Core Runtime Managers is called in start on purpose.
        /// This allows the invidiaul managers to get isntantiated on Awake BEFORE the bootstrap verification.
        /// </remarks>
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
        }

        /// <summary>
        /// Verifies that all required Core Runtime managers are available.
        /// </summary>
        private bool VerifyCoreRuntime()
        {

            bool isValid = true;
            // TO DO: Required manager checks will be added as
            // each Core Runtime manager is implemented.
            // Reporting verification errors must also fall to each manager check
            // isValid &= VerifyRequiredManager(
            // GameStateManager.Instance,
            // nameof(GameStateManager));

            return isValid;
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
