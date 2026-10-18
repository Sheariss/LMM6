using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class StartMenuView : IDisposable
    {
        private readonly StartMenuBinder binder;
        private readonly StartMenuViewBuilder builder;
        private readonly Label errorLabel;

        private VisualElement activePopup;
        private VisualElement popupAnchor;
        private bool popupAbove;

        private Vector2 categoryHomeScrollOffset;
        private bool preserveCategoryHomeScroll;
        private IVisualElementScheduledItem scrollRestore;
        private bool disposed;

        public bool IsOpen { get; private set; }
        public bool HasPopup => activePopup != null;

        public StartMenuView(
            StartMenuBinder binder,
            StartMenuViewBuilder builder)
        {
            this.binder = binder
                ?? throw new ArgumentNullException(nameof(binder));

            this.builder = builder
                ?? throw new ArgumentNullException(nameof(builder));

            errorLabel = new Label();
            errorLabel.AddToClassList("start-menu-empty");
            errorLabel.AddToClassList("start-menu-panel-empty");
            errorLabel.AddToClassList("start-menu-hidden");
            errorLabel.style.flexShrink = 0;

            binder.Root.Insert(1, errorLabel);

            binder.Root.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.ViewPopup.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.PowerPopup.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.CategoryPopup.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.CategoryPopupGrid.style.width = Length.Percent(100);

            Hide();
        }

        public void Render(
            StartMenuEngine engine,
            Action<string> activateApp,
            Action<string> activateDocument,
            Action<AppCategory, VisualElement> openCategory)
        {
            ClosePopups();

            binder.ProfileName.text = engine.User.DisplayName;

            binder.ProfileImage.style.backgroundImage =
                new StyleBackground(StyleKeyword.None);

            binder.ProfileImage.sprite = engine.User.ProfileImage;
            binder.ProfileImage.scaleMode = ScaleMode.ScaleAndCrop;

            SetVisible(
                binder.HomePanel,
                !engine.IsSearching && !engine.ShowingRecent);

            SetVisible(
                binder.RecentPanel,
                !engine.IsSearching && engine.ShowingRecent);

            SetVisible(
                binder.SearchPanel,
                engine.IsSearching);

            IEnumerable<AppDefinition> pinned =
                engine.PinnedExpanded
                    ? engine.PinnedApps
                    : engine.PinnedApps.Take(
                        StartMenuEngine.CollapsedPinnedCount);

            builder.FillApps(
                binder.PinnedGrid,
                pinned,
                activateApp,
                false);

            SetVisible(
                binder.PinnedEmpty,
                engine.PinnedApps.Count == 0);

            binder.PinnedToggle.SetEnabled(
                engine.PinnedApps.Count >
                StartMenuEngine.CollapsedPinnedCount);

            binder.PinnedToggleText.text = engine.PinnedExpanded
                ? "Show less"
                : "Show all";

            binder.PinnedToggle.tooltip = engine.PinnedExpanded
                ? "Show fewer pinned apps"
                : "Show all pinned apps";

            binder.PinnedToggleIcon.EnableInClassList(
                "start-menu-chevron-up",
                engine.PinnedExpanded);

            builder.FillDocuments(
                binder.RecentGrid,
                engine.Documents.Take(
                    StartMenuEngine.RecentPreviewCount),
                activateDocument);

            builder.FillDocuments(
                binder.RecentList,
                engine.Documents,
                activateDocument,
                expanded: true);

            bool noDocuments = engine.Documents.Count == 0;

            SetVisible(binder.RecentEmpty, noDocuments);
            SetVisible(binder.RecentPanelEmpty, noDocuments);

            binder.RecentShowAllButton.SetEnabled(!noDocuments);

            binder.ViewValue.text = engine.ViewMode.ToString();

            SetVisible(
                binder.CategoryGrid,
                engine.ViewMode == StartMenuAppView.Category);

            SetVisible(
                binder.AllAppsGrid,
                engine.ViewMode == StartMenuAppView.Grid);

            SetVisible(
                binder.AlphabeticalList,
                engine.ViewMode == StartMenuAppView.List);

            SetVisible(
                binder.AppsEmpty,
                engine.Apps.Count == 0);

            builder.FillCategories(
                binder.CategoryGrid,
                engine.Apps,
                openCategory,
                activateApp);

            builder.FillApps(
                binder.AllAppsGrid,
                engine.Apps,
                activateApp,
                false);

            builder.FillAlphabetical(
                binder.AlphabeticalList,
                engine.Apps,
                activateApp);

            List<AppDefinition> results =
                engine.GetSearchResults().ToList();

            builder.FillApps(
                binder.SearchResults,
                results,
                activateApp,
                true);

            SetVisible(
                binder.SearchEmpty,
                results.Count == 0);

            binder.CategoryViewButton.EnableInClassList(
                "start-menu-selected",
                engine.ViewMode == StartMenuAppView.Category);

            binder.GridViewButton.EnableInClassList(
                "start-menu-selected",
                engine.ViewMode == StartMenuAppView.Grid);

            binder.ListViewButton.EnableInClassList(
                "start-menu-selected",
                engine.ViewMode == StartMenuAppView.List);
        }

        public void Show()
        {
            if (disposed)
                return;

            binder.Root.style.display = DisplayStyle.Flex;
            IsOpen = true;
            ClearError();
        }

        public void Hide()
        {
            ClosePopups();
            scrollRestore?.Pause();

            binder.Root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        public void ToggleViewPopup()
        {
            TogglePopup(
                binder.ViewPopup,
                binder.ViewButton,
                false);
        }

        public void TogglePowerPopup()
        {
            TogglePopup(
                binder.PowerPopup,
                binder.PowerButton,
                true);
        }

        public void ShowCategoryPopup(
            AppCategory category,
            VisualElement anchor,
            IEnumerable<AppDefinition> apps,
            Action<string> activate,
            Vector2 homeScrollOffset)
        {
            if (disposed)
                return;

            if (activePopup == binder.CategoryPopup &&
                popupAnchor == anchor)
            {
                ClosePopups();
                return;
            }

            List<AppDefinition> categoryApps = apps
                .Where(app => app.Category == category)
                .OrderBy(
                    app => app.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (categoryApps.Count == 0)
                return;

            ClosePopups();

            binder.CategoryPopupTitle.text =
                StartMenuEngine.CategoryName(category);

            builder.FillApps(
                binder.CategoryPopupGrid,
                categoryApps,
                activate,
                false);

            int rows = Mathf.CeilToInt(categoryApps.Count / 4f);

            binder.CategoryPopup.style.height = Mathf.Clamp(
                rows * 92f + 64f,
                156f,
                360f);

            binder.CategoryPopup.style.maxHeight = Mathf.Max(
                1f,
                binder.Root.contentRect.height - 32f);

            binder.CategoryPopupScroll.scrollOffset = Vector2.zero;

            TogglePopup(
                binder.CategoryPopup,
                anchor,
                false);

            categoryHomeScrollOffset = homeScrollOffset;
            preserveCategoryHomeScroll = true;

            RestoreHomeScroll(homeScrollOffset);
        }

        public bool IsInsidePopup(VisualElement target)
        {
            return Contains(activePopup, target);
        }

        public bool IsInsidePopupAnchor(VisualElement target)
        {
            return Contains(popupAnchor, target);
        }

        public void ClosePopups()
        {
            bool restoreScroll = preserveCategoryHomeScroll;
            Vector2 offset = categoryHomeScrollOffset;

            preserveCategoryHomeScroll = false;

            SetVisible(binder.ViewPopup, false);
            SetVisible(binder.PowerPopup, false);
            SetVisible(binder.CategoryPopup, false);

            activePopup = null;
            popupAnchor = null;

            if (restoreScroll)
                RestoreHomeScroll(offset);
        }

        public void ShowError(string message)
        {
            errorLabel.text = message;
            SetVisible(errorLabel, true);
        }

        public void ClearError()
        {
            errorLabel.text = string.Empty;
            SetVisible(errorLabel, false);
        }

        public static bool Contains(
            VisualElement parent,
            VisualElement target)
        {
            return parent != null &&
                target != null &&
                (parent == target || parent.Contains(target));
        }

        private void RestoreHomeScroll(Vector2 offset)
        {
            binder.HomePanel.scrollOffset = offset;

            scrollRestore?.Pause();

            scrollRestore = binder.Root.schedule.Execute(() =>
            {
                if (disposed || !IsOpen)
                    return;

                binder.HomePanel.scrollOffset = offset;
                PositionPopup();
            });
        }

        private void TogglePopup(
            VisualElement popup,
            VisualElement anchor,
            bool above)
        {
            bool wasOpen =
                activePopup == popup &&
                popupAnchor == anchor;

            ClosePopups();

            if (wasOpen || disposed)
                return;

            activePopup = popup;
            popupAnchor = anchor;
            popupAbove = above;

            popup.style.maxWidth = Mathf.Max(
                1f,
                binder.Root.contentRect.width - 16f);

            SetVisible(popup, true);
            popup.BringToFront();
            PositionPopup();
        }

        private void OnGeometryChanged(
            GeometryChangedEvent geometryEvent)
        {
            PositionPopup();
        }

        private void PositionPopup()
        {
            if (disposed ||
                activePopup == null ||
                popupAnchor == null ||
                activePopup.layout.width <= 0f ||
                activePopup.layout.height <= 0f)
            {
                return;
            }

            Rect bounds = binder.Root.contentRect;

            if (bounds.width <= 0f || bounds.height <= 0f)
                return;

            Vector2 anchorMin = binder.Root.WorldToLocal(
                popupAnchor.worldBound.min);

            Vector2 anchorMax = binder.Root.WorldToLocal(
                popupAnchor.worldBound.max);

            float width = activePopup.layout.width;
            float height = activePopup.layout.height;

            float minLeft = bounds.xMin + 8f;
            float maxLeft = Mathf.Max(
                minLeft,
                bounds.xMax - width - 8f);

            float minTop = bounds.yMin + 8f;
            float maxTop = Mathf.Max(
                minTop,
                bounds.yMax - height - 8f);

            float left = Mathf.Clamp(
                anchorMax.x - width,
                minLeft,
                maxLeft);

            float top = popupAbove
                ? anchorMin.y - height - 8f
                : anchorMax.y + 8f;

            if (!popupAbove &&
                top + height > bounds.yMax - 8f)
            {
                top = anchorMin.y - height - 8f;
            }

            if (popupAbove && top < minTop)
                top = anchorMax.y + 8f;

            activePopup.style.right = StyleKeyword.Auto;
            activePopup.style.bottom = StyleKeyword.Auto;
            activePopup.style.left = left;
            activePopup.style.top = Mathf.Clamp(
                top,
                minTop,
                maxTop);
        }

        private static void SetVisible(
            VisualElement element,
            bool visible)
        {
            element.EnableInClassList(
                "start-menu-hidden",
                !visible);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            Hide();
            disposed = true;
            scrollRestore?.Pause();

            binder.Root.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.ViewPopup.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.PowerPopup.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.CategoryPopup.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.CategoryPopupGrid.Clear();
            errorLabel.RemoveFromHierarchy();
        }
    }
}