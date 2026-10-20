using System;
using System.Collections.Generic;
using Atlas.SOS.Notifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notifications
{
    public sealed class NotificationPopupController : IDisposable
    {
        private readonly NotificationInbox inbox;
        private readonly NotificationPopupView view;
        private readonly Func<string, bool> canAccess;
        private readonly Action<string> launch;
        private readonly Func<bool> canShow;
        private readonly List<string> queue = new();
        private readonly IVisualElementScheduledItem ticker;
        private string current;
        private double lastTick;
        private double remaining;
        private bool disposed;

        public NotificationPopupController(VisualElement parent, VisualTreeAsset template,
            NotificationInbox inbox, Func<string, bool> canAccess,
            Action<string> launch, Func<bool> canShow)
        {
            this.inbox = inbox;
            this.canAccess = canAccess;
            this.launch = launch;
            this.canShow = canShow;
            view = new NotificationPopupView(parent, template);
            view.OpenRequested += Open;
            view.CloseRequested += Close;
            inbox.BannerRequested += Enqueue;
            inbox.BannerDismissRequested += Dismiss;
            inbox.Changed += OnChanged;
            lastTick = Time.realtimeSinceStartupAsDouble;
            ticker = parent.schedule.Execute(Tick).Every(50);
        }

        private bool Eligible(NotificationEntry entry) => entry != null && !entry.IsResolved &&
            !inbox.IsBeingViewed && (!inbox.DoNotDisturb || entry.IsPriority) &&
            (string.IsNullOrWhiteSpace(entry.AppId) || canAccess(entry.AppId));

        private void Enqueue(NotificationEntry entry)
        {
            if (disposed || !canShow() || !Eligible(entry) ||
                entry.Id == current || queue.Contains(entry.Id)) return;
            queue.Add(entry.Id);
            OnChanged();
        }

        private void OnChanged()
        {
            if (disposed) return;
            if (!canShow() || inbox.IsBeingViewed) { Clear(); return; }
            queue.RemoveAll(id => !Eligible(inbox.Find(id)));
            if (current != null && !Eligible(inbox.Find(current)))
            {
                current = null;
                view.Hide();
            }
            if (current == null && queue.Count > 0)
            {
                current = queue[0];
                queue.RemoveAt(0);
                remaining = 12;
                lastTick = Time.realtimeSinceStartupAsDouble;
                view.Render(inbox.Find(current), inbox.CanOpen(current, canAccess));
                view.Show();
            }
            else if (current != null)
                view.Render(inbox.Find(current), inbox.CanOpen(current, canAccess));
        }

        private void Dismiss(string id)
        {
            queue.Remove(id);
            if (current != id) return;
            current = null;
            view.Hide();
            OnChanged();
        }

        private void Tick()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            double elapsed = now - lastTick;
            lastTick = now;
            if (!canShow()) { Clear(); return; }
            if (current == null) return;
            NotificationEntry entry = inbox.Find(current);
            if (!Eligible(entry)) { OnChanged(); return; }
            if (Time.timeScale == 0 || entry.RequireInteraction || view.IsInteracting) return;
            remaining -= elapsed;
            if (remaining <= 0) Close();
        }

        private void Open()
        {
            string id = current;
            if (id != null && canShow() && inbox.TryOpen(id, canAccess, launch) && current == id)
                Close();
        }

        private void Close()
        {
            current = null;
            view.Hide();
            OnChanged();
        }

        public void Clear()
        {
            current = null;
            queue.Clear();
            view.Hide();
        }

        public void Dispose()
        {
            disposed = true;
            ticker.Pause();
            inbox.BannerRequested -= Enqueue;
            inbox.BannerDismissRequested -= Dismiss;
            inbox.Changed -= OnChanged;
            view.OpenRequested -= Open;
            view.CloseRequested -= Close;
            queue.Clear();
            view.Dispose();
        }
    }
}
