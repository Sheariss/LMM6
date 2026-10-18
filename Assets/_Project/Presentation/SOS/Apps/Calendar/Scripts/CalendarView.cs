using System;
using System.Collections.Generic;
using Atlas.SOS.Calendar;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Calendar
{
    public sealed class CalendarView
    {
        private readonly CalendarBinder binder;
        private readonly CalendarViewBuilder builder;

        private VisualElement previousFocus;

        public bool DetailsOpen { get; private set; }

        public CalendarView(
            CalendarBinder binder,
            CalendarViewBuilder builder)
        {
            this.binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.builder = builder ??
                throw new ArgumentNullException(nameof(builder));

            ConfigureReadOnlyDetails();
        }

        public void Render(
            CalendarEngine engine,
            Action<DateTime> selectDay,
            Action<CalendarEvent> selectEvent)
        {
            ClearDynamicContent();

            CalendarViewMode mode = engine.EffectiveMode;

            Show(binder.Sidebar, engine.SidebarVisible);
            Show(binder.MonthView, mode == CalendarViewMode.Month);

            Show(
                binder.ScheduleView,
                mode == CalendarViewMode.Day ||
                mode == CalendarViewMode.Week);

            Show(binder.AgendaView, mode == CalendarViewMode.Agenda);

            for (int index = 0; index < binder.ViewButtons.Length; index++)
            {
                binder.ViewButtons[index].EnableInClassList(
                    "cal-view-button--selected",
                    index == (int)mode);
            }

            binder.Period.text = engine.PeriodText();
            binder.Search.SetValueWithoutNotify(engine.Search);
            binder.Timezone.text = engine.TimezoneLabel;

            binder.Previous.SetEnabled(engine.CanMove(-1));
            binder.Next.SetEnabled(engine.CanMove(1));

            binder.Personal.SetValueWithoutNotify(
                engine.IsCategoryEnabled(CalendarCategory.Personal));

            binder.Work.SetValueWithoutNotify(
                engine.IsCategoryEnabled(CalendarCategory.Work));

            binder.Reminders.SetValueWithoutNotify(
                engine.IsCategoryEnabled(CalendarCategory.Reminders));

            builder.BuildUpcoming(
                binder.Upcoming, engine, selectEvent);

            switch (mode)
            {
                case CalendarViewMode.Month:
                    builder.BuildMonth(
                        binder.MonthGrid,
                        engine,
                        selectDay,
                        selectEvent);
                    break;

                case CalendarViewMode.Day:
                case CalendarViewMode.Week:
                    builder.BuildSchedule(
                        binder,
                        engine,
                        selectDay,
                        selectEvent);
                    break;

                case CalendarViewMode.Agenda:
                    List<CalendarEvent> events = engine.AgendaEvents();

                    binder.AgendaHeading.text = engine.IsSearching
                        ? $"Search results ({events.Count})"
                        : "Events · " +
                          engine.MonthStart.ToString(
                              "MMMM yyyy", CalendarEngine.Culture);

                    binder.EmptyAgenda.text = engine.IsSearching
                        ? "No events match your search and filters."
                        : "No events in this month with these filters.";

                    Show(binder.EmptyAgenda, events.Count == 0);

                    builder.BuildAgenda(
                        binder.AgendaEvents,
                        events,
                        selectEvent);
                    break;
            }
        }

        public void ShowDetails(CalendarEvent item)
        {
            if (!DetailsOpen)
            {
                previousFocus = binder.Root.panel?
                    .focusController.focusedElement as VisualElement;
            }

            binder.EditorHeading.text = "Event details";

            binder.EventTitle.SetValueWithoutNotify(item.Title);
            binder.AllDay.SetValueWithoutNotify(item.AllDay);

            binder.StartDate.SetValueWithoutNotify(
                item.Start.ToString(
                    "yyyy-MM-dd", CalendarEngine.Culture));

            DateTime displayedEnd = item.AllDay
                ? item.End.AddDays(-1)
                : item.End;

            binder.EndDate.SetValueWithoutNotify(
                displayedEnd.ToString(
                    "yyyy-MM-dd", CalendarEngine.Culture));

            binder.EndDate.tooltip = item.AllDay
                ? "Last included day"
                : "Event end date";

            binder.StartTime.SetValueWithoutNotify(
                item.AllDay
                    ? string.Empty
                    : item.Start.ToString(
                        "HH:mm", CalendarEngine.Culture));

            binder.EndTime.SetValueWithoutNotify(
                item.AllDay
                    ? string.Empty
                    : item.End.ToString(
                        "HH:mm", CalendarEngine.Culture));

            Show(binder.StartTime, !item.AllDay);
            Show(binder.EndTime, !item.AllDay);

            binder.Category.SetValueWithoutNotify(
                item.Category.ToString());

            binder.Repeat.SetValueWithoutNotify("Authored occurrence");

            binder.Location.SetValueWithoutNotify(item.Location);
            binder.Description.SetValueWithoutNotify(item.Description);

            Show(binder.Validation, false);

            DetailsOpen = true;
            Show(binder.Overlay, true);
            binder.Overlay.BringToFront();
            binder.CloseEditor.Focus();
        }

        public void CloseDetails()
        {
            if (!DetailsOpen)
                return;

            DetailsOpen = false;
            Show(binder.Overlay, false);

            if (previousFocus?.panel != null &&
                previousFocus.enabledInHierarchy)
            {
                previousFocus.Focus();
            }
            else
            {
                binder.Today.Focus();
            }

            previousFocus = null;
        }

        public void CycleDetailButtons()
        {
            // Keep Tab navigation inside the modal.
            // Read-only field text remains selectable with the pointer.
            bool closeFocused =
                ReferenceEquals(
                    binder.Root.panel?.focusController.focusedElement,
                    binder.CloseEditor);

            if (closeFocused)
                binder.CancelEditor.Focus();
            else
                binder.CloseEditor.Focus();
        }

        public void Clear()
        {
            ClearDynamicContent();

            DetailsOpen = false;
            previousFocus = null;

            Show(binder.Overlay, false);
        }

        private void ConfigureReadOnlyDetails()
        {
            binder.CreateEvent.text = "Authored calendar";
            binder.CreateEvent.tooltip =
                "Events are provided by this scenario.";

            binder.CreateEvent.SetEnabled(false);

            foreach (TextField field in new[]
            {
                binder.EventTitle,
                binder.StartDate,
                binder.StartTime,
                binder.EndDate,
                binder.EndTime,
                binder.Location,
                binder.Description
            })
            {
                field.isReadOnly = true;
            }

            binder.AllDay.SetEnabled(false);

            binder.Category.choices = new List<string>
            {
                "Personal", "Work", "Reminders"
            };

            binder.Category.SetEnabled(false);

            binder.Repeat.choices = new List<string>
            {
                "Authored occurrence"
            };

            binder.Repeat.SetEnabled(false);
            binder.CancelEditor.text = "Close";

            Show(binder.SaveEvent, false);
            Show(binder.DeleteEvent, false);
            Show(binder.Validation, false);
            Show(binder.Overlay, false);

            // The schedule uses expanding hourly lists.
            binder.ScheduleGrid.style.height = StyleKeyword.Auto;
        }

        private void ClearDynamicContent()
        {
            // Detach handlers before removing their elements.
            builder.ClearBindings();

            binder.MonthGrid.Clear();
            binder.ScheduleHeader.Clear();
            binder.AllDayEvents.Clear();
            binder.ScheduleGrid.Clear();
            binder.AgendaEvents.Clear();
            binder.Upcoming.Clear();
        }

        private static void Show(VisualElement element, bool visible)
        {
            element.EnableInClassList("cal-hidden", !visible);
        }
    }
}