using System;
using System.Collections;
using Atlas.Presentation.SOS.Desktop;
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

            pendingPresentation = StartCoroutine(ReportDesktopShown());
        }

        private IEnumerator ReportDesktopShown()
        {
            // ShowDesktop returns, then LoginScreenController hides login.
            yield return null;

            pendingPresentation = null;

            if (desktopManager == null ||
                !desktopManager.IsDesktopShown ||
                desktopManager.ActiveUser == null)
            {
                yield break;
            }

            var assistant = AssistantManager.Instance;

            if (assistant == null)
            {
                Debug.LogError(
                    "[AssistantDesktopIntegration] " +
                    "AssistantManager has not initialized successfully.",
                    this);
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
                assistant.SetFact("desktop.guest.visible", false);
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