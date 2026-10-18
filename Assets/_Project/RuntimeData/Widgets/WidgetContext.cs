using System;
using System.Collections.Generic;
using Atlas.SOS.Widgets;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetContext
    {
        private readonly Dictionary<string, string> settings =
            new(StringComparer.Ordinal);

        public string InstanceID { get; }
        public WidgetDefinition Definition { get; }

        internal WidgetContext(WidgetDefinition definition)
        {
            Definition = definition ??
                throw new ArgumentNullException(nameof(definition));

            InstanceID = Guid.NewGuid().ToString("N");
        }

        public string GetSetting(
            string key,
            string fallback = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A setting key is required.");

            return settings.TryGetValue(key, out string value)
                ? value
                : fallback;
        }

        public void SetSetting(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("A setting key is required.");

            if (value == null)
                settings.Remove(key);
            else
                settings[key] = value;
        }

        public Dictionary<string, string> CopySettings()
        {
            return new Dictionary<string, string>(
                settings,
                StringComparer.Ordinal);
        }
    }
}