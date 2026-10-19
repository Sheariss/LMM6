using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.NetworkConnection
{
    public sealed class NetworkConnectionRow
    {
        public VisualElement Root;
        public Button SelectButton;

        public Label Status;
        public VisualElement Expansion;

        public Toggle AutomaticToggle;
        public Button ActionButton;
    }

    public sealed class NetworkConnectionRowState
    {
        public string Id;
        public bool Selected;
        public bool Connected;
        public bool ConnectAutomatically;
        public bool CanConnect;
        public string Status;
    }

    public sealed class NetworkConnectionViewState
    {
        public bool WiFiEnabled;
        public string Message;
        public NetworkConnectionRowState[] Rows;
    }

    public sealed class NetworkConnectionViewBuilder
    {
        private readonly Texture2D securedIcon;
        private readonly Texture2D strongIcon;
        private readonly Texture2D weakIcon;

        public NetworkConnectionViewBuilder()
        {
            securedIcon = LoadIcon(
                "Icons/System/Network/Wifi-Locked/wifi_lock_32dp");

            strongIcon = LoadIcon(
                "Icons/System/Network/Wifi-3-Bar/wifi_32dp");

            weakIcon = LoadIcon(
                "Icons/System/Network/Wifi-1-Bar/wifi_1_bar_32dp");
        }

        public NetworkConnectionView Build(
            VisualTreeAsset panelTemplate,
            IReadOnlyList<NetworkData> networks)
        {
            if (panelTemplate == null)
            {
                throw new ArgumentNullException(
                    nameof(panelTemplate));
            }

            if (networks == null)
                throw new ArgumentNullException(nameof(networks));

            TemplateContainer host = panelTemplate.CloneTree();

            host.name = "NetworkConnectionPanelHost";
            host.pickingMode = PickingMode.Ignore;

            host.style.position = Position.Absolute;
            host.style.left = 12;
            host.style.right = 12;
            host.style.top = 12;
            host.style.bottom = 12;
            host.style.display = DisplayStyle.None;

            var binder = new NetworkConnectionBinder(host);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "The Network Connection panel is missing " +
                    "required elements.");
            }

            binder.Root.pickingMode = PickingMode.Position;
            binder.Message.enableRichText = false;

            var rows =
                new Dictionary<string, NetworkConnectionRow>(
                    StringComparer.Ordinal);

            binder.Networks.Clear();

            foreach (NetworkData network in networks)
            {
                NetworkConnectionRow row = CreateRow(network);

                rows.Add(network.Id, row);
                binder.Networks.Add(row.Root);
            }

            return new NetworkConnectionView(
                host,
                binder,
                rows);
        }

        public NetworkConnectionViewState BuildState(
            NetworkConnectionEngine engine)
        {
            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            string message = engine.Message;

            if (!engine.WiFiEnabled)
                message = "Wi-Fi is turned off.";
            else if (engine.Networks.Count == 0)
                message = "No networks are available.";

            var state = new NetworkConnectionViewState
            {
                WiFiEnabled = engine.WiFiEnabled,
                Message = message,

                Rows = new NetworkConnectionRowState[
                    engine.Networks.Count]
            };

            for (int i = 0; i < engine.Networks.Count; i++)
            {
                NetworkData network = engine.Networks[i];

                bool connected =
                    engine.IsConnected &&
                    ReferenceEquals(
                        network,
                        engine.ConnectedNetwork);

                bool selected =
                    engine.WiFiEnabled &&
                    ReferenceEquals(
                        network,
                        engine.SelectedNetwork);

                string security = network.Secured
                    ? "Secured"
                    : "Open";

                state.Rows[i] = new NetworkConnectionRowState
                {
                    Id = network.Id,
                    Selected = selected,
                    Connected = connected,
                    CanConnect = network.CanConnect,

                    ConnectAutomatically =
                        engine.GetConnectAutomatically(network.Id),

                    Status = connected
                        ? $"Connected, {security.ToLowerInvariant()}"
                        : security
                };
            }

            return state;
        }

        private NetworkConnectionRow CreateRow(NetworkData network)
        {
            var root = new VisualElement
            {
                name = network.Id + "NetworkRow"
            };

            root.AddToClassList("wifi-network");

            var indicator = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

            indicator.AddToClassList(
                "wifi-network-selection-indicator");

            var selectButton = new Button
            {
                name = network.ButtonName,
                tooltip = $"Select {network.DisplayName}"
            };

            selectButton.AddToClassList("wifi-button");
            selectButton.AddToClassList("wifi-network-header");

            var icon = new Image
            {
                image = GetSignalIcon(network),
                scaleMode = ScaleMode.ScaleToFit,
                tintColor = Color.white,
                pickingMode = PickingMode.Ignore
            };

            icon.AddToClassList("wifi-network-icon");

            var copy = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

            copy.AddToClassList("wifi-network-copy");

            var name = new Label(network.DisplayName)
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            name.AddToClassList("wifi-network-name");

            var status = new Label
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            status.AddToClassList("wifi-network-status");

            copy.Add(name);
            copy.Add(status);

            selectButton.Add(icon);
            selectButton.Add(copy);

            var expansion = new VisualElement();
            expansion.AddToClassList("wifi-network-expansion");

            var automatic = new Toggle
            {
                text = "Connect automatically",
                tooltip =
                    "Reconnect when Wi-Fi is turned on."
            };

            automatic.AddToClassList("wifi-auto");

            var actionRow = new VisualElement();
            actionRow.AddToClassList("wifi-network-action-row");

            var actionButton = new Button
            {
                text = "Connect"
            };

            actionButton.AddToClassList("wifi-button");
            actionButton.AddToClassList("wifi-connect-button");

            actionRow.Add(actionButton);

            expansion.Add(automatic);
            expansion.Add(actionRow);

            root.Add(selectButton);
            root.Add(expansion);
            root.Add(indicator);

            return new NetworkConnectionRow
            {
                Root = root,
                SelectButton = selectButton,
                Status = status,
                Expansion = expansion,
                AutomaticToggle = automatic,
                ActionButton = actionButton
            };
        }

        private Texture2D GetSignalIcon(NetworkData network)
        {
            if (network.Secured)
                return securedIcon;

            return network.Signal == NetworkSignal.Weak
                ? weakIcon
                : strongIcon;
        }

        private static Texture2D LoadIcon(string path)
        {
            Texture2D texture = Resources.Load<Texture2D>(path);

            if (texture == null)
            {
                throw new InvalidOperationException(
                    $"Network Connection icon is missing: {path}");
            }

            return texture;
        }
    }
}