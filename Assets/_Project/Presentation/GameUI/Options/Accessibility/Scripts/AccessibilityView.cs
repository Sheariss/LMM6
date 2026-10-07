using System;
using System.Collections.Generic;
using Atlas.Core.Accessibility;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Accessibility
{
    public sealed class AccessibilityView
    {
        private readonly AccessibilityBinder binder;

        private readonly Dictionary<string, Button> navigation = new();

        private readonly Dictionary<
            string, Action<AccessibilitySaveData.Entry>> valueWriters = new();

        public VisualElement Content => binder.Content;
        public ScrollView ContentScroll => binder.ContentScroll;

        public AccessibilityView(AccessibilityBinder binder)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot create AccessibilityView with an invalid binder.");
            }

            this.binder = binder;
        }

        public void SetHeader(string title, string description)
        {
            binder.TitleLabel.text = title;
            binder.SubtitleLabel.text = description;
        }

        public void SetCategory(AccessibilityCategoryDefinition category)
        {
            binder.CategoryTitleLabel.text =
                category?.Label ?? "Accessibility";

            binder.CategoryDescriptionLabel.text =
                category?.Description ?? string.Empty;

            SelectNavigation(category?.Id);
        }

        public void SetSearchResults(int count)
        {
            binder.CategoryTitleLabel.text = "Search results";
            binder.CategoryDescriptionLabel.text =
                $"{count} matching option{(count == 1 ? "" : "s")} across all categories.";

            SelectNavigation(null);
        }

        private void SelectNavigation(string id)
        {
            foreach (var pair in navigation)
            {
                pair.Value.EnableInClassList(
                    "settings-shell__nav-button--selected",
                    pair.Key == id);
            }
        }

        public void ClearNavigation()
        {
            navigation.Clear();
            binder.Navigation.Clear();
        }

        public void AddNavigation(string id, Button button)
        {
            navigation.Add(id, button);
            binder.Navigation.Add(button);
        }

        public void ClearContent()
        {
            valueWriters.Clear();
            Content.Clear();
        }

        public void RegisterValueWriter(
            string id,
            Action<AccessibilitySaveData.Entry> writer)
        {
            valueWriters.Add(id, writer);
        }

        public void RefreshValues(AccessibilitySaveData values)
        {
            foreach (var pair in valueWriters)
            {
                var entry = values.Get(pair.Key);

                if (entry != null)
                    pair.Value(entry);
            }
        }

        public void SetDirty(bool dirty)
        {
            binder.ApplyButton.SetEnabled(dirty);
            binder.StatusLabel.text =
                dirty ? "Unapplied changes" : string.Empty;
        }

        public void Show()
        {
            binder.Root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            binder.Root.style.display = DisplayStyle.None;
        }
    }
}