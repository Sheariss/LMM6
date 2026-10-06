using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Atlas.Core.Settings
{
    [CreateAssetMenu(
        fileName = "SettingsCatalog",
        menuName = "ATLAS/Settings/Settings Catalog")]
    public sealed class SettingsCatalog : ScriptableObject
    {
        [SerializeField]
        private List<SettingCategoryDefinition> categories = new();

        public IReadOnlyList<SettingCategoryDefinition> Categories =>
            categories;

        public SettingDefinition GetSetting(string id)
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
                            string.Equals(setting.Id, id, StringComparison.Ordinal))
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
    public sealed class SettingCategoryDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<SettingSectionDefinition> sections = new();


        public string Id => id;
        public string Label => label;
        public string Description => description;
        public IReadOnlyList<SettingSectionDefinition> Sections => sections;
    }

    [Serializable]
    public sealed class SettingSectionDefinition
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<SettingDefinition> settings = new();

        public string Id => id;
        public string Label => label;
        public string Description => description;
        public IReadOnlyList<SettingDefinition> Settings => settings;
    }

    public enum SettingControlType
    {
        Toggle = 0,
        Slider = 1,
        Dropdown = 2,
        Keybind = 3,
        Button = 4
    }

    [Serializable]
    public sealed class SettingDefinition
    {
        [Header("Identity")]
        [Tooltip("Stable, catalog-wide ID. Example: audio.master")]
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;

        [Header("Presentation")]
        [SerializeField] private SettingControlType controlType;

        [Header("Toggle")]
        [SerializeField] private bool defaultToggleValue;

        [Header("Slider")]
        [SerializeField] private float defaultSliderValue = 1f;
        [SerializeField] private float minimum;
        [SerializeField] private float maximum = 1f;

        [Tooltip("Zero means continuous. Positive values specify increments.")]
        [SerializeField, Min(0f)] private float step = 0.01f;

        [Tooltip("Numeric display format. Examples: P0 for percent, F1 for decimals.")]
        [SerializeField] private string valueFormat = "P0";

        [Header("Dropdown")]
        [SerializeField] private List<SettingOptionDefinition> options = new();

        [Tooltip("The stable ID of the default option.")]
        [SerializeField] private string defaultOptionId;

        [Header("Keybind")]
        [Tooltip("Stable action ID used to locate the action being rebound.")]
        [SerializeField] private string actionId;

        [SerializeField] private bool allowKeyCombinations = true;

        [Header("Button")]
        [SerializeField] private string buttonLabel = "Adjust";

        [Tooltip("Identifies the command the UI invokes when clicked.")]
        [SerializeField] private string commandId;

        public string Id => id;
        public string Label => label;
        public string Description => description;
        public SettingControlType ControlType => controlType;

        public bool DefaultToggleValue => defaultToggleValue;

        public float DefaultSliderValue => defaultSliderValue;
        public float Minimum => minimum;
        public float Maximum => maximum;
        public float Step => step;
        public string ValueFormat => valueFormat;

        public IReadOnlyList<SettingOptionDefinition> Options => options;
        public string DefaultOptionId => defaultOptionId;

        public string ActionId => actionId;
        public bool AllowKeyCombinations => allowKeyCombinations;

        public string ButtonLabel => buttonLabel;
        public string CommandId => commandId;

    }

    [Serializable]
    public sealed class SettingOptionDefinition
    {
        [Tooltip("Stable value stored by the settings system. Example: windowed")]
        [SerializeField] private string id;

        [Tooltip("Text displayed in the dropdown. Example: Windowed")]
        [SerializeField] private string label;

        public string Id => id;
        public string Label => label;
    }
}