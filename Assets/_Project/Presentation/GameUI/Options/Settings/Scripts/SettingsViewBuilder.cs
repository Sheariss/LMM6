using System;
using System.Collections.Generic;
using System.Globalization;
using Atlas.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Settings
{
    public sealed class SettingsViewBuilder
    {
        private readonly SettingsView view;
        private readonly SettingsCatalog catalog;
        private readonly Action<SettingCategoryDefinition> selectCategory;
        private readonly Action<SettingsSaveData.Entry> changeValue;

        public SettingsViewBuilder(
            SettingsView view,
            SettingsCatalog catalog,
            Action<SettingCategoryDefinition> selectCategory,
            Action<SettingsSaveData.Entry> changeValue)
        {
            this.view = view;
            this.catalog = catalog;
            this.selectCategory = selectCategory;
            this.changeValue = changeValue;
        }

        public void BuildNavigation()
        {
            view.ClearNavigation();

            foreach (SettingCategoryDefinition category in catalog.Categories)
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

        public void BuildCategory(
            SettingCategoryDefinition category,
            SettingsSaveData values,
            string search)
        {
            view.ClearContent();
            view.SetCategory(category);
            view.ContentScroll.scrollOffset = Vector2.zero;

            if (category == null)
            {
                view.Content.Add(new Label("No settings are configured."));
                return;
            }

            string query = search?.Trim() ?? string.Empty;
            int rowCount = 0;

            foreach (SettingSectionDefinition section in category.Sections)
            {
                if (section == null)
                    continue;

                VisualElement body = Element("settings-section__body");
                VisualElement lastRow = null;

                foreach (SettingDefinition setting in section.Settings)
                {
                    if (setting == null || !Matches(section, setting, query))
                        continue;

                    lastRow = BuildRow(setting);
                    body.Add(lastRow);
                    rowCount++;
                }

                if (lastRow == null)
                    continue;

                lastRow.AddToClassList("settings-row--last");

                VisualElement sectionElement = Element("settings-section");
                VisualElement header = Element("settings-section__header");

                header.Add(Text(section.Label, "settings-section__title"));
                header.Add(Text(
                    section.Description,
                    "settings-section__description"));

                sectionElement.Add(header);
                sectionElement.Add(body);
                view.Content.Add(sectionElement);
            }

            if (rowCount == 0)
                view.Content.Add(new Label("No matching settings."));

            view.RefreshValues(values);
        }

        private VisualElement BuildRow(SettingDefinition definition)
        {
            VisualElement row = Element("settings-row");
            row.name = $"Setting-{definition.Id}";

            VisualElement info = Element("settings-row__info");
            info.Add(Text(definition.Label, "settings-row__title"));
            info.Add(Text(definition.Description, "settings-row__description"));
            row.Add(info);

            switch (definition.ControlType)
            {
                case SettingControlType.Toggle:
                    AddToggle(row, definition);
                    break;

                case SettingControlType.Slider:
                    AddSlider(row, definition);
                    break;

                case SettingControlType.Dropdown:
                    AddDropdown(row, definition);
                    break;

                case SettingControlType.Keybind:
                case SettingControlType.Button:
                    AddUnavailableAction(row, definition);
                    break;
            }

            return row;
        }

        private void AddToggle(
            VisualElement row,
            SettingDefinition definition)
        {
            VisualElement group = Element("settings-row__toggle-group");

            Toggle control = new()
            {
                tooltip = definition.Label
            };
            control.AddToClassList("settings-row__toggle");

            Label valueLabel = Text(string.Empty, "settings-row__toggle-label");

            control.RegisterValueChangedCallback(evt =>
                changeValue(new SettingsSaveData.Entry
                {
                    Id = definition.Id,
                    ControlType = SettingControlType.Toggle,
                    ToggleValue = evt.newValue
                }));

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
            SettingDefinition definition)
        {
            Slider control = new(definition.Minimum, definition.Maximum)
            {
                tooltip = definition.Label,
                showInputField = false
            };
            control.AddToClassList("settings-row__slider");

            VisualElement group = Element("settings-row__slider-group");
            Label valueLabel = Text(string.Empty, "settings-row__value");

            control.RegisterValueChangedCallback(evt =>
                changeValue(new SettingsSaveData.Entry
                {
                    Id = definition.Id,
                    ControlType = SettingControlType.Slider,
                    SliderValue = evt.newValue
                }));

            view.RegisterValueWriter(definition.Id, value =>
            {
                control.SetValueWithoutNotify(value.SliderValue);
                valueLabel.text = FormatSlider(definition, value.SliderValue);
            });

            group.Add(valueLabel);
            row.Add(control);
            row.Add(group);
        }

        private void AddDropdown(
            VisualElement row,
            SettingDefinition definition)
        {
            List<string> ids = new();
            Dictionary<string, string> labels = new(StringComparer.Ordinal);

            foreach (SettingOptionDefinition option in definition.Options)
            {
                ids.Add(option.Id);
                labels.Add(option.Id, option.Label);
            }

            string DisplayLabel(string id)
            {
                return id != null && labels.TryGetValue(id, out string label)
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

            control.RegisterValueChangedCallback(evt =>
                changeValue(new SettingsSaveData.Entry
                {
                    Id = definition.Id,
                    ControlType = SettingControlType.Dropdown,
                    StringValue = evt.newValue
                }));

            view.RegisterValueWriter(definition.Id, value =>
                control.SetValueWithoutNotify(value.StringValue));

            row.Add(control);
        }

        private static void AddUnavailableAction(
            VisualElement row,
            SettingDefinition definition)
        {
            bool isKeybind =
                definition.ControlType == SettingControlType.Keybind;

            Button button = new()
            {
                text = isKeybind
                    ? "Rebind"
                    : string.IsNullOrWhiteSpace(definition.ButtonLabel)
                        ? "Adjust"
                        : definition.ButtonLabel,
                tooltip = isKeybind
                    ? "Input rebinding integration is not connected."
                    : "Command integration is not connected."
            };

            button.AddToClassList(isKeybind
                ? "settings-row__keybind"
                : "settings-row__button");

            button.SetEnabled(false);
            row.Add(button);
        }

        private static string FormatSlider(
            SettingDefinition definition,
            float value)
        {
            // Your catalog uses "%" for values already in the 0–100 range.
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
            SettingSectionDefinition section,
            SettingDefinition setting,
            string query)
        {
            return query.Length == 0 ||
                Contains(section.Label, query) ||
                Contains(setting.Id, query) ||
                Contains(setting.Label, query) ||
                Contains(setting.Description, query);
        }

        private static bool Contains(string text, string query)
        {
            return text?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
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