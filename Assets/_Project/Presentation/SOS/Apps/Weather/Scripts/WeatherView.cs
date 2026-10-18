using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Weather
{
    public sealed class WeatherView
    {
        private readonly WeatherBinder binder;
        private readonly WeatherViewBuilder builder;

        private bool locationsVisible;

        public WeatherView(
            WeatherBinder binder,
            WeatherViewBuilder builder)
        {
            this.binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.builder = builder ??
                throw new ArgumentNullException(nameof(builder));
        }

        public void ConfigureLocations(
            List<string> names,
            int selectedIndex)
        {
            binder.LocationPicker.choices = names;

            binder.LocationPicker.SetValueWithoutNotify(
                names[selectedIndex]);

            binder.LocationsButton.tooltip =
                "Choose an authored forecast location";

            SetLocationsVisible(false);
        }

        public void ToggleLocations()
        {
            SetLocationsVisible(!locationsVisible);

            if (locationsVisible)
                binder.LocationPicker.Focus();
        }

        public void CloseLocations()
        {
            SetLocationsVisible(false);
            binder.LocationsButton.Focus();
        }

        public void ApplyLayout(float width)
        {
            if (float.IsNaN(width) ||
                float.IsInfinity(width) ||
                width <= 0)
            {
                return;
            }

            bool wide = width >= 760f;
            bool dashboard = width >= 1100f;

            binder.Root.EnableInClassList(
                "weather--wide",
                wide);

            binder.Root.EnableInClassList(
                "weather--dashboard",
                dashboard);

            if (dashboard)
            {
                if (binder.HourlyCard.parent != binder.ForecastGrid)
                    binder.ForecastGrid.Insert(0, binder.HourlyCard);
            }
            else
            {
                if (binder.HourlyCard.parent != binder.Main)
                {
                    int index = binder.Main.IndexOf(binder.DetailsGrid);
                    binder.Main.Insert(index, binder.HourlyCard);
                }
            }
        }

        public void Render(
            WeatherLocation location,
            DateTime reportTime)
        {
            binder.Location.text = location.DisplayName;

            binder.Date.text = reportTime.ToString(
                "dddd, MMMM d",
                CultureInfo.InvariantCulture);

            binder.Temperature.text =
                builder.Temperature(location.TemperatureC);

            binder.Condition.text =
                builder.ConditionName(location.Condition);

            binder.TemperatureRange.text =
                $"High {builder.Temperature(location.HighC)}  ·  " +
                $"Low {builder.Temperature(location.LowC)}";

            binder.Summary.text = location.Summary ?? string.Empty;

            binder.FeelsLike.text =
                builder.Temperature(location.FeelsLikeC);

            binder.FeelsLikeCaption.text =
                location.FeelsLikeCaption ?? string.Empty;

            binder.Wind.text =
                builder.Number(location.WindKph) + " km/h";

            binder.WindCaption.text =
                location.WindCaption ?? string.Empty;

            binder.Humidity.text =
                builder.Number(location.HumidityPercent) + "%";

            binder.HumidityCaption.text =
                location.HumidityCaption ?? string.Empty;

            binder.Visibility.text =
                builder.Number(location.VisibilityKm) + " km";

            binder.VisibilityCaption.text =
                location.VisibilityCaption ?? string.Empty;

            binder.Footer.text =
                "Report updated at " +
                reportTime.ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture);

            binder.LocationPicker.SetValueWithoutNotify(
                location.DisplayName);

            RenderArtwork(location);

            binder.Hourly.Clear();

            for (int i = 0; i < location.Hourly.Count; i++)
            {
                binder.Hourly.Add(builder.BuildHour(
                    location.Hourly[i],
                    reportTime,
                    i));
            }

            binder.Daily.Clear();

            for (int i = 0; i < location.Daily.Count; i++)
            {
                binder.Daily.Add(builder.BuildDay(
                    location.Daily[i],
                    reportTime,
                    i));
            }
        }

        public void Clear()
        {
            binder.Hourly.Clear();
            binder.Daily.Clear();

            binder.Artwork.image = null;
            binder.Artwork.sprite = null;

            binder.LocationPicker.choices = new List<string>();
            binder.LocationPicker.SetValueWithoutNotify(string.Empty);

            SetLocationsVisible(false);
        }

        private void RenderArtwork(WeatherLocation location)
        {
            binder.Artwork.image = null;
            binder.Artwork.sprite = null;
            binder.Artwork.scaleMode = ScaleMode.ScaleToFit;

            bool useIllustration =
                location.Artwork == null &&
                (
                    location.Condition == WeatherCondition.Sunny ||
                    location.Condition == WeatherCondition.PartlyCloudy ||
                    location.Condition == WeatherCondition.Cloudy
                );

            binder.Illustration.style.display = useIllustration
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.Illustration.EnableInClassList(
                "weather-illustration--sunny",
                location.Condition == WeatherCondition.Sunny);

            binder.Illustration.EnableInClassList(
                "weather-illustration--cloudy",
                location.Condition == WeatherCondition.Cloudy);

            binder.Artwork.style.display = DisplayStyle.None;

            if (useIllustration)
                return;

            if (location.Artwork != null)
            {
                binder.Artwork.sprite = location.Artwork;
                binder.Artwork.style.display = DisplayStyle.Flex;
                return;
            }

            Texture2D texture =
                builder.GetConditionTexture(location.Condition);

            if (texture != null)
            {
                binder.Artwork.image = texture;
                binder.Artwork.style.display = DisplayStyle.Flex;
            }
        }

        private void SetLocationsVisible(bool visible)
        {
            locationsVisible = visible;

            binder.LocationsPanel.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // Keep the original pill appearance and label.
            binder.LocationsButton.text = "Locations";
        }
    }
}