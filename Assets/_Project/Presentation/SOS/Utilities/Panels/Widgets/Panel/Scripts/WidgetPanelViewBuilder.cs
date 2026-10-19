using System;
using Atlas.SOS.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetPanelViewBuilder
    {
        public WidgetPanelView Build(VisualTreeAsset panelTemplate)
        {
            if (panelTemplate == null)
                throw new ArgumentNullException(nameof(panelTemplate));

            TemplateContainer host = panelTemplate.CloneTree();

            host.name = "WidgetPanelHost";
            host.pickingMode = PickingMode.Ignore;

            host.style.position = Position.Absolute;
            host.style.left = 12;
            host.style.right = 12;
            host.style.top = 12;
            host.style.bottom = 12;
            host.style.display = DisplayStyle.None;

            var binder = new WidgetPanelBinder(host);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "The widget panel is missing required elements.");
            }

            return new WidgetPanelView(host, binder, this);
        }

        public Button BuildCatalogChoice(
            WidgetDefinition definition,
            Sprite icon,
            bool selected,
            Action<string> onSelect)
        {
            var button = new Button(
                () => onSelect(definition.WidgetID));

            button.tooltip = definition.DisplayName;
            button.AddToClassList("widget-picker__choice");
            button.EnableInClassList("is-selected", selected);

            var image = new Image
            {
                sprite = icon,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };

            image.AddToClassList("widget-picker__choice-icon");

            image.style.display = icon != null
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            var label = new Label(definition.DisplayName)
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList("widget-picker__choice-label");

            button.Add(image);
            button.Add(label);

            return button;
        }
    }
}