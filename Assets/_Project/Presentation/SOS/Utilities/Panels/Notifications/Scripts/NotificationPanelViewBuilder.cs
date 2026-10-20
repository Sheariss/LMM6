using System;
using Atlas.SOS.Notifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notifications
{
    public sealed class NotificationPanelViewBuilder
    {
        public NotificationPanelView Build(VisualTreeAsset template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            TemplateContainer host = template.CloneTree();

            host.name = "NotificationPanelHost";
            host.pickingMode = PickingMode.Ignore;

            host.style.position = Position.Absolute;
            host.style.left = 12;
            host.style.right = 12;
            host.style.top = 12;
            host.style.bottom = 12;
            host.style.display = DisplayStyle.None;

            var binder = new NotificationPanelBinder(host);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "The notification panel is missing required elements.");
            }

            binder.Root.pickingMode = PickingMode.Position;

            return new NotificationPanelView(host, binder, this);
        }

        public VisualElement BuildGroup(
            NotificationGroup group,
            NotificationPanelEngine engine,
            bool settingsAvailable,
            Action<string> expand,
            Action<string> expandCard,
            Action<string> menu,
            Action<string> clearGroup,
            Action<string> dismiss,
            Action<string> open,
            Action settings)
        {
            string sourceId = group.SourceId;
            bool expanded = engine.IsExpanded(sourceId);

            VisualElement root = Element("notification-group");
            VisualElement header = Element("notification-group-header");

            var appIcon = new Image
            {
                sprite = group.Latest.Icon,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };

            appIcon.AddToClassList("notification-app-icon");

            if (group.Latest.Icon == null)
                appIcon.AddToClassList("notification-app-icon--fallback");

            header.Add(appIcon);
            header.Add(Text(
                group.Latest.SourceName,
                "notification-source"));

            var controls = Element("notification-header-controls");
            controls.Add(IconButton(
                "menu-" + sourceId,
                "Notification group actions",
                "notification-more-icon",
                () => menu(sourceId)));
            controls.Add(IconButton(
                "clear-group-" + sourceId,
                "Clear this app's notification history",
                "notification-close-icon",
                () => clearGroup(sourceId)));
            header.Add(controls);
            header.RegisterCallback<FocusInEvent>(_ =>
                header.AddToClassList("has-focus"));
            header.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (!(evt.relatedTarget is VisualElement target) || !header.Contains(target))
                    header.RemoveFromClassList("has-focus");
            });

            root.Add(header);

            if (engine.MenuSourceId == sourceId)
            {
                VisualElement actions = Element("notification-menu");

                actions.Add(TextButton(
                    "clear-group-" + sourceId,
                    "Clear this group",
                    () => clearGroup(sourceId)));

                Button settingsButton = TextButton(
                    "settings-" + sourceId,
                    "Notification settings",
                    settings);

                settingsButton.SetEnabled(settingsAvailable);
                actions.Add(settingsButton);

                root.Add(actions);
            }

            int count = expanded ? group.Entries.Count : 1;

            for (int i = 0; i < count; i++)
            {
                VisualElement card = BuildCard(
                    group.Entries[i],
                    engine,
                    expandCard,
                    dismiss,
                    open);
                if (i == 0 && !expanded && group.Entries.Count > 1)
                {
                    Button more = TextButton(
                        "count-" + sourceId,
                        $"+{group.Entries.Count - 1} " + (group.Entries.Count == 2 ? "notification" : "notifications"),
                        () => expand(sourceId));
                    more.AddToClassList("notification-count");
                    card.Add(more);
                }
                root.Add(card);
            }

            return root;
        }

        private VisualElement BuildCard(
            NotificationEntry entry,
            NotificationPanelEngine engine,
            Action<string> expandCard,
            Action<string> dismiss,
            Action<string> open)
        {
            VisualElement card = Element("notification-card");
            card.name = "notification-card-" + entry.Id;
            bool expanded = engine.IsCardExpanded(entry.Id);
            bool available = engine.CanOpen(entry.Id);
            card.EnableInClassList("is-collapsed", !expanded);
            card.EnableInClassList("can-open", available);
            card.focusable = available;
            card.tabIndex = available ? 0 : -1;
            card.tooltip = available ? "Open in app" : "This destination is currently unavailable.";
            card.RegisterCallback<ClickEvent>(evt =>
            {
                if (available && !IsControl(evt.target as VisualElement, card))
                    open(entry.Id);
            });
            card.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.target != card || !available ||
                    (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.Space))
                    return;
                evt.StopPropagation();
                open(entry.Id);
            });
            VisualElement header = Element("notification-card-header");

            header.Add(Text(
                FormatTime(entry.CreatedUtc),
                "notification-time"));

            Button toggle = IconButton(
                "expand-card-" + entry.Id,
                expanded ? "Collapse notification" : "Expand notification",
                "notification-expand-icon",
                () => expandCard(entry.Id));
            toggle.Q<Image>().EnableInClassList("is-expanded", expanded);
            header.Add(toggle);

            card.Add(header);
            var content = Element("notification-content");
            if (entry.Layout == NotificationLayout.Message || entry.Layout == NotificationLayout.Email)
            {
                var portrait = new Image
                {
                    sprite = entry.ActorImage ?? entry.Icon,
                    scaleMode = ScaleMode.ScaleToFit,
                    pickingMode = PickingMode.Ignore
                };
                portrait.AddToClassList("notification-portrait");
                if (portrait.sprite != null)
                    content.Add(portrait);
            }
            var body = Element("notification-body");
            if (!string.IsNullOrWhiteSpace(entry.ActorName))
                body.Add(Text(entry.ActorName, "notification-title"));
            body.Add(Text(entry.Title, "notification-title"));

            if (!string.IsNullOrWhiteSpace(entry.Message))
                body.Add(Text(entry.Message, "notification-message"));
            content.Add(body);
            card.Add(content);

            if (entry.IsResolved)
            {
                card.Add(Text(
                    entry.ResolutionText,
                    "notification-resolution"));
            }
            else if (expanded && !string.IsNullOrWhiteSpace(entry.OpenLabel) &&
                     (!string.IsNullOrWhiteSpace(entry.AppId) ||
                      !string.IsNullOrWhiteSpace(entry.DestinationId)))
            {
                Button openButton = TextButton(
                    "open-" + entry.Id,
                    entry.OpenLabel,
                    () => open(entry.Id));

                openButton.SetEnabled(available);
                openButton.tooltip = available
                    ? "Open this notification"
                    : "This destination is currently unavailable.";

                openButton.AddToClassList("notification-open");
                card.Add(openButton);
            }

            return card;
        }

        private static bool IsControl(VisualElement target, VisualElement card)
        {
            for (var element = target; element != null && element != card; element = element.parent)
                if (element is Button || element is TextField || element is DropdownField)
                    return true;
            return false;
        }

        private static VisualElement Element(string className)
        {
            var element = new VisualElement();
            element.AddToClassList(className);
            return element;
        }

        private static Label Text(string value, string className)
        {
            var label = new Label(value ?? string.Empty)
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList(className);
            return label;
        }

        private static Button TextButton(
            string name,
            string text,
            Action clicked)
        {
            var button = new Button(clicked)
            {
                name = name,
                text = text
            };

            button.AddToClassList("notification-button");
            return button;
        }

        private static Button IconButton(
            string name,
            string tooltip,
            string iconClass,
            Action clicked)
        {
            Button button = TextButton(name, string.Empty, clicked);

            button.tooltip = tooltip;
            button.AddToClassList("notification-icon-button");

            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };

            image.AddToClassList("notification-icon");
            image.AddToClassList(iconClass);

            button.Add(image);
            return button;
        }

        private static string FormatTime(DateTimeOffset createdUtc)
        {
            DateTime local = createdUtc.LocalDateTime;
            DateTime today = DateTime.Now.Date;

            if (local.Date == today)
                return local.ToString("t");

            if (local.Date == today.AddDays(-1))
                return "Yesterday";

            return local.ToString("ddd, MMM d");
        }
    }
}
