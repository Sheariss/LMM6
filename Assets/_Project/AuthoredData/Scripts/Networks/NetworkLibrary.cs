using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.NetworkConnection
{
    public enum NetworkSignal
    {
        Weak,
        Strong
    }

    [Serializable]
    public sealed class NetworkData
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string buttonName;

        [Header("Connection")]
        [SerializeField] private bool secured = true;

        [Tooltip(
            "Represents an available saved connection profile. " +
            "This vertical slice does not request passwords.")]
        [SerializeField] private bool canConnect = true;

        [SerializeField] private bool connectAutomatically = true;

        [SerializeField]
        private NetworkSignal signal =
            NetworkSignal.Strong;

        public string Id => id;
        public string DisplayName => displayName;
        public string ButtonName => buttonName;
        public bool Secured => secured;
        public bool CanConnect => canConnect;
        public bool ConnectAutomatically => connectAutomatically;
        public NetworkSignal Signal => signal;

        public NetworkData()
        {
        }

        public NetworkData(
            string id,
            string displayName,
            string buttonName,
            bool secured,
            bool canConnect,
            NetworkSignal signal)
        {
            this.id = id;
            this.displayName = displayName;
            this.buttonName = buttonName;
            this.secured = secured;
            this.canConnect = canConnect;
            this.signal = signal;
        }
    }

    [CreateAssetMenu(
        fileName = "NetworkLibrary",
        menuName = "ATLAS/SOS/Network Connection/Network Library")]
    public sealed class NetworkLibrary : ScriptableObject
    {
        [Header("Starting State")]
        [SerializeField] private bool initiallyEnabled = true;

        [Tooltip("Leave blank to start without a connection.")]
        [SerializeField] private string initialNetworkId = "home";

        [Header("Available Networks")]
        [SerializeField]
        private List<NetworkData> networks = new()
        {
            new NetworkData(
                "home",
                "Home Network",
                "HomeNetworkButton",
                true,
                true,
                NetworkSignal.Strong),

            new NetworkData(
                "office",
                "Office Network",
                "OfficeNetworkButton",
                true,
                true,
                NetworkSignal.Strong),

            new NetworkData(
                "guest",
                "Guest Wi-Fi",
                "GuestNetworkButton",
                false,
                true,
                NetworkSignal.Strong),

            new NetworkData(
                "studio",
                "Studio_5G",
                "StudioNetworkButton",
                true,
                true,
                NetworkSignal.Strong),

            new NetworkData(
                "hidden",
                "Hidden Network",
                "HiddenNetworkButton",
                true,
                false,
                NetworkSignal.Weak)
        };

        public bool InitiallyEnabled => initiallyEnabled;
        public string InitialNetworkId => initialNetworkId;
        public IReadOnlyList<NetworkData> Networks => networks;
    }
}