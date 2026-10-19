using System;
using Atlas.SOS.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetPanelView
    {
        private readonly WidgetPanelViewBuilder builder;

        public TemplateContainer Host { get; }
        public WidgetPanelBinder Binder { get; }

        public WidgetPanelView(
            TemplateContainer host,
            WidgetPanelBinder binder,
            WidgetPanelViewBuilder builder)
        {
            Host = host ??
                throw new ArgumentNullException(nameof(host));

            Binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.builder = builder ??
                throw new ArgumentNullException(nameof(builder));

            Binder.PreviewImage.scaleMode = ScaleMode.ScaleToFit;
            Binder.PreviewTitle.enableRichText = false;
            Binder.PreviewDescription.enableRichText = false;
        }

        public void RenderNavigation(WidgetPanelEngine engine)
        {
            bool discover = engine.Page == WidgetPanelPage.Discover;

            Binder.PageTitle.text = discover ? "Discover" : "Widgets";

            Binder.DiscoverButton.EnableInClassList(
                "is-selected", discover);

            Binder.WidgetsButton.EnableInClassList(
                "is-selected", !discover);

            SetVisible(Binder.DiscoverPage, discover);
            SetVisible(Binder.WidgetsPage, !discover);
            SetVisible(Binder.AddButton, !discover);
            SetVisible(Binder.Picker, engine.PickerOpen);

            // Prevent keyboard focus entering the covered content.
            Binder.DiscoverButton.SetEnabled(!engine.PickerOpen);
            Binder.WidgetsButton.SetEnabled(!engine.PickerOpen);
            Binder.AddButton.SetEnabled(!engine.PickerOpen);
            Binder.WidgetsPage.SetEnabled(!engine.PickerOpen);
        }

        public void RenderCatalog(
            WidgetPanelEngine engine,
            Action<string> onSelect)
        {
            Binder.CatalogList.Clear();

            var definitions = engine.GetAvailableDefinitions();

            SetVisible(Binder.CatalogEmpty, definitions.Count == 0);

            foreach (WidgetDefinition definition in definitions)
            {
                Binder.CatalogList.Add(
                    builder.BuildCatalogChoice(
                        definition,
                        engine.GetIcon(definition),
                        definition == engine.SelectedDefinition,
                        onSelect));
            }

            RenderSelection(engine);
        }

        public void RenderSelection(WidgetPanelEngine engine)
        {
            WidgetDefinition selected = engine.SelectedDefinition;

            Binder.PreviewTitle.text =
                selected?.DisplayName ?? "Choose a widget";

            Binder.PreviewDescription.text =
                selected?.Description ?? string.Empty;

            Binder.PreviewImage.sprite = selected?.PreviewImage;

            bool hasPreview = selected?.PreviewImage != null;

            SetVisible(Binder.PreviewImage, hasPreview);
            SetVisible(Binder.PreviewEmpty, selected != null && !hasPreview);

            int count = selected != null
                ? engine.GetCount(selected.WidgetID)
                : 0;

            Binder.PinButton.text = count > 1
                ? "Unpin all"
                : count == 1
                    ? "Unpin widget"
                    : "Pin widget";

            Binder.PinButton.SetEnabled(selected != null);
        }

        public void Show()
        {
            Host.style.display = DisplayStyle.Flex;
            Host.BringToFront();
            Binder.DiscoverButton.Focus();
        }

        public void Hide()
        {
            Host.style.display = DisplayStyle.None;
        }

        public void Attach(VisualElement widgetRoot)
        {
            Binder.Grid.Add(widgetRoot);
        }

        public void ClearWidgets()
        {
            Binder.Grid.Clear();
        }

        public void SetState(
            int widgetCount,
            int failedCount,
            float minimumGridWidth)
        {
            Binder.Grid.style.minWidth = minimumGridWidth;

            SetVisible(Binder.EmptyState, widgetCount == 0);

            Binder.Status.text = failedCount > 0
                ? $"{failedCount} widget(s) could not be displayed."
                : string.Empty;

            SetVisible(Binder.Status, failedCount > 0);
        }

        public void Detach()
        {
            Binder.CatalogList.Clear();
            Binder.PreviewImage.sprite = null;
            Host.RemoveFromHierarchy();
        }

        private static void SetVisible(
            VisualElement element,
            bool visible)
        {
            element.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}