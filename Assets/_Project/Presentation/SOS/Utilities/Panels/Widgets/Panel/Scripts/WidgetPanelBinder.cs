using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetPanelBinder : UIBinder
    {
        public VisualElement Root { get; }

        public Button DiscoverButton { get; }
        public Button WidgetsButton { get; }
        public Label PageTitle { get; }
        public Button AddButton { get; }

        public VisualElement DiscoverPage { get; }
        public VisualElement WidgetsPage { get; }

        public Label Status { get; }
        public Label EmptyState { get; }
        public VisualElement Grid { get; }

        public VisualElement Picker { get; }
        public Button ClosePickerButton { get; }
        public VisualElement CatalogList { get; }
        public Label CatalogEmpty { get; }

        public Label PreviewTitle { get; }
        public Image PreviewImage { get; }
        public Label PreviewEmpty { get; }
        public Label PreviewDescription { get; }
        public Button PinButton { get; }

        public WidgetPanelBinder(VisualElement instanceRoot)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));

            Root = Bind<VisualElement>(
                instanceRoot, "WidgetPanelRoot");

            DiscoverButton = Bind<Button>(
                instanceRoot, "DiscoverButton");

            WidgetsButton = Bind<Button>(
                instanceRoot, "WidgetsButton");

            PageTitle = Bind<Label>(
                instanceRoot, "PageTitle");

            AddButton = Bind<Button>(
                instanceRoot, "AddWidgetsButton");

            DiscoverPage = Bind<VisualElement>(
                instanceRoot, "DiscoverPage");

            WidgetsPage = Bind<VisualElement>(
                instanceRoot, "WidgetsPage");

            Status = Bind<Label>(
                instanceRoot, "WidgetPanelStatusLabel");

            EmptyState = Bind<Label>(
                instanceRoot, "WidgetPanelEmptyState");

            Grid = Bind<VisualElement>(
                instanceRoot, "WidgetPanelGrid");

            Picker = Bind<VisualElement>(
                instanceRoot, "WidgetPicker");

            ClosePickerButton = Bind<Button>(
                instanceRoot, "ClosePickerButton");

            CatalogList = Bind<VisualElement>(
                instanceRoot, "WidgetCatalogList");

            CatalogEmpty = Bind<Label>(
                instanceRoot, "WidgetCatalogEmpty");

            PreviewTitle = Bind<Label>(
                instanceRoot, "WidgetPreviewTitle");

            PreviewImage = Bind<Image>(
                instanceRoot, "WidgetPreviewImage");

            PreviewEmpty = Bind<Label>(
                instanceRoot, "WidgetPreviewEmpty");

            PreviewDescription = Bind<Label>(
                instanceRoot, "WidgetPreviewDescription");

            PinButton = Bind<Button>(
                instanceRoot, "PinWidgetButton");
        }
    }
}