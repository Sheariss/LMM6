using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.DateAndTime
{
    public sealed class DateTimePanelBinder : UIBinder
    {
        public VisualElement Root { get; }

        public Label CurrentTime { get; }
        public Label CurrentDate { get; }
        public Label Timezone { get; }

        public Button CollapseButton { get; }
        public ScrollView Body { get; }

        public Label Month { get; }
        public Button PreviousMonthButton { get; }
        public Button NextMonthButton { get; }
        public VisualElement DaysGrid { get; }

        public Button DecreaseFocusButton { get; }
        public Button IncreaseFocusButton { get; }
        public Label FocusDuration { get; }

        public Button StartFocusButton { get; }
        public Image StartFocusIcon { get; }
        public Label StartFocusLabel { get; }
        public Label FocusStatus { get; }

        public DateTimePanelBinder(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            Root = Bind<VisualElement>(
                root, "DateTimePanel");

            CurrentTime = Bind<Label>(
                root, "CurrentTimeLabel");

            CurrentDate = Bind<Label>(
                root, "CurrentDateLabel");

            Timezone = Bind<Label>(
                root, "CalendarTimezoneLabel");

            CollapseButton = Bind<Button>(
                root, "CollapseCalendarButton");

            Body = Bind<ScrollView>(
                root, "CalendarBody");

            Month = Bind<Label>(
                root, "MonthLabel");

            PreviousMonthButton = Bind<Button>(
                root, "PreviousMonthButton");

            NextMonthButton = Bind<Button>(
                root, "NextMonthButton");

            DaysGrid = Bind<VisualElement>(
                root, "DaysGrid");

            DecreaseFocusButton = Bind<Button>(
                root, "DecreaseFocusButton");

            IncreaseFocusButton = Bind<Button>(
                root, "IncreaseFocusButton");

            FocusDuration = Bind<Label>(
                root, "FocusDurationLabel");

            StartFocusButton = Bind<Button>(
                root, "StartFocusButton");

            StartFocusIcon = Bind<Image>(
                root, "StartFocusIcon");

            StartFocusLabel = Bind<Label>(
                root, "StartFocusLabel");

            FocusStatus = Bind<Label>(
                root, "FocusStatusLabel");
        }
    }
}