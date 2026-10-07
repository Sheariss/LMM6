using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Core.Accessibility
{
    [CreateAssetMenu(
        fileName = "AccessibilityCatalog",
        menuName = "ATLAS/Accessibility/Accessibility Catalog")]
    public sealed class AccessibilityCatalog : ScriptableObject
    {
        [SerializeField]
        private List<AccessibilityCategoryDefinition> categories = new();

        public IReadOnlyList<AccessibilityCategoryDefinition> Categories =>
            categories;

        public AccessibilitySettingDefinition GetSetting(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (var category in categories)
            {
                if (category == null)
                    continue;

                foreach (var section in category.Sections)
                {
                    if (section == null)
                        continue;

                    foreach (var setting in section.Settings)
                    {
                        if (setting != null &&
                            string.Equals(
                                setting.Id,
                                id,
                                StringComparison.Ordinal))
                        {
                            return setting;
                        }
                    }
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class AccessibilityCategoryDefinition
    {
        [Tooltip("Stable category ID. Example: visual")]
        [SerializeField] private string id;

        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;

        [Tooltip("Additional search terms for this category.")]
        [SerializeField] private List<string> keywords = new();

        [SerializeField]
        private List<AccessibilitySectionDefinition> sections = new();

        public string Id => id;
        public string Label => label;
        public string Description => description;
        public IReadOnlyList<string> Keywords => keywords;

        public IReadOnlyList<AccessibilitySectionDefinition> Sections =>
            sections;
    }

    [Serializable]
    public sealed class AccessibilitySectionDefinition
    {
        [Tooltip("Stable section ID. Example: visual.readability")]
        [SerializeField] private string id;

        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;

        [Tooltip("Additional search terms for this section.")]
        [SerializeField] private List<string> keywords = new();

        [SerializeField]
        private List<AccessibilitySettingDefinition> settings = new();

        public string Id => id;
        public string Label => label;
        public string Description => description;
        public IReadOnlyList<string> Keywords => keywords;

        public IReadOnlyList<AccessibilitySettingDefinition> Settings =>
            settings;
    }

    public enum AccessibilityControlType
    {
        Toggle = 0,
        Slider = 1,
        Dropdown = 2,
        Color = 3,
        Font = 4
    }

    [Serializable]
    public sealed class AccessibilitySettingDefinition
    {
        [Header("Identity")]

        [Tooltip(
            "Stable, catalog-wide ID. Example: visual.text-size. " +
            "Keep this unchanged after saved preferences use it.")]
        [SerializeField] private string id;

        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;

        [Tooltip(
            "Alternative search terms. Example: captions, dialogue, reading.")]
        [SerializeField] private List<string> keywords = new();

        [Header("Presentation")]

        [SerializeField] private AccessibilityControlType controlType;

        [Header("Toggle")]

        [SerializeField] private bool defaultToggleValue;

        [Header("Slider")]

        [SerializeField] private float defaultSliderValue = 1f;
        [SerializeField] private float minimum;
        [SerializeField] private float maximum = 1f;

        [Tooltip("Zero means continuous. Positive values specify increments.")]
        [SerializeField, Min(0f)] private float step = 0.01f;

        [Tooltip(
            "Numeric display format. Examples: P0 for 0–1 percentages, " +
            "F1 for decimals, or % for values already in the 0–100 range.")]
        [SerializeField] private string valueFormat = "P0";

        [Header("Dropdown")]

        [SerializeField]
        private List<AccessibilityOptionDefinition> options = new();

        [Tooltip("Stable ID of the default dropdown option.")]
        [SerializeField] private string defaultOptionId;

        [Header("Color")]

        [Tooltip("Default non-HDR color.")]
        [SerializeField, ColorUsage(true, false)]
        private Color defaultColorValue = Color.white;

        [Tooltip(
            "Whether players can adjust transparency. " +
            "When disabled, the engine should enforce an alpha of 1.")]
        [SerializeField] private bool allowAlpha;

        [Tooltip("Optional named swatches offered by the color control.")]
        [SerializeField]
        private List<AccessibilityColorPresetDefinition> colorPresets = new();

        [Header("Font")]

        [Tooltip("Fonts available for this setting.")]
        [SerializeField]
        private List<AccessibilityFontOptionDefinition> fontOptions = new();

        [Tooltip("Stable ID of the default font option.")]
        [SerializeField] private string defaultFontId;

        [SerializeField, TextArea]
        private string fontPreviewText =
            "The quick brown fox jumps over the lazy dog. 0123456789";

        public string Id => id;
        public string Label => label;
        public string Description => description;
        public IReadOnlyList<string> Keywords => keywords;

        public AccessibilityControlType ControlType => controlType;

        public bool DefaultToggleValue => defaultToggleValue;

        public float DefaultSliderValue => defaultSliderValue;
        public float Minimum => minimum;
        public float Maximum => maximum;
        public float Step => step;
        public string ValueFormat => valueFormat;

        public IReadOnlyList<AccessibilityOptionDefinition> Options =>
            options;

        public string DefaultOptionId => defaultOptionId;

        public Color DefaultColorValue => defaultColorValue;
        public bool AllowAlpha => allowAlpha;

        public IReadOnlyList<AccessibilityColorPresetDefinition> ColorPresets =>
            colorPresets;

        public IReadOnlyList<AccessibilityFontOptionDefinition> FontOptions =>
            fontOptions;

        public string DefaultFontId => defaultFontId;
        public string FontPreviewText => fontPreviewText;
    }

    [Serializable]
    public sealed class AccessibilityOptionDefinition
    {
        [Tooltip("Stable value stored in preferences. Example: high-contrast")]
        [SerializeField] private string id;

        [Tooltip("Text displayed to the player.")]
        [SerializeField] private string label;

        public string Id => id;
        public string Label => label;
    }

    [Serializable]
    public sealed class AccessibilityColorPresetDefinition
    {
        [Tooltip("Display name for the swatch. Example: Warm Yellow")]
        [SerializeField] private string label;

        [SerializeField, ColorUsage(true, false)]
        private Color value = Color.white;

        public string Label => label;
        public Color Value => value;
    }

    [Serializable]
    public sealed class AccessibilityFontOptionDefinition
    {
        [Tooltip(
            "Stable value stored in preferences. " +
            "The font asset itself is not stored in save data.")]
        [SerializeField] private string id;

        [Tooltip("Text displayed in the font dropdown.")]
        [SerializeField] private string label;

        [SerializeField] private Font font;

        public string Id => id;
        public string Label => label;
        public Font Font => font;
    }
}