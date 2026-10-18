using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Calendar
{
    public sealed class CalendarBinder : UIBinder
    {
        public VisualElement Root { get; }

        public Button ToggleSidebar { get; }
        public Button Today { get; }
        public Button Previous { get; }
        public Button Next { get; }
        public Label Period { get; }
        public TextField Search { get; }

        // Order matches CalendarViewMode.
        public Button[] ViewButtons { get; }

        public VisualElement Sidebar { get; }
        public Button CreateEvent { get; }
        public Toggle Personal { get; }
        public Toggle Work { get; }
        public Toggle Reminders { get; }
        public VisualElement Upcoming { get; }
        public Label Timezone { get; }

        public VisualElement MonthView { get; }
        public VisualElement MonthGrid { get; }

        public VisualElement ScheduleView { get; }
        public VisualElement ScheduleHeader { get; }
        public VisualElement AllDayEvents { get; }
        public VisualElement ScheduleGrid { get; }
        public ScrollView ScheduleScroll { get; }

        public ScrollView AgendaView { get; }
        public Label AgendaHeading { get; }
        public VisualElement AgendaEvents { get; }
        public Label EmptyAgenda { get; }

        public VisualElement Overlay { get; }
        public Label EditorHeading { get; }
        public Button CloseEditor { get; }
        public Button CancelEditor { get; }
        public Button SaveEvent { get; }
        public Button DeleteEvent { get; }

        public TextField EventTitle { get; }
        public Toggle AllDay { get; }
        public TextField StartDate { get; }
        public TextField StartTime { get; }
        public TextField EndDate { get; }
        public TextField EndTime { get; }
        public DropdownField Category { get; }
        public DropdownField Repeat { get; }
        public TextField Location { get; }
        public TextField Description { get; }
        public Label Validation { get; }

        public CalendarBinder(VisualElement instanceRoot)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));

            Root = Bind<VisualElement>(instanceRoot, "CalendarRoot");

            // UIBinder logs the missing root and sets IsValid = false.
            // Avoid querying children against a null root.
            if (Root == null)
                return;

            ToggleSidebar = Bind<Button>(Root, "ToggleSidebarButton");
            Today = Bind<Button>(Root, "TodayButton");
            Previous = Bind<Button>(Root, "PreviousPeriodButton");
            Next = Bind<Button>(Root, "NextPeriodButton");
            Period = Bind<Label>(Root, "PeriodLabel");
            Search = Bind<TextField>(Root, "SearchField");

            ViewButtons = new[]
            {
                Bind<Button>(Root, "DayViewButton"),
                Bind<Button>(Root, "WeekViewButton"),
                Bind<Button>(Root, "MonthViewButton"),
                Bind<Button>(Root, "AgendaViewButton")
            };

            Sidebar = Bind<VisualElement>(Root, "CalendarSidebar");
            CreateEvent = Bind<Button>(Root, "CreateEventButton");
            Personal = Bind<Toggle>(Root, "PersonalCalendarToggle");
            Work = Bind<Toggle>(Root, "WorkCalendarToggle");
            Reminders = Bind<Toggle>(Root, "RemindersCalendarToggle");
            Upcoming = Bind<VisualElement>(Root, "UpcomingEventsContainer");
            Timezone = Bind<Label>(Root, "TimezoneLabel");

            MonthView = Bind<VisualElement>(Root, "MonthView");
            MonthGrid = Bind<VisualElement>(Root, "MonthGrid");

            ScheduleView = Bind<VisualElement>(Root, "ScheduleView");
            ScheduleHeader = Bind<VisualElement>(Root, "ScheduleHeader");
            AllDayEvents = Bind<VisualElement>(Root, "AllDayEvents");
            ScheduleGrid = Bind<VisualElement>(Root, "ScheduleGrid");
            ScheduleScroll = Bind<ScrollView>(Root, "ScheduleScroll");

            AgendaView = Bind<ScrollView>(Root, "AgendaView");
            AgendaHeading = Bind<Label>(Root, "AgendaHeading");
            AgendaEvents = Bind<VisualElement>(Root, "AgendaEventsContainer");
            EmptyAgenda = Bind<Label>(Root, "EmptyAgendaLabel");

            Overlay = Bind<VisualElement>(Root, "EventEditorOverlay");
            EditorHeading = Bind<Label>(Root, "EventEditorHeading");
            CloseEditor = Bind<Button>(Root, "CloseEventEditorButton");
            CancelEditor = Bind<Button>(Root, "CancelEventButton");
            SaveEvent = Bind<Button>(Root, "SaveEventButton");
            DeleteEvent = Bind<Button>(Root, "DeleteEventButton");

            EventTitle = Bind<TextField>(Root, "EventTitleField");
            AllDay = Bind<Toggle>(Root, "AllDayToggle");
            StartDate = Bind<TextField>(Root, "StartDateField");
            StartTime = Bind<TextField>(Root, "StartTimeField");
            EndDate = Bind<TextField>(Root, "EndDateField");
            EndTime = Bind<TextField>(Root, "EndTimeField");
            Category = Bind<DropdownField>(Root, "EventCalendarField");
            Repeat = Bind<DropdownField>(Root, "RepeatField");
            Location = Bind<TextField>(Root, "LocationField");
            Description = Bind<TextField>(Root, "DescriptionField");
            Validation = Bind<Label>(Root, "EventValidationLabel");
        }
    }
}