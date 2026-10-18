using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.SOS.Apps;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class StartMenuController : IDisposable
    {
        private readonly StartMenuBinder binder;
        private readonly StartMenuEngine engine;
        private readonly StartMenuView view;
        private readonly FileOpenRouter fileOpenRouter;

        private readonly VisualElement documentRoot;
        private readonly Button startButton;
        private readonly Func<bool> canUse;
        private readonly Action<string> launchApp;

        private readonly List<Action> unsubscribe = new();

        private VisualElement categoryPointerTarget;
        private Vector2 categoryPointerScrollOffset;
        private Vector2 homeScrollPosition;
        private bool disposed;

        public StartMenuController(
            StartMenuBinder binder,
            StartMenuEngine engine,
            StartMenuView view,
            FileOpenRouter fileOpenRouter,
            VisualElement documentRoot,
            Button startButton,
            Func<bool> canUse,
            Action<string> launchApp)
        {
            this.binder = binder
                ?? throw new ArgumentNullException(nameof(binder));

            this.engine = engine
                ?? throw new ArgumentNullException(nameof(engine));

            this.view = view
                ?? throw new ArgumentNullException(nameof(view));

            this.fileOpenRouter = fileOpenRouter
                ?? throw new ArgumentNullException(nameof(fileOpenRouter));

            this.documentRoot = documentRoot
                ?? throw new ArgumentNullException(nameof(documentRoot));

            this.startButton = startButton
                ?? throw new ArgumentNullException(nameof(startButton));

            this.canUse = canUse
                ?? throw new ArgumentNullException(nameof(canUse));

            this.launchApp = launchApp
                ?? throw new ArgumentNullException(nameof(launchApp));

            engine.Refresh();
            Render();

            BindClick(startButton, Toggle);
            BindClick(binder.PinnedToggle, TogglePinned);
            BindClick(binder.RecentShowAllButton, ShowRecent);
            BindClick(binder.RecentBackButton, ShowHome);
            BindClick(binder.ViewButton, ToggleViewPopup);
            BindClick(binder.PowerButton, TogglePowerPopup);

            BindClick(
                binder.CategoryViewButton,
                () => SetView(StartMenuAppView.Category));

            BindClick(
                binder.GridViewButton,
                () => SetView(StartMenuAppView.Grid));

            BindClick(
                binder.ListViewButton,
                () => SetView(StartMenuAppView.List));

            BindClick(binder.LockButton, view.ClosePopups);
            BindClick(binder.SleepButton, view.ClosePopups);
            BindClick(binder.ShutdownButton, view.ClosePopups);
            BindClick(binder.RestartButton, view.ClosePopups);

            binder.SearchField.RegisterValueChangedCallback(
                OnSearchChanged);

            documentRoot.RegisterCallback<PointerDownEvent>(
                OnPointerDown,
                TrickleDown.TrickleDown);

            documentRoot.RegisterCallback<KeyDownEvent>(
                OnKeyDown,
                TrickleDown.TrickleDown);
        }

        public void Open()
        {
            if (!EnsureUsable())
                return;

            categoryPointerTarget = null;

            engine.SetQuery(string.Empty);
            engine.ShowingRecent = false;

            binder.SearchField.SetValueWithoutNotify(string.Empty);

            engine.Refresh();
            Render();
            view.Show();

            binder.HomePanel.scrollOffset = Vector2.zero;
            binder.SearchField.Focus();
        }

        public void Close()
        {
            if (disposed)
                return;

            categoryPointerTarget = null;
            view.Hide();
        }

        public void Refresh()
        {
            if (!EnsureUsable())
                return;

            engine.Refresh();
            Render();
        }

        private void Toggle()
        {
            if (view.IsOpen)
                Close();
            else
                Open();
        }

        private void TogglePinned()
        {
            if (!EnsureUsable())
                return;

            engine.PinnedExpanded = !engine.PinnedExpanded;
            Render();
        }

        private void ShowRecent()
        {
            if (!EnsureUsable())
                return;

            homeScrollPosition = binder.HomePanel.scrollOffset;
            engine.ShowingRecent = true;

            view.ClosePopups();
            Render();

            binder.RecentScrollView.scrollOffset = Vector2.zero;
            binder.RecentBackButton.Focus();
        }

        private void ShowHome()
        {
            if (!EnsureUsable())
                return;

            engine.ShowingRecent = false;
            Render();

            binder.HomePanel.scrollOffset = homeScrollPosition;
            binder.RecentShowAllButton.Focus();
        }

        private void SetView(StartMenuAppView mode)
        {
            if (!EnsureUsable())
                return;

            engine.ViewMode = mode;

            view.ClosePopups();
            Render();

            binder.ViewButton.Focus();
        }

        private void OpenCategoryPopup(
            AppCategory category,
            VisualElement anchor)
        {
            if (!EnsureUsable())
                return;

            Vector2 offset = StartMenuView.Contains(
                anchor,
                categoryPointerTarget)
                    ? categoryPointerScrollOffset
                    : binder.HomePanel.scrollOffset;

            categoryPointerTarget = null;

            view.ShowCategoryPopup(
                category,
                anchor,
                engine.Apps,
                OpenApp,
                offset);
        }

        private void ToggleViewPopup()
        {
            if (EnsureUsable())
                view.ToggleViewPopup();
        }

        private void TogglePowerPopup()
        {
            if (EnsureUsable())
                view.TogglePowerPopup();
        }

        private void OnSearchChanged(ChangeEvent<string> change)
        {
            if (!EnsureUsable())
                return;

            engine.SetQuery(change.newValue);

            view.ClosePopups();
            view.ClearError();
            Render();

            binder.SearchScrollView.scrollOffset = Vector2.zero;
        }

        private void OpenApp(string appID)
        {
            if (!EnsureUsable())
                return;

            engine.Refresh();

            if (engine.FindApp(appID) == null)
            {
                Render();
                view.ShowError("This app is no longer available.");
                return;
            }

            Close();
            launchApp(appID);
        }

        private void OpenDocument(string path)
        {
            if (!EnsureUsable())
                return;

            engine.Refresh();

            StartMenuDocument document = engine.FindDocument(path);

            if (document == null)
            {
                Render();
                view.ShowError(
                    "This document is no longer available.");
                return;
            }

            try
            {
                if (!fileOpenRouter.TryOpen(
                        document.Request,
                        out string error))
                {
                    view.ShowError(
                        string.IsNullOrWhiteSpace(error)
                            ? "The document could not be opened."
                            : error);

                    return;
                }

                Close();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                view.ShowError("The document could not be opened.");
            }
        }

        private void OnPointerDown(
            PointerDownEvent pointerEvent)
        {
            categoryPointerTarget = null;

            if (disposed || !view.IsOpen)
                return;

            if (!EnsureUsable())
                return;

            var target = pointerEvent.target as VisualElement;

            if (StartMenuView.Contains(
                    binder.CategoryGrid,
                    target))
            {
                categoryPointerTarget = target;
                categoryPointerScrollOffset =
                    binder.HomePanel.scrollOffset;
            }

            if (StartMenuView.Contains(startButton, target))
                return;

            if (!StartMenuView.Contains(binder.Root, target))
            {
                Close();
                return;
            }

            if (view.HasPopup &&
                !view.IsInsidePopup(target) &&
                !view.IsInsidePopupAnchor(target) &&
                !StartMenuView.Contains(binder.ViewButton, target) &&
                !StartMenuView.Contains(binder.PowerButton, target))
            {
                view.ClosePopups();
            }
        }

        private void OnKeyDown(KeyDownEvent keyEvent)
        {
            categoryPointerTarget = null;

            if (disposed || !view.IsOpen || !EnsureUsable())
                return;

            if (keyEvent.keyCode == KeyCode.Escape)
            {
                if (view.HasPopup)
                {
                    view.ClosePopups();
                }
                else if (engine.IsSearching)
                {
                    binder.SearchField.value = string.Empty;
                    binder.SearchField.Focus();
                }
                else if (engine.ShowingRecent)
                {
                    ShowHome();
                }
                else
                {
                    Close();
                    startButton.Focus();
                }

                keyEvent.StopPropagation();
                return;
            }

            bool submit =
                keyEvent.keyCode == KeyCode.Return ||
                keyEvent.keyCode == KeyCode.KeypadEnter;

            if (!submit ||
                !engine.IsSearching ||
                !StartMenuView.Contains(
                    binder.SearchField,
                    keyEvent.target as VisualElement))
            {
                return;
            }

            AppDefinition firstResult =
                engine.GetSearchResults().FirstOrDefault();

            if (firstResult != null)
            {
                keyEvent.StopPropagation();
                OpenApp(firstResult.AppID);
            }
        }

        private bool EnsureUsable()
        {
            if (disposed)
                return false;

            if (canUse())
                return true;

            Close();
            return false;
        }

        private void Render()
        {
            view.Render(
                engine,
                OpenApp,
                OpenDocument,
                OpenCategoryPopup);
        }

        private void BindClick(Button button, Action action)
        {
            button.clicked += action;
            unsubscribe.Add(() => button.clicked -= action);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            view.Hide();
            disposed = true;
            categoryPointerTarget = null;

            foreach (Action release in unsubscribe)
                release();

            unsubscribe.Clear();

            binder.SearchField.UnregisterValueChangedCallback(
                OnSearchChanged);

            documentRoot.UnregisterCallback<PointerDownEvent>(
                OnPointerDown,
                TrickleDown.TrickleDown);

            documentRoot.UnregisterCallback<KeyDownEvent>(
                OnKeyDown,
                TrickleDown.TrickleDown);

            view.Dispose();

            binder.PinnedGrid.Clear();
            binder.RecentGrid.Clear();
            binder.RecentList.Clear();
            binder.CategoryGrid.Clear();
            binder.AllAppsGrid.Clear();
            binder.AlphabeticalList.Clear();
            binder.SearchResults.Clear();
            binder.CategoryPopupGrid.Clear();
        }
    }
}