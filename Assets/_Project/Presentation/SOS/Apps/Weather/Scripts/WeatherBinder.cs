using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Weather
{
    public sealed class WeatherBinder : UIBinder
    {
        public VisualElement Root { get; }
        public VisualElement Main { get; }
        public VisualElement HourlyCard { get; }
        public VisualElement DetailsGrid { get; }
        public VisualElement ForecastGrid { get; }

        public Button LocationsButton { get; }
        public VisualElement LocationsPanel { get; }
        public DropdownField LocationPicker { get; }

        public Label Location { get; }
        public Label Date { get; }
        public Label Temperature { get; }
        public Label Condition { get; }
        public Label TemperatureRange { get; }
        public Label Summary { get; }

        public Image Artwork { get; }
        public VisualElement Illustration { get; }

        public Label FeelsLike { get; }
        public Label FeelsLikeCaption { get; }
        public Label Wind { get; }
        public Label WindCaption { get; }
        public Label Humidity { get; }
        public Label HumidityCaption { get; }
        public Label Visibility { get; }
        public Label VisibilityCaption { get; }

        public VisualElement Hourly { get; }
        public VisualElement Daily { get; }
        public Label Footer { get; }

        public WeatherBinder(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            Root = Bind<VisualElement>(root, "WeatherRoot");
            Main = Bind<VisualElement>(root, "weather-app");
            HourlyCard = Bind<VisualElement>(root, "hourly-card");
            DetailsGrid = Bind<VisualElement>(root, "details-grid");
            ForecastGrid = Bind<VisualElement>(root, "forecast-grid");

            LocationsButton = Bind<Button>(root, "locations-button");
            LocationsPanel = Bind<VisualElement>(root, "locations-panel");
            LocationPicker = Bind<DropdownField>(root, "location-picker");

            Location = Bind<Label>(root, "location-label");
            Date = Bind<Label>(root, "date-label");
            Temperature = Bind<Label>(root, "temperature-label");
            Condition = Bind<Label>(root, "condition-label");
            TemperatureRange = Bind<Label>(root, "temperature-range-label");
            Summary = Bind<Label>(root, "summary-label");

            Artwork = Bind<Image>(root, "weather-artwork");
            Illustration = Bind<VisualElement>(root, "weather-illustration");

            FeelsLike = Bind<Label>(root, "feels-like-label");
            FeelsLikeCaption = Bind<Label>(root, "feels-like-caption");
            Wind = Bind<Label>(root, "wind-label");
            WindCaption = Bind<Label>(root, "wind-caption");
            Humidity = Bind<Label>(root, "humidity-label");
            HumidityCaption = Bind<Label>(root, "humidity-caption");
            Visibility = Bind<Label>(root, "visibility-label");
            VisibilityCaption = Bind<Label>(root, "visibility-caption");

            Hourly = Bind<VisualElement>(root, "hourly-container");
            Daily = Bind<VisualElement>(root, "daily-container");
            Footer = Bind<Label>(root, "weather-footer");

            if (!IsValid)
                return;

            Label[] authoredLabels =
            {
                Location, Date, Temperature, Condition, TemperatureRange,
                Summary, FeelsLike, FeelsLikeCaption, Wind, WindCaption,
                Humidity, HumidityCaption, Visibility, VisibilityCaption,
                Footer
            };

            foreach (Label label in authoredLabels)
                label.enableRichText = false;
        }
    }
}