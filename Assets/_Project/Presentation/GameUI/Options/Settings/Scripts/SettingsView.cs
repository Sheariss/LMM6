using System;
using System.Collections.Generic;
using Atlas.Core.Settings;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Settings
{
    public sealed class SettingsView
    {
        private readonly Label title;
        private readonly Label subtitle;
        private readonly Label categoryTitle;
        private readonly Label categoryDescription;
        private readonly Label status;

        private readonly Dictionary<string, Button> navigation = new();
        private readonly Dictionary<string, Action<SettingsSaveData.Entry>>
            valueWriters = new();

        public VisualElement Root { get; }
        public VisualElement Navigation { get; }
        public VisualElement Content { get; }
        public ScrollView ContentScroll { get; }

        public TextField SearchField { get; }
        public Button SearchClearButton { get; }
        public Button BackButton { get; }
        public Button ResetButton { get; }
        public Button ApplyButton { get; }
        public Button DoneButton { get; }

        public SettingsView(SettingsBinder binder)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "Cannot create SettingsView with an invalid SettingsBinder.");
            }

            Root = binder.Root;

            title = binder.TitleLabel;
            subtitle = binder.SubtitleLabel;
            categoryTitle = binder.CategoryTitleLabel;
            categoryDescription = binder.CategoryDescriptionLabel;
            status = binder.StatusLabel;

            Navigation = binder.Navigation;
            Content = binder.Content;
            ContentScroll = binder.ContentScroll;

            SearchField = binder.SearchField;
            SearchClearButton = binder.SearchClearButton;
            BackButton = binder.BackButton;
            ResetButton = binder.ResetButton;
            ApplyButton = binder.ApplyButton;
            DoneButton = binder.DoneButton;
        }

        public void SetHeader(string heading, string description)
        {
            title.text = heading;
            subtitle.text = description;
        }

        public void SetCategory(SettingCategoryDefinition category)
        {
            categoryTitle.text = category?.Label ?? "Settings";
            categoryDescription.text = category?.Description ?? string.Empty;

            foreach (var pair in navigation)
            {
                pair.Value.EnableInClassList(
                    "settings-shell__nav-button--selected",
                    pair.Key == category?.Id);
            }
        }

        public void SetSearchResults(int count)
        {
            categoryTitle.text = "Search results";
            categoryDescription.text =
                $"{count} matching option{(count == 1 ? "" : "s")} across all categories.";

            foreach (var pair in navigation)
            {
                pair.Value.EnableInClassList(
                    "settings-shell__nav-button--selected",
                    false);
            }
        }

        public void ClearNavigation()
        {
            navigation.Clear();
            Navigation.Clear();
        }

        public void AddNavigation(string id, Button button)
        {
            navigation.Add(id, button);
            Navigation.Add(button);
        }

        public void ClearContent()
        {
            valueWriters.Clear();
            Content.Clear();
        }

        public void RegisterValueWriter(
            string id,
            Action<SettingsSaveData.Entry> writer)
        {
            valueWriters.Add(id, writer);
        }

        public void RefreshValues(SettingsSaveData data)
        {
            foreach (var pair in valueWriters)
            {
                SettingsSaveData.Entry entry = data.Get(pair.Key);

                if (entry != null)
                    pair.Value(entry);
            }
        }

        public void SetDirty(bool dirty)
        {
            ApplyButton.SetEnabled(dirty);
            status.text = dirty ? "Unapplied changes" : string.Empty;
        }

        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            Root.style.display = DisplayStyle.None;
        }

        private static T Require<T>(VisualElement root, string name)
            where T : VisualElement
        {
            return root.Q<T>(name) ??
                throw new InvalidOperationException(
                    $"Settings UI is missing '{name}' ({typeof(T).Name}).");
        }
    }
}