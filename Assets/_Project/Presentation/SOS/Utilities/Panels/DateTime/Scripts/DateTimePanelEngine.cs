using System;
using Atlas.Presentation.SOS.Calendar;
using Atlas.SOS.Calendar;

namespace Atlas.Presentation.SOS.DateAndTime
{
    public sealed class DateTimePanelEngine
    {
        public const int MinimumFocusMinutes = 5;
        public const int MaximumFocusMinutes = 240;
        public const int FocusStepMinutes = 5;

        private readonly CalendarEngine calendar;
        private double focusDeadline;

        public DateTime Now => calendar.Now;
        public DateTime Today => calendar.Today;
        public string TimezoneLabel => calendar.TimezoneLabel;

        public DateTime Month { get; private set; }
        public DateTime SelectedDate { get; private set; }

        public bool Collapsed { get; private set; }

        public int FocusMinutes { get; private set; } = 30;
        public bool IsFocusing { get; private set; }
        public bool FocusCompleted { get; private set; }
        public int RemainingSeconds { get; private set; }

        public DateTime GridStart =>
            Month.AddDays(-(int)Month.DayOfWeek);

        public bool CanMovePrevious =>
            Month > CalendarEngine.MinimumDate;

        public bool CanMoveNext =>
            Month < new DateTime(
                CalendarEngine.MaximumDate.Year,
                CalendarEngine.MaximumDate.Month,
                1);

        public DateTimePanelEngine(CalendarLibrary library)
        {
            calendar = new CalendarEngine(library);

            SelectedDate = Today;
            Month = StartOfMonth(Today);
        }

        public bool IsSupported(DateTime date)
        {
            return date.Date >= CalendarEngine.MinimumDate &&
                   date.Date <= CalendarEngine.MaximumDate;
        }

        public DateTime GetDay(int index)
        {
            if (index < 0 || index >= 42)
                throw new ArgumentOutOfRangeException(nameof(index));

            return GridStart.AddDays(index);
        }

        public void MoveMonth(int direction)
        {
            if (direction < 0 && CanMovePrevious)
                Month = Month.AddMonths(-1);
            else if (direction > 0 && CanMoveNext)
                Month = Month.AddMonths(1);
        }

        public void SelectDate(DateTime date)
        {
            if (!IsSupported(date))
                return;

            SelectedDate = date.Date;
            Month = StartOfMonth(SelectedDate);
        }

        public void ToggleCollapsed()
        {
            Collapsed = !Collapsed;
        }

        public void SetScenarioTime(DateTime scenarioLocalTime)
        {
            DateTime previousToday = Today;

            bool followToday = SelectedDate == previousToday;
            bool followMonth = Month == StartOfMonth(previousToday);

            calendar.SetScenarioTime(scenarioLocalTime);

            if (followToday)
                SelectedDate = Today;

            if (followMonth)
                Month = StartOfMonth(Today);
        }

        public void ChangeFocusDuration(int direction)
        {
            if (IsFocusing || direction == 0)
                return;

            int next = FocusMinutes +
                (direction < 0 ? -FocusStepMinutes : FocusStepMinutes);

            FocusMinutes = Math.Max(
                MinimumFocusMinutes,
                Math.Min(MaximumFocusMinutes, next));

            FocusCompleted = false;
        }

        public void ToggleFocus(double realtime)
        {
            FocusCompleted = false;

            if (IsFocusing)
            {
                IsFocusing = false;
                RemainingSeconds = 0;
                return;
            }

            RemainingSeconds = FocusMinutes * 60;
            focusDeadline = realtime + RemainingSeconds;
            IsFocusing = true;
        }

        public bool Tick(double realtime)
        {
            if (!IsFocusing)
                return false;

            int next = (int)Math.Ceiling(
                Math.Max(0d, focusDeadline - realtime));

            if (next == RemainingSeconds)
                return false;

            RemainingSeconds = next;

            if (RemainingSeconds == 0)
            {
                IsFocusing = false;
                FocusCompleted = true;
            }

            return true;
        }

        private static DateTime StartOfMonth(DateTime date)
        {
            return new DateTime(date.Year, date.Month, 1);
        }
    }
}