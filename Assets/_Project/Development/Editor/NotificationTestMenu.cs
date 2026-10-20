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
                    (entry.SourceId == RouteSource && entry.Key == RouteKey))
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
