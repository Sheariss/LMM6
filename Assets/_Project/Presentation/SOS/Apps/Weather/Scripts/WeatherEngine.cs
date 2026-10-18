using System;
using System.Collections.Generic;
using System.Globalization;

namespace Atlas.Presentation.SOS.Weather
{
    public sealed class WeatherEngine
    {
        private readonly IReadOnlyList<WeatherLocation> locations;
        private readonly DateTime[] reportTimes;

        public int SelectedIndex { get; private set; }
        public WeatherLocation Current => locations[SelectedIndex];
        public DateTime ReportedAt => reportTimes[SelectedIndex];

        public WeatherEngine(WeatherLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));

            locations = library.Locations;

            Require(
                locations != null && locations.Count > 0,
                "The library needs at least one location.");

            reportTimes = new DateTime[locations.Count];

            var names = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < locations.Count; i++)
            {
                WeatherLocation location = locations[i];

                Require(location != null, $"Location {i} is missing.");
                Require(
                    !string.IsNullOrWhiteSpace(location.DisplayName),
                    $"Location {i} needs a display name.");

                Require(
                    names.Add(location.DisplayName.Trim()),
                    $"Duplicate location name: {location.DisplayName}");

                bool validDate = DateTime.TryParseExact(
                    location.ReportedAt,
                    "yyyy-MM-dd HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime reportTime);

                Require(
                    validDate &&
                    reportTime <= DateTime.MaxValue.AddDays(-6),
                    $"{location.DisplayName}: invalid report timestamp.");

                reportTimes[i] = reportTime;

                Require(
                    ValidCondition(location.Condition),
                    $"{location.DisplayName}: invalid current condition.");

                Require(
                    location.LowC <= location.HighC,
                    $"{location.DisplayName}: low exceeds high.");

                Require(
                    location.WindKph >= 0 &&
                    location.VisibilityKm >= 0 &&
                    ValidPercent(location.HumidityPercent),
                    $"{location.DisplayName}: invalid current metrics.");

                Require(
                    location.Hourly != null &&
                    location.Hourly.Count == 6,
                    $"{location.DisplayName}: exactly six hourly entries are required.");

                Require(
                    location.Daily != null &&
                    location.Daily.Count == 5,
                    $"{location.DisplayName}: exactly five daily entries are required.");

                for (int h = 0; h < location.Hourly.Count; h++)
                {
                    WeatherHour hour = location.Hourly[h];

                    Require(
                        hour != null &&
                        ValidCondition(hour.Condition) &&
                        ValidPercent(hour.RainPercent) &&
                        hour.PressureHpa > 0,
                        $"{location.DisplayName}: invalid hourly entry {h}.");
                }

                for (int d = 0; d < location.Daily.Count; d++)
                {
                    WeatherDay day = location.Daily[d];

                    Require(
                        day != null &&
                        ValidCondition(day.DayCondition) &&
                        ValidCondition(day.NightCondition) &&
                        day.WindKph >= 0 &&
                        ValidPercent(day.HumidityPercent) &&
                        ValidPercent(day.RainPercent) &&
                        day.UVIndex >= 0 &&
                        day.UVIndex <= 15,
                        $"{location.DisplayName}: invalid daily entry {d}.");
                }
            }
        }

        public List<string> GetLocationNames()
        {
            var names = new List<string>(locations.Count);

            foreach (WeatherLocation location in locations)
                names.Add(location.DisplayName);

            return names;
        }

        public bool SelectLocation(int index)
        {
            if (index < 0 ||
                index >= locations.Count ||
                index == SelectedIndex)
            {
                return false;
            }

            SelectedIndex = index;
            return true;
        }

        private static bool ValidPercent(int value)
        {
            return value >= 0 && value <= 100;
        }

        private static bool ValidCondition(WeatherCondition condition)
        {
            return Enum.IsDefined(typeof(WeatherCondition), condition);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("[Weather] " + message);
        }
    }
}