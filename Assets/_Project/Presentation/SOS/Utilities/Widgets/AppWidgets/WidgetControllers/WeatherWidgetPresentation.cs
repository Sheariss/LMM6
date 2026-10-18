using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.Presentation.SOS.Weather;
using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WeatherWidgetBinder : UIBinder
    {
        public Label Location { get; }
        public Label Temperature { get; }
        public Label Condition { get; }
        public Label Range { get; }
        public Label Summary { get; }
        public Image Artwork { get; }
        public VisualElement Forecast { get; }

        public WeatherWidgetBinder(VisualElement root)
        {
            Location = Bind<Label>(root, "WeatherLocation");
            Temperature = Bind<Label>(root, "WeatherTemperature");
            Condition = Bind<Label>(root, "WeatherCondition");
            Range = Bind<Label>(root, "WeatherRange");
            Summary = Bind<Label>(root, "WeatherSummary");
            Artwork = Bind<Image>(root, "WeatherArtwork");
            Forecast = Bind<VisualElement>(root, "WeatherForecast");

            if (!IsValid)
                throw new InvalidOperationException(
                    "Weather widget content elements are missing.");
        }
    }

    public sealed class WeatherWidgetState
    {
        public string Location;
        public string Temperature;
        public string Condition;
        public string Range;
        public string Summary;

        public Sprite Artwork;
        public Texture2D FallbackArtwork;

        public readonly List<VisualElement> Forecast = new();
    }

    public sealed class WeatherWidgetViewBuilder
    {
        private static readonly CultureInfo Culture =
            CultureInfo.InvariantCulture;

        public WeatherWidgetState Build(WeatherLibrary library)
        {
            var engine = new WeatherEngine(library);
            var weatherPresentation = new WeatherViewBuilder();

            WeatherLocation location = engine.Current;

            var state = new WeatherWidgetState
            {
                Location = location.DisplayName,
                Temperature = Temperature(location.TemperatureC),
                Condition = weatherPresentation.ConditionName(
                    location.Condition),
                Range = $"High {Temperature(location.HighC)}  " +
                    $"Low {Temperature(location.LowC)}",
                Summary = location.Summary,
                Artwork = location.Artwork,
                FallbackArtwork = weatherPresentation.GetConditionTexture(
                    location.Condition)
            };

            for (int i = 0; i < location.Daily.Count; i++)
            {
                WeatherDay day = location.Daily[i];

                var column = new VisualElement
                {
                    pickingMode = PickingMode.Ignore,
                    tooltip = weatherPresentation.ConditionName(
                        day.DayCondition)
                };

                column.AddToClassList("weather-widget__day");

                string dayLabel = i == 0
                    ? "Today"
                    : engine.ReportedAt.AddDays(i).ToString("ddd", Culture);

                column.Add(WidgetText.Create(
                    dayLabel,
                    "weather-widget__day-name"));

                Texture2D texture =
                    weatherPresentation.GetConditionTexture(day.DayCondition);

                if (texture != null)
                {
                    var image = new Image
                    {
                        image = texture,
                        scaleMode = ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore
                    };

                    image.AddToClassList("weather-widget__day-icon");
                    column.Add(image);
                }
                else
                {
                    column.Add(WidgetText.Create(
                        weatherPresentation.ConditionName(day.DayCondition),
                        "weather-widget__day-condition"));
                }

                column.Add(WidgetText.Create(
                    Temperature(day.DayTemperatureC),
                    "weather-widget__day-temperature"));

                column.Add(WidgetText.Create(
                    Temperature(day.NightTemperatureC),
                    "weather-widget__night-temperature"));

                state.Forecast.Add(column);
            }

            return state;
        }

        private static string Temperature(int value)
        {
            return value.ToString(Culture) + "\u00B0";
        }
    }

    public sealed class WeatherWidgetView
    {
        private readonly WeatherWidgetBinder binder;

        public WeatherWidgetView(WeatherWidgetBinder binder)
        {
            this.binder = binder;
        }

        public void Render(WeatherWidgetState state)
        {
            binder.Location.text = state.Location;
            binder.Temperature.text = state.Temperature;
            binder.Condition.text = state.Condition;
            binder.Range.text = state.Range;
            binder.Summary.text = state.Summary ?? string.Empty;

            binder.Artwork.sprite = null;
            binder.Artwork.image = null;
            binder.Artwork.scaleMode = ScaleMode.ScaleToFit;

            if (state.Artwork != null)
                binder.Artwork.sprite = state.Artwork;
            else
                binder.Artwork.image = state.FallbackArtwork;

            binder.Forecast.Clear();

            foreach (VisualElement column in state.Forecast)
                binder.Forecast.Add(column);
        }
    }
}