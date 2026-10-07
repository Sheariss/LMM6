using System;
using System.Collections.Generic;
using Atlas.AuthoredData;
using Atlas.Core.Settings;

namespace Atlas.Presentation.Settings
{
    public sealed class SettingsEngine
    {
        private readonly Dictionary<string, SettingDefinition> definitions =
            new(StringComparer.Ordinal);

        public SettingsEngine(SettingsCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            HashSet<string> categoryIds = new(StringComparer.Ordinal);

            foreach (SettingCategoryDefinition category in catalog.Categories)
            {
                if (category == null)
                    continue;

                if (string.IsNullOrWhiteSpace(category.Id) ||
                    !categoryIds.Add(category.Id))
                {
                    throw new InvalidOperationException(
                        $"Missing or duplicate category ID: '{category.Id}'.");
                }

                foreach (SettingSectionDefinition section in category.Sections)
                {
                    if (section == null)
                        continue;

                    foreach (SettingDefinition setting in section.Settings)
                    {
                        if (setting == null)
                            continue;

                        ValidateDefinition(setting);

                        if (definitions.ContainsKey(setting.Id))
                        {
                            throw new InvalidOperationException(
                                $"Duplicate setting ID: '{setting.Id}'.");
                        }

                        definitions.Add(setting.Id, setting);
                    }
                }
            }
        }

        public SettingsSaveData CreateDefaults()
        {
            return Normalize(null);
        }

        public SettingsSaveData Normalize(SettingsSaveData source)
        {
            SettingsSaveData result = new();

            foreach (SettingDefinition definition in definitions.Values)
            {
                // Commands are actions, not stored preferences.
                if (definition.ControlType == SettingControlType.Button)
                    continue;

                result.Set(NormalizeEntry(
                    definition,
                    source?.Get(definition.Id)));
            }

            return result;
        }

        public void SetValue(
            SettingsSaveData target,
            SettingsSaveData.Entry proposed)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (proposed == null ||
                string.IsNullOrWhiteSpace(proposed.Id) ||
                !definitions.TryGetValue(proposed.Id, out var definition))
            {
                throw new ArgumentException("Unknown setting ID.");
            }

            if (definition.ControlType == SettingControlType.Button)
                throw new InvalidOperationException("Buttons have no stored value.");

            if (proposed.ControlType != definition.ControlType)
                throw new ArgumentException("Setting value type does not match.");

            target.Set(NormalizeEntry(definition, proposed));
        }

        public bool AreEqual(SettingsSaveData left, SettingsSaveData right)
        {
            SettingsSaveData a = Normalize(left);
            SettingsSaveData b = Normalize(right);

            foreach (SettingsSaveData.Entry entry in a.Values)
            {
                SettingsSaveData.Entry other = b.Get(entry.Id);

                if (entry.ToggleValue != other.ToggleValue ||
                    entry.SliderValue != other.SliderValue ||
                    !string.Equals(
                        entry.StringValue,
                        other.StringValue,
                        StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

        private static SettingsSaveData.Entry NormalizeEntry(
            SettingDefinition definition,
            SettingsSaveData.Entry source)
        {
            if (source != null &&
                source.ControlType != definition.ControlType)
            {
                source = null;
            }

            SettingsSaveData.Entry result = new()
            {
                Id = definition.Id,
                ControlType = definition.ControlType
            };

            switch (definition.ControlType)
            {
                case SettingControlType.Toggle:
                    result.ToggleValue =
                        source?.ToggleValue ?? definition.DefaultToggleValue;
                    break;

                case SettingControlType.Slider:
                    result.SliderValue = NormalizeSlider(
                        definition,
                        source?.SliderValue ?? definition.DefaultSliderValue);
                    break;

                case SettingControlType.Dropdown:
                    result.StringValue = NormalizeOption(
                        definition,
                        source?.StringValue);
                    break;

                case SettingControlType.Keybind:
                    // Opaque binding override; an input integration must
                    // validate this before applying it to an input action.
                    result.StringValue = source?.StringValue ?? string.Empty;
                    break;
            }

            return result;
        }

        private static float NormalizeSlider(
            SettingDefinition definition,
            float value)
        {
            if (!IsFinite(value))
                value = definition.DefaultSliderValue;

            if (!IsFinite(value))
                value = definition.Minimum;

            double minimum = definition.Minimum;
            double maximum = definition.Maximum;
            double normalized = Math.Max(minimum, Math.Min(maximum, value));

            if (definition.Step > 0f)
            {
                double increments = Math.Round(
                    (normalized - minimum) / definition.Step,
                    MidpointRounding.AwayFromZero);

                normalized = minimum + increments * definition.Step;
            }

            return (float)Math.Max(minimum, Math.Min(maximum, normalized));
        }

        private static string NormalizeOption(
            SettingDefinition definition,
            string candidate)
        {
            if (ContainsOption(definition, candidate))
                return candidate;

            if (ContainsOption(definition, definition.DefaultOptionId))
                return definition.DefaultOptionId;

            return definition.Options.Count > 0
                ? definition.Options[0].Id
                : string.Empty;
        }

        private static bool ContainsOption(
            SettingDefinition definition,
            string id)
        {
            foreach (SettingOptionDefinition option in definition.Options)
            {
                if (string.Equals(option.Id, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static void ValidateDefinition(SettingDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
                throw new InvalidOperationException("A setting has no ID.");

            if (!Enum.IsDefined(typeof(SettingControlType), definition.ControlType))
            {
                throw new InvalidOperationException(
                    $"Unknown control type for '{definition.Id}'.");
            }

            if (definition.ControlType == SettingControlType.Slider)
            {
                if (!IsFinite(definition.Minimum) ||
                    !IsFinite(definition.Maximum) ||
                    !IsFinite(definition.Step) ||
                    definition.Minimum > definition.Maximum ||
                    definition.Step < 0f)
                {
                    throw new InvalidOperationException(
                        $"Invalid slider range or step for '{definition.Id}'.");
                }
            }

            if (definition.ControlType != SettingControlType.Dropdown)
                return;

            HashSet<string> ids = new(StringComparer.Ordinal);

            foreach (SettingOptionDefinition option in definition.Options)
            {
                if (option == null ||
                    string.IsNullOrWhiteSpace(option.Id) ||
                    !ids.Add(option.Id))
                {
                    throw new InvalidOperationException(
                        $"Missing or duplicate option ID in '{definition.Id}'.");
                }
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}