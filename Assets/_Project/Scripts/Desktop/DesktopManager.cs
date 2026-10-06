using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using Atlas.SOS.Authentication;
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
        // -------------------- DEPENDENCIES --------------------

        [SerializeField] private UIDocument desktopDocument;

        private DesktopBinder binder;
        private DesktopView view;
        private Coroutine preparationRoutine;

        // -------------------- STATE --------------------

        public DesktopPreparationState State { get; private set; }
        public UserInfo ActiveUser { get; private set; }
        public string FailureMessage { get; private set; }

        public event Action DesktopReady;
        public event Action<string> DesktopFailed;

        // -------------------- LIFECYCLE --------------------

        private void OnEnable()
        {
            BindDocument();
            view?.Hide();
        }

        private void LateUpdate()
        {
            if (State != DesktopPreparationState.Preparing &&
                State != DesktopPreparationState.Ready)
            {
                return;
            }

            if (!IsAuthenticatedAccount(ActiveUser))
            {
                Fail("The authenticated account changed.");
                return;
            }

            if (!IsDocumentCurrent())
            {
                // Hide any replacement tree before reporting failure.
                BindDocument();
                Fail("The desktop UI changed. Please retry.");
            }
        }

        private void OnDisable()
        {
            bool wasPreparing =
                State == DesktopPreparationState.Preparing;

            ClearDesktop();

            if (wasPreparing)
                Fail("Desktop preparation was interrupted. Please retry.");
        }

        // -------------------- PREPARATION --------------------

        public void LoadDesktop(UserInfo user)
        {
            if (!isActiveAndEnabled)
            {
                Fail("The desktop manager is not active.");
                return;
            }

            if (!IsAuthenticatedAccount(user))
            {
                Fail("A signed-in account is required.");
                return;
            }

            // Rebinding the login screen must not restart an active load.
            if (ReferenceEquals(ActiveUser, user) &&
                IsDocumentCurrent() &&
                (State == DesktopPreparationState.Preparing ||
                 State == DesktopPreparationState.Ready))
            {
                return;
            }

            ClearDesktop();
            ActiveUser = user;

            if (!BindDocument() || !IsDocumentCurrent())
            {
                Fail("The desktop UI is missing or is not attached.");
                return;
            }

            // Treat a missing account wallpaper as a configuration error.
            if (user.Wallpaper == null)
            {
                Fail("This account does not have a desktop wallpaper.");
                return;
            }

            State = DesktopPreparationState.Preparing;
            preparationRoutine = StartCoroutine(PrepareDesktop());
        }

        private IEnumerator PrepareDesktop()
        {
            // Let the login controller present Welcome first.
            // Two frame yields also cover calls made before UI rendering.
            yield return null;
            yield return null;

            if (!ValidatePreparation(out string error))
            {
                preparationRoutine = null;
                Fail(error);
                yield break;
            }

            Exception preparationError = null;

            try
            {
                view.Prepare(ActiveUser.Wallpaper);
            }
            catch (Exception exception)
            {
                preparationError = exception;
            }

            if (preparationError != null)
            {
                Debug.LogException(preparationError, this);

                preparationRoutine = null;
                Fail("The desktop could not be prepared. Please retry.");
                yield break;
            }

            // Allow UI Toolkit to process the updated shell.
            yield return null;

            preparationRoutine = null;

            if (!ValidatePreparation(out error))
            {
                Fail(error);
                yield break;
            }

            State = DesktopPreparationState.Ready;
            FailureMessage = null;

            // Readiness does not automatically reveal the desktop.
            DesktopReady?.Invoke();
        }

        // -------------------- REVEAL / RESET --------------------

        public bool ShowDesktop()
        {
            if (State != DesktopPreparationState.Ready)
                return false;

            if (!ValidatePreparation(out string error))
            {
                BindDocument();
                Fail(error);
                return false;
            }

            view.Show();
            return true;
        }

        public void ClearDesktop()
        {
            CancelPreparation();
            view?.Clear();

            ActiveUser = null;
            FailureMessage = null;
            State = DesktopPreparationState.Idle;
        }

        // -------------------- VALIDATION --------------------

        private bool BindDocument()
        {
            view?.Hide();

            binder = null;
            view = null;

            if (desktopDocument == null)
                return false;

            binder = new DesktopBinder(
                desktopDocument.rootVisualElement
            );

            view = new DesktopView(binder);
            view.Hide();

            return binder.IsValid;
        }

        private bool IsDocumentCurrent()
        {
            return desktopDocument != null &&
                   desktopDocument.isActiveAndEnabled &&
                   binder != null &&
                   binder.IsValid &&
                   binder.DocumentRoot ==
                       desktopDocument.rootVisualElement &&
                   binder.Root ==
                       desktopDocument.rootVisualElement
                           .Q<VisualElement>("DesktopRoot") &&
                   binder.Root.panel != null;
        }

        private bool ValidatePreparation(out string error)
        {
            if (!IsAuthenticatedAccount(ActiveUser))
            {
                error = "The authenticated account changed.";
                return false;
            }

            if (!IsDocumentCurrent())
            {
                error = "The desktop UI is unavailable. Please retry.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsAuthenticatedAccount(UserInfo user)
        {
            AuthenticationManager authentication =
                AuthenticationManager.Instance;

            return user != null &&
                   authentication != null &&
                   authentication.IsSignedIn &&
                   ReferenceEquals(authentication.CurrentUser, user);
        }

        // -------------------- FAILURE / CANCELLATION --------------------

        private void Fail(string message)
        {
            CancelPreparation();
            view?.Hide();

            FailureMessage = message;
            State = DesktopPreparationState.Failed;

            DesktopFailed?.Invoke(message);
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