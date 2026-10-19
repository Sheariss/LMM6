using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.Presentation.SOS.Calendar;
using Atlas.SOS.Calendar;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class CalendarWidgetBinder : UIBinder
    {
        public Label Date { get; }
        public Label EmptyState { get; }
        public VisualElement Events { get; }

        public CalendarWidgetBinder(VisualElement root)
        {
            Date = Bind<Label>(root, "CalendarDate");
            EmptyState = Bind<Label>(root, "CalendarEmptyState");
            Events = Bind<VisualElement>(root, "CalendarEvents");

            if (!IsValid)
                throw new InvalidOperationException(
                    "Calendar widget content elements are missing.");
        }
    }

    public sealed class CalendarWidgetState
    {
        public string Date;
        public readonly List<VisualElement> Events = new();
    }

    public sealed class CalendarWidgetViewBuilder
    {
        private static readonly CultureInfo Culture =
            CultureInfo.InvariantCulture;

        public CalendarWidgetState Build(CalendarLibrary library)
        {
            var engine = new CalendarEngine(library);

            var state = new CalendarWidgetState
            {
                Date = engine.Now.ToString("dddd, MMMM d", Culture)
            };

            foreach (CalendarEvent item in engine.UpcomingEvents(3))
            {
                var row = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };

                row.AddToClassList("calendar-widget__event");

                row.Add(WidgetText.Create(
                    item.Title,
                    "calendar-widget__event-title"));

                string day = item.Start.Date == engine.Today
                    ? "Today"
                    : item.Start.ToString("ddd, MMM d", Culture);

                string timing;

                if (item.Start <= engine.Now)
                {
                    timing = item.AllDay
                        ? "In progress \u00B7 All day"
                        : "In progress \u00B7 Ends " +
                            item.End.ToString("ddd HH:mm", Culture);
                }
                else
                {
                    timing = item.AllDay
                        ? day + " \u00B7 All day"
                        : day + " \u00B7 " +
                            item.Start.ToString("HH:mm", Culture);
                }

                row.Add(WidgetText.Create(
                    timing,
                    "calendar-widget__event-time"));

                if (!string.IsNullOrWhiteSpace(item.Location))
                {
                    row.Add(WidgetText.Create(
                        item.Location,
                        "calendar-widget__event-location"));
                }

                state.Events.Add(row);
            }

            return state;
        }
    }

    public sealed class CalendarWidgetView
    {
        private readonly CalendarWidgetBinder binder;

        public CalendarWidgetView(CalendarWidgetBinder binder)
        {
            this.binder = binder;
        }

        public void Render(CalendarWidgetState state)
        {
            binder.Date.text = state.Date;
            binder.Events.Clear();

            foreach (VisualElement row in state.Events)
                binder.Events.Add(row);

            binder.EmptyState.style.display = state.Events.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}