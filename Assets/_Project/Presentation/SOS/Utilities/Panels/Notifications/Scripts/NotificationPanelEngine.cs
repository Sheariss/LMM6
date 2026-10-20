using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.SOS.Notifications;

namespace Atlas.Presentation.SOS.Notifications
{
    public sealed class NotificationGroup
    {
        public string SourceId { get; }
        public IReadOnlyList<NotificationEntry> Entries { get; }
        public NotificationEntry Latest => Entries[0];

        public NotificationGroup(
            string sourceId,
            IReadOnlyList<NotificationEntry> entries)
        {
            SourceId = sourceId;
            Entries = entries;
        }
    }

    public sealed class NotificationPanelEngine : IDisposable
    {
        private readonly NotificationInbox inbox;
        private readonly Func<string, bool> canAccessApp;

        private readonly HashSet<string> expanded =
            new(StringComparer.Ordinal);
        private readonly HashSet<string> expandedCards = new(StringComparer.Ordinal);

        public string MenuSourceId { get; private set; }
        public bool DoNotDisturb => inbox.DoNotDisturb;

        public event Action Changed;

        public NotificationPanelEngine(
            NotificationInbox inbox,
            Func<string, bool> canAccessApp)
        {
            this.inbox = inbox ??
                throw new ArgumentNullException(nameof(inbox));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            inbox.Changed += OnInboxChanged;
        }

        public IReadOnlyList<NotificationEntry> GetEntries()
        {
            return inbox.GetSnapshot()
                .Where(entry =>
                    string.IsNullOrWhiteSpace(entry.AppId) ||
                    canAccessApp(entry.AppId))
                .ToArray();
        }

        public IReadOnlyList<NotificationGroup> GetGroups()
        {
            return GetEntries()
                .GroupBy(entry => entry.SourceId, StringComparer.Ordinal)
                .Select(group => new NotificationGroup(
                    group.Key,
                    group.ToArray()))
                .ToArray();
        }

        public bool IsExpanded(string sourceId)
        {
            return expanded.Contains(sourceId);
        }

        public void ToggleExpanded(string sourceId)
        {
            if (!expanded.Add(sourceId))
                expanded.Remove(sourceId);

            MenuSourceId = null;
            Changed?.Invoke();
        }

        public void ToggleMenu(string sourceId)
        {
            MenuSourceId = MenuSourceId == sourceId
                ? null
                : sourceId;

            Changed?.Invoke();
        }

        public bool CloseMenu()
        {
            if (MenuSourceId == null)
                return false;

            MenuSourceId = null;
            Changed?.Invoke();
            return true;
        }

        public void ResetNavigation()
        {
            expanded.Clear();
            expandedCards.Clear();
            MenuSourceId = null;
        }

        public bool IsCardExpanded(string id) => expandedCards.Contains(id);

        public void ToggleCard(string id)
        {
            if (!GetEntries().Any(entry => entry.Id == id))
                return;
            if (!expandedCards.Add(id))
                expandedCards.Remove(id);
            Changed?.Invoke();
        }

        public void ToggleDoNotDisturb()
        {
            inbox.SetDoNotDisturb(!inbox.DoNotDisturb);
        }

        // Treat opening the inbox as seeing all accessible entries.
        public void MarkAllRead()
        {
            inbox.MarkRead(GetEntries().Select(entry => entry.Id));
        }

        public void Dismiss(string notificationId)
        {
            inbox.Dismiss(GetEntries()
                .Where(entry => entry.Id == notificationId)
                .Select(entry => entry.Id));
        }

        public void ClearGroup(string sourceId)
        {
            MenuSourceId = null;

            inbox.Dismiss(GetEntries()
                .Where(entry => entry.SourceId == sourceId)
                .Select(entry => entry.Id));
        }

        public void ClearAll()
        {
            MenuSourceId = null;
            inbox.Dismiss(GetEntries().Select(entry => entry.Id));
        }

        public bool CanOpen(string notificationId)
        {
            return inbox.CanOpen(notificationId, canAccessApp);
        }

        public bool TryOpen(
            string notificationId,
            Action<string> launchApp)
        {
            // Revalidate access and destination at click time.
            if (!GetEntries().Any(entry => entry.Id == notificationId))
                return false;

            return inbox.TryOpen(
                notificationId,
                canAccessApp,
                launchApp);
        }

        private void OnInboxChanged()
        {
            var sources = new HashSet<string>(
                GetEntries().Select(entry => entry.SourceId),
                StringComparer.Ordinal);

            expanded.RemoveWhere(id => !sources.Contains(id));
            var ids = new HashSet<string>(GetEntries().Select(entry => entry.Id));
            expandedCards.RemoveWhere(id => !ids.Contains(id));

            if (MenuSourceId != null && !sources.Contains(MenuSourceId))
                MenuSourceId = null;

            Changed?.Invoke();
        }

        public void Dispose()
        {
            inbox.Changed -= OnInboxChanged;
            Changed = null;
        }
    }
}
