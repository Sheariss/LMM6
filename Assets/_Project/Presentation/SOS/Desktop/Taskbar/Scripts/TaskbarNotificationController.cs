using System;
using System.Linq;
using Atlas.SOS.Notifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarNotificationController : IDisposable
    {
        private readonly TaskbarBinder binder;
        private readonly NotificationInbox inbox;
        private readonly Func<string, bool> canAccessApp;

        private readonly Texture2D normalIcon;
        private readonly Texture2D unreadIcon;
        private readonly Texture2D pausedIcon;

        public TaskbarNotificationController(
            TaskbarBinder binder,
            NotificationInbox inbox,
            Func<string, bool> canAccessApp)
        {
            this.binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.inbox = inbox ??
                throw new ArgumentNullException(nameof(inbox));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            normalIcon = Load("Notifications/notifications_32dp");
            unreadIcon = Load(
                "Notifications-Unread/notifications_unread_32dp");
            pausedIcon = Load(
                "Notifications-Paused/notifications_paused_32dp");

            inbox.Changed += Render;
            Render();
        }

        private static Texture2D Load(string suffix)
        {
            Texture2D texture = Resources.Load<Texture2D>(
                "Icons/System/Notifications/" + suffix);

            if (texture == null)
            {
                throw new InvalidOperationException(
                    "Missing notification icon: " + suffix);
            }

            return texture;
        }

        private void Render()
        {
            int unread = inbox.GetSnapshot().Count(entry =>
                !entry.IsRead &&
                (string.IsNullOrWhiteSpace(entry.AppId) ||
                 canAccessApp(entry.AppId)));

            Texture2D texture = inbox.DoNotDisturb
                ? pausedIcon
                : unread > 0 ? unreadIcon : normalIcon;

            binder.NotificationsIcon.image = null;
            binder.NotificationsIcon.style.backgroundImage =
                new StyleBackground(texture);

            binder.NotificationsIcon.style.backgroundSize =
                new BackgroundSize(BackgroundSizeType.Contain);

            binder.NotificationsButton.RemoveFromClassList(
                "has-notifications");

            // The unread image asset supplies the indicator.
            binder.NotificationIndicator.style.display = DisplayStyle.None;

            string status = unread == 0
                ? "No unread notifications"
                : $"{unread} unread notifications";

            binder.NotificationsButton.tooltip = inbox.DoNotDisturb
                ? status + ". Do not disturb is on."
                : status;
        }

        public void Dispose()
        {
            inbox.Changed -= Render;
        }
    }
}
