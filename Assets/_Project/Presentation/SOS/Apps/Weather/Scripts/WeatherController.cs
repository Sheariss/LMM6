using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Weather
{
    public sealed class WeatherController : AppContentController
    {
        [Header("Authored Data")]
        [SerializeField] private WeatherLibrary weatherLibrary;

        private WeatherBinder binder;
        private WeatherEngine engine;
        private WeatherView view;

        protected override void OnInitialize()
        {
            if (weatherLibrary == null)
            {
                throw new InvalidOperationException(
                    "[Weather] WeatherLibrary is not assigned.");
            }

            binder = new WeatherBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "[Weather] Required UI elements are missing.");
            }

            engine = new WeatherEngine(weatherLibrary);

            view = new WeatherView(
                binder,
                new WeatherViewBuilder());

            view.ConfigureLocations(
                engine.GetLocationNames(),
                engine.SelectedIndex);

            BindClick(
                binder.LocationsButton,
                view.ToggleLocations);

            BindCallback<ChangeEvent<string>>(
                binder.LocationPicker,
                OnLocationChanged);

            BindCallback<KeyDownEvent>(
                binder.LocationsPanel,
                OnLocationsKeyDown);

            BindCallback<GeometryChangedEvent>(
                binder.Root,
                OnGeometryChanged);

            Refresh();

            view.ApplyLayout(binder.Root.contentRect.width);
        }

        protected override void OnShutdown()
        {
            // Base Shutdown unregisters callbacks before this method runs.
            view?.Clear();

            view = null;
            engine = null;
            binder = null;
        }

        private void OnLocationChanged(ChangeEvent<string> evt)
        {
            if (engine.SelectLocation(binder.LocationPicker.index))
                Refresh();

            view.CloseLocations();
        }

        private void OnLocationsKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;

            view.CloseLocations();
            evt.StopPropagation();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            view.ApplyLayout(binder.Root.contentRect.width);
        }

        private void Refresh()
        {
            view.Render(engine.Current, engine.ReportedAt);
        }
    }
}