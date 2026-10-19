using System;
using System.Collections.Generic;
using Atlas.Core.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Audio
{
    public sealed class AudioVolumeRow
    {
        public string Id;
        public Button MuteButton;
        public Image Icon;
        public Slider Slider;
        public Label Value;
        public VisualElement Fill;
    }

    public sealed class AudioPanelViewState
    {
        public bool Expanded;

        public readonly Dictionary<string, float> Volumes =
            new(StringComparer.Ordinal);

        public string DeviceId;
        public string PlaybackId;

        public bool Mono;
        public float Balance;
        public bool Subtitles;
    }

    public sealed class AudioPanelViewBuilder
    {
        public AudioPanelView Build(
            VisualTreeAsset template,
            AudioPanelEngine engine)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            if (engine == null)
                throw new ArgumentNullException(nameof(engine));

            TemplateContainer host = template.CloneTree();

            host.name = "AudioPanelHost";
            host.pickingMode = PickingMode.Ignore;

            host.style.position = Position.Absolute;
            host.style.left = 12;
            host.style.right = 12;
            host.style.top = 12;
            host.style.bottom = 12;
            host.style.display = DisplayStyle.None;

            var binder = new AudioPanelBinder(host);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "The audio panel is missing required elements.");
            }

            binder.Root.pickingMode = PickingMode.Position;
            binder.DeviceName.enableRichText = false;

            var rows = new Dictionary<string, AudioVolumeRow>(
                StringComparer.Ordinal);

            ConfigureSlider(
                binder.MasterSlider,
                engine.GetDefinition("master"));

            rows.Add("master", new AudioVolumeRow
            {
                Id = "master",
                MuteButton = binder.MasterMuteButton,
                Icon = binder.MasterIcon,
                Slider = binder.MasterSlider,
                Value = binder.MasterValue,
                Fill = AddSliderFill(binder.MasterSlider)
            });

            binder.Categories.Clear();

            foreach (string id in AudioPanelEngine.CategoryIds)
            {
                SettingDefinition definition = engine.GetDefinition(id);
                AudioVolumeRow row = BuildVolumeRow(
                    definition,
                    binder.Categories);

                rows.Add(id, row);
            }

            AddSwitchThumb(binder.MonoToggle);
            AddSwitchThumb(binder.SubtitlesToggle);

            AddDropdownIcon(binder.DeviceField);
            AddDropdownIcon(binder.PlaybackField);

            // Balance is centered at 50 in the current accessibility catalog.
            binder.BalanceSlider.lowValue = 0;
            binder.BalanceSlider.highValue = 100;

            binder.BalanceSlider.tooltip =
                "Left/right balance; 50 is centered";

            var view = new AudioPanelView(
                host,
                binder,
                rows,
                LoadVolumeIcon("volume_up"),
                LoadVolumeIcon("volume_off"));

            view.ConfigureOptions(
                binder.DeviceField,
                engine.GetDefinition("device").Options);

            view.ConfigureOptions(
                binder.PlaybackField,
                engine.GetDefinition("playback-mode").Options);

            return view;
        }

        public AudioPanelViewState BuildState(AudioPanelEngine engine)
        {
            var settings = engine.CaptureSettings();
            var accessibility = engine.CaptureAccessibility();

            var state = new AudioPanelViewState
            {
                Expanded = engine.Expanded,

                DeviceId = settings.Get("device").StringValue,
                PlaybackId = settings.Get("playback-mode").StringValue,

                Mono = accessibility.Get("mono-audio").ToggleValue,

                Balance = accessibility
                    .Get("left-right-balance").SliderValue,

                Subtitles = settings.Get("subtitles").ToggleValue
            };

            state.Volumes.Add(
                "master",
                settings.Get("master").SliderValue);

            foreach (string id in AudioPanelEngine.CategoryIds)
                state.Volumes.Add(id, settings.Get(id).SliderValue);

            return state;
        }

        private static AudioVolumeRow BuildVolumeRow(
            SettingDefinition definition,
            VisualElement parent)
        {
            var root = new VisualElement
            {
                name = definition.Id + "AudioRow"
            };

            root.AddToClassList("audio-category-row");

            var label = new Label(definition.Label)
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList("audio-category-label");

            var controls = new VisualElement();
            controls.AddToClassList("audio-volume-controls");

            var mute = new Button
            {
                name = definition.Id + "MuteButton"
            };

            mute.AddToClassList("audio-button");
            mute.AddToClassList("audio-icon-button");

            var icon = new Image
            {
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
                tintColor = Color.white
            };

            icon.AddToClassList("audio-icon");
            mute.Add(icon);

            var slider = new Slider
            {
                name = definition.Id + "VolumeSlider",
                tooltip = definition.Label
            };

            slider.AddToClassList("audio-slider");
            ConfigureSlider(slider, definition);

            var value = new Label
            {
                pickingMode = PickingMode.Ignore
            };

            value.AddToClassList("audio-volume-value");

            controls.Add(mute);
            controls.Add(slider);
            controls.Add(value);

            root.Add(label);
            root.Add(controls);
            parent.Add(root);

            return new AudioVolumeRow
            {
                Id = definition.Id,
                MuteButton = mute,
                Icon = icon,
                Slider = slider,
                Value = value,
                Fill = AddSliderFill(slider)
            };
        }

        private static void ConfigureSlider(
            Slider slider,
            SettingDefinition definition)
        {
            slider.lowValue = definition.Minimum;
            slider.highValue = definition.Maximum;

            slider.pageSize = definition.Step > 0f
                ? definition.Step
                : 1f;
        }

        private static VisualElement AddSliderFill(Slider slider)
        {
            VisualElement tracker = slider.Q<VisualElement>(
                className: "unity-base-slider__tracker");

            if (tracker == null)
            {
                throw new InvalidOperationException(
                    $"Slider '{slider.name}' has no native tracker.");
            }

            var fill = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

            fill.AddToClassList("audio-slider-fill");
            tracker.Add(fill);

            return fill;
        }

        private static void AddSwitchThumb(Toggle toggle)
        {
            VisualElement input = toggle.Q<VisualElement>(
                className: "unity-toggle__input");

            if (input == null)
            {
                throw new InvalidOperationException(
                    $"Toggle '{toggle.name}' has no native input.");
            }

            var thumb = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

            thumb.AddToClassList("audio-switch-thumb");
            input.Add(thumb);
        }

        private static void AddDropdownIcon(DropdownField field)
        {
            VisualElement input = field.Q<VisualElement>(
                className: "unity-base-popup-field__input");

            if (input == null)
            {
                throw new InvalidOperationException(
                    $"Dropdown '{field.name}' has no native input.");
            }

            var icon = new Image
            {
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };

            icon.AddToClassList("audio-icon");
            icon.AddToClassList("audio-dropdown-icon");

            input.Add(icon);
        }

        private static Texture2D LoadVolumeIcon(string name)
        {
            string path =
                $"Icons/System/Volume/{name}" +
                "_32dp_FFFFFF_FILL0_wght400_GRAD0_opsz40";

            Texture2D icon = Resources.Load<Texture2D>(path);

            if (icon == null)
            {
                throw new InvalidOperationException(
                    $"Missing audio icon '{path}'.");
            }

            return icon;
        }
    }
}