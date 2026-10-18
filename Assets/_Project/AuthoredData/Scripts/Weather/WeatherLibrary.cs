using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Weather
{
    public enum WeatherCondition
    {
        Sunny,
        PartlyCloudy,
        Cloudy,
        Rain,
        Storm,
        Windy,
        Snow,
        ClearNight
    }

    [CreateAssetMenu(
        fileName = "WeatherLibrary",
        menuName = "ATLAS/SOS/Weather/Weather Library")]
    public sealed class WeatherLibrary : ScriptableObject
    {
        [SerializeField]
        private List<WeatherLocation> locations = new()
        {
            new WeatherLocation()
        };

        public IReadOnlyList<WeatherLocation> Locations => locations;
    }

    [Serializable]
    public sealed class WeatherLocation
    {
        [field: SerializeField]
        public string DisplayName { get; private set; } = "Westbrook";

        [field: SerializeField,
            Tooltip("Fictional local report time: yyyy-MM-dd HH:mm")]
        public string ReportedAt { get; private set; } = "2026-10-15 14:00";

        [field: SerializeField]
        public WeatherCondition Condition { get; private set; }
            = WeatherCondition.PartlyCloudy;

        [field: SerializeField,
            Tooltip("Optional override for the main weather illustration.")]
        public Sprite Artwork { get; private set; }

        [field: SerializeField]
        public int TemperatureC { get; private set; } = 24;

        [field: SerializeField]
        public int HighC { get; private set; } = 26;

        [field: SerializeField]
        public int LowC { get; private set; } = 18;

        [field: SerializeField, TextArea(2, 4)]
        public string Summary { get; private set; }
            = "A clear afternoon ahead. Rain expected tonight.";

        [field: SerializeField]
        public int FeelsLikeC { get; private set; } = 26;

        [field: SerializeField]
        public string FeelsLikeCaption { get; private set; }
            = "Slightly warmer";

        [field: SerializeField, Min(0)]
        public int WindKph { get; private set; } = 12;

        [field: SerializeField]
        public string WindCaption { get; private set; }
            = "From the northeast";

        [field: SerializeField, Range(0, 100)]
        public int HumidityPercent { get; private set; } = 64;

        [field: SerializeField]
        public string HumidityCaption { get; private set; }
            = "Comfortable";

        [field: SerializeField, Min(0)]
        public int VisibilityKm { get; private set; } = 10;

        [field: SerializeField]
        public string VisibilityCaption { get; private set; }
            = "Clear conditions";

        [SerializeField]
        private WeatherHour[] hourly =
        {
            new(24, WeatherCondition.PartlyCloudy, 5, 1012),
            new(25, WeatherCondition.Sunny, 5, 1012),
            new(24, WeatherCondition.Cloudy, 10, 1011),
            new(23, WeatherCondition.Cloudy, 15, 1010),
            new(21, WeatherCondition.Rain, 60, 1009),
            new(20, WeatherCondition.Rain, 75, 1009)
        };

        [SerializeField]
        private WeatherDay[] daily =
        {
            new(26, 18, WeatherCondition.PartlyCloudy,
                WeatherCondition.ClearNight, 12, 64, 3, 5),

            new(23, 17, WeatherCondition.Rain,
                WeatherCondition.Rain, 11, 75, 2, 60),

            new(24, 18, WeatherCondition.Cloudy,
                WeatherCondition.Cloudy, 10, 70, 3, 20),

            new(28, 19, WeatherCondition.Sunny,
                WeatherCondition.ClearNight, 11, 60, 5, 5),

            new(29, 20, WeatherCondition.Sunny,
                WeatherCondition.ClearNight, 10, 58, 5, 5)
        };

        public IReadOnlyList<WeatherHour> Hourly => hourly;
        public IReadOnlyList<WeatherDay> Daily => daily;
    }

    [Serializable]
    public sealed class WeatherHour
    {
        [field: SerializeField]
        public int TemperatureC { get; private set; }

        [field: SerializeField]
        public WeatherCondition Condition { get; private set; }

        [field: SerializeField, Range(0, 100)]
        public int RainPercent { get; private set; }

        [field: SerializeField, Min(1)]
        public int PressureHpa { get; private set; }

        public WeatherHour()
            : this(24, WeatherCondition.PartlyCloudy, 5, 1012)
        {
        }

        public WeatherHour(
            int temperatureC,
            WeatherCondition condition,
            int rainPercent,
            int pressureHpa)
        {
            TemperatureC = temperatureC;
            Condition = condition;
            RainPercent = rainPercent;
            PressureHpa = pressureHpa;
        }
    }

    [Serializable]
    public sealed class WeatherDay
    {
        [field: SerializeField]
        public int DayTemperatureC { get; private set; }

        [field: SerializeField]
        public int NightTemperatureC { get; private set; }

        [field: SerializeField]
        public WeatherCondition DayCondition { get; private set; }

        [field: SerializeField]
        public WeatherCondition NightCondition { get; private set; }

        [field: SerializeField, Min(0)]
        public int WindKph { get; private set; }

        [field: SerializeField, Range(0, 100)]
        public int HumidityPercent { get; private set; }

        [field: SerializeField, Range(0, 15)]
        public int UVIndex { get; private set; }

        [field: SerializeField, Range(0, 100)]
        public int RainPercent { get; private set; }

        public WeatherDay()
            : this(
                26, 18,
                WeatherCondition.PartlyCloudy,
                WeatherCondition.ClearNight,
                12, 64, 3, 5)
        {
        }

        public WeatherDay(
            int dayTemperatureC,
            int nightTemperatureC,
            WeatherCondition dayCondition,
            WeatherCondition nightCondition,
            int windKph,
            int humidityPercent,
            int uvIndex,
            int rainPercent)
        {
            DayTemperatureC = dayTemperatureC;
            NightTemperatureC = nightTemperatureC;
            DayCondition = dayCondition;
            NightCondition = nightCondition;
            WindKph = windKph;
            HumidityPercent = humidityPercent;
            UVIndex = uvIndex;
            RainPercent = rainPercent;
        }
    }
}