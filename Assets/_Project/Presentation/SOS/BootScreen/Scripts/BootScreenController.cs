using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.BootScreen
{
    public sealed class BootScreenController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        private UIDocument uiDoc;

        // -------------------- CONFIGURATION --------------------
        [Header("Boot Screen")]
        [SerializeField] [Min(0f)]
        private float minimumBootDuration = 2.5f;

        private float currentProgress;

        // -------------------- RUNTIME DEPENDENCIES --------------------

        // private SOSManager sosManager;
        // private SaveManager saveManager;
        // private ProgressionManager progressionManager;

        // -------------------- HELPERS --------------------
        private BootScreenBinder binder;

        // -------------------- STATE --------------------
        private float bootStartedAt;

        // -------------------- LIFECYCLE --------------------
        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[BootScreenController] UIDocument was not found in parent hierarchy.");
                return;
            }

            if (!ResolveDependencies())
            {
                return;
            }

            binder = new BootScreenBinder(uiDoc.rootVisualElement);

            if (!binder.IsValid)
            {
                Debug.LogError("[BootScreenController] UI bindings are invalid.");
                return;
            }

            bootStartedAt = Time.realtimeSinceStartup;

            SetProgress(0f);
            SetDescription("Starting system...");

            StartCoroutine(StartBootScreenFlow());
        }

        // -------------------- DEPENDENCIES --------------------
        private bool ResolveDependencies()
        {
            /*
            sosManager = SOSManager.Instance;
            saveManager = SaveManager.Instance;
            progressionManager = ProgressionManager.Instance;

            if (sosManager == null)
            {
                Debug.LogError(
                    "[BootScreenController] SOSManager instance was not found."
                );

                return false;
            }
            */

            return true;
        }

        // -------------------- BOOT FLOW --------------------
        private IEnumerator StartBootScreenFlow()
        {
            /*
             * Eventually:
             *
             * 1. Check SaveManager
             * 2. Check ProgressionManager
             * 3. Determine unlocked accounts
             * 4. Determine last active user
             * 5. Refresh lock screen
             * 6. Refresh desktop
             * 7. Initialize SOS session
             */

            float stageDuration = minimumBootDuration / 5f;

            // -------------------- START --------------------

            SetDescription("Starting system...");
            SetProgress(0f);

            yield return AnimateProgressTo(
                0.15f,
                stageDuration
            );

            // -------------------- INITIALIZE --------------------

            SetDescription("Initializing system...");

            yield return AnimateProgressTo(
                0.40f,
                stageDuration
            );

            // TODO:
            // Check save / progression data.

            // -------------------- USER CONFIGURATION --------------------

            SetDescription("Loading user configuration...");

            yield return AnimateProgressTo(
                0.65f,
                stageDuration
            );

            // TODO:
            // Determine unlocked accounts / active user.

            // -------------------- ENVIRONMENT --------------------

            SetDescription("Preparing system environment...");

            yield return AnimateProgressTo(
                0.90f,
                stageDuration
            );

            // TODO:
            // Refresh LockScreen / Desktop / Apps.

            // -------------------- DESKTOP --------------------

            SetDescription("Loading desktop...");

            yield return AnimateProgressTo(
                1f,
                stageDuration
            );

            SetDescription("Ready");

            yield return WaitForMinimumBootDuration();

            CompleteBoot();
        }

        // -------------------- PROGRESS --------------------
        public void SetProgress(float progress)
        {
            if (binder == null || !binder.IsValid)
            {
                return;
            }

            progress = Mathf.Clamp01(progress);

            currentProgress = progress;

            binder.FillProgress.style.width =
                new StyleLength(
                    Length.Percent(progress * 100f)
                );
        }

        private IEnumerator AnimateProgressTo(float targetProgress, float duration)
        {
            targetProgress = Mathf.Clamp01(targetProgress);

            float startProgress = currentProgress;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(
                    elapsedTime / duration
                );

                // Smooth acceleration/deceleration.
                t = Mathf.SmoothStep(0f, 1f, t);

                float progress = Mathf.Lerp(
                    startProgress,
                    targetProgress,
                    t
                );

                SetProgress(progress);

                yield return null;
            }

            SetProgress(targetProgress);
        }

        // -------------------- DESCRIPTION --------------------
        public void SetDescription(string description)
        {
            if (binder == null || !binder.IsValid)
            {
                return;
            }

            binder.BootDescription.text = description;
        }

        // -------------------- MINIMUM BOOT TIME --------------------
        private IEnumerator WaitForMinimumBootDuration()
        {
            float elapsedTime = Time.realtimeSinceStartup - bootStartedAt;

            float remainingTime = minimumBootDuration - elapsedTime;

            if (remainingTime > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingTime);
            }
        }

        // -------------------- COMPLETE --------------------
        private void CompleteBoot()
        {
            Debug.Log(
                "[BootScreenController] Boot sequence completed."
            );

            /*
             * TODO:
             *
             * Tell SOSManager that boot is complete.
             *
             * Example:
             *
             * sosManager.CompleteBoot();
             *
             * or:
             *
             * sosManager.ShowLockScreen();
             */
        }
    }
}