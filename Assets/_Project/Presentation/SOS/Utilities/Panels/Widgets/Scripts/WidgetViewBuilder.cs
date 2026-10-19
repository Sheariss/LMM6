using System;
using Atlas.SOS.Widgets;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetViewBuilder
    {
        public WidgetView Build(WidgetContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            WidgetDefinition definition = context.Definition;

            if (definition.WidgetUXML == null)
            {
                throw new InvalidOperationException(
                    $"Widget '{definition.WidgetID}' has no UXML.");
            }

            TemplateContainer root =
                definition.WidgetUXML.CloneTree();

            var binder = new WidgetBinder(root);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    $"Widget '{definition.WidgetID}' " +
                    "is missing required shell elements.");
            }

            // Whole-card launching requires no nested buttons.
            foreach (Button button in binder.Root.Query<Button>().ToList())
            {
                if (button != binder.Root)
                {
                    throw new InvalidOperationException(
                        $"Widget '{definition.WidgetID}' contains " +
                        "a nested Button. Use display elements " +
                        "inside the widget shell.");
                }
            }

            root.name = "Widget-" + context.InstanceID;
            root.AddToClassList("widget-instance");

            root.style.width = definition.Size.x;
            root.style.height = definition.Size.y;
            root.style.flexShrink = 0;

            binder.Root.style.width = Length.Percent(100);
            binder.Root.style.height = Length.Percent(100);

            return new WidgetView(root, binder);
        }
    }
}