using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetBinder : UIBinder
    {
        public VisualElement Root { get; }

        public Button CloseButton { get; }
        public Button AddButton { get; }

        public VisualElement Picker { get; }
        public DropdownField LayoutField { get; }
        public Image Preview { get; }
        public Label Description { get; }
        public Button ConfirmAddButton { get; }

        public Label Status { get; }
        public Label EmptyState { get; }
        public VisualElement Grid { get; }

        public WidgetBinder(VisualElement instanceRoot)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));

            Root = Bind<VisualElement>(
                instanceRoot, "WidgetsPanel");

            CloseButton = Bind<Button>(
                instanceRoot, "WidgetsCloseButton");

            AddButton = Bind<Button>(
                instanceRoot, "WidgetsAddButton");

            Picker = Bind<VisualElement>(
                instanceRoot, "WidgetPicker");

            LayoutField = Bind<DropdownField>(
                instanceRoot, "WidgetLayoutField");

            Preview = Bind<Image>(
                instanceRoot, "WidgetPreview");

            Description = Bind<Label>(
                instanceRoot, "WidgetDescription");

            ConfirmAddButton = Bind<Button>(
                instanceRoot, "WidgetConfirmAddButton");

            Status = Bind<Label>(
                instanceRoot, "WidgetsStatus");

            EmptyState = Bind<Label>(
                instanceRoot, "WidgetsEmptyState");

            Grid = Bind<VisualElement>(
                instanceRoot, "WidgetGrid");
        }
    }
}