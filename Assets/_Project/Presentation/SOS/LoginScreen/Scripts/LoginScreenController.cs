using Atlas.AuthoredData.Users;
using UnityEngine;
using UnityEngine.UIElements;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.LoginScreen
{
    public sealed class LoginScreenController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        private UIDocument uiDoc;

        // -------------------- AUTHORED DATA --------------------
        [Header("User Data")]
        [SerializeField]
        private UserLibrarySO userLibrary;

        [SerializeField]
        private string defaultUserId = "admin";

        // -------------------- HELPERS --------------------
        private LoginScreenBinder binder;
        private LoginScreenView view;
        private LoginScreenViewBuilder builder;

        // -------------------- STATE --------------------
        private UserInfo selectedUser;

        // -------------------- LIFECYCLE --------------------
        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[LoginScreenController] UIDocument was not found in parent hierarchy.");
                return;
            }

            if (userLibrary == null)
            {
                Debug.LogError("[LoginScreenController] UserLibrarySO was not assigned.");
                return;
            }

            binder = new LoginScreenBinder(uiDoc.rootVisualElement);

            if (!binder.IsValid)
            {
                return;
            }

            view = new LoginScreenView(binder);

            builder = new LoginScreenViewBuilder(userLibrary);

            BindActions();

            InitializeSelectedUser();

            RefreshView();
        }


        // -------------------- INITIALIZATION --------------------
        private void InitializeSelectedUser()
        {
            selectedUser =
                userLibrary.GetUser(defaultUserId);

            if (selectedUser != null)
            {
                return;
            }

            selectedUser =
                builder.GetFirstVisibleUser();

            if (selectedUser == null)
            {
                Debug.LogError(
                    "[LoginScreenController] No visible users are available."
                );
            }
        }


        // -------------------- ACTION BINDING --------------------
        private void BindActions()
        {
            binder.BindActions(
                OnUser1Pressed,
                OnUser2Pressed,
                OnSignInPressed,
                OnForgotPasswordPressed,
                OnAccessibilityPressed,
                OnPowerPressed
            );
        }


        // -------------------- VIEW --------------------
        public void RefreshView()
        {
            if (builder == null || view == null)
            {
                return;
            }

            LoginScreenView.ViewState state = builder.Build(selectedUser);

            view.Apply(state);
        }


        // -------------------- USER SELECTION --------------------
        private void SelectUser(UserInfo user)
        {
            if (user == null)
            {
                return;
            }

            if (!builder.IsUserVisible(user))
            {
                return;
            }

            selectedUser = user;

            RefreshView();
        }

        private void OnUser1Pressed()
        {
            SelectUser(builder.GetVisibleUser(0));
        }

        private void OnUser2Pressed()
        {
            SelectUser(builder.GetVisibleUser(1));
        }


        // -------------------- LOGIN ACTIONS --------------------
        // TODO: Guest sign in vs admin sign in, how to hgandle password vs jsut regular sign in button
        private void OnSignInPressed()
        {
            if (selectedUser == null)
            {
                return;
            }

            Debug.Log($"[LoginScreenController] Sign in requested for '{selectedUser.UserId}'.");

            Hide();

            // Authentication/session logic goes here later.
        }

        private void OnForgotPasswordPressed()
        {
            Debug.Log("[LoginScreenController] Forgot Password pressed.");
        }


        // -------------------- SYSTEM ACTIONS --------------------
        private void OnAccessibilityPressed()
        {
            Debug.Log("[LoginScreenController] Accessibility pressed.");
        }

        private void OnPowerPressed()
        {
            Debug.Log("[LoginScreenController] Power pressed.");
        }

        // -------------------- PANEL VISIBILITY --------------------
        private void Show()
        {
            binder.Root.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            Debug.Log("[LoginScreenController] Hide requested.");
            binder.Root.style.display = DisplayStyle.None;
        }
    }
}