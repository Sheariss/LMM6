using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.NetworkConnection
{
    public sealed class NetworkConnectionController : PanelRuntime
    {
        private sealed class RowSubscription
        {
            public NetworkConnectionRow Row;

            public Action SelectHandler;
            public Action ActionHandler;

            public EventCallback<ChangeEvent<bool>> AutomaticHandler;
        }

        [Header("Authored Data")]
        [SerializeField] private NetworkLibrary networkLibrary;

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelTemplate;

        [Header("Settings Destination")]
        [SerializeField] private string settingsAppId = "control-panel";

        private readonly List<RowSubscription> subscriptions = new();

        private NetworkConnectionEngine engine;
        private NetworkConnectionViewBuilder builder;
        private NetworkConnectionView view;

        private Func<string, bool> canAccessApp;
        private Action<string> launchApp;

        private bool initialized;

        public override VisualElement Surface =>
            view?.Binder.Root;

        public override void Initialize(
            VisualElement parent,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "NetworkConnectionController must be active.");
            }

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            this.launchApp = launchApp ??
                throw new ArgumentNullException(nameof(launchApp));

            try
            {
                engine = new NetworkConnectionEngine(networkLibrary);

                builder = new NetworkConnectionViewBuilder();

                view = builder.Build(
                    panelTemplate,
                    engine.Networks);

                parent.Add(view.Host);

                RegisterEvents();

                initialized = true;

                Render();
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

            Render();
            view.Show();
        }

        public override void Hide()
        {
            // Selection, preferences, and connection survive dismissal.
            view?.Hide();
        }

        public override void Shutdown()
        {
            initialized = false;

            Hide();
            UnregisterEvents();

            view?.Detach();

            view = null;
            builder = null;
            engine = null;

            canAccessApp = null;
            launchApp = null;
        }

        private void RegisterEvents()
        {
            view.Binder.WiFiToggle.RegisterValueChangedCallback(
                OnWiFiChanged);

            view.Binder.SettingsButton.clicked += OnSettingsPressed;
            view.Binder.RefreshButton.clicked += OnRefreshPressed;

            foreach (var entry in view.Rows)
            {
                string id = entry.Key;

                var subscription = new RowSubscription
                {
                    Row = entry.Value,

                    SelectHandler = () => OnNetworkSelected(id),

                    ActionHandler = () => OnNetworkAction(id),

                    AutomaticHandler = evt =>
                        OnAutomaticChanged(id, evt.newValue)
                };

                subscriptions.Add(subscription);

                subscription.Row.SelectButton.clicked +=
                    subscription.SelectHandler;

                subscription.Row.ActionButton.clicked +=
                    subscription.ActionHandler;

                subscription.Row.AutomaticToggle
                    .RegisterValueChangedCallback(
                        subscription.AutomaticHandler);
            }

            engine.Changed += Render;
        }

        private void UnregisterEvents()
        {
            if (engine != null)
                engine.Changed -= Render;

            foreach (RowSubscription subscription in subscriptions)
            {
                subscription.Row.SelectButton.clicked -=
                    subscription.SelectHandler;

                subscription.Row.ActionButton.clicked -=
                    subscription.ActionHandler;

                subscription.Row.AutomaticToggle
                    .UnregisterValueChangedCallback(
                        subscription.AutomaticHandler);
            }

            subscriptions.Clear();

            if (view == null)
                return;

            view.Binder.WiFiToggle.UnregisterValueChangedCallback(
                OnWiFiChanged);

            view.Binder.SettingsButton.clicked -= OnSettingsPressed;
            view.Binder.RefreshButton.clicked -= OnRefreshPressed;
        }

        private void OnWiFiChanged(ChangeEvent<bool> evt)
        {
            if (!initialized)
                return;

            engine.SetWiFiEnabled(evt.newValue);
        }

        private void OnNetworkSelected(string id)
        {
            if (!initialized)
                return;

            engine.SelectNetwork(id);
        }

        private void OnAutomaticChanged(string id, bool enabled)
        {
            if (!initialized)
                return;

            engine.SetConnectAutomatically(id, enabled);
        }

        private void OnNetworkAction(string id)
        {
            if (!initialized)
                return;

            bool isConnectedNetwork =
                engine.ConnectedNetwork != null &&
                string.Equals(
                    engine.ConnectedNetwork.Id,
                    id,
                    StringComparison.Ordinal);

            if (isConnectedNetwork)
                engine.Disconnect();
            else
                engine.TryConnect(id);

            view.FocusAction(id);
        }

        private void OnRefreshPressed()
        {
            if (!initialized)
                return;

            engine.Refresh();
        }

        private void OnSettingsPressed()
        {
            if (!initialized || !CanOpenSettings())
                return;

            launchApp(settingsAppId);
            RequestClose();
        }

        private bool CanOpenSettings()
        {
            return !string.IsNullOrWhiteSpace(settingsAppId) &&
                   canAccessApp != null &&
                   canAccessApp(settingsAppId);
        }

        private void Render()
        {
            if (!initialized || view == null)
                return;

            view.Render(builder.BuildState(engine));

            view.SetSettingsAvailable(
                CanOpenSettings());
        }

        private void OnDisable()
        {
            Shutdown();
        }
    }
}