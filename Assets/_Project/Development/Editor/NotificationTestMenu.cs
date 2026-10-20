#if UNITY_EDITOR
using System.Linq;
using Atlas.SOS.Authentication;
using Atlas.SOS.Notifications;
using UnityEditor;
using UnityEngine;

namespace Atlas.Development
{
    public static class NotificationTestMenu
    {
        private const string MenuRoot = "ATLAS/Development/Notifications/";
        private const string SampleSource = "development.notifications.samples";
        private const string RouteSource = "development.notifications.destination";
        private const string FirstKey = "first";
        private const string SecondKey = "second";
        private const string RouteKey = "unavailable";
        private const string PopupSource = "development.notifications.popups";
        private const string LayoutSource = "development.notifications.layouts";

        [MenuItem(MenuRoot + "Post timed popup", false, 110)]
        private static void PostTimedPopup() => PostPopup(false);

        [MenuItem(MenuRoot + "Post persistent popup", false, 111)]
        private static void PostPersistentPopup() => PostPopup(true);

        private static void PostPopup(bool persistent)
        {
            if (!TryGetInbox(out NotificationInbox inbox, out _)) return;
            inbox.Post(new NotificationRequest
            {
                SourceId = PopupSource, SourceName = "Popup test",
                Key = System.Guid.NewGuid().ToString("N"),
                Title = persistent ? "Persistent notification" : "Timed notification",
                Message = persistent ? "This stays visible until opened or closed. Closing preserves inbox history." :
                    "This closes after 12 seconds. Hover or keyboard focus pauses its timer.",
                AppId = "file-explorer", RequireInteraction = persistent
            });
        }

        [MenuItem(MenuRoot + "Post card layout samples", false, 112)]
        private static void PostLayoutSamples()
        {
            if (!TryGetInbox(out NotificationInbox inbox, out _)) return;
            var icon = Resources.Load<Sprite>("AppIcons/Assistant/icons8-sparkle-48");
            foreach (var layout in new[] { NotificationLayout.Message, NotificationLayout.Email, NotificationLayout.Standard })
                inbox.Post(new NotificationRequest
                {
                    SourceId = LayoutSource + "." + layout, SourceName = layout + " layout test",
                    Key = "layout", Title = layout == NotificationLayout.Email ? "Workspace training update" : "A new message",
                    Message = "This is a longer preview to check truncation when collapsed and wrapping when expanded. Clicking the content opens the destination app; expanding the card only reveals its content.",
                    ActorName = layout == NotificationLayout.Standard ? null : "ATLAS Team",
                    Icon = icon, ActorImage = icon, Layout = layout,
                    OpenLabel = layout == NotificationLayout.Email ? null : "Open",
                    AppId = "file-explorer", RequestBanner = false
                });
        }

        [MenuItem(MenuRoot + "Deliver scheduled Assistant reminder now", false, 113)]
        private static void DeliverAssistantReminder()
        {
            if (!CanUseMenu()) return;
            var assistant = Atlas.Presentation.SOS.Assistant.AssistantManager.Instance;
            if (assistant == null || assistant.ActiveUserId != AuthenticationManager.Instance.CurrentUser.UserId)
                return;
            assistant.ProcessReminders(System.DateTime.UtcNow.AddHours(2).Ticks);
        }

        [MenuItem(MenuRoot + "Post sample notifications", false, 100)]
        private static void PostSamples()
        {
            if (!TryGetInbox(out NotificationInbox inbox, out string userId))
                return;

            inbox.Post(new NotificationRequest
            {
                SourceId = SampleSource,
                SourceName = "Notification test",
                Key = FirstKey,
                Title = "Open File Explorer",
                Message = "Select Open to launch File Explorer. This is a test notification.",
                AppId = "file-explorer",
                RequestBanner = false
            });

            inbox.Post(new NotificationRequest
            {
                SourceId = SampleSource,
                SourceName = "Notification test",
                Key = SecondKey,
                Title = "Another notification in this group",
                Message = "Expand the group to see both entries. Removing this entry only clears notification history.",
                RequestBanner = false
            });

            inbox.Post(new NotificationRequest
            {
                SourceId = RouteSource,
                SourceName = "Destination test",
                Key = RouteKey,
                Title = "Unavailable destination",
                Message = "Open should be disabled because this test destination has no registered handler.",
                DestinationId = "development.notifications.unregistered",
                TargetId = "test-target",
                RequestBanner = false
            });

            Debug.Log(
                $"[Notification Test] Posted three samples for '{userId}'. " +
                "Repeated posting reuses the same entries. To restart after " +
                "resolving, use Clear sample notifications, then post again.");
        }

        [MenuItem(MenuRoot + "Update first sample", false, 101)]
        private static void UpdateFirst()
        {
            if (!TryGetInbox(out NotificationInbox inbox, out string userId))
                return;

            if (inbox.FindByKey(SampleSource, FirstKey) == null)
            {
                Debug.LogWarning("[Notification Test] Post the sample notifications first.");
                return;
            }

            inbox.Post(new NotificationRequest
            {
                SourceId = SampleSource,
                SourceName = "Notification test",
                Key = FirstKey,
                Title = "Updated File Explorer notification",
                Message = "This existing entry was updated. The entry count, read state and creation time should stay unchanged.",
                AppId = "file-explorer",
                RequestBanner = false
            });

            Debug.Log($"[Notification Test] Updated the first sample for '{userId}'.");
        }

        [MenuItem(MenuRoot + "Resolve first sample", false, 102)]
        private static void ResolveFirst()
        {
            if (!TryGetInbox(out NotificationInbox inbox, out string userId))
                return;

            if (inbox.FindByKey(SampleSource, FirstKey) == null)
            {
                Debug.LogWarning("[Notification Test] Post the sample notifications first.");
                return;
            }

            inbox.Resolve(SampleSource, FirstKey, "Completed (test)");
            Debug.Log($"[Notification Test] Resolved the first sample for '{userId}'.");
        }

        [MenuItem(MenuRoot + "Clear sample notifications", false, 103)]
        private static void ClearSamples()
        {
            if (!TryGetInbox(out NotificationInbox inbox, out string userId))
                return;

            string[] ids = inbox.GetSnapshot()
                .Where(entry =>
                    (entry.SourceId == SampleSource &&
                     (entry.Key == FirstKey || entry.Key == SecondKey)) ||
                    (entry.SourceId == RouteSource && entry.Key == RouteKey)
                    || entry.SourceId == PopupSource || entry.SourceId.StartsWith(LayoutSource + ".", System.StringComparison.Ordinal))
                .Select(entry => entry.Id)
                .ToArray();

            inbox.Dismiss(ids);
            Debug.Log(
                $"[Notification Test] Cleared {ids.Length} samples for '{userId}'. " +
                "Other notifications were preserved.");
        }

        [MenuItem(MenuRoot + "Post sample notifications", true)]
        [MenuItem(MenuRoot + "Update first sample", true)]
        [MenuItem(MenuRoot + "Resolve first sample", true)]
        [MenuItem(MenuRoot + "Clear sample notifications", true)]
        [MenuItem(MenuRoot + "Post timed popup", true)]
        [MenuItem(MenuRoot + "Post persistent popup", true)]
        [MenuItem(MenuRoot + "Post card layout samples", true)]
        [MenuItem(MenuRoot + "Deliver scheduled Assistant reminder now", true)]
        private static bool CanUseMenu()
        {
            AuthenticationManager authentication = AuthenticationManager.Instance;

            return EditorApplication.isPlaying &&
                   !EditorApplication.isCompiling &&
                   authentication != null &&
                   authentication.IsSignedIn &&
                   !string.IsNullOrWhiteSpace(authentication.CurrentUser.UserId);
        }

        private static bool TryGetInbox(
            out NotificationInbox inbox,
            out string userId)
        {
            inbox = null;
            userId = null;

            if (!CanUseMenu())
            {
                Debug.LogWarning(
                    "[Notification Test] Enter Play Mode and sign in before using these commands.");
                return false;
            }

            userId = AuthenticationManager.Instance.CurrentUser.UserId;
            inbox = NotificationService.ForUser(userId);
            return true;
        }
    }
}
#endif
