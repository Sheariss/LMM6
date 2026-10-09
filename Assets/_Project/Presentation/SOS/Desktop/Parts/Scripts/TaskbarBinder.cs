using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarBinder : UIBinder
    {
        public VisualElement Root { get; }

        public GroupBox WidgetGroup { get; }
        public Button WidgetsButton { get; }
        public Image WidgetIcon { get; }
        public Label WidgetPrimaryLabel { get; }
        public Label WidgetSecondaryLabel { get; }

        public VisualElement MiddleSection { get; }
        public Button StartButton { get; }
        public VisualElement TaskbarSearchContainer { get; }
        public Image SearchIcon { get; }
        public TextField TaskbarSearchField { get; }
        public GroupBox AppGroup { get; }

        public GroupBox StatusGroup { get; }
        public Button HiddenIconsButton { get; }
        public Button AssistantButton { get; }
        public Image AssistantIcon { get; }
        public Button NetworkButton { get; }
        public Image NetworkIcon { get; }
        public Button VolumeButton { get; }
        public Image VolumeIcon { get; }
        public Button DateTimeButton { get; }
        public Label TimeLabel { get; }
        public Label DateLabel { get; }
        public Button NotificationsButton { get; }
        public Image NotificationsIcon { get; }
        public VisualElement NotificationIndicator { get; }

        public TaskbarBinder(VisualElement taskbarRoot)
        {
            if (taskbarRoot == null)
            {
                Debug.LogError(
                    $"[{nameof(TaskbarBinder)}] Taskbar root is null."
                );

                IsValid = false;
                return;
            }

            Root = taskbarRoot;

            WidgetGroup = Bind<GroupBox>(Root, "WidgetGroup");
            WidgetsButton = Bind<Button>(Root, "WidgetsButton");
            WidgetIcon = Bind<Image>(Root, "WidgetIcon");
            WidgetPrimaryLabel = Bind<Label>(Root, "WidgetPrimaryLabel");
            WidgetSecondaryLabel = Bind<Label>(Root, "WidgetSecondaryLabel");

            MiddleSection = Bind<VisualElement>(Root, "MiddleSection");
            StartButton = Bind<Button>(Root, "StartButton");
            TaskbarSearchContainer =
                Bind<VisualElement>(Root, "TaskbarSearchContainer");
            SearchIcon = Bind<Image>(Root, "SearchIcon");
            TaskbarSearchField =
                Bind<TextField>(Root, "TaskbarSearchField");
            AppGroup = Bind<GroupBox>(Root, "AppGroup");

            StatusGroup = Bind<GroupBox>(Root, "StatusGroup");
            HiddenIconsButton = Bind<Button>(Root, "HiddenIconsButton");
            AssistantButton = Bind<Button>(Root, "AssistantButton");
            AssistantIcon = Bind<Image>(Root, "AssistantIcon");
            NetworkButton = Bind<Button>(Root, "NetworkButton");
            NetworkIcon = Bind<Image>(Root, "NetworkIcon");
            VolumeButton = Bind<Button>(Root, "VolumeButton");
            VolumeIcon = Bind<Image>(Root, "VolumeIcon");
            DateTimeButton = Bind<Button>(Root, "DateTimeButton");
            TimeLabel = Bind<Label>(Root, "TimeLabel");
            DateLabel = Bind<Label>(Root, "DateLabel");
            NotificationsButton = Bind<Button>(Root, "NotificationsButton");
            NotificationsIcon = Bind<Image>(Root, "NotificationsIcon");
            NotificationIndicator =
                Bind<VisualElement>(Root, "NotificationIndicator");
        }
    }
}