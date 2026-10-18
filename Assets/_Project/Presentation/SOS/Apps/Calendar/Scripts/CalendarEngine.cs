using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atlas.SOS.Calendar;

namespace Atlas.Presentation.SOS.Calendar
{
    public enum CalendarViewMode
    {
        Day,
        Week,
        Month,
        Agenda
    }

    public sealed class CalendarEvent
    {
        public string ID { get; }
        public string Title { get; }
        public CalendarCategory Category { get; }
        public DateTime Start { get; }
        public DateTime End { get; }
        public bool AllDay { get; }
        public string Location { get; }
        public string Description { get; }

        internal CalendarEvent(
            CalendarEventDefinition definition,
            DateTime start,
            DateTime end)
        {
            ID = definition.id.Trim();
            Title = definition.title.Trim();
            Category = definition.calendar;
            Start = start;
            End = end;
            AllDay = definition.allDay;
            Location = definition.location?.Trim() ?? string.Empty;
            Description = definition.description ?? string.Empty;
        }

        // Half-open interval: [Start, End).
        public bool Overlaps(DateTime start, DateTime end)
        {
            return Start < end && End > start;
        }
    }

    public sealed class CalendarEngine
    {
        public static readonly CultureInfo Culture =
            CultureInfo.InvariantCulture;

        public static readonly DateTime MinimumDate =
            new(1900, 1, 1);

        public static readonly DateTime MaximumDate =
            new(2100, 12, 31);

        private static readonly DateTime MaximumEnd =
            new(2101, 1, 1);

        private const string LocalFormat = "yyyy-MM-dd HH:mm";

        private readonly List<CalendarEvent> events = new();
        private readonly bool[] enabledCategories = { true, true, true };

        public DateTime Now { get; private set; }
        public DateTime Today => Now.Date;

        public DateTime AnchorDate { get; private set; }

        public CalendarViewMode Mode { get; private set; } =
            CalendarViewMode.Month;

        public string Search { get; private set; } = string.Empty;

        public bool IsSearching => Search.Length > 0;
        public bool SidebarVisible { get; private set; } = true;

        public string TimezoneLabel { get; }

        public CalendarViewMode EffectiveMode =>
            IsSearching ? CalendarViewMode.Agenda : Mode;

        public DateTime MonthStart =>
            new(AnchorDate.Year, AnchorDate.Month, 1);

        public DateTime WeekStart =>
            AnchorDate.AddDays(-(int)AnchorDate.DayOfWeek);

        public CalendarEngine(CalendarLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));

            Now = ParseLocal(library.ScenarioLocalNow, "scenario time");
            ValidateScenarioTime(Now);

            AnchorDate = Today;
            TimezoneLabel = library.TimezoneLabel ?? string.Empty;

            HashSet<string> ids = new(StringComparer.Ordinal);

            foreach (CalendarEventDefinition definition in library.Events)
            {
                if (definition == null)
                    throw new InvalidOperationException(
                        "Calendar library contains a null event.");

                if (string.IsNullOrWhiteSpace(definition.id))
                    throw new InvalidOperationException(
                        "Every calendar event requires an ID.");

                string id = definition.id.Trim();

                if (!ids.Add(id))
                    throw new InvalidOperationException(
                        $"Duplicate calendar event ID: '{id}'.");

                if (string.IsNullOrWhiteSpace(definition.title))
                    throw new InvalidOperationException(
                        $"Calendar event '{id}' requires a title.");

                if (!Enum.IsDefined(
                        typeof(CalendarCategory), definition.calendar))
                {
                    throw new InvalidOperationException(
                        $"Calendar event '{id}' has an invalid category.");
                }

                DateTime start = ParseLocal(
                    definition.startLocal, $"{id}: start");

                DateTime end = ParseLocal(
                    definition.endLocal, $"{id}: end");

                if (start < MinimumDate ||
                    start >= MaximumEnd ||
                    end > MaximumEnd ||
                    end <= start)
                {
                    throw new InvalidOperationException(
                        $"Calendar event '{id}' has an invalid range. " +
                        "Use a positive duration within 1900–2100.");
                }

                if (definition.allDay &&
                    (start.TimeOfDay != TimeSpan.Zero ||
                     end.TimeOfDay != TimeSpan.Zero))
                {
                    throw new InvalidOperationException(
                        $"All-day event '{id}' must start and end at 00:00.");
                }

                events.Add(new CalendarEvent(definition, start, end));
            }

            events.Sort((left, right) =>
            {
                int result = left.Start.CompareTo(right.Start);

                return result != 0
                    ? result
                    : string.CompareOrdinal(left.ID, right.ID);
            });
        }

        public void SetScenarioTime(DateTime scenarioLocalTime)
        {
            scenarioLocalTime = DateTime.SpecifyKind(
                scenarioLocalTime, DateTimeKind.Unspecified);

            ValidateScenarioTime(scenarioLocalTime);
            Now = scenarioLocalTime;
        }

        public void SetMode(CalendarViewMode mode)
        {
            if (!Enum.IsDefined(typeof(CalendarViewMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));

            Mode = mode;
        }

        public void SetSearch(string text)
        {
            Search = text?.Trim() ?? string.Empty;
        }

        public void ToggleSidebar()
        {
            SidebarVisible = !SidebarVisible;
        }

        public bool IsCategoryEnabled(CalendarCategory category)
        {
            return enabledCategories[CategoryIndex(category)];
        }

        public void SetCategoryEnabled(
            CalendarCategory category,
            bool enabled)
        {
            enabledCategories[CategoryIndex(category)] = enabled;
        }

        public void GoToday()
        {
            AnchorDate = Today;
            Search = string.Empty;
        }

        public void SelectDay(DateTime date)
        {
            AnchorDate = Clamp(date.Date);
            Mode = CalendarViewMode.Day;
            Search = string.Empty;
        }

        public bool CanMove(int direction)
        {
            if (direction == 0 || IsSearching)
                return false;

            bool backward = direction < 0;

            if (Mode == CalendarViewMode.Month ||
                Mode == CalendarViewMode.Agenda)
            {
                return backward
                    ? MonthStart > MinimumDate
                    : MonthStart < new DateTime(2100, 12, 1);
            }

            return backward
                ? AnchorDate > MinimumDate
                : AnchorDate < MaximumDate;
        }

        public void Move(int direction)
        {
            if (!CanMove(direction))
                return;

            int step = direction < 0 ? -1 : 1;

            DateTime next = Mode switch
            {
                CalendarViewMode.Day => AnchorDate.AddDays(step),
                CalendarViewMode.Week => AnchorDate.AddDays(step * 7),
                _ => AnchorDate.AddMonths(step)
            };

            AnchorDate = Clamp(next);
        }

        public List<CalendarEvent> EventsOn(DateTime date)
        {
            DateTime start = date.Date;
            DateTime end = start.AddDays(1);

            return Matching()
                .Where(item => item.Overlaps(start, end))
                .OrderByDescending(item => item.AllDay)
                .ThenBy(item => item.Start)
                .ThenBy(item => item.ID, StringComparer.Ordinal)
                .ToList();
        }

        public List<CalendarEvent> AgendaEvents()
        {
            IEnumerable<CalendarEvent> result = Matching();

            if (!IsSearching)
            {
                DateTime start = MonthStart;
                DateTime end = start.AddMonths(1);

                result = result.Where(item => item.Overlaps(start, end));
            }

            return result.ToList();
        }

        public List<CalendarEvent> UpcomingEvents(int count = 5)
        {
            return Matching()
                .Where(item => item.End > Now)
                .OrderBy(item => item.Start < Now ? Now : item.Start)
                .ThenBy(item => item.Start)
                .ThenBy(item => item.ID, StringComparer.Ordinal)
                .Take(Math.Max(0, count))
                .ToList();
        }

        public string PeriodText()
        {
            if (IsSearching)
                return "Search results";

            return Mode switch
            {
                CalendarViewMode.Day =>
                    AnchorDate.ToString("ddd, d MMM yyyy", Culture),

                CalendarViewMode.Week =>
                    WeekStart.ToString("d MMM yyyy", Culture) +
                    " – " +
                    WeekStart.AddDays(6).ToString("d MMM yyyy", Culture),

                _ => MonthStart.ToString("MMMM yyyy", Culture)
            };
        }

        private IEnumerable<CalendarEvent> Matching()
        {
            return events.Where(item =>
                enabledCategories[(int)item.Category] &&
                (!IsSearching ||
                 Contains(item.Title, Search) ||
                 Contains(item.Location, Search) ||
                 Contains(item.Description, Search) ||
                 Contains(item.Category.ToString(), Search)));
        }

        private static bool Contains(string value, string search)
        {
            return value.IndexOf(
                search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static int CategoryIndex(CalendarCategory category)
        {
            int index = (int)category;

            if (index < 0 || index > 2)
                throw new ArgumentOutOfRangeException(nameof(category));

            return index;
        }

        private static DateTime ParseLocal(string value, string field)
        {
            if (!DateTime.TryParseExact(
                    value,
                    LocalFormat,
                    Culture,
                    DateTimeStyles.None,
                    out DateTime result))
            {
                throw new InvalidOperationException(
                    $"Invalid calendar {field}. Expected {LocalFormat}.");
            }

            return DateTime.SpecifyKind(
                result, DateTimeKind.Unspecified);
        }

        private static void ValidateScenarioTime(DateTime value)
        {
            if (value.Date < MinimumDate || value.Date > MaximumDate)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Scenario date must be within 1900–2100.");
            }
        }

        private static DateTime Clamp(DateTime date)
        {
            if (date < MinimumDate)
                return MinimumDate;

            return date > MaximumDate ? MaximumDate : date;
        }
    }
}