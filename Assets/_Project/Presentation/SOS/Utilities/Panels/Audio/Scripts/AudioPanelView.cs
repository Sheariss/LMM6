using System;
using System.Collections.Generic;
using Atlas.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Audio
{
    public sealed class AudioPanelView
    {
        private readonly Dictionary<string, AudioVolumeRow> rows;

        private readonly Dictionary<
            DropdownField,
            IReadOnlyList<SettingOptionDefinition>> options = new();

        private readonly Texture2D volumeIcon;
        private readonly Texture2D mutedIcon;

        public TemplateContainer Host { get; }
        public AudioPanelBinder Binder { get; }

        public IReadOnlyDictionary<string, AudioVolumeRow> Rows => rows;

        public AudioPanelView(
            TemplateContainer host,
            AudioPanelBinder binder,
            Dictionary<string, AudioVolumeRow> rows,
            Texture2D volumeIcon,
            Texture2D mutedIcon)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Binder = binder ?? throw new ArgumentNullException(nameof(binder));

            this.rows = rows ??
                throw new ArgumentNullException(nameof(rows));

            this.volumeIcon = volumeIcon;
            this.mutedIcon = mutedIcon;

            Binder.MasterIcon.scaleMode = ScaleMode.ScaleToFit;
            Binder.MasterIcon.tintColor = Color.white;
        }

        public void Show()
        {
            Host.style.display = DisplayStyle.Flex;
            Host.BringToFront();

            Binder.MasterSlider.Focus();
        }

        public void Hide()
        {
            Host.style.display = DisplayStyle.None;
        }

        public void Detach()
        {
            Host.RemoveFromHierarchy();
        }

        public void ConfigureOptions(
            DropdownField field,
            IReadOnlyList<SettingOptionDefinition> definitions)
        {
            if (definitions == null || definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Dropdown '{field.name}' has no options.");
            }

            var choices = new List<string>();

            foreach (SettingOptionDefinition definition in definitions)
                choices.Add(definition.Label);

            options.Add(field, definitions);
            field.choices = choices;
        }

        public string GetSelectedOptionId(DropdownField field)
        {
            int index = field.index;
            IReadOnlyList<SettingOptionDefinition> definitions =
                options[field];

            return index >= 0 && index < definitions.Count
                ? definitions[index].Id
                : null;
        }

        public void Render(AudioPanelViewState state)
        {
            Binder.Root.EnableInClassList(
                "audio-panel--expanded",
                state.Expanded);

            Binder.ExpandButton.tooltip = state.Expanded
                ? "Collapse audio controls"
                : "Expand audio controls";

            foreach (var entry in rows)
            {
                AudioVolumeRow row = entry.Value;
                float value = state.Volumes[entry.Key];
                bool muted = value <= 0f;

                row.Slider.SetValueWithoutNotify(value);
                row.Value.text = Mathf.RoundToInt(value).ToString();

                row.Icon.image = muted ? mutedIcon : volumeIcon;

                row.MuteButton.tooltip = muted
                    ? $"Unmute {row.Slider.tooltip}"
                    : $"Mute {row.Slider.tooltip}";

                float fraction = Mathf.InverseLerp(
                    row.Slider.lowValue,
                    row.Slider.highValue,
                    value);

                row.Fill.style.width =
                    Length.Percent(fraction * 100f);
            }

            SetOption(Binder.DeviceField, state.DeviceId);
            SetOption(Binder.PlaybackField, state.PlaybackId);

            Binder.DeviceName.text = Binder.DeviceField.value;
            Binder.DeviceName.tooltip = Binder.DeviceField.value;

            SetSwitch(Binder.MonoToggle, state.Mono);
            SetSwitch(Binder.SubtitlesToggle, state.Subtitles);

            Binder.BalanceSlider.SetValueWithoutNotify(state.Balance);

            int balance = Mathf.RoundToInt(state.Balance);

            Binder.BalanceValue.text =
                $"{100 - balance} / {balance}";
        }

        public void SetSettingsAvailable(bool available)
        {
            Binder.SettingsButton.SetEnabled(available);

            Binder.SettingsButton.tooltip = available
                ? "Open Control Panel"
                : "Control Panel is unavailable for this user.";
        }

        private void SetOption(DropdownField field, string id)
        {
            IReadOnlyList<SettingOptionDefinition> definitions =
                options[field];

            foreach (SettingOptionDefinition definition in definitions)
            {
                if (string.Equals(
                    definition.Id,
                    id,
                    StringComparison.Ordinal))
                {
                    field.SetValueWithoutNotify(definition.Label);
                    return;
                }
            }

            throw new InvalidOperationException(
                $"Dropdown '{field.name}' has no option '{id}'.");
        }

        private static void SetSwitch(Toggle toggle, bool enabled)
        {
            toggle.SetValueWithoutNotify(enabled);
            toggle.EnableInClassList("audio-switch--on", enabled);
        }
    }
}