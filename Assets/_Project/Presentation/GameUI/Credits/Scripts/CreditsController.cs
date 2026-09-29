using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Credits
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class CreditsController : MonoBehaviour
    {
        [Header("Scrolling")]
        [SerializeField] private float scrollSpeed = 35f;
        [SerializeField] private float fastForwardMultiplier = 4f;
        [SerializeField] private float startDelay = 1.5f;
        [SerializeField] private float endDelay = 2.5f;

        private CreditsView view;
        private CreditsViewBuilder builder;

        private float startTimer;
        private float endTimer;
        private bool isFastForwarding;
        private bool hasReachedEnd;
        private bool isExiting;

        private void Awake()
        {
            UIDocument document = GetComponent<UIDocument>();

            view = new CreditsView(document.rootVisualElement);
            builder = new CreditsViewBuilder(view);
        }

        private void OnEnable()
        {
            view.RegisterCallbacks(
                OnFastForwardStarted,
                OnFastForwardEnded,
                OnSkipPressed
            );
        }

        private void Start()
        {
            builder.Build();

            startTimer = startDelay;
            endTimer = 0f;
            hasReachedEnd = false;
            isExiting = false;

            view.SetFastForwarding(false);
            view.ScrollToTop();
        }

        private void Update()
        {
            if (isExiting)
                return;

            if (startTimer > 0f)
            {
                startTimer -= Time.unscaledDeltaTime;
                return;
            }

            if (hasReachedEnd)
            {
                endTimer += Time.unscaledDeltaTime;

                if (endTimer >= endDelay)
                    ReturnToMainMenu();

                return;
            }

            float speed = isFastForwarding
                ? scrollSpeed * fastForwardMultiplier
                : scrollSpeed;

            view.Scroll(speed * Time.unscaledDeltaTime);

            if (view.HasReachedEnd())
            {
                hasReachedEnd = true;
                endTimer = 0f;
                view.HideControls();
            }
        }

        private void OnDisable()
        {
            view.UnregisterCallbacks(
                OnFastForwardStarted,
                OnFastForwardEnded,
                OnSkipPressed
            );
        }

        private void OnFastForwardStarted()
        {
            if (hasReachedEnd || isExiting)
                return;

            isFastForwarding = true;
            view.SetFastForwarding(true);
        }

        private void OnFastForwardEnded()
        {
            isFastForwarding = false;
            view.SetFastForwarding(false);
        }

        private void OnSkipPressed()
        {
            if (isExiting)
                return;

            ReturnToMainMenu();
        }

        private void ReturnToMainMenu()
        {
            if (isExiting)
                return;

            isExiting = true;

            GameStateManager.Instance.EnterMainMenu();
        }
    }
}