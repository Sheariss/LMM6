using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class StartMenuViewBuilder
    {
        private readonly Texture2D backIcon = Resources.Load<Texture2D>(
            "Icons/Arrows 1/Arrow-Back/arrow_back_32dp");

        private readonly Texture2D documentFallback =
            Resources.Load<Texture2D>(
                "AppIcons/Document/icons8-document-48");

        public void FillApps(
            VisualElement container,
            IEnumerable<AppDefinition> apps,
            Action<string> activate,
            bool list)
        {
            container.Clear();

            foreach (AppDefinition app in apps)
                container.Add(CreateAppButton(app, activate, list));
        }

        public void FillAlphabetical(
            VisualElement container,
            IEnumerable<AppDefinition> apps,
            Action<string> activate)
        {
            container.Clear();

            foreach (var group in apps.GroupBy(app =>
                         Initial(app.DisplayName)))
            {
                var section = new VisualElement();
                section.AddToClassList("start-menu-letter-group");

                section.Add(CreateLabel(
                    group.Key,
                    "start-menu-letter-heading"));

                foreach (AppDefinition app in group)
                    section.Add(CreateAppButton(app, activate, true));

                container.Add(section);
            }
        }

        public void FillCategories(
            VisualElement container,
            IEnumerable<AppDefinition> apps,
            AppCategory? expandedCategory,
            Action<AppCategory?> selectCategory,
            Action<string> activate)
        {
            container.Clear();

            if (expandedCategory.HasValue)
            {
                var section = new VisualElement();
                section.AddToClassList("start-menu-category-group");

                var header = new VisualElement();
                header.AddToClassList("start-menu-section-header");

                var back = new Button(() => selectCategory(null))
                {
                    tooltip = "Back to categories"
                };

                back.AddToClassList("start-menu-section-button");

                var image = new Image
                {
                    image = backIcon,
                    pickingMode = PickingMode.Ignore,
                    scaleMode = ScaleMode.ScaleToFit
                };

                image.AddToClassList("start-menu-popup-icon");

                back.Add(image);
                back.Add(CreateLabel(
                    "Back",
                    "start-menu-button-text"));

                header.Add(CreateLabel(
                    StartMenuEngine.CategoryName(expandedCategory.Value),
                    "start-menu-heading"));

                header.Add(back);
                section.Add(header);

                var grid = new VisualElement();
                grid.AddToClassList("start-menu-app-grid");

                FillApps(
                    grid,
                    apps.Where(app =>
                        app.Category == expandedCategory.Value),
                    activate,
                    false);

                section.Add(grid);
                container.Add(section);
                return;
            }

            var groups = apps
                .GroupBy(app => app.Category)
                .OrderBy(
                    group => StartMenuEngine.CategoryName(group.Key),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                AppCategory category = group.Key;

                var item = new VisualElement();
                item.AddToClassList("start-menu-category-item");

                var card = new Button(() => selectCategory(category))
                {
                    tooltip = StartMenuEngine.CategoryName(category)
                };

                card.AddToClassList("start-menu-category-card");

                var preview = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };

                preview.AddToClassList("start-menu-category-preview");

                foreach (AppDefinition app in group.Take(4))
                {
                    var slot = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore
                    };

                    slot.AddToClassList("start-menu-category-icon-slot");

                    slot.Add(CreateImage(
                        AppIcon(app),
                        "start-menu-category-icon"));

                    preview.Add(slot);
                }

                card.Add(preview);
                item.Add(card);

                item.Add(CreateLabel(
                    StartMenuEngine.CategoryName(category),
                    "start-menu-category-name"));

                container.Add(item);
            }
        }

        public void FillDocuments(
    VisualElement container,
    IEnumerable<StartMenuDocument> documents,
    Action<string> activate,
    bool expanded = false)
        {
            container.Clear();

            foreach (StartMenuDocument document in documents)
            {
                string path = document.Item.Path;

                var button = new Button(() => activate(path))
                {
                    tooltip = path
                };

                button.AddToClassList("start-menu-document-button");

                if (expanded)
                    button.AddToClassList("start-menu-document-button-expanded");

                Image icon = CreateImage(
                    document.Icon,
                    "start-menu-document-icon");

                if (document.Icon == null)
                    icon.image = documentFallback;

                var details = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };

                details.AddToClassList("start-menu-document-details");

                details.Add(CreateLabel(
                    document.Item.Name,
                    "start-menu-document-name"));

                string date = string.IsNullOrWhiteSpace(
                    document.Item.ModifiedDate)
                        ? "Date unavailable"
                        : document.Item.ModifiedDate;

                button.Add(icon);

                if (expanded)
                {
                    details.Add(CreateLabel(
                        path,
                        "start-menu-document-path"));

                    button.Add(details);

                    button.Add(CreateLabel(
                        date,
                        "start-menu-document-date start-menu-document-date-column"));
                }
                else
                {
                    details.Add(CreateLabel(
                        date,
                        "start-menu-document-date"));

                    button.Add(details);
                }

                container.Add(button);
            }
        }

        private static Button CreateAppButton(
            AppDefinition app,
            Action<string> activate,
            bool list)
        {
            var button = new Button(() => activate(app.AppID))
            {
                tooltip = app.DisplayName
            };

            button.AddToClassList(list
                ? "start-menu-list-button"
                : "start-menu-app-button");

            button.Add(CreateImage(
                AppIcon(app),
                list
                    ? "start-menu-list-icon"
                    : "start-menu-app-icon"));

            button.Add(CreateLabel(
                app.DisplayName,
                list
                    ? "start-menu-list-name"
                    : "start-menu-app-name"));

            return button;
        }

        private static Sprite AppIcon(AppDefinition app)
        {
            return app.AppIcon48 != null
                ? app.AppIcon48
                : app.AppIcon96;
        }

        private static Image CreateImage(Sprite sprite, string className)
        {
            var image = new Image
            {
                sprite = sprite,
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };

            image.AddToClassList(className);
            return image;
        }

        private static Label CreateLabel(string text, string classNames)
        {
            var label = new Label(text)
            {
                pickingMode = PickingMode.Ignore
            };

            foreach (string className in classNames.Split(
                         new[] { ' ' },
                         StringSplitOptions.RemoveEmptyEntries))
            {
                label.AddToClassList(className);
            }

            return label;
        }

        private static string Initial(string displayName)
        {
            string name = (displayName ?? string.Empty).Trim();

            if (name.Length == 0 || !char.IsLetter(name[0]))
                return "#";

            return char.ToUpperInvariant(name[0]).ToString();
        }
    }
}