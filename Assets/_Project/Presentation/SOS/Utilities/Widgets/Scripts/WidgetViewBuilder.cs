using System;
using Atlas.SOS.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetFrame
    {
        public VisualElement Root { get; }
        public VisualElement Content { get; }
        public Button OpenButton { get; }

        public WidgetFrame(
            VisualElement root,
            VisualElement content,
            Button openButton)
        {
            Root = root;
            Content = content;
            OpenButton = openButton;
        }
    }

    public sealed class WidgetViewBuilder
    {
        public WidgetFrame Build(
            WidgetContext context,
            Sprite icon)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            WidgetDefinition definition = context.Definition;

            if (definition.WidgetUXML == null)
            {
                throw new InvalidOperationException(
                    $"Widget '{definition.WidgetID}' has no UXML.");
            }

            TemplateContainer instance =
                definition.WidgetUXML.CloneTree();

            var shell = new WidgetShellBinder(instance);

            if (!shell.IsValid)
            {
                throw new InvalidOperationException(
                    $"Widget '{definition.WidgetID}' " +
                    "is missing required shell elements.");
            }

            instance.name = "Widget-" + context.InstanceID;
            instance.AddToClassList("widget-instance");

            instance.style.width = definition.Size.x;
            instance.style.height = definition.Size.y;
            instance.style.flexShrink = 0;

            shell.Root.style.width = Length.Percent(100);
            shell.Root.style.height = Length.Percent(100);

            shell.Title.enableRichText = false;
            shell.ActionLabel.enableRichText = false;
            shell.Title.text = definition.DisplayName;

            shell.Icon.sprite = icon;
            shell.Icon.scaleMode = ScaleMode.ScaleToFit;
            shell.Icon.style.display = icon != null
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            bool opensApp = definition.HasAssociatedApp;

            shell.Root.SetEnabled(opensApp);
            shell.Root.focusable = opensApp;
            shell.Root.tooltip = opensApp
                ? $"Open {definition.DisplayName}"
                : definition.DisplayName;

            shell.Footer.style.display = opensApp
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // Footer wording comes from the individual widget UXML.
            // Examples: "See full forecast" or "Open Calendar".

            return new WidgetFrame(
                instance,
                shell.Content,
                shell.Root);
        }
    }
}