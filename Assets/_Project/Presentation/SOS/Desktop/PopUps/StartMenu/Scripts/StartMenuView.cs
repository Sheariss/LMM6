using System;
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
        private Button popupAnchor;
        private bool popupAbove;

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

            Hide();
        }

        public void Render(
            StartMenuEngine engine,
            Action<string> activateApp,
            Action<string> activateDocument,
            Action<AppCategory?> selectCategory)
        {
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

            SetVisible(binder.SearchPanel, engine.IsSearching);

            var pinned = engine.PinnedExpanded
                ? engine.PinnedApps
                : engine.PinnedApps.Take(
                    StartMenuEngine.CollapsedPinnedCount);

            builder.FillApps(
                binder.PinnedGrid,
                pinned,
                activateApp,
                false);

            SetVisible(binder.PinnedEmpty, engine.PinnedApps.Count == 0);

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
                engine.Documents.Take(StartMenuEngine.RecentPreviewCount),
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

            SetVisible(binder.AppsEmpty, engine.Apps.Count == 0);

            builder.FillCategories(
                binder.CategoryGrid,
                engine.Apps,
                engine.ExpandedCategory,
                selectCategory,
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

            var results = engine.GetSearchResults().ToList();

            builder.FillApps(
                binder.SearchResults,
                results,
                activateApp,
                true);

            SetVisible(binder.SearchEmpty, results.Count == 0);

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
            binder.Root.style.display = DisplayStyle.Flex;
            IsOpen = true;
            ClearError();
        }

        public void Hide()
        {
            ClosePopups();
            binder.Root.style.display = DisplayStyle.None;
            IsOpen = false;
        }

        public void ToggleViewPopup()
        {
            TogglePopup(binder.ViewPopup, binder.ViewButton, false);
        }

        public void TogglePowerPopup()
        {
            TogglePopup(binder.PowerPopup, binder.PowerButton, true);
        }

        public bool IsInsidePopup(VisualElement target)
        {
            return Contains(activePopup, target);
        }

        public void ClosePopups()
        {
            SetVisible(binder.ViewPopup, false);
            SetVisible(binder.PowerPopup, false);
            activePopup = null;
            popupAnchor = null;
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

        private void TogglePopup(
            VisualElement popup,
            Button anchor,
            bool above)
        {
            bool wasOpen = activePopup == popup;

            ClosePopups();

            if (wasOpen)
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

        private void OnGeometryChanged(GeometryChangedEvent geometryEvent)
        {
            PositionPopup();
        }

        private void PositionPopup()
        {
            if (activePopup == null ||
                popupAnchor == null ||
                activePopup.layout.width <= 0f ||
                activePopup.layout.height <= 0f)
            {
                return;
            }

            Rect bounds = binder.Root.contentRect;

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

            if (!popupAbove && top + height > bounds.yMax - 8f)
                top = anchorMin.y - height - 8f;

            if (popupAbove && top < minTop)
                top = anchorMax.y + 8f;

            activePopup.style.right = StyleKeyword.Auto;
            activePopup.style.bottom = StyleKeyword.Auto;
            activePopup.style.left = left;
            activePopup.style.top = Mathf.Clamp(top, minTop, maxTop);
        }

        private static void SetVisible(
            VisualElement element,
            bool visible)
        {
            element.EnableInClassList("start-menu-hidden", !visible);
        }

        public void Dispose()
        {
            Hide();

            binder.Root.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.ViewPopup.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.PowerPopup.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            errorLabel.RemoveFromHierarchy();
        }
    }
}