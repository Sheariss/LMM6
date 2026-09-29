using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Core.GameState;

namespace Atlas.Presentation.Credits
{
    public sealed class CreditsController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        private UIDocument uiDoc;

        // -------------------- RUNTIME DEPENDENCIES --------------------
        private GameStateManager gameStateManager;

        // -------------------- AUTHORED DATA --------------------
        [SerializeField]
        private CreditsLibrary creditsLibrary;

        // -------------------- SCROLLING --------------------
        [Header("Scrolling")]
        [SerializeField] private float scrollSpeed = 35f;
        [SerializeField] private float fastForwardMultiplier = 4f;
        [SerializeField] private float startDelay = 1.5f;
        [SerializeField] private float endDelay = 2.5f;

        // -------------------- HELPERS --------------------
        private CreditsBinder binder;
        private CreditsView view;
        private CreditsViewBuilder builder;

        // -------------------- STATE --------------------
        private float startTimer;
        private float endTimer;
        private bool isFastForwarding;
        private bool hasReachedEnd;
        private bool isExiting;
        private bool isInitialized;

        // -------------------- LIFECYCLE --------------------
        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[CreditsController] UIDocument was not found in parent hierarchy.");
                return;
            }

            if (!ResolveDependencies())
            {
                return;
            }

            binder = new CreditsBinder(uiDoc.rootVisualElement);

            view = new CreditsView(binder);

            builder = new CreditsViewBuilder(view, creditsLibrary);

            builder.Build();
            BindActions();
            InitializeView();

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized || isExiting)
            {
                return;
            }

            if (startTimer > 0f)
            {
                startTimer -= Time.unscaledDeltaTime;
                return;
            }

            if (hasReachedEnd)
            {
                endTimer += Time.unscaledDeltaTime;

                if (endTimer >= endDelay)
                {
                    ReturnToMainMenu();
                }

                return;
            }

            float speed = isFastForwarding
                ? scrollSpeed * fastForwardMultiplier
                : scrollSpeed;

            view.Scroll(speed * Time.unscaledDeltaTime);

            if (!view.HasReachedEnd())
            {
                return;
            }

            OnCreditsReachedEnd();
        }

        private void OnDestroy()
        {
            if (view == null)
            {
                return;
            }

            view.UnregisterCallbacks();
        }

        // -------------------- DEPENDENCIES --------------------
        private bool ResolveDependencies()
        {
            gameStateManager = GameStateManager.Instance;

            bool valid = true;

            if (gameStateManager == null)
            {
                Debug.LogError("[CreditsController] GameStateManager instance was not found.");
                valid = false;
            }

            if (creditsLibrary == null)
            {
                Debug.LogError("[CreditsController] CreditsLibrary reference was not assigned.");
                valid = false;
            }

            return valid;
        }

        // -------------------- ACTION BINDING --------------------
        private void BindActions()
        {
            view.RegisterCallbacks(
                OnFastForwardStarted,
                OnFastForwardEnded,
                OnSkipPressed
            );
        }

        // -------------------- VIEW --------------------

        private void InitializeView()
        {
            startTimer = startDelay;
            endTimer = 0f;
            isFastForwarding = false;
            hasReachedEnd = false;
            isExiting = false;

            view.SetFastForwarding(
                false,
                fastForwardMultiplier
            );

            view.ShowControls();
            view.ScrollToTop();
            view.Focus();
        }

        // -------------------- ACTIONS --------------------

        private void OnFastForwardStarted()
        {
            if (hasReachedEnd || isExiting || isFastForwarding)
            {
                return;
            }

            isFastForwarding = true;

            view.SetFastForwarding(true, fastForwardMultiplier);
        }

        private void OnFastForwardEnded()
        {
            if (!isFastForwarding)
            {
                return;
            }

            isFastForwarding = false;

            view.SetFastForwarding(false, fastForwardMultiplier);
        }

        private void OnSkipPressed()
        {
            if (isExiting)
            {
                return;
            }

            ReturnToMainMenu();
        }

        private void OnCreditsReachedEnd()
        {
            hasReachedEnd = true;
            isFastForwarding = false;
            endTimer = 0f;

            view.SetFastForwarding(false, fastForwardMultiplier);

            view.HideControls();
        }

        // -------------------- NAVIGATION --------------------

        private void ReturnToMainMenu()
        {
            if (isExiting)
            {
                return;
            }

            isExiting = true;
            gameStateManager.EnterMainMenu();
        }
    }
}