using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Atlas.Core.GameState;
using Atlas.SOS.Authentication;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public sealed class LoginScreenController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------

        private UIDocument uiDocument;

        // -------------------- RUNTIME DEPENDENCIES --------------------

        private AuthenticationManager authenticationManager;
        private GameStateManager gameStateManager;

        // -------------------- SYSTEM ACTIONS --------------------

        [SerializeField] private UnityEvent accessibilityRequested = new UnityEvent();
        [SerializeField] private UnityEvent powerRequested = new UnityEvent();

        // -------------------- WELCOME --------------------

        private const float WelcomeDurationSeconds = 4f;
        private Coroutine welcomeRoutine;

        // -------------------- HELPERS --------------------

        private LoginScreenBinder binder;
        private LoginScreenView view;
        private LoginScreenEngine engine;
        private LoginScreenViewBuilder builder;
        private LoginScreenView.ViewState currentState;
        private VisualElement boundDocumentRoot;

        private bool started;
        private bool callbacksRegistered;

        // -------------------- LIFECYCLE --------------------

        private void Start()
        {
            started = true;
            Initialize();
        }

        private void OnEnable()
        {
            if (started)
                Initialize();
        }

        private void OnDisable()
        {
            CancelWelcomeDelay();
            UnregisterCallbacks();

            view?.ClearInputs();
            view?.SetVisible(false);

            binder = null;
            view = null;
            boundDocumentRoot = null;
        }

        private void Initialize()
        {
            uiDocument = GetComponentInParent<UIDocument>();

            if (uiDocument == null)
            {
                Debug.LogError(
                    "[LoginScreenController] UIDocument was not found in parent hierarchy."
                );
                return;
            }

            if (!ResolveDependencies())
                return;

            if (engine == null)
            {
                engine = new LoginScreenEngine(authenticationManager);
                builder = new LoginScreenViewBuilder();
            }

            engine.Initialize();
            BindDocument();
        }

        // -------------------- DEPENDENCIES --------------------

        private bool ResolveDependencies()
        {
            authenticationManager = AuthenticationManager.Instance;
            gameStateManager = GameStateManager.Instance;

            bool valid = true;

            if (authenticationManager == null)
            {
                Debug.LogError(
                    "[LoginScreenController] AuthenticationManager instance was not found."
                );
                valid = false;
            }
            else if (authenticationManager.UserLibrary == null)
            {
                Debug.LogError(
                    "[LoginScreenController] AuthenticationManager UserLibrary was not assigned."
                );
                valid = false;
            }

            if (gameStateManager == null)
            {
                Debug.LogError(
                    "[LoginScreenController] GameStateManager instance was not found."
                );
                valid = false;
            }

            return valid;
        }

        // -------------------- DOCUMENT BINDING --------------------

        private void BindDocument()
        {
            UnregisterCallbacks();

            boundDocumentRoot = uiDocument.rootVisualElement;
            binder = new LoginScreenBinder(boundDocumentRoot);

            if (!binder.IsValid)
            {
                view = null;
                return;
            }

            view = new LoginScreenView(binder);

            RegisterCallbacks();
            RefreshView();
            view.SetVisible(!authenticationManager.IsSignedIn);

            if (!authenticationManager.IsSignedIn)
                view.FocusPrimary(currentState);
        }

        // -------------------- CALLBACKS --------------------

        private void RegisterCallbacks()
        {
            binder.User1Button.clicked += OnUser1Pressed;
            binder.User2Button.clicked += OnUser2Pressed;

            binder.PasswordSubmitButton.clicked += OnSignInPressed;
            binder.SignInButton.clicked += OnSignInPressed;
            binder.ForgotPasswordButton.clicked += OnForgotPasswordPressed;
            binder.LoginMessageOkButton.clicked += OnMessageOkPressed;

            binder.AccessibilityButton.clicked += OnAccessibilityPressed;
            binder.PowerButton.clicked += OnPowerPressed;

            binder.Password.RegisterCallback<KeyDownEvent>(
                OnPasswordKeyDown,
                TrickleDown.TrickleDown
            );

            callbacksRegistered = true;
        }

        private void UnregisterCallbacks()
        {
            if (!callbacksRegistered)
                return;

            binder.User1Button.clicked -= OnUser1Pressed;
            binder.User2Button.clicked -= OnUser2Pressed;

            binder.PasswordSubmitButton.clicked -= OnSignInPressed;
            binder.SignInButton.clicked -= OnSignInPressed;
            binder.ForgotPasswordButton.clicked -= OnForgotPasswordPressed;
            binder.LoginMessageOkButton.clicked -= OnMessageOkPressed;

            binder.AccessibilityButton.clicked -= OnAccessibilityPressed;
            binder.PowerButton.clicked -= OnPowerPressed;

            binder.Password.UnregisterCallback<KeyDownEvent>(
                OnPasswordKeyDown,
                TrickleDown.TrickleDown
            );

            callbacksRegistered = false;
        }

        // -------------------- ACCOUNT SELECTION --------------------

        private void OnUser1Pressed()
        {
            SelectUser(currentState.User1?.UserId);
        }

        private void OnUser2Pressed()
        {
            SelectUser(currentState.User2?.UserId);
        }

        private void SelectUser(string userId)
        {
            if (!engine.SelectUser(userId))
                return;

            view.ClearInputs();
            RefreshView();
            view.FocusPrimary(currentState);
        }

        // -------------------- LOGIN ACTIONS --------------------

        private void OnSignInPressed()
        {
            if (engine.Section != LoginScreenSection.Credentials ||
                !currentState.CanSubmit)
            {
                return;
            }

            bool succeeded = engine.TrySignIn(binder.Password.value);

            view.ClearInputs();
            RefreshView();

            if (succeeded)
            {
                CancelWelcomeDelay();
                welcomeRoutine = StartCoroutine(FinishSignIn());
                return;
            }

            view.FocusPrimary(currentState);
        }

        private IEnumerator FinishSignIn()
        {
            // Keep Welcome and its spinner visible during the simulated load.
            yield return new WaitForSecondsRealtime(WelcomeDurationSeconds);

            welcomeRoutine = null;
            view?.SetVisible(false);
        }

        private void CancelWelcomeDelay()
        {
            if (welcomeRoutine == null)
                return;

            StopCoroutine(welcomeRoutine);
            welcomeRoutine = null;
        }

        private void OnForgotPasswordPressed()
        {
            engine.RequestRecovery();
            view.ClearInputs();
            RefreshView();
            view.FocusPrimary(currentState);
        }

        private void OnMessageOkPressed()
        {
            engine.DismissMessage();
            RefreshView();
            view.FocusPrimary(currentState);
        }

        private void OnPasswordKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            if (!currentState.ShowPassword ||
                !binder.PasswordSubmitButton.enabledInHierarchy)
            {
                return;
            }

            evt.StopPropagation();
            OnSignInPressed();
        }

        // -------------------- SYSTEM ACTIONS --------------------

        private void OnAccessibilityPressed()
        {
            accessibilityRequested.Invoke();
        }

        private void OnPowerPressed()
        {
            powerRequested.Invoke();
        }

        // -------------------- PRESENTATION --------------------

        public void RefreshView()
        {
            if (builder == null || view == null)
                return;

            currentState = builder.Build(engine);
            view.Apply(currentState);
        }

        public void Show()
        {
            if (engine == null || authenticationManager.IsSignedIn)
                return;

            engine.Initialize();

            // Rebind if the UIDocument recreated its visual tree.
            if (view == null ||
                boundDocumentRoot != uiDocument.rootVisualElement ||
                binder.Root.panel == null)
            {
                BindDocument();
                return;
            }

            view.ClearInputs();
            RefreshView();
            view.SetVisible(true);
            view.FocusPrimary(currentState);
        }

        public void Hide()
        {
            CancelWelcomeDelay();
            view?.ClearInputs();
            view?.SetVisible(false);
        }
    }
}