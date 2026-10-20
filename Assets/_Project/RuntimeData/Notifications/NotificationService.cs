using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Atlas.SOS.Notifications
{
    public enum NotificationLayout { Standard, Message, Email, Status }

    public sealed class NotificationRequest
    {
        // Identifies the publishing system and its group.
        public string SourceId;
        public string SourceName;

        // Stable within SourceId, for example an Assistant offer ID.
        public string Key;

        public string Title;
        public string Message;
        public string AppId;
        public Sprite Icon;

        // Optional app-owned route.
        // Example: DestinationId = "assistant.offer",
        // TargetId = "guest.introduction.offer".
        public string DestinationId;
        public string TargetId;

        public bool IsPriority;
        public bool RequestBanner = true;
        public bool RequireInteraction;
        public NotificationLayout Layout;
        public string ActorName;
        public Sprite ActorImage;
        public string OpenLabel = "Open";
    }

    public sealed class NotificationEntry
    {
        public string Id { get; }
        public string SourceId { get; }
        public string Key { get; }
        public DateTimeOffset CreatedUtc { get; }

        public string SourceName { get; private set; }
        public string Title { get; private set; }
        public string Message { get; private set; }
        public string AppId { get; private set; }
        public Sprite Icon { get; private set; }

        public string DestinationId { get; private set; }
        public string TargetId { get; private set; }

        public bool IsPriority { get; private set; }
        public bool IsRead { get; internal set; }
        public bool IsResolved { get; internal set; }
        public string ResolutionText { get; internal set; }
        public bool RequireInteraction { get; private set; }
        public NotificationLayout Layout { get; private set; }
        public string ActorName { get; private set; }
        public Sprite ActorImage { get; private set; }
        public string OpenLabel { get; private set; }

        internal NotificationEntry(NotificationRequest request)
        {
            Id = Guid.NewGuid().ToString("N");
            SourceId = request.SourceId;
            Key = request.Key;
            CreatedUtc = DateTimeOffset.UtcNow;

            Apply(request);
        }

        internal bool Apply(NotificationRequest request)
        {
            string message = request.Message ?? string.Empty;
            string appId = request.AppId ?? string.Empty;
            string destinationId = request.DestinationId ?? string.Empty;
            string targetId = request.TargetId ?? string.Empty;

            bool changed =
                SourceName != request.SourceName ||
                Title != request.Title ||
                Message != message ||
                AppId != appId ||
                Icon != request.Icon ||
                DestinationId != destinationId ||
                TargetId != targetId ||
                IsPriority != request.IsPriority ||
                RequireInteraction != request.RequireInteraction ||
                Layout != request.Layout || ActorName != request.ActorName ||
                ActorImage != request.ActorImage || OpenLabel != request.OpenLabel;

            SourceName = request.SourceName;
            Title = request.Title;
            Message = message;
            AppId = appId;
            Icon = request.Icon;
            DestinationId = destinationId;
            TargetId = targetId;
            IsPriority = request.IsPriority;
            RequireInteraction = request.RequireInteraction;
            Layout = request.Layout;
            ActorName = request.ActorName;
            ActorImage = request.ActorImage;
            OpenLabel = request.OpenLabel;

            return changed;
        }
    }

    public sealed class NotificationInbox
    {
        private const int Capacity = 200;

        private sealed class DestinationHandler
        {
            public Func<NotificationEntry, bool> CanOpen;
            public Func<NotificationEntry, bool> Open;
        }

        private sealed class Subscription : IDisposable
        {
            private Action dispose;

            public Subscription(Action dispose)
            {
                this.dispose = dispose;
            }

            public void Dispose()
            {
                Action action = dispose;
                dispose = null;
                action?.Invoke();
            }
        }

        private readonly List<NotificationEntry> entries = new();

        private readonly Dictionary<string, DestinationHandler> destinations =
            new(StringComparer.Ordinal);

        private bool opening;
        private int viewers;

        public bool DoNotDisturb { get; private set; }
        public bool IsBeingViewed => viewers > 0;

        public IDisposable BeginViewing()
        {
            viewers++;
            Changed?.Invoke();
            return new Subscription(() =>
            {
                viewers--;
                Changed?.Invoke();
            });
        }

        public event Action Changed;

        // Desktop popup hosts subscribe for their currently bound account.
        // The entry has already been stored when this fires.
        public event Action<NotificationEntry> BannerRequested;
        public event Action<string> BannerDismissRequested;

        // Presentation only: leave notification history and domain state intact.
        public void DismissBanner(string notificationId) => BannerDismissRequested?.Invoke(notificationId);

        public IReadOnlyList<NotificationEntry> GetSnapshot()
        {
            return entries.ToArray();
        }

        public NotificationEntry Find(string notificationId)
        {
            return entries.Find(entry => entry.Id == notificationId);
        }

        public NotificationEntry FindByKey(string sourceId, string key)
        {
            return entries.Find(entry =>
                entry.SourceId == sourceId &&
                entry.Key == key);
        }

        public NotificationEntry Post(NotificationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Require(request.SourceId, nameof(request.SourceId));
            Require(request.SourceName, nameof(request.SourceName));
            Require(request.Key, nameof(request.Key));
            Require(request.Title, nameof(request.Title));

            if (!string.IsNullOrWhiteSpace(request.DestinationId) &&
                string.IsNullOrWhiteSpace(request.TargetId))
            {
                throw new ArgumentException(
                    "A specific destination requires a target ID.",
                    nameof(request));
            }

            NotificationEntry existing = FindByKey(
                request.SourceId,
                request.Key);

            if (existing != null)
            {
                if (existing.Apply(request))
                    Changed?.Invoke();

                return existing;
            }

            var entry = new NotificationEntry(request);

            entries.Insert(0, entry);

            if (entries.Count > Capacity)
                entries.RemoveRange(Capacity, entries.Count - Capacity);

            Changed?.Invoke();

            // Changed subscribers may have resolved or removed the entry.
            if (Find(entry.Id) == entry &&
                !entry.IsResolved &&
                request.RequestBanner &&
                (!DoNotDisturb || entry.IsPriority))
            {
                BannerRequested?.Invoke(entry);
            }

            return entry;
        }

        public bool Resolve(
            string sourceId,
            string key,
            string resolutionText = "Resolved")
        {
            NotificationEntry entry = FindByKey(sourceId, key);

            if (entry == null)
                return false;

            string text = string.IsNullOrWhiteSpace(resolutionText)
                ? "Resolved"
                : resolutionText;

            if (entry.IsResolved && entry.ResolutionText == text)
                return false;

            entry.IsResolved = true;
            entry.ResolutionText = text;

            Changed?.Invoke();
            return true;
        }

        public void SetDoNotDisturb(bool enabled)
        {
            if (DoNotDisturb == enabled)
                return;

            DoNotDisturb = enabled;
            Changed?.Invoke();
        }

        public void MarkRead(IEnumerable<string> notificationIds)
        {
            if (notificationIds == null)
                throw new ArgumentNullException(nameof(notificationIds));

            var ids = new HashSet<string>(
                notificationIds,
                StringComparer.Ordinal);

            bool changed = false;

            foreach (NotificationEntry entry in entries)
            {
                if (!entry.IsRead && ids.Contains(entry.Id))
                {
                    entry.IsRead = true;
                    changed = true;
                }
            }

            if (changed)
                Changed?.Invoke();
        }

        // Removes history only. It does not resolve an Assistant offer.
        public void Dismiss(IEnumerable<string> notificationIds)
        {
            if (notificationIds == null)
                throw new ArgumentNullException(nameof(notificationIds));

            var ids = new HashSet<string>(
                notificationIds,
                StringComparer.Ordinal);

            if (entries.RemoveAll(entry => ids.Contains(entry.Id)) > 0)
                Changed?.Invoke();
        }

        // Register from an app's session-level adapter, not its window.
        public IDisposable RegisterDestination(
            string destinationId,
            Func<NotificationEntry, bool> canOpen,
            Func<NotificationEntry, bool> open)
        {
            Require(destinationId, nameof(destinationId));

            if (canOpen == null)
                throw new ArgumentNullException(nameof(canOpen));

            if (open == null)
                throw new ArgumentNullException(nameof(open));

            if (destinations.ContainsKey(destinationId))
            {
                throw new InvalidOperationException(
                    $"Notification destination '{destinationId}' " +
                    "is already registered for this inbox.");
            }

            var handler = new DestinationHandler
            {
                CanOpen = canOpen,
                Open = open
            };

            destinations.Add(destinationId, handler);
            Changed?.Invoke();

            return new Subscription(() =>
            {
                if (destinations.TryGetValue(
                        destinationId,
                        out DestinationHandler current) &&
                    ReferenceEquals(current, handler))
                {
                    destinations.Remove(destinationId);
                    Changed?.Invoke();
                }
            });
        }

        // Call when route eligibility changes without changing an entry.
        public void RefreshDestinations()
        {
            Changed?.Invoke();
        }

        public bool CanOpen(
            string notificationId,
            Func<string, bool> canAccessApp)
        {
            if (canAccessApp == null)
                throw new ArgumentNullException(nameof(canAccessApp));

            NotificationEntry entry = Find(notificationId);

            if (entry == null || entry.IsResolved)
                return false;

            if (!string.IsNullOrWhiteSpace(entry.AppId) &&
                !canAccessApp(entry.AppId))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(entry.DestinationId))
            {
                return destinations.TryGetValue(
                           entry.DestinationId,
                           out DestinationHandler handler) &&
                       handler.CanOpen(entry);
            }

            return !string.IsNullOrWhiteSpace(entry.AppId);
        }

        public bool TryOpen(
            string notificationId,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            if (launchApp == null)
                throw new ArgumentNullException(nameof(launchApp));

            if (opening || !CanOpen(notificationId, canAccessApp))
                return false;

            NotificationEntry entry = Find(notificationId);

            if (entry == null)
                return false;

            opening = true;

            try
            {
                bool opened;

                if (!string.IsNullOrWhiteSpace(entry.DestinationId))
                {
                    if (!destinations.TryGetValue(
                            entry.DestinationId,
                            out DestinationHandler handler))
                    {
                        return false;
                    }

                    // The adapter revalidates its current domain state.
                    opened = handler.Open(entry);
                }
                else
                {
                    launchApp(entry.AppId);
                    opened = true;
                }

                if (opened)
                    MarkRead(new[] { entry.Id });

                return opened;
            }
            finally
            {
                opening = false;
            }
        }

        private static void Require(string value, string parameter)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "A non-empty value is required.",
                    parameter);
            }
        }
    }

    public static class NotificationService
    {
        private static readonly Dictionary<string, NotificationInbox> inboxes =
            new(StringComparer.Ordinal);

        public static NotificationInbox ForUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException(
                    "A notification inbox requires a user ID.",
                    nameof(userId));
            }

            if (!inboxes.TryGetValue(userId, out NotificationInbox inbox))
            {
                inbox = new NotificationInbox();
                inboxes.Add(userId, inbox);
            }

            return inbox;
        }

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            inboxes.Clear();
        }
    }
}
