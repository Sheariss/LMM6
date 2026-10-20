using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Presentation.SOS.Apps;
using Atlas.Presentation.SOS.Desktop;
using Atlas.SOS.Authentication;
using Atlas.SOS.Notifications;
using UnityEngine;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantNotificationIntegration : MonoBehaviour
    {
        private const string SourceId = "assistant";
        private const string DestinationId = "assistant.offer";
        private const string AssistantAppId = "assistant";

        [SerializeField] private DesktopManager desktopManager;
        [SerializeField] private AppController appController;
        [SerializeField] private Sprite assistantIcon;

        // Membership persists when the desktop is rebuilt or this component is
        // temporarily disabled. Clearing history must not republish an offer.
        private readonly Dictionary<string, HashSet<string>> publishedByUser =
            new(StringComparer.Ordinal);

        private AssistantManager manager;
        private NotificationInbox inbox;
        private IDisposable destinationRegistration;
        private HashSet<string> published;
        private string userId;
        private bool synchronizing;

        private void OnEnable()
        {
            if (desktopManager == null || appController == null)
            {
                Debug.LogError(
                    "[AssistantNotificationIntegration] Assign DesktopManager and AppController.",
                    this);
                return;
            }

            desktopManager.DesktopShown += OnDesktopShown;
            desktopManager.DesktopCleared += Detach;

            if (desktopManager.IsDesktopShown)
                OnDesktopShown();
        }

        private void Start()
        {
            // Covers enabling alongside another component's Awake.
            if (manager == null && desktopManager != null && appController != null &&
                desktopManager.IsDesktopShown)
            {
                OnDesktopShown();
            }
        }

        private void OnDisable()
        {
            if (desktopManager != null)
            {
                desktopManager.DesktopShown -= OnDesktopShown;
                desktopManager.DesktopCleared -= Detach;
            }

            Detach();
        }

        private void OnDesktopShown()
        {
            Detach();

            var assistant = AssistantManager.Instance;

            if (assistant == null || desktopManager.ActiveUser == null)
                return;

            userId = desktopManager.ActiveUser.UserId;
            assistant.SetActiveUser(userId);

            manager = assistant;
            inbox = NotificationService.ForUser(userId);

            if (!publishedByUser.TryGetValue(userId, out published))
            {
                published = new HashSet<string>(StringComparer.Ordinal);
                publishedByUser.Add(userId, published);
            }

            try
            {
                destinationRegistration = inbox.RegisterDestination(
                    DestinationId,
                    CanOpen,
                    Open);

                manager.Changed += Synchronize;
                Synchronize();
            }
            catch
            {
                Detach();
                throw;
            }
        }

        private bool IsCurrentContext()
        {
            var authentication = AuthenticationManager.Instance;

            return isActiveAndEnabled &&
                   manager != null && inbox != null &&
                   desktopManager != null && desktopManager.IsDesktopShown &&
                   desktopManager.ActiveUser != null &&
                   desktopManager.ActiveUser.UserId == userId &&
                   manager.ActiveUserId == userId &&
                   authentication != null && authentication.IsSignedIn &&
                   authentication.CurrentUser.UserId == userId;
        }

        private bool CanOpen(NotificationEntry entry)
        {
            return IsCurrentContext() &&
                   appController != null && appController.isActiveAndEnabled &&
                   desktopManager.ActiveUser.CanAccessApp(AssistantAppId) &&
                   entry != null && !entry.IsResolved &&
                   entry.SourceId == SourceId &&
                   entry.DestinationId == DestinationId &&
                   entry.Key == entry.TargetId &&
                   manager.CanPresentOffer(entry.TargetId);
        }

        private bool Open(NotificationEntry entry)
        {
            if (!CanOpen(entry))
                return false;

            // Launch restores the catalog's single Assistant instance. Do not
            // accept an offer or start its tutorial as a side effect.
            appController.Launch(AssistantAppId);

            if (!IsCurrentContext() ||
                appController.GetWindowPreviews(AssistantAppId).Count == 0)
            {
                return false;
            }

            return manager.TrySelectOffer(entry.TargetId);
        }

        private void Synchronize()
        {
            if (synchronizing || !IsCurrentContext())
                return;

            synchronizing = true;

            try
            {
                foreach (var offer in manager.Offers.ToArray())
                {
                    var definition = manager.Catalog.FindOffer(offer.id);
                    NotificationEntry existing = inbox.FindByKey(SourceId, offer.id);

                    bool actionable =
                        offer.status == AssistantOfferStatus.Presented ||
                        offer.status == AssistantOfferStatus.Deferred;

                    if (definition != null && actionable &&
                        manager.CanPresentOffer(offer.id))
                    {
                        // An existing record may outlive a rebuilt adapter.
                        if (existing != null)
                            published.Add(offer.id);

                        bool firstPublication = existing == null && published.Add(offer.id);
                        if (existing == null && !firstPublication)
                            continue;

                        string message = manager.GetOfferStatusMessage(offer);
                        bool deferredInvitation = offer.status == AssistantOfferStatus.Deferred &&
                            !string.IsNullOrWhiteSpace(definition.invitationConversationId);
                        string title = string.IsNullOrWhiteSpace(definition.title)
                            ? "Guidance available" : definition.title;
                        if (deferredInvitation)
                            title = offer.reminderSuppressed ? "Introduction available anytime" :
                                offer.reminderDueUtcTicks > 0 ? "Introduction reminder scheduled" : "Introduction reminder";

                        NotificationEntry posted = inbox.Post(new NotificationRequest
                        {
                            SourceId = SourceId,
                            SourceName = "ATLAS Assistant",
                            Key = offer.id,
                            Title = title,
                            Message = message,
                            AppId = AssistantAppId,
                            Icon = assistantIcon,
                            DestinationId = DestinationId,
                            TargetId = offer.id,
                            // Offer priority orders invitations; it must not
                            // automatically bypass the player's DND setting.
                            IsPriority = false,
                            RequireInteraction = offer.id == "guest.introduction.offer",
                            Layout = deferredInvitation
                                ? NotificationLayout.Status : NotificationLayout.Standard,
                            OpenLabel = "Open Assistant",
                            RequestBanner = firstPublication &&
                                offer.status == AssistantOfferStatus.Presented
                        });
                        if (offer.status == AssistantOfferStatus.Deferred)
                            inbox.DismissBanner(posted.Id);
                    }
                    else if (existing != null)
                    {
                        switch (offer.status)
                        {
                            case AssistantOfferStatus.Accepted:
                                inbox.Resolve(SourceId, offer.id, "Accepted");
                                break;
                            case AssistantOfferStatus.Dismissed:
                                inbox.Resolve(SourceId, offer.id, "Dismissed");
                                break;
                            case AssistantOfferStatus.Expired:
                                inbox.Resolve(SourceId, offer.id, "Expired");
                                break;
                        }
                    }
                }

                // Also refresh temporary eligibility changes, such as a
                // blocked presentation, without expiring the underlying offer.
                inbox.RefreshDestinations();
            }
            finally
            {
                synchronizing = false;
            }
        }

        private void Detach()
        {
            if (manager != null)
                manager.Changed -= Synchronize;

            // Revoke eligibility before disposal raises the inbox Changed event.
            manager = null;
            destinationRegistration?.Dispose();
            destinationRegistration = null;
            inbox = null;
            published = null;
            userId = null;
        }
    }
}
