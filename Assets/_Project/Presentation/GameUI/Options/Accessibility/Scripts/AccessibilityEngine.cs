using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Core.Accessibility
{
    public sealed class AccessibilityEngine
    {
        private readonly Dictionary<string, AccessibilitySettingDefinition>
            definitions = new(StringComparer.Ordinal);

        public AccessibilityEngine(AccessibilityCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));

            HashSet<string> categoryIds = new(StringComparer.Ordinal);

            foreach (var category in catalog.Categories)
            {
                if (category == null)
                    continue;

                ValidateUniqueId(categoryIds, category.Id, "catalog categories");

                HashSet<string> sectionIds = new(StringComparer.Ordinal);

                foreach (var section in category.Sections)
                {
                    if (section == null)
                        continue;

                    ValidateUniqueId(sectionIds, section.Id, category.Id);

                    foreach (var definition in section.Settings)
                    {
                        if (definition == null)
                            continue;

                        ValidateDefinition(definition);

                        if (definitions.ContainsKey(definition.Id))
                        {
                            throw new InvalidOperationException(
                                $"Duplicate accessibility setting ID: '{definition.Id}'.");
                        }

                        definitions.Add(definition.Id, definition);
                    }
                }
            }
        }

        public AccessibilitySaveData CreateDefaults()
        {
            return Normalize(null);
        }

        public AccessibilitySaveData Normalize(AccessibilitySaveData source)
        {
            AccessibilitySaveData result = new();

            foreach (var definition in definitions.Values)
            {
                result.Set(NormalizeEntry(
                    definition,
                    source?.Get(definition.Id)));
            }

            return result;
        }

        public void SetValue(
            AccessibilitySaveData target,
            AccessibilitySaveData.Entry proposed)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            if (proposed == null ||
                string.IsNullOrWhiteSpace(proposed.Id) ||
                !definitions.TryGetValue(proposed.Id, out var definition))
            {
                throw new ArgumentException(
                    "Unknown accessibility setting ID.",
                    nameof(proposed));
            }

            if (proposed.ControlType != definition.ControlType)
            {
                throw new ArgumentException(
                    $"Value type does not match '{definition.Id}'.",
                    nameof(proposed));
            }

            target.Set(NormalizeEntry(definition, proposed));
        }

        public bool AreEqual(
            AccessibilitySaveData left,
            AccessibilitySaveData right)
        {
            AccessibilitySaveData a = Normalize(left);
            AccessibilitySaveData b = Normalize(right);

            foreach (var entry in a.Values)
            {
                var other = b.Get(entry.Id);

                if (entry.ToggleValue != other.ToggleValue ||
                    entry.SliderValue != other.SliderValue ||
                    !string.Equals(
                        entry.StringValue,
                        other.StringValue,
                        StringComparison.Ordinal) ||
                    !entry.ColorValue.Equals(other.ColorValue))
                {
                    return false;
                }
            }

            return true;
        }

        private static AccessibilitySaveData.Entry NormalizeEntry(
            AccessibilitySettingDefinition definition,
            AccessibilitySaveData.Entry source)
        {
            // If a saved preference has changed type, restore its default.
            if (source != null &&
                source.ControlType != definition.ControlType)
            {
                source = null;
            }

            // Only populate the field used by this control type.
            AccessibilitySaveData.Entry result = new()
            {
                Id = definition.Id,
                ControlType = definition.ControlType
            };

            switch (definition.ControlType)
            {
                case AccessibilityControlType.Toggle:
                    result.ToggleValue =
                        source?.ToggleValue ?? definition.DefaultToggleValue;
                    break;

                case AccessibilityControlType.Slider:
                    result.SliderValue = NormalizeSlider(
                        definition,
                        source?.SliderValue ?? definition.DefaultSliderValue);
                    break;

                case AccessibilityControlType.Dropdown:
                    result.StringValue = NormalizeOption(
                        definition,
                        source?.StringValue);
                    break;

                case AccessibilityControlType.Color:
                    result.ColorValue = NormalizeColor(
                        definition,
                        source?.ColorValue ?? definition.DefaultColorValue);
                    break;

                case AccessibilityControlType.Font:
                    result.StringValue = NormalizeFont(
                        definition,
                        source?.StringValue);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported control type for '{definition.Id}'.");
            }

            return result;
        }

        private static float NormalizeSlider(
            AccessibilitySettingDefinition definition,
            float value)
        {
            if (!IsFinite(value))
                value = definition.DefaultSliderValue;

            if (!IsFinite(value))
                value = definition.Minimum;

            double minimum = definition.Minimum;
            double maximum = definition.Maximum;

            double normalized = Math.Max(
                minimum,
                Math.Min(maximum, value));

            if (definition.Step > 0f)
            {
                double increments = Math.Round(
                    (normalized - minimum) / definition.Step,
                    MidpointRounding.AwayFromZero);

                normalized = minimum + increments * definition.Step;
            }

            return (float)Math.Max(
                minimum,
                Math.Min(maximum, normalized));
        }

        private static string NormalizeOption(
            AccessibilitySettingDefinition definition,
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
            AccessibilitySettingDefinition definition,
            string id)
        {
            foreach (var option in definition.Options)
            {
                if (string.Equals(option.Id, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static string NormalizeFont(
            AccessibilitySettingDefinition definition,
            string candidate)
        {
            if (ContainsFont(definition, candidate))
                return candidate;

            if (ContainsFont(definition, definition.DefaultFontId))
                return definition.DefaultFontId;

            return definition.FontOptions.Count > 0
                ? definition.FontOptions[0].Id
                : string.Empty;
        }

        private static bool ContainsFont(
            AccessibilitySettingDefinition definition,
            string id)
        {
            foreach (var option in definition.FontOptions)
            {
                if (string.Equals(option.Id, id, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static Color NormalizeColor(
            AccessibilitySettingDefinition definition,
            Color value)
        {
            Color fallback = definition.DefaultColorValue;

            return new Color(
                NormalizeChannel(value.r, fallback.r),
                NormalizeChannel(value.g, fallback.g),
                NormalizeChannel(value.b, fallback.b),
                definition.AllowAlpha
                    ? NormalizeChannel(value.a, fallback.a)
                    : 1f);
        }

        private static float NormalizeChannel(float value, float fallback)
        {
            if (!IsFinite(value))
                value = fallback;

            if (!IsFinite(value))
                value = 1f;

            return Mathf.Clamp01(value);
        }

        private static void ValidateDefinition(
            AccessibilitySettingDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new InvalidOperationException(
                    "An accessibility setting has no ID.");
            }

            if (!Enum.IsDefined(
                    typeof(AccessibilityControlType),
                    definition.ControlType))
            {
                throw new InvalidOperationException(
                    $"Unknown control type for '{definition.Id}'.");
            }

            switch (definition.ControlType)
            {
                case AccessibilityControlType.Slider:
                    ValidateSlider(definition);
                    break;

                case AccessibilityControlType.Dropdown:
                    ValidateOptions(definition);
                    break;

                case AccessibilityControlType.Font:
                    ValidateFonts(definition);
                    break;

                case AccessibilityControlType.Color:
                    ValidateColorPresets(definition);
                    break;
            }
        }

        private static void ValidateSlider(
            AccessibilitySettingDefinition definition)
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

        private static void ValidateOptions(
            AccessibilitySettingDefinition definition)
        {
            if (definition.Options == null)
            {
                throw new InvalidOperationException(
                    $"Missing dropdown options for '{definition.Id}'.");
            }

            HashSet<string> ids = new(StringComparer.Ordinal);

            foreach (var option in definition.Options)
            {
                if (option == null)
                {
                    throw new InvalidOperationException(
                        $"Null dropdown option in '{definition.Id}'.");
                }

                ValidateUniqueId(ids, option.Id, definition.Id);
            }
        }

        private static void ValidateFonts(
            AccessibilitySettingDefinition definition)
        {
            if (definition.FontOptions == null)
            {
                throw new InvalidOperationException(
                    $"Missing font options for '{definition.Id}'.");
            }

            HashSet<string> ids = new(StringComparer.Ordinal);

            foreach (var option in definition.FontOptions)
            {
                if (option == null)
                {
                    throw new InvalidOperationException(
                        $"Null font option in '{definition.Id}'.");
                }

                ValidateUniqueId(ids, option.Id, definition.Id);

                if (option.Font == null)
                {
                    throw new InvalidOperationException(
                        $"Font option '{option.Id}' in '{definition.Id}' " +
                        "has no assigned font asset.");
                }
            }
        }

        private static void ValidateColorPresets(
            AccessibilitySettingDefinition definition)
        {
            if (definition.ColorPresets == null)
            {
                throw new InvalidOperationException(
                    $"Missing color preset list for '{definition.Id}'.");
            }

            foreach (var preset in definition.ColorPresets)
            {
                if (preset == null)
                {
                    throw new InvalidOperationException(
                        $"Null color preset in '{definition.Id}'.");
                }

                Color value = preset.Value;

                if (!IsNormalizedChannel(value.r) ||
                    !IsNormalizedChannel(value.g) ||
                    !IsNormalizedChannel(value.b) ||
                    !IsNormalizedChannel(value.a))
                {
                    throw new InvalidOperationException(
                        $"Color preset '{preset.Label}' in '{definition.Id}' " +
                        "must contain RGBA values between 0 and 1.");
                }
            }
        }

        private static void ValidateUniqueId(
            HashSet<string> ids,
            string id,
            string owner)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
            {
                throw new InvalidOperationException(
                    $"Missing or duplicate ID '{id}' in '{owner}'.");
            }
        }

        private static bool IsNormalizedChannel(float value)
        {
            return IsFinite(value) && value >= 0f && value <= 1f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}