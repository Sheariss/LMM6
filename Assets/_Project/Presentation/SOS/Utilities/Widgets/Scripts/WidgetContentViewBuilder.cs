using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Weather;
using Atlas.SOS.Calendar;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    [Serializable]
    public sealed class WidgetDataBinding
    {
        [SerializeField] private string appID;
        [SerializeField] private ScriptableObject dataSource;

        public string AppID => appID;
        public ScriptableObject DataSource => dataSource;
    }

    public sealed class WidgetContentViewBuilder
    {
        private readonly Dictionary<string, ScriptableObject> sources =
            new(StringComparer.Ordinal);

        public WidgetContentViewBuilder(
            IReadOnlyList<WidgetDataBinding> bindings)
        {
            if (bindings == null)
                throw new ArgumentNullException(nameof(bindings));

            foreach (WidgetDataBinding binding in bindings)
            {
                if (binding == null ||
                    string.IsNullOrWhiteSpace(binding.AppID) ||
                    binding.DataSource == null)
                {
                    throw new InvalidOperationException(
                        "Each widget data binding needs an app ID " +
                        "and a data source.");
                }

                if (!sources.TryAdd(binding.AppID, binding.DataSource))
                {
                    throw new InvalidOperationException(
                        $"Duplicate widget data binding: '{binding.AppID}'.");
                }
            }
        }

        public void Populate(
            WidgetContext context,
            VisualElement contentRoot)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (contentRoot == null)
                throw new ArgumentNullException(nameof(contentRoot));

            string appID = context.Definition.AssociatedAppID;

            switch (appID)
            {
                case "weather":
                    {
                        WeatherLibrary library = Require<WeatherLibrary>(appID);

                        var binder = new WeatherWidgetBinder(contentRoot);
                        var view = new WeatherWidgetView(binder);
                        var builder = new WeatherWidgetViewBuilder();

                        view.Render(builder.Build(library));
                        break;
                    }

                case "clock":
                    {
                        // Initial clock snapshot uses the Calendar scenario time.
                        CalendarLibrary library = Require<CalendarLibrary>(appID);

                        var binder = new ClockWidgetBinder(contentRoot);
                        var view = new ClockWidgetView(binder);
                        var builder = new ClockWidgetViewBuilder();

                        view.Render(builder.Build(library));
                        break;
                    }

                case "calendar":
                    {
                        CalendarLibrary library = Require<CalendarLibrary>(appID);

                        var binder = new CalendarWidgetBinder(contentRoot);
                        var view = new CalendarWidgetView(binder);
                        var builder = new CalendarWidgetViewBuilder();

                        view.Render(builder.Build(library));
                        break;
                    }

                default:
                    throw new InvalidOperationException(
                        $"No widget content presentation exists for '{appID}'.");
            }
        }

        private T Require<T>(string appID) where T : ScriptableObject
        {
            if (!sources.TryGetValue(appID, out ScriptableObject source))
            {
                throw new InvalidOperationException(
                    $"No widget data source is assigned for '{appID}'.");
            }

            if (source is not T typed)
            {
                throw new InvalidOperationException(
                    $"Widget '{appID}' requires {typeof(T).Name}, " +
                    $"but received {source.GetType().Name}.");
            }

            return typed;
        }
    }

    internal static class WidgetText
    {
        public static Label Create(string text, string className)
        {
            var label = new Label(text ?? string.Empty)
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList(className);
            return label;
        }
    }
}