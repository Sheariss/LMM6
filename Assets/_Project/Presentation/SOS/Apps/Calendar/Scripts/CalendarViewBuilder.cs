using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Calendar
{
    public sealed class CalendarViewBuilder
    {
        private readonly List<Action> cleanup = new();

        public void ClearBindings()
        {
            foreach (Action action in cleanup)
                action();

            cleanup.Clear();
        }

        public void BuildMonth(
            VisualElement target,
            CalendarEngine engine,
            Action<DateTime> selectDay,
            Action<CalendarEvent> selectEvent)
        {
            DateTime first = engine.MonthStart;
            DateTime gridStart = first.AddDays(-(int)first.DayOfWeek);

            for (int week = 0; week < 6; week++)
            {
                VisualElement row = Element("cal-week-row");
                target.Add(row);

                for (int weekday = 0; weekday < 7; weekday++)
                {
                    DateTime date = gridStart.AddDays(week * 7 + weekday);

                    VisualElement cell = Element("cal-day");
                    cell.name = "Day" +
                        date.ToString("yyyyMMdd", CalendarEngine.Culture);

                    cell.EnableInClassList(
                        "cal-day--outside",
                        date.Month != first.Month ||
                        date.Year != first.Year);

                    cell.EnableInClassList(
                        "cal-day--today", date == engine.Today);

                    cell.EnableInClassList(
                        "cal-day--selected",
                        date == engine.AnchorDate);

                    Button dayButton = Button(
                        date.Day.ToString(CalendarEngine.Culture),
                        () => selectDay(date),
                        "cal-button",
                        "cal-day-number");

                    dayButton.tooltip = date.ToString(
                        "dddd, d MMMM yyyy", CalendarEngine.Culture);

                    dayButton.SetEnabled(
                        date >= CalendarEngine.MinimumDate &&
                        date <= CalendarEngine.MaximumDate);

                    cell.Add(dayButton);

                    List<CalendarEvent> events = engine.EventsOn(date);

                    foreach (CalendarEvent item in events.Take(2))
                        cell.Add(EventChip(item, selectEvent));

                    if (events.Count > 2)
                    {
                        cell.Add(Button(
                            $"+{events.Count - 2} more",
                            () => selectDay(date),
                            "cal-button",
                            "cal-more-events"));
                    }

                    row.Add(cell);
                }
            }
        }

        public void BuildSchedule(
            CalendarBinder binder,
            CalendarEngine engine,
            Action<DateTime> selectDay,
            Action<CalendarEvent> selectEvent)
        {
            int count = engine.Mode == CalendarViewMode.Week ? 7 : 1;

            DateTime start = count == 7
                ? engine.WeekStart
                : engine.AnchorDate;

            List<CalendarEvent>[] dailyEvents =
                new List<CalendarEvent>[count];

            binder.ScheduleHeader.Add(HourLabel(string.Empty));
            binder.AllDayEvents.Add(HourLabel("All day"));

            for (int day = 0; day < count; day++)
            {
                DateTime date = start.AddDays(day);

                dailyEvents[day] = engine.EventsOn(date);

                Button heading = Button(
                    date.ToString("ddd d MMM", CalendarEngine.Culture),
                    () => selectDay(date),
                    "cal-button",
                    "cal-schedule-day-heading");

                heading.SetEnabled(
                    date >= CalendarEngine.MinimumDate &&
                    date <= CalendarEngine.MaximumDate);

                if (date == engine.Today)
                    heading.tooltip = "Today in the scenario";

                binder.ScheduleHeader.Add(heading);

                VisualElement allDayColumn = Element("cal-time-slot");

                foreach (CalendarEvent item in
                         dailyEvents[day].Where(item => item.AllDay))
                {
                    allDayColumn.Add(EventChip(item, selectEvent));
                }

                binder.AllDayEvents.Add(allDayColumn);
            }

            for (int hour = 0; hour < 24; hour++)
            {
                int selectedHour = hour;

                VisualElement row = Element("cal-hour-row");

                // Each hour expands if it contains several events.
                row.style.height = StyleKeyword.Auto;
                row.style.minHeight = 60;

                row.Add(HourLabel($"{hour:00}:00"));

                for (int day = 0; day < count; day++)
                {
                    DateTime date = start.AddDays(day);
                    VisualElement slot = Element("cal-time-slot");

                    slot.style.paddingTop = 4;
                    slot.style.paddingLeft = 3;
                    slot.style.paddingRight = 3;

                    foreach (CalendarEvent item in dailyEvents[day])
                    {
                        if (item.AllDay)
                            continue;

                        DateTime visibleStart =
                            item.Start < date ? date : item.Start;

                        if (visibleStart.Hour != selectedHour)
                            continue;

                        string caption = item.Start < date
                            ? "Continues · " + item.Title
                            : item.Start.ToString(
                                "HH:mm", CalendarEngine.Culture) +
                              "  " + item.Title;

                        slot.Add(EventChip(
                            item, selectEvent, caption));
                    }

                    row.Add(slot);
                }

                binder.ScheduleGrid.Add(row);
            }
        }

        public void BuildAgenda(
            VisualElement target,
            IEnumerable<CalendarEvent> events,
            Action<CalendarEvent> selectEvent)
        {
            foreach (CalendarEvent item in events)
            {
                Button row = Button(
                    string.Empty,
                    () => selectEvent(item),
                    "cal-button",
                    "cal-agenda-row");

                row.tooltip = DetailsTooltip(item);

                row.Add(Label(
                    item.Start.ToString(
                        "ddd d MMM", CalendarEngine.Culture),
                    "cal-agenda-date"));

                row.Add(Label(
                    item.AllDay
                        ? "All day"
                        : item.Start.ToString(
                            "HH:mm", CalendarEngine.Culture) +
                          " – " +
                          item.End.ToString(
                              item.Start.Date == item.End.Date
                                  ? "HH:mm"
                                  : "d MMM HH:mm",
                              CalendarEngine.Culture),
                    "cal-agenda-time"));

                row.Add(Label(item.Title, "cal-agenda-title"));

                target.Add(row);
            }
        }

        public void BuildUpcoming(
            VisualElement target,
            CalendarEngine engine,
            Action<CalendarEvent> selectEvent)
        {
            List<CalendarEvent> events = engine.UpcomingEvents();

            if (events.Count == 0)
            {
                target.Add(Label(
                    "No upcoming events.", "cal-caption"));

                return;
            }

            foreach (CalendarEvent item in events)
            {
                Button card = Button(
                    string.Empty,
                    () => selectEvent(item),
                    "cal-button",
                    "cal-upcoming-card");

                card.tooltip = DetailsTooltip(item);

                string date = item.Start.Date == engine.Today
                    ? "TODAY"
                    : item.Start.ToString(
                        "ddd d MMM", CalendarEngine.Culture);

                string time = item.AllDay
                    ? "ALL DAY"
                    : item.Start.ToString(
                        "HH:mm", CalendarEngine.Culture);

                string status = item.Start <= engine.Now &&
                                item.End > engine.Now
                    ? "ONGOING · "
                    : string.Empty;

                card.Add(Label(
                    status + date + " · " + time,
                    "cal-caption"));

                card.Add(Label(item.Title, "cal-upcoming-title"));

                card.Add(Label(
                    item.Category +
                    (item.Location.Length > 0
                        ? " · " + item.Location
                        : string.Empty),
                    "cal-caption"));

                target.Add(card);
            }
        }

        private Button EventChip(
            CalendarEvent item,
            Action<CalendarEvent> selectEvent,
            string caption = null)
        {
            caption ??= item.AllDay
                ? item.Title
                : item.Start.ToString(
                    "HH:mm", CalendarEngine.Culture) +
                  "  " + item.Title;

            string categoryClass = item.Category switch
            {
                Atlas.SOS.Calendar.CalendarCategory.Personal =>
                    "cal-event--personal",

                Atlas.SOS.Calendar.CalendarCategory.Work =>
                    "cal-event--work",

                _ => "cal-event--reminder"
            };

            Button button = Button(
                caption,
                () => selectEvent(item),
                "cal-button",
                "cal-event",
                categoryClass);

            button.tooltip = DetailsTooltip(item);

            return button;
        }

        private Button Button(
            string text,
            Action action,
            params string[] classes)
        {
            Button button = new()
            {
                text = text,
                enableRichText = false
            };

            foreach (string className in classes)
                button.AddToClassList(className);

            button.clicked += action;
            cleanup.Add(() => button.clicked -= action);

            return button;
        }

        private static VisualElement Element(string className)
        {
            VisualElement element = new();
            element.AddToClassList(className);
            return element;
        }

        private static Label Label(string text, string className)
        {
            Label label = new(text)
            {
                pickingMode = PickingMode.Ignore,
                enableRichText = false
            };

            label.AddToClassList(className);
            return label;
        }

        private static Label HourLabel(string text)
        {
            return Label(text, "cal-hour-label");
        }

        private static string DetailsTooltip(CalendarEvent item)
        {
            DateTime displayEnd = item.AllDay
                ? item.End.AddDays(-1)
                : item.End;

            string format = item.AllDay
                ? "yyyy-MM-dd"
                : "yyyy-MM-dd HH:mm";

            return item.Title + "\n" +
                   item.Start.ToString(format, CalendarEngine.Culture) +
                   " – " +
                   displayEnd.ToString(format, CalendarEngine.Culture) +
                   "\n" + item.Category +
                   (item.Location.Length > 0
                       ? "\n" + item.Location
                       : string.Empty);
        }
    }
}