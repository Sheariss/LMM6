using System;
using System.Collections.Generic;

namespace Atlas.Presentation.SOS.NetworkConnection
{
    public sealed class NetworkConnectionEngine
    {
        private readonly IReadOnlyList<NetworkData> networks;

        private readonly Dictionary<string, NetworkData> byId =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string, bool> automatic =
            new(StringComparer.Ordinal);

        private string preferredNetworkId;

        public IReadOnlyList<NetworkData> Networks => networks;

        public bool WiFiEnabled { get; private set; }

        public NetworkData SelectedNetwork { get; private set; }

        public NetworkData ConnectedNetwork { get; private set; }

        public string Message { get; private set; } = string.Empty;

        public bool IsConnected =>
            WiFiEnabled && ConnectedNetwork != null;

        public event Action Changed;

        public NetworkConnectionEngine(NetworkLibrary library)
        {
            if (library == null)
                throw new ArgumentNullException(nameof(library));

            if (library.Networks == null)
            {
                throw new InvalidOperationException(
                    "The network library has no network collection.");
            }

            var entries = new List<NetworkData>();
            var buttonNames = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (NetworkData network in library.Networks)
            {
                if (network == null ||
                    string.IsNullOrWhiteSpace(network.Id) ||
                    string.IsNullOrWhiteSpace(network.DisplayName) ||
                    string.IsNullOrWhiteSpace(network.ButtonName))
                {
                    throw new InvalidOperationException(
                        "Every network requires an ID, display name, " +
                        "and button name.");
                }

                if (byId.ContainsKey(network.Id))
                {
                    throw new InvalidOperationException(
                        $"Duplicate network ID '{network.Id}'.");
                }

                if (!buttonNames.Add(network.ButtonName))
                {
                    throw new InvalidOperationException(
                        "Duplicate network button name " +
                        $"'{network.ButtonName}'.");
                }

                byId.Add(network.Id, network);

                automatic.Add(
                    network.Id,
                    network.ConnectAutomatically);

                entries.Add(network);
            }

            networks = entries.AsReadOnly();

            NetworkData initial = null;

            if (!string.IsNullOrWhiteSpace(library.InitialNetworkId))
            {
                if (!byId.TryGetValue(
                        library.InitialNetworkId,
                        out initial))
                {
                    throw new InvalidOperationException(
                        "The initial network ID is not in the library.");
                }

                if (!initial.CanConnect)
                {
                    throw new InvalidOperationException(
                        "The initial network must be connectable.");
                }
            }

            WiFiEnabled = library.InitiallyEnabled;

            ConnectedNetwork = WiFiEnabled ? initial : null;
            SelectedNetwork = ConnectedNetwork;

            preferredNetworkId = initial?.Id;
        }

        public void SelectNetwork(string id)
        {
            if (!WiFiEnabled)
                return;

            if (string.IsNullOrWhiteSpace(id) ||
                !byId.TryGetValue(id, out NetworkData network))
            {
                return;
            }

            if (ReferenceEquals(SelectedNetwork, network))
                return;

            SelectedNetwork = network;
            Message = string.Empty;

            Changed?.Invoke();
        }

        public bool GetConnectAutomatically(string id)
        {
            return id != null &&
                   automatic.TryGetValue(id, out bool enabled) &&
                   enabled;
        }

        public void SetConnectAutomatically(string id, bool enabled)
        {
            if (id == null ||
                !automatic.TryGetValue(id, out bool current) ||
                current == enabled)
            {
                return;
            }

            automatic[id] = enabled;

            Changed?.Invoke();
        }

        public void SetWiFiEnabled(bool enabled)
        {
            if (WiFiEnabled == enabled)
                return;

            WiFiEnabled = enabled;
            ConnectedNetwork = null;
            SelectedNetwork = null;
            Message = string.Empty;

            if (enabled)
            {
                ConnectedNetwork = FindAutomaticConnection();
                SelectedNetwork = ConnectedNetwork;
            }

            Changed?.Invoke();
        }

        public bool TryConnect(string id)
        {
            if (!WiFiEnabled)
                return Reject("Turn on Wi-Fi before connecting.");

            if (string.IsNullOrWhiteSpace(id) ||
                !byId.TryGetValue(id, out NetworkData network))
            {
                return Reject("That network is unavailable.");
            }

            if (!network.CanConnect)
            {
                return Reject(
                    "No saved connection profile is available " +
                    "for this network.");
            }

            ConnectedNetwork = network;
            SelectedNetwork = network;
            preferredNetworkId = network.Id;
            Message = string.Empty;

            Changed?.Invoke();
            return true;
        }

        public void Disconnect()
        {
            if (ConnectedNetwork == null)
                return;

            // Keep this network expanded after disconnecting.
            SelectedNetwork = ConnectedNetwork;
            ConnectedNetwork = null;
            Message = string.Empty;

            Changed?.Invoke();
        }

        public void Refresh()
        {
            // Networks are authored and do not change during the slice.
            // Refresh clears transient connection errors.
            Message = string.Empty;
            Changed?.Invoke();
        }

        private NetworkData FindAutomaticConnection()
        {
            if (!string.IsNullOrWhiteSpace(preferredNetworkId) &&
                byId.TryGetValue(
                    preferredNetworkId,
                    out NetworkData preferred) &&
                preferred.CanConnect &&
                GetConnectAutomatically(preferred.Id))
            {
                return preferred;
            }

            foreach (NetworkData network in networks)
            {
                if (network.CanConnect &&
                    GetConnectAutomatically(network.Id))
                {
                    return network;
                }
            }

            return null;
        }

        private bool Reject(string message)
        {
            Message = message;
            Changed?.Invoke();

            return false;
        }
    }
}