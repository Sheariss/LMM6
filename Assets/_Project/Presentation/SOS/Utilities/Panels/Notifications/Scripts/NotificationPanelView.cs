using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notifications
{
    public sealed class NotificationPanelView
    {
        private readonly NotificationPanelViewBuilder builder;
        private int renderVersion;

        public TemplateContainer Host { get; }
        public NotificationPanelBinder Binder { get; }

        public event Action<string> ExpandRequested;
        public event Action<string> MenuRequested;
        public event Action<string> ClearGroupRequested;
        public event Action<string> DismissRequested;
        public event Action<string> OpenRequested;
        public event Action SettingsRequested;

        public NotificationPanelView(
            TemplateContainer host,
            NotificationPanelBinder binder,
            NotificationPanelViewBuilder builder)
        {
            Host = host;
            Binder = binder;
            this.builder = builder;
        }

        public void Render(
            NotificationPanelEngine engine,
            bool settingsAvailable)
        {
            var focused = Binder.Root.panel?.focusController
                .focusedElement as VisualElement;

            bool restoreFocus =
                focused != null &&
                Binder.Groups.Contains(focused);

            string focusName = restoreFocus ? focused.name : null;
            Vector2 scroll = Binder.Scroll.scrollOffset;

            var groups = engine.GetGroups();
            bool hasEntries = groups.Count > 0;

            Binder.DndButton.EnableInClassList(
                "notification-dnd-active",
                engine.DoNotDisturb);

            Binder.DndButton.tooltip = engine.DoNotDisturb
                ? "Turn do not disturb off"
                : "Turn do not disturb on";

            SetVisible(Binder.DndBanner, engine.DoNotDisturb);
            SetVisible(Binder.Scroll, hasEntries);
            SetVisible(Binder.Empty, !hasEntries);

            Binder.ClearAllButton.SetEnabled(hasEntries);
            Binder.SettingsButton.SetEnabled(settingsAvailable);

            Binder.Groups.Clear();

            foreach (NotificationGroup group in groups)
            {
                Binder.Groups.Add(builder.BuildGroup(
                    group,
                    engine,
                    settingsAvailable,
                    id => ExpandRequested?.Invoke(id),
                    id => MenuRequested?.Invoke(id),
                    id => ClearGroupRequested?.Invoke(id),
                    id => DismissRequested?.Invoke(id),
                    id => OpenRequested?.Invoke(id),
                    () => SettingsRequested?.Invoke()));
            }

            int version = ++renderVersion;

            Binder.Root.schedule.Execute(() =>
            {
                if (version != renderVersion ||
                    Host.panel == null ||
                    Host.resolvedStyle.display == DisplayStyle.None)
                {
                    return;
                }

                Binder.Scroll.scrollOffset = scroll;

                if (restoreFocus)
                {
                    VisualElement target =
                        string.IsNullOrWhiteSpace(focusName)
                            ? null
                            : Binder.Groups.Q<VisualElement>(focusName);

                    if (target != null && target.enabledInHierarchy)
                        target.Focus();
                    else
                        Binder.Root.Focus();
                }
            });
        }

        public void Show()
        {
            Host.style.display = DisplayStyle.Flex;
            Host.BringToFront();
            Binder.Root.Focus();
        }

        public void Hide()
        {
            Host.style.display = DisplayStyle.None;
        }

        public void Detach()
        {
            renderVersion++;
            Host.RemoveFromHierarchy();
        }

        private static void SetVisible(
            VisualElement element,
            bool visible)
        {
            element.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}
