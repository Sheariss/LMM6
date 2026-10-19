using System;
using System.Globalization;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.DateAndTime
{
    public sealed class DateTimePanelView
    {
        private static readonly CultureInfo Culture =
            CultureInfo.InvariantCulture;

        private readonly Button[] days;

        public TemplateContainer Host { get; }
        public DateTimePanelBinder Binder { get; }

        public DateTimePanelView(
            TemplateContainer host,
            DateTimePanelBinder binder,
            Button[] days)
        {
            Host = host ??
                throw new ArgumentNullException(nameof(host));

            Binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.days = days ??
                throw new ArgumentNullException(nameof(days));
        }

        public Button GetDayButton(int index)
        {
            return days[index];
        }

        public void Render(DateTimePanelEngine engine)
        {
            Binder.CurrentTime.text =
                engine.Now.ToString("h:mm tt", Culture);

            Binder.CurrentDate.text =
                engine.Now.ToString("dddd, MMMM d", Culture);

            Binder.Timezone.text = engine.TimezoneLabel;

            SetVisible(
                Binder.Timezone,
                !string.IsNullOrWhiteSpace(engine.TimezoneLabel));

            Binder.Month.text =
                engine.Month.ToString("MMMM yyyy", Culture);

            Binder.Root.EnableInClassList(
                "calendar-collapsed",
                engine.Collapsed);

            SetVisible(Binder.Body, !engine.Collapsed);

            Binder.CollapseButton.tooltip = engine.Collapsed
                ? "Expand calendar"
                : "Collapse calendar";

            Binder.PreviousMonthButton.SetEnabled(
                engine.CanMovePrevious);

            Binder.NextMonthButton.SetEnabled(
                engine.CanMoveNext);

            for (int index = 0; index < days.Length; index++)
            {
                DateTime date = engine.GetDay(index);
                Button button = days[index];

                button.text = date.Day.ToString(Culture);

                button.tooltip = date.ToString(
                    "dddd, MMMM d, yyyy",
                    Culture);

                button.SetEnabled(engine.IsSupported(date));

                button.EnableInClassList(
                    "calendar-day-outside",
                    date.Month != engine.Month.Month ||
                    date.Year != engine.Month.Year);

                button.EnableInClassList(
                    "calendar-day-today",
                    date == engine.Today);

                button.EnableInClassList(
                    "calendar-day-selected",
                    date == engine.SelectedDate);
            }

            RenderFocus(engine);
        }

        public void RenderFocus(DateTimePanelEngine engine)
        {
            if (engine.IsFocusing)
            {
                int minutes = engine.RemainingSeconds / 60;
                int seconds = engine.RemainingSeconds % 60;

                Binder.FocusDuration.text =
                    minutes.ToString(Culture) + ":" +
                    seconds.ToString("00", Culture);
            }
            else
            {
                Binder.FocusDuration.text =
                    engine.FocusMinutes.ToString(Culture) + " min";
            }

            Binder.DecreaseFocusButton.SetEnabled(
                !engine.IsFocusing &&
                engine.FocusMinutes >
                    DateTimePanelEngine.MinimumFocusMinutes);

            Binder.IncreaseFocusButton.SetEnabled(
                !engine.IsFocusing &&
                engine.FocusMinutes <
                    DateTimePanelEngine.MaximumFocusMinutes);

            Binder.StartFocusLabel.text =
                engine.IsFocusing ? "End" : "Focus";

            Binder.StartFocusButton.tooltip = engine.IsFocusing
                ? "End focus session"
                : "Start focus session";

            SetVisible(
                Binder.StartFocusIcon,
                !engine.IsFocusing);

            Binder.FocusStatus.text = engine.FocusCompleted
                ? "Focus session complete."
                : string.Empty;

            SetVisible(
                Binder.FocusStatus,
                engine.FocusCompleted);
        }

        public void Show()
        {
            Host.style.display = DisplayStyle.Flex;
            Host.BringToFront();

            Binder.CollapseButton.Focus();
        }

        public void Hide()
        {
            Host.style.display = DisplayStyle.None;
        }

        public void Detach()
        {
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