using System;
using Atlas.SOS.Notifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notifications
{
    public sealed class NotificationPopupView : IDisposable
    {
        private readonly TemplateContainer host;
        private readonly VisualElement root;
        private readonly Image icon;
        private readonly Label source;
        private readonly Label title;
        private readonly Label message;
        private readonly Button open;
        private bool hovered;
        private int version;

        public event Action OpenRequested;
        public event Action CloseRequested;
        public bool IsInteracting => hovered ||
            (root.panel?.focusController.focusedElement is VisualElement focused &&
             (focused == root || root.Contains(focused)));

        public NotificationPopupView(VisualElement parent, VisualTreeAsset template)
        {
            host = template.CloneTree();
            host.name = "NotificationPopupHost";
            host.pickingMode = PickingMode.Ignore;
            host.style.position = Position.Absolute;
            host.style.left = host.style.right = host.style.top = host.style.bottom = 0;
            root = host.Q<VisualElement>("NotificationPopup");
            icon = host.Q<Image>("PopupIcon");
            source = host.Q<Label>("PopupSource");
            title = host.Q<Label>("PopupTitle");
            message = host.Q<Label>("PopupMessage");
            open = host.Q<Button>("PopupOpen");
            var close = host.Q<Button>("PopupClose");
            if (root == null || icon == null || source == null || title == null ||
                message == null || open == null || close == null)
                throw new InvalidOperationException("Notification popup template is incomplete.");
            source.enableRichText = title.enableRichText = message.enableRichText = false;
            icon.scaleMode = ScaleMode.ScaleToFit;
            open.clicked += () => OpenRequested?.Invoke();
            close.clicked += () => CloseRequested?.Invoke();
            root.RegisterCallback<PointerEnterEvent>(_ => hovered = true);
            root.RegisterCallback<PointerLeaveEvent>(_ => hovered = false);
            root.RegisterCallback<ClickEvent>(evt =>
            {
                for (var target = evt.target as VisualElement; target != null && target != root; target = target.parent)
                    if (target is Button) return;
                if (open.enabledSelf) OpenRequested?.Invoke();
            });
            parent.Add(host);
            Hide();
        }

        public void Render(NotificationEntry entry, bool available)
        {
            if (entry.Icon != null)
                icon.sprite = entry.Icon;
            else
                icon.image = Resources.Load<Texture2D>("Icons/System/Notifications/Notifications/notifications_32dp");
            source.text = entry.SourceName;
            title.text = entry.Title;
            message.text = entry.Message;
            open.text = string.IsNullOrWhiteSpace(entry.OpenLabel) ? "Open" : entry.OpenLabel;
            open.SetEnabled(available);
            open.tooltip = available ? "Open in app" : "This destination is currently unavailable.";
        }

        public void Show()
        {
            host.style.display = DisplayStyle.Flex;
            host.BringToFront();
            int expected = ++version;
            root.schedule.Execute(() =>
            {
                if (version == expected && host.panel != null)
                    root.AddToClassList("is-visible");
            }).StartingIn(16);
        }

        public void Hide()
        {
            version++;
            hovered = false;
            root.RemoveFromClassList("is-visible");
            host.style.display = DisplayStyle.None;
        }

        public void Dispose()
        {
            Hide();
            OpenRequested = null;
            CloseRequested = null;
            host.RemoveFromHierarchy();
        }
    }
}
