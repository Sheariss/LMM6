using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarPreviewController : IDisposable
    {
        private sealed class Anchor
        {
            public Button Button;

            public EventCallback<PointerEnterEvent> Enter;
            public EventCallback<PointerLeaveEvent> Leave;
            public EventCallback<KeyDownEvent> Key;

            public Action Click;
        }

        private readonly Dictionary<string, Anchor> anchors =
            new(StringComparer.Ordinal);

        private readonly AppController apps;
        private readonly VisualElement document;
        private readonly TaskbarPreviewEngine engine;
        private readonly TaskbarPreviewView view;

        private IVisualElementScheduledItem showTimer;
        private IVisualElementScheduledItem hideTimer;
        private IVisualElementScheduledItem watchTimer;

        private string currentAppID;
        private string pendingAppID;
        private string hoveredAppID;

        private bool pointerInside;
        private bool disposed;

        public bool IsVisible => currentAppID != null;

        public TaskbarPreviewController(
            AppController apps,
            WindowPreviewCaptureService captures,
            VisualElement document,
            VisualElement previewLayer,
            VisualTreeAsset template,
            TaskbarPinnedButtonController taskbar,
            IReadOnlyList<AppDefinition> availableApps,
            Func<bool> canUse)
        {
            this.apps = apps != null
                ? apps
                : throw new ArgumentNullException(nameof(apps));

            this.document = document ??
                throw new ArgumentNullException(nameof(document));

            if (taskbar == null)
                throw new ArgumentNullException(nameof(taskbar));

            if (availableApps == null)
                throw new ArgumentNullException(nameof(availableApps));

            engine = new TaskbarPreviewEngine(
                apps,
                captures,
                canUse);

            view = new TaskbarPreviewViewBuilder().Build(
                template,
                previewLayer);

            try
            {
                foreach (AppDefinition app in availableApps)
                {
                    if (app != null &&
                        taskbar.TryGetButton(
                            app.AppID,
                            out Button button))
                    {
                        Bind(app.AppID, button);
                    }
                }

                view.Surface.RegisterCallback<PointerEnterEvent>(
                    OnEnter);

                view.Surface.RegisterCallback<PointerLeaveEvent>(
                    OnLeave);

                view.Surface.RegisterCallback<FocusOutEvent>(
                    OnFocusOut);

                view.Surface.RegisterCallback<KeyDownEvent>(
                    OnPreviewKey);

                document.RegisterCallback<PointerDownEvent>(
                    OnDocumentPointer,
                    TrickleDown.TrickleDown);

                document.RegisterCallback<KeyDownEvent>(
                    OnDocumentKey);

                apps.WindowSelectionRequested +=
                    OnSelectionRequested;

                apps.AppStateChanged += OnAppStateChanged;

                watchTimer = document.schedule
                    .Execute(Watch)
                    .Every(150);

                watchTimer.Pause();
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Bind(string appID, Button button)
        {
            if (anchors.ContainsKey(appID))
                return;

            var anchor = new Anchor
            {
                Button = button
            };

            anchor.Enter = evt =>
            {
                if (evt.pointerType !=
                    UnityEngine.UIElements.PointerType.mouse)
                {
                    return;
                }

                if (currentAppID != appID)
                    Hide();

                hoveredAppID = appID;
                CancelHide();

                if (currentAppID == appID)
                    return;

                CancelShow();
                pendingAppID = appID;

                showTimer = document.schedule.Execute(() =>
                {
                    pendingAppID = null;
                    showTimer = null;

                    if (!disposed && hoveredAppID == appID)
                        Open(appID, false);
                }).StartingIn(350);
            };

            anchor.Leave = evt =>
            {
                if (hoveredAppID == appID)
                    hoveredAppID = null;

                if (pendingAppID == appID)
                    CancelShow();

                if (currentAppID == appID)
                    ScheduleHide();
            };

            anchor.Key = evt =>
            {
                if (evt.keyCode != KeyCode.UpArrow)
                    return;

                Open(appID, true);

                evt.PreventDefault();
                evt.StopPropagation();
            };

            anchor.Click = () =>
            {
                // The taskbar's existing click handler handles
                // single-window activation/minimization.
                if (engine.GetWindows(appID).Count <= 1)
                    Hide();
            };

            anchors.Add(appID, anchor);

            button.RegisterCallback(anchor.Enter);
            button.RegisterCallback(anchor.Leave);
            button.RegisterCallback(anchor.Key);
            button.clicked += anchor.Click;
        }

        private void Open(string appID, bool focus)
        {
            CancelShow();
            CancelHide();

            if (disposed ||
                !engine.CanUse ||
                !anchors.TryGetValue(
                    appID,
                    out Anchor anchor) ||
                anchor.Button.panel == null)
            {
                Hide();
                return;
            }

            currentAppID = appID;

            if (!Refresh())
                return;

            view.Show();
            watchTimer?.Resume();

            if (focus)
                view.FocusFirst();
        }

        private bool Refresh()
        {
            if (currentAppID == null ||
                !engine.CanUse ||
                !anchors.TryGetValue(
                    currentAppID,
                    out Anchor anchor))
            {
                Hide();
                return false;
            }

            IReadOnlyList<AppWindowPreview> windows =
                engine.GetWindows(currentAppID);

            if (windows.Count == 0)
            {
                Hide();
                return false;
            }

            view.Render(
                windows,
                engine.GetThumbnail,
                Select,
                CloseWindow);

            if (!view.Position(anchor.Button))
            {
                Hide();
                return false;
            }

            return true;
        }

        private void Select(IAppWindow window)
        {
            string appID = currentAppID;

            // Activation raises app-state events. Clear the preview
            // first so those events do not rebuild the closing popup.
            Hide();

            if (appID != null)
                engine.Activate(appID, window);
        }

        private void CloseWindow(IAppWindow window)
        {
            if (currentAppID != null)
                engine.Close(currentAppID, window);
        }

        private void ScheduleHide()
        {
            CancelHide();

            hideTimer = document.schedule.Execute(() =>
            {
                hideTimer = null;

                if (pointerInside ||
                    view.HasFocus ||
                    (currentAppID != null &&
                     hoveredAppID == currentAppID))
                {
                    return;
                }

                Hide();
            }).StartingIn(250);
        }

        public void Hide()
        {
            CancelShow();
            CancelHide();

            currentAppID = null;
            hoveredAppID = null;
            pointerInside = false;

            watchTimer?.Pause();
            view.Hide();
        }

        private void Watch()
        {
            if (!disposed && IsVisible)
                Refresh();
        }

        private void OnSelectionRequested(
            string appID,
            IReadOnlyList<IAppWindow> windows)
        {
            // Get a fresh snapshot through the engine.
            Open(appID, true);
        }

        private void OnAppStateChanged(
            string appID,
            int count,
            bool focused)
        {
            if (currentAppID == appID)
                Refresh();
        }

        private void OnEnter(PointerEnterEvent evt)
        {
            pointerInside = true;
            CancelHide();
        }

        private void OnLeave(PointerLeaveEvent evt)
        {
            pointerInside = false;
            ScheduleHide();
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (!view.Contains(evt.relatedTarget as VisualElement))
                ScheduleHide();
        }

        private void OnPreviewKey(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.LeftArrow)
            {
                view.MoveFocus(-1);
            }
            else if (evt.keyCode == KeyCode.RightArrow)
            {
                view.MoveFocus(1);
            }
            else
            {
                return;
            }

            evt.PreventDefault();
            evt.StopPropagation();
        }

        private void OnDocumentKey(KeyDownEvent evt)
        {
            if (!IsVisible ||
                evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            string appID = currentAppID;
            Hide();

            if (anchors.TryGetValue(
                appID,
                out Anchor anchor))
            {
                anchor.Button.Focus();
            }

            evt.StopPropagation();
        }

        private void OnDocumentPointer(PointerDownEvent evt)
        {
            var target = evt.target as VisualElement;

            if (view.Contains(target))
                return;

            foreach (Anchor anchor in anchors.Values)
            {
                if (target == anchor.Button ||
                    (target != null &&
                     anchor.Button.Contains(target)))
                {
                    return;
                }
            }

            Hide();
        }

        private void CancelShow()
        {
            showTimer?.Pause();
            showTimer = null;
            pendingAppID = null;
        }

        private void CancelHide()
        {
            hideTimer?.Pause();
            hideTimer = null;
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Hide();

            watchTimer?.Pause();
            watchTimer = null;

            apps.WindowSelectionRequested -=
                OnSelectionRequested;

            apps.AppStateChanged -= OnAppStateChanged;

            foreach (Anchor anchor in anchors.Values)
            {
                anchor.Button.UnregisterCallback(anchor.Enter);
                anchor.Button.UnregisterCallback(anchor.Leave);
                anchor.Button.UnregisterCallback(anchor.Key);

                anchor.Button.clicked -= anchor.Click;
            }

            anchors.Clear();

            view.Surface.UnregisterCallback<PointerEnterEvent>(
                OnEnter);

            view.Surface.UnregisterCallback<PointerLeaveEvent>(
                OnLeave);

            view.Surface.UnregisterCallback<FocusOutEvent>(
                OnFocusOut);

            view.Surface.UnregisterCallback<KeyDownEvent>(
                OnPreviewKey);

            document.UnregisterCallback<PointerDownEvent>(
                OnDocumentPointer,
                TrickleDown.TrickleDown);

            document.UnregisterCallback<KeyDownEvent>(
                OnDocumentKey);

            view.Dispose();
        }
    }
}