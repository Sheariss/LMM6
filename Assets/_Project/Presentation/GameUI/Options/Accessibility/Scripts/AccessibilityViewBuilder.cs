using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.Core.Accessibility;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Accessibility
{
    public sealed class AccessibilityViewBuilder
    {
        private readonly AccessibilityView view;
        private readonly AccessibilityCatalog catalog;
        private readonly Action<AccessibilityCategoryDefinition> selectCategory;
        private readonly Action<AccessibilitySaveData.Entry> changeValue;

        public AccessibilityViewBuilder(
            AccessibilityView view,
            AccessibilityCatalog catalog,
            Action<AccessibilityCategoryDefinition> selectCategory,
            Action<AccessibilitySaveData.Entry> changeValue)
        {
            this.view = view;
            this.catalog = catalog;
            this.selectCategory = selectCategory;
            this.changeValue = changeValue;
        }

        public void BuildNavigation()
        {
            view.ClearNavigation();

            foreach (var category in catalog.Categories)
            {
                if (category == null)
                    continue;

                Button button = new(() => selectCategory(category))
                {
                    text = category.Label,
                    tooltip = category.Description
                };

                button.AddToClassList("settings-shell__nav-button");
                view.AddNavigation(category.Id, button);
            }
        }

        public void BuildContent(
            AccessibilityCategoryDefinition selectedCategory,
            AccessibilitySaveData values,
            string search)
        {
            view.ClearContent();
            view.ContentScroll.scrollOffset = Vector2.zero;

            string query = search?.Trim() ?? string.Empty;
            bool searching = query.Length > 0;
            int count = 0;

            if (searching)
            {
                foreach (var category in catalog.Categories)
                {
                    if (category != null)
                        count += BuildCategory(category, query, true);
                }

                view.SetSearchResults(count);
            }
            else
            {
                view.SetCategory(selectedCategory);

                if (selectedCategory != null)
                    count = BuildCategory(selectedCategory, query, false);
            }

            if (count == 0)
            {
                view.Content.Add(new Label(searching
                    ? "No matching accessibility options."
                    : "No accessibility options are configured in this category."));
            }

            view.RefreshValues(values);
        }

        private int BuildCategory(
            AccessibilityCategoryDefinition category,
            string query,
            bool showCategory)
        {
            int count = 0;

            foreach (var section in category.Sections)
            {
                if (section == null)
                    continue;

                VisualElement body = Element("settings-section__body");
                VisualElement lastRow = null;

                foreach (var definition in section.Settings)
                {
                    if (definition == null ||
                        !Matches(category, section, definition, query))
                    {
                        continue;
                    }

                    lastRow = BuildRow(definition);
                    body.Add(lastRow);
                    count++;
                }

                if (lastRow == null)
                    continue;

                lastRow.AddToClassList("settings-row--last");

                VisualElement container = Element("settings-section");
                VisualElement header = Element("settings-section__header");

                string title = showCategory
                    ? $"{category.Label} / {section.Label}"
                    : section.Label;

                header.Add(Text(title, "settings-section__title"));
                header.Add(Text(
                    section.Description,
                    "settings-section__description"));

                container.Add(header);
                container.Add(body);
                view.Content.Add(container);
            }

            return count;
        }

        private VisualElement BuildRow(AccessibilitySettingDefinition definition)
        {
            VisualElement row = Element("settings-row");
            row.name = $"Accessibility-{definition.Id}";

            VisualElement info = Element("settings-row__info");
            info.Add(Text(definition.Label, "settings-row__title"));
            info.Add(Text(
                definition.Description,
                "settings-row__description"));
            row.Add(info);

            switch (definition.ControlType)
            {
                case AccessibilityControlType.Toggle:
                    AddToggle(row, definition);
                    break;

                case AccessibilityControlType.Slider:
                    AddSlider(row, definition);
                    break;

                case AccessibilityControlType.Dropdown:
                    AddChoice(row, definition, false);
                    break;

                case AccessibilityControlType.Font:
                    AddChoice(row, definition, true);
                    break;

                case AccessibilityControlType.Color:
                    AddColor(row, definition);
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported control type for '{definition.Id}'.");
            }

            return row;
        }

        private static AccessibilitySaveData.Entry Entry(
            AccessibilitySettingDefinition definition)
        {
            return new AccessibilitySaveData.Entry
            {
                Id = definition.Id,
                ControlType = definition.ControlType
            };
        }

        private void AddToggle(
            VisualElement row,
            AccessibilitySettingDefinition definition)
        {
            VisualElement group = Element("settings-row__toggle-group");

            Toggle control = new() { tooltip = definition.Label };
            control.AddToClassList("settings-row__toggle");

            Label valueLabel = Text("", "settings-row__toggle-label");

            control.RegisterValueChangedCallback(evt =>
            {
                var proposed = Entry(definition);
                proposed.ToggleValue = evt.newValue;
                changeValue(proposed);
            });

            view.RegisterValueWriter(definition.Id, value =>
            {
                control.SetValueWithoutNotify(value.ToggleValue);
                valueLabel.text = value.ToggleValue ? "On" : "Off";
            });

            group.Add(control);
            group.Add(valueLabel);
            row.Add(group);
        }

        private void AddSlider(
            VisualElement row,
            AccessibilitySettingDefinition definition)
        {
            VisualElement group = Element("settings-row__slider-group");

            Slider control = new(definition.Minimum, definition.Maximum)
            {
                tooltip = definition.Label,
                showInputField = false
            };

            control.AddToClassList("settings-row__slider");
            Label valueLabel = Text("", "settings-row__value");

            control.RegisterValueChangedCallback(evt =>
            {
                var proposed = Entry(definition);
                proposed.SliderValue = evt.newValue;
                changeValue(proposed);
            });

            view.RegisterValueWriter(definition.Id, value =>
            {
                control.SetValueWithoutNotify(value.SliderValue);
                valueLabel.text = FormatSlider(definition, value.SliderValue);
            });

            group.Add(control);
            group.Add(valueLabel);
            row.Add(group);
        }

        private void AddChoice(
            VisualElement row,
            AccessibilitySettingDefinition definition,
            bool isFont)
        {
            List<string> ids = new();
            Dictionary<string, string> labels = new(StringComparer.Ordinal);
            Dictionary<string, Font> fonts = new(StringComparer.Ordinal);

            if (isFont)
            {
                foreach (var option in definition.FontOptions)
                {
                    ids.Add(option.Id);
                    labels.Add(option.Id, option.Label);
                    fonts.Add(option.Id, option.Font);
                }
            }
            else
            {
                foreach (var option in definition.Options)
                {
                    ids.Add(option.Id);
                    labels.Add(option.Id, option.Label);
                }
            }

            string DisplayLabel(string id)
            {
                return id != null && labels.TryGetValue(id, out var label)
                    ? label
                    : string.Empty;
            }

            DropdownField control = new()
            {
                choices = ids,
                tooltip = definition.Label,
                formatSelectedValueCallback = DisplayLabel,
                formatListItemCallback = DisplayLabel
            };

            control.AddToClassList("settings-row__control");
            control.SetEnabled(ids.Count > 0);

            Label sample = null;

            if (isFont)
            {
                VisualElement group = Element("accessibility-font-control");
                sample = Text(
                    definition.FontPreviewText,
                    "accessibility-font-control__sample");

                group.Add(control);
                group.Add(sample);
                row.Add(group);
            }
            else
            {
                row.Add(control);
            }

            control.RegisterValueChangedCallback(evt =>
            {
                var proposed = Entry(definition);
                proposed.StringValue = evt.newValue;
                changeValue(proposed);
            });

            view.RegisterValueWriter(definition.Id, value =>
            {
                control.SetValueWithoutNotify(value.StringValue);

                if (sample != null &&
                    value.StringValue != null &&
                    fonts.TryGetValue(value.StringValue, out var font))
                {
                    sample.style.unityFont = font;
                }
            });
        }

        private void AddColor(
            VisualElement row,
            AccessibilitySettingDefinition definition)
        {
            VisualElement group = Element("accessibility-color-control");
            VisualElement editor = Element("accessibility-color-control__editor");
            VisualElement swatch = Element("accessibility-color-control__swatch");

            TextField hex = new()
            {
                isDelayed = true,
                tooltip = definition.AllowAlpha
                    ? "Enter #RRGGBBAA or #RRGGBB."
                    : "Enter #RRGGBB."
            };
            hex.AddToClassList("settings-row__control");

            Label error = Text("", "accessibility-color-control__error");
            error.style.display = DisplayStyle.None;

            Color current = definition.DefaultColorValue;

            string FormatColor(Color value)
            {
                return "#" + (definition.AllowAlpha
                    ? ColorUtility.ToHtmlStringRGBA(value)
                    : ColorUtility.ToHtmlStringRGB(value));
            }

            void Submit(Color value)
            {
                var proposed = Entry(definition);
                proposed.ColorValue = value;
                changeValue(proposed);
            }

            hex.RegisterValueChangedCallback(evt =>
            {
                string candidate = (evt.newValue ?? "").Trim();

                if (!candidate.StartsWith("#", StringComparison.Ordinal))
                    candidate = "#" + candidate;

                bool validLength = candidate.Length == 7 ||
                    (definition.AllowAlpha && candidate.Length == 9);

                if (!validLength ||
                    !ColorUtility.TryParseHtmlString(candidate, out Color color))
                {
                    hex.SetValueWithoutNotify(FormatColor(current));
                    error.text = definition.AllowAlpha
                        ? "Use #RRGGBB or #RRGGBBAA. Previous color retained."
                        : "Use #RRGGBB. Previous color retained.";
                    error.style.display = DisplayStyle.Flex;
                    return;
                }

                Submit(color);
            });

            editor.Add(swatch);
            editor.Add(hex);
            group.Add(editor);

            VisualElement presets = Element("accessibility-color-control__presets");

            foreach (var preset in definition.ColorPresets)
            {
                Button button = new(() => Submit(preset.Value))
                {
                    text = string.IsNullOrWhiteSpace(preset.Label)
                        ? FormatColor(preset.Value)
                        : preset.Label,
                    tooltip = FormatColor(preset.Value)
                };

                button.AddToClassList("settings-shell__button");
                button.AddToClassList("settings-shell__button--secondary");
                button.AddToClassList("accessibility-color-control__preset");
                presets.Add(button);
            }

            group.Add(presets);
            group.Add(error);
            row.Add(group);

            view.RegisterValueWriter(definition.Id, value =>
            {
                current = value.ColorValue;
                hex.SetValueWithoutNotify(FormatColor(current));
                swatch.style.backgroundColor = current;
                error.style.display = DisplayStyle.None;
            });
        }

        private static string FormatSlider(
            AccessibilitySettingDefinition definition,
            float value)
        {
            if (definition.ValueFormat == "%")
                return value.ToString("0.##", CultureInfo.CurrentCulture) + "%";

            string format = string.IsNullOrWhiteSpace(definition.ValueFormat)
                ? "0.##"
                : definition.ValueFormat;

            try
            {
                return value.ToString(format, CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                return value.ToString("0.##", CultureInfo.CurrentCulture);
            }
        }

        private static bool Matches(
            AccessibilityCategoryDefinition category,
            AccessibilitySectionDefinition section,
            AccessibilitySettingDefinition setting,
            string query)
        {
            return query.Length == 0 ||
                Contains(category.Id, query) ||
                Contains(category.Label, query) ||
                Contains(category.Description, query) ||
                ContainsKeywords(category.Keywords, query) ||
                Contains(section.Id, query) ||
                Contains(section.Label, query) ||
                Contains(section.Description, query) ||
                ContainsKeywords(section.Keywords, query) ||
                Contains(setting.Id, query) ||
                Contains(setting.Label, query) ||
                Contains(setting.Description, query) ||
                ContainsKeywords(setting.Keywords, query);
        }

        private static bool ContainsKeywords(
            IReadOnlyList<string> keywords,
            string query)
        {
            if (keywords == null)
                return false;

            foreach (string keyword in keywords)
            {
                if (Contains(keyword, query))
                    return true;
            }

            return false;
        }

        private static bool Contains(string text, string query)
        {
            return text?.IndexOf(
                query,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static VisualElement Element(string className)
        {
            VisualElement element = new();
            element.AddToClassList(className);
            return element;
        }

        private static Label Text(string text, string className)
        {
            Label label = new(text ?? string.Empty);
            label.AddToClassList(className);
            return label;
        }
    }
}