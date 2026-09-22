using Atlas.Core.GameState;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.LockScreen
{
    public sealed class LockScreenController : MonoBehaviour
    {
        // -------------------- UI DOCUMENT --------------------

        private UIDocument uiDoc;

        // -------------------- HELPERS --------------------

        private LockScreenBinder binder;

        // -------------------- LIFECYCLE --------------------

        private void Start()
        {
            uiDoc = GetComponentInParent<UIDocument>();

            if (uiDoc == null)
            {
                Debug.LogError(
                    "[LockScreenController] UIDocument was not found in parent hierarchy."
                );

                return;
            }

            binder =
                new LockScreenBinder(
                    uiDoc.rootVisualElement
                );

            RegisterCallbacks();
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        // -------------------- CALLBACK REGISTRATION --------------------

        private void RegisterCallbacks()
        {
            binder.Root.RegisterCallback<PointerDownEvent>(
                OnPointerDown
            );

            binder.Root.RegisterCallback<NavigationSubmitEvent>(
                OnSubmit
            );
        }

        private void UnregisterCallbacks()
        {
            if (binder == null)
            {
                return;
            }

            binder.Root.UnregisterCallback<PointerDownEvent>(
                OnPointerDown
            );

            binder.Root.UnregisterCallback<NavigationSubmitEvent>(
                OnSubmit
            );
        }

        // -------------------- INPUT --------------------

        private void OnPointerDown(PointerDownEvent evt)
        {
            OpenSignIn();
        }

        private void OnSubmit(NavigationSubmitEvent evt)
        {
            OpenSignIn();
        }

        // -------------------- NAVIGATION --------------------

        private void OpenSignIn()
        {
            Debug.Log("[LockScreenController] Sign-in requested.");
            Hide();

            // TODO: Sign-in transition later.
        }

        // -------------------- PANEL VISIBILITY --------------------
        private void Show()
        {
            binder.Root.style.display = DisplayStyle.Flex;
        }

        private void Hide()
        {
            binder.Root.style.display = DisplayStyle.None;
        }
    }
}