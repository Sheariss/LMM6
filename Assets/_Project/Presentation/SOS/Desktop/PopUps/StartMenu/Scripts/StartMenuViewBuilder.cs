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
            container.style.width = Length.Percent(100);

            foreach (AppDefinition app in apps)
                container.Add(CreateAppButton(app, activate, list));
        }

        public void FillAlphabetical(
            VisualElement container,
            IEnumerable<AppDefinition> apps,
            Action<string> activate)
        {
            container.Clear();
            container.style.width = Length.Percent(100);

            var orderedApps = apps.OrderBy(
                app => app.DisplayName,
                StringComparer.OrdinalIgnoreCase);

            foreach (var group in orderedApps.GroupBy(
                         app => Initial(app.DisplayName)))
            {
                var section = new VisualElement();
                section.AddToClassList("start-menu-letter-group");
                section.style.width = Length.Percent(100);

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
            Action<AppCategory, VisualElement> openCategory,
            Action<string> activate)
        {
            container.Clear();
            container.style.width = Length.Percent(100);

            var groups = apps
                .GroupBy(app => app.Category)
                .OrderBy(
                    group => StartMenuEngine.CategoryName(group.Key),
                    StringComparer.OrdinalIgnoreCase);

            foreach (var group in groups)
            {
                AppCategory category = group.Key;
                string categoryName =
                    StartMenuEngine.CategoryName(category);

                List<AppDefinition> categoryApps = group
                    .OrderBy(
                        app => app.DisplayName,
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var item = new VisualElement();
                item.AddToClassList("start-menu-category-item");

                var card = new VisualElement
                {
                    focusable = true,
                    tooltip = $"Open {categoryName}"
                };

                card.AddToClassList("start-menu-category-card");

                var preview = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };

                preview.AddToClassList("start-menu-category-preview");

                for (int index = 0; index < 3; index++)
                {
                    Button appButton;

                    if (index < categoryApps.Count)
                    {
                        AppDefinition app = categoryApps[index];

                        appButton = new Button(
                            () => activate(app.AppID))
                        {
                            tooltip = app.DisplayName
                        };

                        appButton.Add(CreateImage(
                            AppIcon(app),
                            "start-menu-category-icon"));
                    }
                    else
                    {
                        appButton = new Button();
                        appButton.SetEnabled(false);
                    }

                    appButton.AddToClassList(
                        "start-menu-category-slot-button");

                    preview.Add(appButton);
                }

                var moreButton = new Button(
                    () => openCategory(category, item))
                {
                    tooltip = $"Show all {categoryName} apps"
                };

                moreButton.AddToClassList(
                    "start-menu-category-slot-button");

                moreButton.AddToClassList(
                    "start-menu-category-more-button");

                var miniGrid = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };

                miniGrid.AddToClassList(
                    "start-menu-category-mini-grid");

                for (int index = 0; index < 4; index++)
                {
                    var miniSlot = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore
                    };

                    miniSlot.AddToClassList(
                        "start-menu-category-mini-slot");

                    int appIndex = index + 3;

                    if (appIndex < categoryApps.Count)
                    {
                        miniSlot.Add(CreateImage(
                            AppIcon(categoryApps[appIndex]),
                            "start-menu-category-mini-icon"));
                    }

                    miniGrid.Add(miniSlot);
                }

                moreButton.Add(miniGrid);
                preview.Add(moreButton);
                card.Add(preview);

                card.RegisterCallback<GeometryChangedEvent>(
                    geometryEvent =>
                    {
                        float width = geometryEvent.newRect.width;

                        if (width <= 0f || float.IsNaN(width))
                            return;

                        if (!Mathf.Approximately(
                                geometryEvent.newRect.height,
                                width))
                        {
                            card.style.height = width;
                        }

                        float innerSize = card.contentRect.width;

                        if (innerSize <= 0f)
                            return;

                        preview.style.width = innerSize;
                        preview.style.height = innerSize;

                        foreach (VisualElement slot in preview.Children())
                        {
                            slot.style.width = innerSize * 0.5f;
                            slot.style.height = innerSize * 0.5f;
                        }
                    });

                miniGrid.RegisterCallback<GeometryChangedEvent>(
                    geometryEvent =>
                    {
                        float width = geometryEvent.newRect.width;
                        float height = geometryEvent.newRect.height;

                        if (width <= 0f || height <= 0f)
                            return;

                        foreach (VisualElement slot in miniGrid.Children())
                        {
                            slot.style.width = width * 0.5f;
                            slot.style.height = height * 0.5f;
                        }
                    });

                card.RegisterCallback<ClickEvent>(clickEvent =>
                {
                    var target = clickEvent.target as VisualElement;

                    for (VisualElement current = target;
                         current != null && current != card;
                         current = current.parent)
                    {
                        if (current is Button)
                            return;
                    }

                    openCategory(category, item);
                });

                card.RegisterCallback<KeyDownEvent>(keyEvent =>
                {
                    if (keyEvent.target != card)
                        return;

                    bool activateCategory =
                        keyEvent.keyCode == KeyCode.Return ||
                        keyEvent.keyCode == KeyCode.KeypadEnter ||
                        keyEvent.keyCode == KeyCode.Space;

                    if (!activateCategory)
                        return;

                    keyEvent.StopPropagation();
                    openCategory(category, item);
                });

                var nameButton = new Button(
                    () => openCategory(category, item))
                {
                    text = categoryName,
                    tooltip = $"Show all {categoryName} apps"
                };

                nameButton.AddToClassList(
                    "start-menu-category-name-button");

                item.Add(card);
                item.Add(nameButton);
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
            container.style.width = Length.Percent(100);

            foreach (StartMenuDocument document in documents)
            {
                string path = document.Item.Path;

                var button = new Button(() => activate(path))
                {
                    tooltip = path
                };

                button.AddToClassList("start-menu-document-button");

                if (expanded)
                {
                    button.AddToClassList(
                        "start-menu-document-button-expanded");
                }

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
                        "start-menu-document-date " +
                        "start-menu-document-date-column"));
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

        private static Image CreateImage(
            Sprite sprite,
            string className)
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

        private static Label CreateLabel(
            string text,
            string classNames)
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