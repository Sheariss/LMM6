using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.SOS.Widgets
{
    [Serializable]
    public sealed class WidgetDefinition
    {
        [Header("Identity")]
        [SerializeField] private string widgetID;
        [SerializeField] private string displayName;

        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Associated App")]
        [SerializeField] private string associatedAppID;

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite previewImage;

        [Tooltip("Complete widget UXML, including its unpacked shell.")]
        [SerializeField] private VisualTreeAsset widgetUXML;

        [Tooltip("Total widget size, including header and footer.")]
        [SerializeField] private Vector2 size = new(400f, 320f);

        [Header("Instances")]
        [SerializeField] private bool allowMultipleInstances;

        public string WidgetID => widgetID;
        public string DisplayName => displayName;
        public string Description => description;
        public string AssociatedAppID => associatedAppID;

        public Sprite Icon => icon;
        public Sprite PreviewImage => previewImage;
        public VisualTreeAsset WidgetUXML => widgetUXML;
        public Vector2 Size => size;

        public bool AllowMultipleInstances => allowMultipleInstances;

        public bool HasAssociatedApp =>
            !string.IsNullOrWhiteSpace(associatedAppID);
    }

    [CreateAssetMenu(
        fileName = "Widget Catalog",
        menuName = "ATLAS/Widget Catalog")]
    public sealed class WidgetCatalog : ScriptableObject
    {
        [SerializeField] private List<WidgetDefinition> widgets = new();

        public IReadOnlyList<WidgetDefinition> Widgets => widgets;

        public bool TryGetWidget(
            string widgetID,
            out WidgetDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(widgetID) || widgets == null)
                return false;

            foreach (WidgetDefinition widget in widgets)
            {
                if (widget != null &&
                    string.Equals(
                        widget.WidgetID,
                        widgetID,
                        StringComparison.Ordinal))
                {
                    definition = widget;
                    return true;
                }
            }

            return false;
        }
    }
}