using System;
using System.Collections.Generic;
using Atlas.Core.Accessibility;
using Atlas.Core.Settings;
using UnityEngine;

namespace Atlas.Presentation.SOS.Audio
{
    public sealed class AudioPanelEngine : IDisposable
    {
        public static IReadOnlyList<string> CategoryIds { get; } =
            Array.AsReadOnly(new[]
            {
                "music",
                "sfx",
                "ui",
                "notif"
            });

        private readonly SettingsManager settings;
        private readonly AccessibilityManager accessibility;

        private readonly Dictionary<string, float> restoreVolumes =
            new(StringComparer.Ordinal);

        public bool Expanded { get; private set; }

        public event Action Changed;

        public AudioPanelEngine(
            SettingsManager settings,
            AccessibilityManager accessibility)
        {
            this.settings = settings != null
                ? settings
                : throw new ArgumentNullException(nameof(settings));

            this.accessibility = accessibility != null
                ? accessibility
                : throw new ArgumentNullException(nameof(accessibility));

            settings.Initialize();
            accessibility.Initialize();

            RequireSetting("master", SettingControlType.Slider);

            foreach (string id in CategoryIds)
                RequireSetting(id, SettingControlType.Slider);

            RequireSetting("device", SettingControlType.Dropdown);
            RequireSetting("playback-mode", SettingControlType.Dropdown);
            RequireSetting("subtitles", SettingControlType.Toggle);

            RequireAccessibility(
                "mono-audio",
                AccessibilityControlType.Toggle);

            RequireAccessibility(
                "left-right-balance",
                AccessibilityControlType.Slider);

            settings.SettingsChanged += OnPreferencesChanged;
            accessibility.AccessibilityChanged += OnPreferencesChanged;
        }

        public SettingsSaveData CaptureSettings()
        {
            return settings.Current;
        }

        public AccessibilitySaveData CaptureAccessibility()
        {
            return accessibility.Current;
        }

        public SettingDefinition GetDefinition(string id)
        {
            return settings.Catalog.GetSetting(id) ??
                throw new InvalidOperationException(
                    $"Missing setting '{id}'.");
        }

        public void SetExpanded(bool expanded)
        {
            if (Expanded == expanded)
                return;

            Expanded = expanded;
            Changed?.Invoke();
        }

        public void SetVolume(string id, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return;

            SettingsSaveData proposed = settings.Current;
            SettingsSaveData.Entry entry = RequireEntry(proposed, id);

            entry.SliderValue = Mathf.Round(
                Mathf.Clamp(value, 0f, 100f));

            settings.Engine.SetValue(proposed, entry);
            settings.Apply(proposed);
        }

        public void ToggleMute(string id)
        {
            float current = RequireEntry(
                settings.Current,
                id).SliderValue;

            if (current > 0f)
            {
                restoreVolumes[id] = current;
                SetVolume(id, 0f);
                return;
            }

            if (!restoreVolumes.TryGetValue(id, out float restored))
            {
                restored = GetDefinition(id).DefaultSliderValue;

                if (restored <= 0f)
                    restored = 50f;
            }

            SetVolume(id, restored);
        }

        public void SetOption(string id, string optionId)
        {
            SettingsSaveData proposed = settings.Current;
            SettingsSaveData.Entry entry = RequireEntry(proposed, id);

            entry.StringValue = optionId;

            settings.Engine.SetValue(proposed, entry);
            settings.Apply(proposed);
        }

        public void SetSubtitles(bool enabled)
        {
            SettingsSaveData proposed = settings.Current;
            SettingsSaveData.Entry entry = RequireEntry(
                proposed,
                "subtitles");

            entry.ToggleValue = enabled;

            settings.Engine.SetValue(proposed, entry);
            settings.Apply(proposed);
        }

        public void SetMonoAudio(bool enabled)
        {
            AccessibilitySaveData proposed = accessibility.Current;
            AccessibilitySaveData.Entry entry = proposed.Get("mono-audio");

            entry.ToggleValue = enabled;

            accessibility.Engine.SetValue(proposed, entry);
            accessibility.Apply(proposed);
        }

        public void SetBalance(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return;

            AccessibilitySaveData proposed = accessibility.Current;
            AccessibilitySaveData.Entry entry =
                proposed.Get("left-right-balance");

            entry.SliderValue = Mathf.Round(
                Mathf.Clamp(value, 0f, 100f));

            accessibility.Engine.SetValue(proposed, entry);
            accessibility.Apply(proposed);
        }

        public void Dispose()
        {
            settings.SettingsChanged -= OnPreferencesChanged;
            accessibility.AccessibilityChanged -= OnPreferencesChanged;

            Changed = null;
        }

        private void OnPreferencesChanged()
        {
            Changed?.Invoke();
        }

        private void RequireSetting(
            string id,
            SettingControlType controlType)
        {
            if (GetDefinition(id).ControlType != controlType)
            {
                throw new InvalidOperationException(
                    $"Setting '{id}' has the wrong control type.");
            }

            RequireEntry(settings.Current, id);
        }

        private void RequireAccessibility(
            string id,
            AccessibilityControlType controlType)
        {
            AccessibilitySaveData.Entry entry =
                accessibility.Current.Get(id);

            if (entry == null || entry.ControlType != controlType)
            {
                throw new InvalidOperationException(
                    $"Missing or incompatible accessibility setting '{id}'.");
            }
        }

        private static SettingsSaveData.Entry RequireEntry(
            SettingsSaveData data,
            string id)
        {
            return data.Get(id) ??
                throw new InvalidOperationException(
                    $"Missing setting value '{id}'.");
        }
    }
}