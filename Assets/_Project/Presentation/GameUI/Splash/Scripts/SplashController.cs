using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Core.GameState;

namespace Atlas.Presentation.GameUI.Splash
{
    public sealed class SplashController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        [SerializeField] private UIDocument uiDoc;

        // -------------------- RUNTIME DEPENDENCIES --------------------
        [SerializeField]private GameStateManager gameStateManager;

        // -------------------- SPLASH DURATIONS --------------------
        [Header("Splash Durations")]
        [SerializeField] [Min(0f)] private float universityDuration = 2f;
        [SerializeField] [Min(0f)] private float teamDuration = 2f;
        [SerializeField] [Min(0f)] private float capstoneDuration = 2.5f;
        [SerializeField] [Min(0f)] private float accessibilityDuration = 3f;

        // -------------------- SKIP SETTINGS --------------------
        [Header("Skip Settings")]
        [SerializeField] private bool universitySkippable = true;
        [SerializeField] private bool teamSkippable = true;
        [SerializeField] private bool capstoneSkippable = true;
        [SerializeField] private bool accessibilitySkippable = false;

        // -------------------- HELPERS --------------------
        private SplashBinder binder;
        private SplashView view;

        private Coroutine splashSequence;

        // -------------------- STATE --------------------
        private bool skipRequested;
        private bool isRunning;

        // -------------------- LIFECYCLE --------------------
        private void Awake()
        {
            binder = new SplashBinder(uiDoc.rootVisualElement);
            view = new SplashView(binder);

            view.HideAll();
        }

        private void OnEnable()
        {
            StartSplashSequence();
        }

        private void OnDisable()
        {
            StopSplashSequence();
        }

        // -------------------- SPLASH SEQUENCE --------------------
        public void StartSplashSequence()
        {
            if (!isInitialized)
            {
                Debug.LogError(
                    "[SplashController] Cannot start splash sequence " +
                    "before initialization."
                );

                return;
            }

            if (isRunning)
            {
                Debug.LogWarning(
                    "[SplashController] Splash sequence is already running."
                );

                return;
            }

            splashSequence = StartCoroutine(
                RunSplashSequence()
            );
        }

        public void StopSplashSequence()
        {
            if (splashSequence == null)
            {
                return;
            }

            StopCoroutine(splashSequence);

            splashSequence = null;
            skipRequested = false;
            isRunning = false;
        }

        // -------------------- SKIP --------------------
        public void RequestSkip()
        {
            if (!isRunning)
            {
                return;
            }

            skipRequested = true;
        }

        // -------------------- FLOW --------------------
        private IEnumerator RunSplashSequence()
        {
            isRunning = true;

            yield return ShowSplash(
                SplashType.University,
                universityDuration,
                universitySkippable
            );

            yield return ShowSplash(
                SplashType.Team,
                teamDuration,
                teamSkippable
            );

            yield return ShowSplash(
                SplashType.Capstone,
                capstoneDuration,
                capstoneSkippable
            );

            yield return ShowSplash(
                SplashType.Accessibility,
                accessibilityDuration,
                accessibilitySkippable
            );

            CompleteSplashSequence();
        }

        private IEnumerator ShowSplash(SplashType splashType, float duration, bool isSkippable)
        {
            skipRequested = false;

            view.Show(
                splashType
            );

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                if (
                    isSkippable &&
                    skipRequested
                )
                {
                    break;
                }

                elapsedTime +=
                    Time.unscaledDeltaTime;

                yield return null;
            }

            skipRequested = false;
        }

        private void CompleteSplashSequence()
        {
            view.HideAll();

            splashSequence = null;
            skipRequested = false;
            isRunning = false;

            gameStateManager.EnterMainMenu();
        }
    }
}