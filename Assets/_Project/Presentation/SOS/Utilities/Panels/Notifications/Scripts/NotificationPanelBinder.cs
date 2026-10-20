using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notifications
{
    public sealed class NotificationPanelBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Button DndButton { get; }
        public Button ClearAllButton { get; }
        public Button SettingsButton { get; }

        public VisualElement DndBanner { get; }
        public ScrollView Scroll { get; }
        public VisualElement Groups { get; }
        public VisualElement Empty { get; }

        public NotificationPanelBinder(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            Root = Bind<VisualElement>(root, "NotificationPanel");
            DndButton = Bind<Button>(root, "NotificationDndButton");
            ClearAllButton = Bind<Button>(
                root,
                "NotificationClearAllButton");
            SettingsButton = Bind<Button>(
                root,
                "NotificationSettingsButton");

            DndBanner = Bind<VisualElement>(
                root,
                "NotificationDndBanner");
            Scroll = Bind<ScrollView>(root, "NotificationScroll");
            Groups = Bind<VisualElement>(root, "NotificationGroups");
            Empty = Bind<VisualElement>(root, "NotificationEmpty");
        }
    }
}
