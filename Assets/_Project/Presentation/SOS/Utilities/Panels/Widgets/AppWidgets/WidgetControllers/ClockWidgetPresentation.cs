using System;
using System.Globalization;
using Atlas.SOS.Calendar;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class ClockWidgetBinder : UIBinder
    {
        public Label Time { get; }
        public Label Date { get; }
        public Label Timezone { get; }

        public ClockWidgetBinder(VisualElement root)
        {
            Time = Bind<Label>(root, "ClockTime");
            Date = Bind<Label>(root, "ClockDate");
            Timezone = Bind<Label>(root, "ClockTimezone");

            if (!IsValid)
                throw new InvalidOperationException(
                    "Clock widget content elements are missing.");
        }
    }

    public sealed class ClockWidgetState
    {
        public string Time;
        public string Date;
        public string Timezone;
    }

    public sealed class ClockWidgetViewBuilder
    {
        public ClockWidgetState Build(CalendarLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));

            bool valid = DateTime.TryParseExact(
                library.ScenarioLocalNow,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime time);

            if (!valid)
            {
                throw new InvalidOperationException(
                    "Clock widget requires a valid scenario time " +
                    "in yyyy-MM-dd HH:mm format.");
            }

            return new ClockWidgetState
            {
                Time = time.ToString(
                    "HH:mm", CultureInfo.InvariantCulture),

                Date = time.ToString(
                    "dddd, MMMM d", CultureInfo.InvariantCulture),

                Timezone = library.TimezoneLabel ?? string.Empty
            };
        }
    }

    public sealed class ClockWidgetView
    {
        private readonly ClockWidgetBinder binder;

        public ClockWidgetView(ClockWidgetBinder binder)
        {
            this.binder = binder;
        }

        public void Render(ClockWidgetState state)
        {
            binder.Time.text = state.Time;
            binder.Date.text = state.Date;
            binder.Timezone.text = state.Timezone;
        }
    }
}