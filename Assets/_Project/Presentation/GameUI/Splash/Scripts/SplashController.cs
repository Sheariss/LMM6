using System.Collections;
using UnityEngine;
using Atlas.Core.State;

namespace Atlas.Presentation.Splash
{
    public sealed class SplashController : MonoBehaviour
    {
        [Header("Splash Durations")]
        [SerializeField] [Min(0f)] private float universityDuration = 2f;
        [SerializeField] [Min(0f)] private float teamDuration = 2f;
        [SerializeField] [Min(0f)] private float capstoneDuration = 2.5f;
        [SerializeField] [Min(0f)] private float accessibilityDuration = 3f;


        [Header("Skip Settings")]
        [SerializeField] private bool universitySkippable = true;
        [SerializeField] private bool teamSkippable = true;
        [SerializeField] private bool capstoneSkippable = true;
        [SerializeField] private bool accessibilitySkippable = false;

        private SplashUXML uiDoc;
        private GameStateManager gameStateManager;

        private Coroutine splashSequence;

        private bool skipRequested;
        private bool isRunning;
        private bool isInitialized;

        public void Initialize(SplashUXML splashDoc, GameStateManager stateManager)
        {
            if (splashDoc == null)
            {
                Debug.LogError("[SplashController] SplashUXML cannot be null.");
                return;
            }

            if (stateManager == null)
            {
                Debug.LogError("[SplashController] GameStateManager cannot be null.");
                return;
            }

            uiDoc = splashDoc;
            gameStateManager = stateManager;

            isInitialized = true;
        }

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

        public void RequestSkip()
        {
            if (!isRunning)
            {
                return;
            }

            skipRequested = true;
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

            view.Show(splashType);

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                if (isSkippable && skipRequested)
                {
                    break;
                }

                elapsedTime += Time.unscaledDeltaTime;

                yield return null;
            }

            skipRequested = false;
        }

        private void CompleteSplashSequence()
        {
            isRunning = false;
            splashSequence = null;
            skipRequested = false;

            gameStateManager.EnterMainMenu();
        }

        private void OnDisable()
        {
            StopSplashSequence();
        }
    }
}