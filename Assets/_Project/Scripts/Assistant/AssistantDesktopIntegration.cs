using System;
using System.Collections;
using Atlas.Presentation.SOS.Desktop;
using Atlas.SOS.Authentication;
using UnityEngine;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantDesktopIntegration : MonoBehaviour
    {
        [SerializeField] private DesktopManager desktopManager;

        private Coroutine pendingPresentation;

        private void OnEnable()
        {
            if (desktopManager == null)
            {
                Debug.LogError(
                    "[AssistantDesktopIntegration] Assign DesktopManager.",
                    this);
                return;
            }

            desktopManager.DesktopShown += OnDesktopShown;
            desktopManager.DesktopCleared += OnDesktopCleared;

            // Also handles enabling the harness after desktop presentation.
            if (desktopManager.IsDesktopShown)
                OnDesktopShown();
        }

        private void OnDisable()
        {
            if (desktopManager != null)
            {
                desktopManager.DesktopShown -= OnDesktopShown;
                desktopManager.DesktopCleared -= OnDesktopCleared;
            }

            OnDesktopCleared();
        }

        private void OnDesktopShown()
        {
            CancelPendingPresentation();

            var assistant = AssistantManager.Instance;

            if (assistant == null || desktopManager.ActiveUser == null)
                return;

            string userId = desktopManager.ActiveUser.UserId;
            assistant.SetActiveUser(userId);
            assistant.SetFact("desktop.guest.visible", userId == "guest");

            pendingPresentation = StartCoroutine(ReportDesktopShown(userId));
        }

        private IEnumerator ReportDesktopShown(string expectedUserId)
        {
            // Real time keeps the delay consistent with presentation timing.
            yield return new WaitForSecondsRealtime(3f);

            pendingPresentation = null;

            if (desktopManager == null ||
                !desktopManager.IsDesktopShown ||
                desktopManager.ActiveUser == null ||
                desktopManager.ActiveUser.UserId != expectedUserId)
            {
                yield break;
            }

            var assistant = AssistantManager.Instance;
            var authentication = AuthenticationManager.Instance;

            if (assistant == null)
            {
                Debug.LogError(
                    "[AssistantDesktopIntegration] " +
                    "AssistantManager has not initialized successfully.",
                    this);
                yield break;
            }

            if (assistant.ActiveUserId != expectedUserId ||
                authentication == null || !authentication.IsSignedIn ||
                authentication.CurrentUser.UserId != expectedUserId)
            {
                yield break;
            }

            bool isGuest = string.Equals(
                desktopManager.ActiveUser.UserId,
                "guest",
                StringComparison.Ordinal);

            assistant.SetFact("desktop.guest.visible", isGuest);

            if (isGuest)
                assistant.ReportSignal("guest.introduction.ready");
        }

        private void OnDesktopCleared()
        {
            CancelPendingPresentation();

            var assistant = AssistantManager.Instance;

            if (assistant != null)
                assistant.SetActiveUser(null);
        }

        private void CancelPendingPresentation()
        {
            if (pendingPresentation == null)
                return;

            StopCoroutine(pendingPresentation);
            pendingPresentation = null;
        }
    }
}
