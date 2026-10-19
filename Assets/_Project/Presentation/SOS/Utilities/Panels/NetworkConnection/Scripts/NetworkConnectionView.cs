using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.NetworkConnection
{
    public sealed class NetworkConnectionView
    {
        private readonly Dictionary<string, NetworkConnectionRow> rows;

        public TemplateContainer Host { get; }
        public NetworkConnectionBinder Binder { get; }

        public IReadOnlyDictionary<string, NetworkConnectionRow> Rows =>
            rows;

        public NetworkConnectionView(
            TemplateContainer host,
            NetworkConnectionBinder binder,
            Dictionary<string, NetworkConnectionRow> rows)
        {
            Host = host ??
                throw new ArgumentNullException(nameof(host));

            Binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.rows = rows ??
                throw new ArgumentNullException(nameof(rows));
        }

        public void Show()
        {
            Host.style.display = DisplayStyle.Flex;
            Host.BringToFront();

            Binder.WiFiToggle.Focus();
        }

        public void Hide()
        {
            Host.style.display = DisplayStyle.None;
        }

        public void Render(NetworkConnectionViewState state)
        {
            Binder.WiFiToggle.SetValueWithoutNotify(
                state.WiFiEnabled);

            Binder.WiFiToggle.EnableInClassList(
                "wifi-switch--on",
                state.WiFiEnabled);

            Binder.Message.text = state.Message;

            SetVisible(
                Binder.Message,
                !string.IsNullOrEmpty(state.Message));

            SetVisible(
                Binder.Networks,
                state.WiFiEnabled);

            Binder.RefreshButton.SetEnabled(state.WiFiEnabled);

            foreach (NetworkConnectionRowState rowState in state.Rows)
            {
                NetworkConnectionRow row = rows[rowState.Id];

                row.Root.EnableInClassList(
                    "wifi-network--selected",
                    rowState.Selected);

                row.Status.text = rowState.Status;

                SetVisible(row.Status, rowState.Selected);
                SetVisible(row.Expansion, rowState.Selected);

                SetVisible(
                    row.AutomaticToggle,
                    rowState.Selected && !rowState.Connected);

                row.AutomaticToggle.SetValueWithoutNotify(
                    rowState.ConnectAutomatically);

                row.ActionButton.text = rowState.Connected
                    ? "Disconnect"
                    : "Connect";

                row.ActionButton.SetEnabled(
                    rowState.Connected || rowState.CanConnect);

                row.ActionButton.tooltip =
                    !rowState.Connected && !rowState.CanConnect
                        ? "No saved connection profile is available."
                        : row.ActionButton.text;
            }
        }

        public void SetSettingsAvailable(bool available)
        {
            Binder.SettingsButton.SetEnabled(available);

            Binder.SettingsButton.tooltip = available
                ? "Open Control Panel"
                : "Control Panel is unavailable for this user.";
        }

        public void FocusAction(string id)
        {
            if (id != null && rows.TryGetValue(id, out var row))
                row.ActionButton.Focus();
        }

        public void Detach()
        {
            Host.RemoveFromHierarchy();
        }

        private static void SetVisible(
            VisualElement element,
            bool visible)
        {
            element.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }
    }
}