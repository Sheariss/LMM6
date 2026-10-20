using System;
using Atlas.Presentation.SOS.Panels;
using Atlas.SOS.Authentication;
using Atlas.SOS.Notifications;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notifications
{
    public sealed class NotificationPanelController : PanelRuntime
    {
        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelTemplate;

        [Header("Settings Destination")]
        [SerializeField] private string settingsAppId = "control-panel";

        private NotificationPanelEngine engine;
        private NotificationPanelView view;

        private Func<string, bool> canAccessApp;
        private Action<string> launchApp;

        private bool initialized;
        private bool visible;
        private bool synchronizing;

        public override VisualElement Surface => view?.Binder.Root;

        public override void Initialize(
            VisualElement parent,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "NotificationPanelController must be active.");
            }

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            this.launchApp = launchApp ??
                throw new ArgumentNullException(nameof(launchApp));

            AuthenticationManager authentication =
                AuthenticationManager.Instance;

            if (authentication == null ||
                !authentication.IsSignedIn ||
                authentication.CurrentUser == null)
            {
                throw new InvalidOperationException(
                    "Notifications require an authenticated user.");
            }

            try
            {
                NotificationInbox inbox = NotificationService.ForUser(
                    authentication.CurrentUser.UserId);

                engine = new NotificationPanelEngine(
                    inbox,
                    this.canAccessApp);

                view = new NotificationPanelViewBuilder()
                    .Build(panelTemplate);

                parent.Add(view.Host);

                BindEvents();

                initialized = true;
                OnChanged();
                view.Hide();
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public override void Show()
        {
            if (!initialized)
                return;

            engine.ResetNavigation();
            view.Binder.Scroll.scrollOffset = Vector2.zero;

            visible = true;
            OnChanged();
            view.Show();
        }

        public override void Hide()
        {
            visible = false;
            view?.Hide();
        }

        public override void Shutdown()
        {
            initialized = false;
            visible = false;

            if (view != null && engine != null)
            {
                view.Binder.DndButton.clicked -= ToggleDoNotDisturb;
                view.Binder.ClearAllButton.clicked -= ClearAll;
                view.Binder.SettingsButton.clicked -= OpenSettings;

                view.ExpandRequested -= engine.ToggleExpanded;
                view.MenuRequested -= engine.ToggleMenu;
                view.ClearGroupRequested -= engine.ClearGroup;
                view.DismissRequested -= engine.Dismiss;
                view.OpenRequested -= OpenNotification;
                view.SettingsRequested -= OpenSettings;

                view.Binder.Root.UnregisterCallback<KeyDownEvent>(
                    OnKeyDown);
            }

            if (engine != null)
                engine.Changed -= OnChanged;

            engine?.Dispose();
            view?.Detach();

            engine = null;
            view = null;
            canAccessApp = null;
            launchApp = null;
            synchronizing = false;
        }

        private void BindEvents()
        {
            view.Binder.DndButton.clicked += ToggleDoNotDisturb;
            view.Binder.ClearAllButton.clicked += ClearAll;
            view.Binder.SettingsButton.clicked += OpenSettings;

            view.ExpandRequested += engine.ToggleExpanded;
            view.MenuRequested += engine.ToggleMenu;
            view.ClearGroupRequested += engine.ClearGroup;
            view.DismissRequested += engine.Dismiss;
            view.OpenRequested += OpenNotification;
            view.SettingsRequested += OpenSettings;

            view.Binder.Root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            engine.Changed += OnChanged;
        }

        private void ToggleDoNotDisturb()
        {
            if (initialized)
                engine.ToggleDoNotDisturb();
        }

        private void ClearAll()
        {
            if (initialized)
                engine.ClearAll();
        }

        private void OnChanged()
        {
            if (!initialized || synchronizing)
                return;

            synchronizing = true;

            try
            {
                if (visible)
                    engine.MarkAllRead();

                view.Render(engine, CanOpenSettings());
            }
            finally
            {
                synchronizing = false;
            }
        }

        private bool CanOpenSettings()
        {
            return !string.IsNullOrWhiteSpace(settingsAppId) &&
                   canAccessApp != null &&
                   canAccessApp(settingsAppId);
        }

        private void OpenSettings()
        {
            if (!initialized || !CanOpenSettings())
                return;

            launchApp(settingsAppId);

            if (initialized)
                RequestClose();
        }

        private void OpenNotification(string notificationId)
        {
            if (!initialized)
                return;

            if (engine.TryOpen(notificationId, launchApp) && initialized)
                RequestClose();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!initialized || evt.keyCode != KeyCode.Escape)
                return;

            if (engine.CloseMenu())
            {
                view.Binder.Root.Focus();
                evt.StopPropagation();
            }
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
