using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetShellBinder : UIBinder
    {
        public Button Root { get; }
        public VisualElement Header { get; }
        public Image Icon { get; }
        public Label Title { get; }
        public VisualElement Content { get; }
        public VisualElement Footer { get; }
        public Label ActionLabel { get; }
        public Label ActionArrow { get; }

        public WidgetShellBinder(VisualElement instanceRoot)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));

            Root = Bind<Button>(
                instanceRoot, "WidgetRoot");

            Header = Bind<VisualElement>(
                instanceRoot, "WidgetHeader");

            Icon = Bind<Image>(
                instanceRoot, "WidgetIcon");

            Title = Bind<Label>(
                instanceRoot, "WidgetTitle");

            Content = Bind<VisualElement>(
                instanceRoot, "WidgetContent");

            Footer = Bind<VisualElement>(
                instanceRoot, "WidgetFooter");

            ActionLabel = Bind<Label>(
                instanceRoot, "WidgetActionLabel");

            ActionArrow = Bind<Label>(
                instanceRoot, "WidgetActionArrow");
        }
    }
}