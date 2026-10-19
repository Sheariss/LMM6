using System;
using Atlas.SOS.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetView
    {
        public TemplateContainer Root { get; }
        public WidgetBinder Binder { get; }

        public VisualElement Content => Binder.Content;

        public WidgetView(
            TemplateContainer root,
            WidgetBinder binder)
        {
            Root = root ??
                throw new ArgumentNullException(nameof(root));

            Binder = binder ??
                throw new ArgumentNullException(nameof(binder));
        }

        public void Render(
            WidgetDefinition definition,
            Sprite icon,
            bool canOpen)
        {
            Binder.Title.enableRichText = false;
            Binder.ActionLabel.enableRichText = false;

            Binder.Title.text = definition.DisplayName;

            Binder.Icon.sprite = icon;
            Binder.Icon.scaleMode = ScaleMode.ScaleToFit;
            Binder.Icon.style.display = icon != null
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            Binder.Root.SetEnabled(canOpen);
            Binder.Root.focusable = canOpen;

            Binder.Root.tooltip = canOpen
                ? $"Open {definition.DisplayName}"
                : definition.DisplayName;

            Binder.Footer.style.display = definition.HasAssociatedApp
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void Detach()
        {
            Root.RemoveFromHierarchy();
        }
    }
}