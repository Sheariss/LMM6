using System;
using System.Collections.Generic;
using Atlas.SOS.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetView
    {
        private readonly WidgetBinder binder;
        private readonly WidgetViewBuilder builder;

        private List<WidgetDefinition> choices = new();

        public bool PickerVisible { get; private set; }

        public WidgetDefinition SelectedDefinition
        {
            get
            {
                int index = binder.LayoutField.index;

                return index >= 0 && index < choices.Count
                    ? choices[index]
                    : null;
            }
        }

        public WidgetView(
            WidgetBinder binder,
            WidgetViewBuilder builder)
        {
            this.binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.builder = builder ??
                throw new ArgumentNullException(nameof(builder));

            binder.Preview.scaleMode = ScaleMode.ScaleToFit;

            ShowPicker(false);
            SetStatus(null);
        }

        public WidgetFrame BuildFrame(
            WidgetContext context,
            Sprite icon)
        {
            return builder.Build(context, icon);
        }

        public void Attach(WidgetFrame frame)
        {
            binder.Grid.Add(frame.Root);
        }

        public void SetCatalog(List<WidgetDefinition> definitions)
        {
            string previousID = SelectedDefinition?.WidgetID;

            choices = definitions;
            var labels = new List<string>(choices.Count);
            int selectedIndex = 0;

            for (int i = 0; i < choices.Count; i++)
            {
                WidgetDefinition definition = choices[i];

                // The ID distinguishes layouts with matching display names.
                labels.Add(
                    $"{definition.DisplayName} ({definition.WidgetID})");

                if (definition.WidgetID == previousID)
                    selectedIndex = i;
            }

            binder.LayoutField.choices = labels;
            binder.LayoutField.SetValueWithoutNotify(
                labels.Count > 0 ? labels[selectedIndex] : string.Empty);

            binder.LayoutField.SetEnabled(labels.Count > 0);
        }

        public void SetSelection(bool canAdd, string reason)
        {
            WidgetDefinition definition = SelectedDefinition;

            binder.Preview.sprite = definition?.PreviewImage;
            binder.Preview.style.display =
                definition?.PreviewImage != null
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            string description = definition?.Description ??
                "No widgets are available.";

            if (!string.IsNullOrWhiteSpace(reason))
                description += "\n" + reason;

            binder.Description.text = description;
            binder.ConfirmAddButton.SetEnabled(
                definition != null && canAdd);
        }

        public void RefreshInstances(
            IReadOnlyList<WidgetContext> instances)
        {
            binder.EmptyState.style.display = instances.Count == 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            // Large fixed layouts remain reachable through horizontal scroll.
            float minimumWidth = 0f;

            foreach (WidgetContext instance in instances)
            {
                minimumWidth = Mathf.Max(
                    minimumWidth,
                    instance.Definition.Size.x);
            }

            binder.Grid.style.minWidth = minimumWidth;
        }

        public void ShowPicker(bool visible)
        {
            PickerVisible = visible;
            binder.Picker.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (visible)
                binder.LayoutField.Focus();
        }

        public void SetStatus(string message)
        {
            binder.Status.text = message ?? string.Empty;
            binder.Status.style.display =
                string.IsNullOrWhiteSpace(message)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
        }

        public void Clear()
        {
            binder.Grid.Clear();
            binder.Preview.sprite = null;
            choices.Clear();

            binder.LayoutField.choices = new List<string>();
            binder.LayoutField.SetValueWithoutNotify(string.Empty);

            ShowPicker(false);
            SetStatus(null);
        }
    }
}