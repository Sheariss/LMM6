using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Weather
{
    public sealed class WeatherViewBuilder
    {
        private const string RainIconPath =
            "WeatherIcons/icons8-rain-48";

        private const string PressureIconPath =
            "Icons/Media/Speed/speed_24dp";

        private readonly Dictionary<string, Texture2D> textures = new();

        public string Number(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        public string Temperature(int value)
        {
            return Number(value) + "°";
        }

        public string ConditionName(WeatherCondition condition)
        {
            return condition switch
            {
                WeatherCondition.Sunny => "Sunny",
                WeatherCondition.PartlyCloudy => "Partly cloudy",
                WeatherCondition.Cloudy => "Cloudy",
                WeatherCondition.Rain => "Rain",
                WeatherCondition.Storm => "Storm",
                WeatherCondition.Windy => "Windy",
                WeatherCondition.Snow => "Snow",
                WeatherCondition.ClearNight => "Clear night",
                _ => "Unknown"
            };
        }

        public Texture2D GetConditionTexture(WeatherCondition condition)
        {
            string path = condition switch
            {
                WeatherCondition.Sunny =>
                    "WeatherIcons/icons8-sun-48",

                WeatherCondition.PartlyCloudy =>
                    "WeatherIcons/icons8-partly-cloudy-day-48",

                WeatherCondition.Cloudy =>
                    "WeatherIcons/icons8-clouds-48",

                WeatherCondition.Rain =>
                    RainIconPath,

                WeatherCondition.Storm =>
                    "WeatherIcons/icons8-storm-48",

                WeatherCondition.Windy =>
                    "WeatherIcons/icons8-windy-weather-48",

                WeatherCondition.ClearNight =>
                    "WeatherIcons/icons8-night-48",

                _ => null
            };

            return LoadTexture(path);
        }

        public VisualElement BuildHour(
            WeatherHour hour,
            DateTime reportTime,
            int index)
        {
            DateTime time = reportTime.AddHours(index);

            var root = new VisualElement
            {
                tooltip = time.ToString(
                    "ddd, MMM d HH:mm",
                    CultureInfo.InvariantCulture)
            };

            root.AddToClassList("hour-item");
            root.EnableInClassList("hour-selected", index == 0);

            root.Add(Text(
                index == 0
                    ? "Now"
                    : time.ToString("HH:mm", CultureInfo.InvariantCulture),
                "hour-time"));

            root.Add(BuildConditionIcon(
                hour.Condition,
                "forecast-icon"));

            var divider = new VisualElement();
            divider.AddToClassList("hour-divider");
            root.Add(divider);

            root.Add(Text(
                Temperature(hour.TemperatureC),
                "hour-temp"));

            root.Add(BuildHourMetric(
                RainIconPath,
                Number(hour.RainPercent) + "%",
                "Chance of rain",
                new Color(0.66f, 0.78f, 0.98f)));

            root.Add(BuildHourMetric(
                PressureIconPath,
                Number(hour.PressureHpa) + " hPa",
                "Atmospheric pressure",
                new Color(0.70f, 0.75f, 0.82f)));

            return root;
        }

        public VisualElement BuildDay(
            WeatherDay day,
            DateTime reportDate,
            int index)
        {
            DateTime date = reportDate.Date.AddDays(index);

            var root = new VisualElement();
            root.AddToClassList("day-card");

            string dateText = index == 0
                ? "Today, " + date.ToString(
                    "MMM d", CultureInfo.InvariantCulture)
                : date.ToString(
                    "ddd, MMM d", CultureInfo.InvariantCulture);

            root.Add(Text(dateText, "day-date"));

            root.Add(Text("Day", "day-period"));
            root.Add(BuildDayReading(
                day.DayTemperatureC,
                day.DayCondition,
                "day-temperature"));
            root.Add(Text(
                ConditionName(day.DayCondition),
                "day-condition"));

            var divider = new VisualElement();
            divider.AddToClassList("day-divider");
            root.Add(divider);

            root.Add(Text("Night", "day-period"));
            root.Add(BuildDayReading(
                day.NightTemperatureC,
                day.NightCondition,
                "night-temperature"));
            root.Add(Text(
                ConditionName(day.NightCondition),
                "day-condition"));

            var metrics = new VisualElement();
            metrics.AddToClassList("day-metrics");

            metrics.Add(BuildDayMetric(
                "Wind",
                Number(day.WindKph) + " km/h"));

            metrics.Add(BuildDayMetric(
                "Humidity",
                Number(day.HumidityPercent) + "%"));

            metrics.Add(BuildDayMetric(
                "UV index",
                Number(day.UVIndex)));

            metrics.Add(BuildDayMetric(
                "Rain",
                Number(day.RainPercent) + "%"));

            root.Add(metrics);
            return root;
        }

        private VisualElement BuildConditionIcon(
            WeatherCondition condition,
            string className)
        {
            Texture2D texture = GetConditionTexture(condition);

            if (texture == null)
            {
                return Text(
                    ConditionName(condition),
                    "icon-fallback");
            }

            var image = new Image
            {
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                tooltip = ConditionName(condition)
            };

            image.AddToClassList(className);
            return image;
        }

        private VisualElement BuildHourMetric(
            string iconPath,
            string value,
            string tooltip,
            Color tint)
        {
            var root = new VisualElement
            {
                tooltip = tooltip + ": " + value
            };

            root.AddToClassList("hour-metric");

            Texture2D texture = LoadTexture(iconPath);

            if (texture != null)
            {
                var icon = new Image
                {
                    image = texture,
                    scaleMode = ScaleMode.ScaleToFit,
                    tintColor = tint
                };

                icon.AddToClassList("metric-icon");
                root.Add(icon);
            }

            root.Add(Text(value, "metric-value"));
            return root;
        }

        private VisualElement BuildDayReading(
            int temperatureC,
            WeatherCondition condition,
            string temperatureClass)
        {
            var root = new VisualElement();
            root.AddToClassList("day-reading");

            root.Add(Text(
                Temperature(temperatureC),
                temperatureClass));

            root.Add(BuildConditionIcon(condition, "day-icon"));
            return root;
        }

        private VisualElement BuildDayMetric(string title, string value)
        {
            var root = new VisualElement();
            root.AddToClassList("day-metric-row");

            root.Add(Text(title, "day-metric-label"));
            root.Add(Text(value, "day-metric-value"));

            return root;
        }

        private Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (!textures.TryGetValue(path, out Texture2D texture))
            {
                texture = Resources.Load<Texture2D>(path);
                textures.Add(path, texture);
            }

            return texture;
        }

        private static Label Text(string value, string className)
        {
            var label = new Label(value ?? string.Empty)
            {
                enableRichText = false
            };

            label.AddToClassList(className);
            return label;
        }
    }
}