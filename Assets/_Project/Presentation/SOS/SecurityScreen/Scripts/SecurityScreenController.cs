using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.SecurityScreen
{
    public sealed class SecurityScreenController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------
        private UIDocument uiDoc;

        // -------------------- HELPERS --------------------
        private SecurityScreenBinder binder;

        // -------------------- LIFECYCLE --------------------
        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError("[SecurityScreenController] UIDocument was not found in parent hierarchy.");
                return;
            }

            BindUI();
            RegisterCallbacks();

            Hide();
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }


        // -------------------- UI BINDING --------------------
        private void BindUI()
        {
            binder = new SecurityScreenBinder(
                uiDoc.rootVisualElement
            );
        }


        // -------------------- CALLBACKS --------------------
        private void RegisterCallbacks()
        {
            binder.LockButton.clicked += OnLockPressed;
            binder.SwitchUserButton.clicked += OnSwitchUserPressed;
            binder.SignOutButton.clicked += OnSignOutPressed;
            binder.ChangePasswordButton.clicked += OnChangePasswordPressed;
            binder.TaskManagerButton.clicked += OnTaskManagerPressed;
            binder.CancelButton.clicked += OnCancelPressed;
        }

        private void UnregisterCallbacks()
        {
            if (binder == null)
                return;

            binder.LockButton.clicked -= OnLockPressed;
            binder.SwitchUserButton.clicked -= OnSwitchUserPressed;
            binder.SignOutButton.clicked -= OnSignOutPressed;
            binder.ChangePasswordButton.clicked -= OnChangePasswordPressed;
            binder.TaskManagerButton.clicked -= OnTaskManagerPressed;
            binder.CancelButton.clicked -= OnCancelPressed;
        }


        // -------------------- BUTTON EVENTS --------------------
        private void OnLockPressed()
        {
            Debug.Log("[SecurityScreenController] Lock pressed.");

            // TODO: Lock workstation.
        }

        private void OnSwitchUserPressed()
        {
            Debug.Log("[SecurityScreenController] Switch User pressed.");

            // TODO: Switch user.
        }

        private void OnSignOutPressed()
        {
            Debug.Log("[SecurityScreenController] Sign Out pressed.");

            // TODO: Sign out current user.
        }

        private void OnChangePasswordPressed()
        {
            Debug.Log("[SecurityScreenController] Change Password pressed.");

            // TODO: Open change password flow.
        }

        private void OnTaskManagerPressed()
        {
            Debug.Log("[SecurityScreenController] Task Manager pressed.");

            // TODO: Open task manager.
        }

        private void OnCancelPressed()
        {
            Debug.Log("[SecurityScreenController] Cancel pressed.");

            // TODO: Close security screen.
        }

        // -------------------- PANEL VISIBILITY --------------------
        public void Show()
        {
            if (binder?.Root == null)
                return;

            binder.Root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            if (binder?.Root == null)
                return;

            binder.Root.style.display = DisplayStyle.None;
        }
    }
}