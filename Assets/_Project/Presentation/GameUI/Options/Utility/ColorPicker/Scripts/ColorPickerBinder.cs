using System;
using System.Collections.Generic;
using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.ColorPicker
{
    public sealed class ColorPickerBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Label Title { get; }
        public Button CloseButton { get; }

        public VisualElement SvArea { get; }
        public VisualElement SvGradient { get; }
        public VisualElement SvSelector { get; }
        public VisualElement HueGradient { get; }
        public VisualElement AlphaCheckerboard { get; }
        public VisualElement AlphaGradient { get; }

        public Slider HueSlider { get; }
        public Slider AlphaSlider { get; }
        public DropdownField FormatDropdown { get; }
        public TextField AlphaInput { get; }
        public Label InputError { get; }

        public VisualElement PresetSwatches { get; }
        public VisualElement CustomSwatches { get; }
        public Button AddPresetButton { get; }
        public Button AddCustomButton { get; }
        public Button EditPresetsButton { get; }
        public Button EditCustomButton { get; }
        public Label CustomEmptyMessage { get; }

        public Dictionary<ColorPickerFormat, VisualElement> Groups { get; }
            = new();

        public Dictionary<ColorPickerFormat, TextField[]> Fields { get; }
            = new();

        public ColorPickerBinder(VisualElement documentRoot)
        {
            if (documentRoot == null)
            {
                Debug.LogError("[ColorPickerBinder] Document root is null.");
                IsValid = false;
                return;
            }

            Root = documentRoot.name == "color-picker"
                ? documentRoot
                : Bind<VisualElement>(documentRoot, "color-picker");

            if (Root == null)
                return;

            Title = Bind<Label>(Root, "picker-title");
            CloseButton = Bind<Button>(Root, "close-button");

            SvArea = Bind<VisualElement>(Root, "sv-area");
            SvGradient = Bind<VisualElement>(Root, "sv-gradient");
            SvSelector = Bind<VisualElement>(Root, "sv-selector");
            HueGradient = Bind<VisualElement>(Root, "hue-gradient");
            AlphaCheckerboard = Bind<VisualElement>(Root, "alpha-checkerboard");
            AlphaGradient = Bind<VisualElement>(Root, "alpha-gradient");

            HueSlider = Bind<Slider>(Root, "hue-slider");
            AlphaSlider = Bind<Slider>(Root, "alpha-slider");
            FormatDropdown = Bind<DropdownField>(Root, "format-dropdown");
            AlphaInput = Bind<TextField>(Root, "alpha-input");
            InputError = Bind<Label>(Root, "input-error");

            PresetSwatches = Bind<VisualElement>(Root, "preset-swatches");
            CustomSwatches = Bind<VisualElement>(Root, "custom-swatches");
            AddPresetButton = Bind<Button>(Root, "add-preset-button");
            AddCustomButton = Bind<Button>(Root, "add-custom-button");
            EditPresetsButton = Bind<Button>(Root, "edit-presets-button");
            EditCustomButton = Bind<Button>(Root, "edit-custom-button");
            CustomEmptyMessage = Bind<Label>(Root, "custom-empty-message");

            AddFields(ColorPickerFormat.HEX, "hex-fields", "hex-input");
            AddFields(ColorPickerFormat.RGB, "rgb-fields", "rgb-r", "rgb-g", "rgb-b");
            AddFields(ColorPickerFormat.HSL, "hsl-fields", "hsl-h", "hsl-s", "hsl-l");
            AddFields(ColorPickerFormat.HSV, "hsv-fields", "hsv-h", "hsv-s", "hsv-v");
            AddFields(ColorPickerFormat.OKLCH, "oklch-fields",
                "oklch-l", "oklch-c", "oklch-h");

            if (!IsValid)
                return;

            FormatDropdown.choices =
                new List<string>(Enum.GetNames(typeof(ColorPickerFormat)));

            AlphaInput.isDelayed = true;

            foreach (TextField[] fields in Fields.Values)
            {
                foreach (TextField field in fields)
                    field.isDelayed = true;
            }
        }

        private void AddFields(
            ColorPickerFormat format,
            string groupName,
            params string[] fieldNames)
        {
            Groups.Add(format, Bind<VisualElement>(Root, groupName));

            TextField[] fields = new TextField[fieldNames.Length];

            for (int i = 0; i < fields.Length; i++)
                fields[i] = Bind<TextField>(Root, fieldNames[i]);

            Fields.Add(format, fields);
        }
    }
}