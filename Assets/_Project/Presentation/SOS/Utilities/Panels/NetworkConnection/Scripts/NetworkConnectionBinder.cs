using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.NetworkConnection
{
    public sealed class NetworkConnectionBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Toggle WiFiToggle { get; }

        public ScrollView NetworkList { get; }
        public Label Message { get; }
        public VisualElement Networks { get; }

        public Button SettingsButton { get; }
        public Button RefreshButton { get; }

        public NetworkConnectionBinder(VisualElement instanceRoot)
        {
            if (instanceRoot == null)
            {
                throw new ArgumentNullException(
                    nameof(instanceRoot));
            }

            Root = Bind<VisualElement>(
                instanceRoot, "WiFiPanel");

            WiFiToggle = Bind<Toggle>(
                instanceRoot, "WiFiToggle");

            NetworkList = Bind<ScrollView>(
                instanceRoot, "NetworkList");

            Message = Bind<Label>(
                instanceRoot, "NetworkMessage");

            Networks = Bind<VisualElement>(
                instanceRoot, "AvailableNetworks");

            SettingsButton = Bind<Button>(
                instanceRoot, "NetworkSettingsButton");

            RefreshButton = Bind<Button>(
                instanceRoot, "RefreshNetworksButton");

            if (!IsValid)
                return;

            VisualElement input = WiFiToggle.Q<VisualElement>(
                className: "unity-toggle__input");

            if (input == null)
            {
                throw new InvalidOperationException(
                    "The Wi-Fi Toggle has no native input element.");
            }

            var thumb = new VisualElement
            {
                name = "WiFiSwitchThumb",
                pickingMode = PickingMode.Ignore
            };

            thumb.AddToClassList("wifi-switch-thumb");
            input.Add(thumb);
        }
    }
}