using System;
using System.Collections;
using Atlas.SOS.Authentication;
using UnityEngine;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.Desktop
{
    public enum DesktopPreparationState
    {
        Idle,
        Preparing,
        Ready,
        Failed
    }

    public sealed class DesktopManager : MonoBehaviour
    {
        [Header("Desktop")]
        [SerializeField] private DesktopController desktopController;

        [Header("Welcome Timing")]
        [SerializeField, Min(0f)]
        private float minimumWelcomeDuration = 2.5f;

        private DesktopController sessionController;
        private Coroutine preparationRoutine;

        public DesktopPreparationState State { get; private set; }
        public UserInfo ActiveUser { get; private set; }
        public string FailureMessage { get; private set; }

        public event Action DesktopReady;
        public event Action<string> DesktopFailed;

        private void OnEnable()
        {
            ClearDesktop();
        }

        private void OnDisable()
        {
            if (State == DesktopPreparationState.Preparing)
            {
                Fail("Desktop preparation was interrupted. Please retry.");
                return;
            }

            ClearDesktop();
        }

        private void LateUpdate()
        {
            if (State != DesktopPreparationState.Preparing &&
                State != DesktopPreparationState.Ready)
            {
                return;
            }

            if (!IsAuthenticatedUser(ActiveUser))
            {
                // Sign-out or account changes invalidate the old desktop.
                ClearDesktop();
                return;
            }

            if (!ValidateSession(out string error))
            {
                Fail(error);
                return;
            }

            if (State == DesktopPreparationState.Ready &&
                !IsControllerPrepared())
            {
                Fail("The desktop UI changed. Please retry.");
            }
        }

        public void LoadDesktop(UserInfo user)
        {
            Debug.Log(
                $"[DesktopManager] Load requested for '{user?.UserId}'.",
                this);

            if (!isActiveAndEnabled)
            {
                Fail("The desktop manager is not active.");
                return;
            }

            if (!IsAuthenticatedUser(user))
            {
                Fail("A currently signed-in account is required.");
                return;
            }

            // Repeated requests do not rebuild a valid session.
            if (ReferenceEquals(ActiveUser, user) &&
                ValidateSession(out _) &&
                (State == DesktopPreparationState.Preparing ||
                 (State == DesktopPreparationState.Ready &&
                  IsControllerPrepared())))
            {
                return;
            }

            ClearDesktop();

            ActiveUser = user;
            sessionController = desktopController;

            if (!ValidateSession(out string error))
            {
                Fail(error);
                return;
            }

            State = DesktopPreparationState.Preparing;
            preparationRoutine = StartCoroutine(PrepareDesktop());
        }

        private IEnumerator PrepareDesktop()
        {
            float preparationStartedAt = Time.realtimeSinceStartup;

            Debug.Log(
                $"[DesktopManager] Preparation started for " +
                $"'{ActiveUser?.UserId}'.",
                this);

            // Allow the login screen to present Welcome first.
            yield return null;
            yield return null;

            if (!ValidateSession(out string error))
            {
                preparationRoutine = null;
                Fail(error);
                yield break;
            }

            // Build the desktop while Welcome remains visible.
            if (!TryPrepareController(out error))
            {
                preparationRoutine = null;
                Fail(error);
                yield break;
            }

            // Allow UI Toolkit to process the populated desktop.
            yield return null;

            // Preparation time counts toward the minimum Welcome duration.
            float remainingTime = minimumWelcomeDuration -
                (Time.realtimeSinceStartup - preparationStartedAt);

            if (remainingTime > 0f)
            {
                Debug.Log(
                    $"[DesktopManager] Waiting {remainingTime:F2} seconds " +
                    "before reporting readiness.",
                    this);

                yield return new WaitForSecondsRealtime(remainingTime);
            }

            // Keep the coroutine cancellable until the wait finishes.
            preparationRoutine = null;

            // Recheck the user and controller after waiting.
            if (!ValidateSession(out error))
            {
                Fail(error);
                yield break;
            }

            if (!IsControllerPrepared())
            {
                Fail("The desktop UI changed during preparation.");
                yield break;
            }

            State = DesktopPreparationState.Ready;
            FailureMessage = null;

            Debug.Log(
                $"[DesktopManager] Desktop ready for '{ActiveUser.UserId}'.",
                this);

            // The login controller decides when to reveal the desktop.
            DesktopReady?.Invoke();
        }

        public bool ShowDesktop()
        {
            if (State != DesktopPreparationState.Ready)
            {
                Debug.LogWarning(
                    $"[DesktopManager] Cannot show desktop while " +
                    $"state is {State}.",
                    this);

                return false;
            }

            if (!ValidateSession(out string error))
            {
                Fail(error);
                return false;
            }

            if (!sessionController.Show(ActiveUser, out error))
            {
                Fail(error);
                return false;
            }

            Debug.Log(
                $"[DesktopManager] Desktop shown for '{ActiveUser.UserId}'.",
                this);

            return true;
        }

        public void ClearDesktop()
        {
            CancelPreparation();

            sessionController?.Clear();

            // Clear a replacement controller if the reference changed.
            if (desktopController != sessionController)
                desktopController?.Clear();

            sessionController = null;
            ActiveUser = null;
            FailureMessage = null;
            State = DesktopPreparationState.Idle;
        }

        private bool ValidateSession(out string error)
        {
            if (!IsAuthenticatedUser(ActiveUser))
            {
                error = "The authenticated account changed or signed out.";
                return false;
            }

            if (sessionController == null ||
                !sessionController.isActiveAndEnabled)
            {
                error = "The desktop controller is missing or disabled.";
                return false;
            }

            if (sessionController != desktopController)
            {
                error = "The desktop controller changed. Please retry.";
                return false;
            }

            error = null;
            return true;
        }

        private bool IsControllerPrepared()
        {
            return sessionController != null &&
                   ReferenceEquals(
                       sessionController.ActiveUser,
                       ActiveUser) &&
                   sessionController.IsPreparedForCurrentUser;
        }

        private bool TryPrepareController(out string error)
        {
            try
            {
                return sessionController.Prepare(ActiveUser, out error);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);

                error = "The desktop could not be prepared. Please retry.";
                return false;
            }
        }

        private static bool IsAuthenticatedUser(UserInfo user)
        {
            AuthenticationManager authentication =
                AuthenticationManager.Instance;

            return user != null &&
                   authentication != null &&
                   authentication.IsSignedIn &&
                   ReferenceEquals(authentication.CurrentUser, user);
        }

        private void Fail(string message)
        {
            ClearDesktop();

            FailureMessage = string.IsNullOrWhiteSpace(message)
                ? "Desktop preparation failed."
                : message;

            State = DesktopPreparationState.Failed;

            Debug.LogError(
                $"[DesktopManager] {FailureMessage}",
                this);

            DesktopFailed?.Invoke(FailureMessage);
        }

        private void CancelPreparation()
        {
            if (preparationRoutine == null)
                return;

            StopCoroutine(preparationRoutine);
            preparationRoutine = null;
        }
    }
}