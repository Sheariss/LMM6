using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Audio
{
    public sealed class AudioPanelBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Label DeviceName { get; }

        public Button ExpandButton { get; }

        public Button MasterMuteButton { get; }
        public Image MasterIcon { get; }
        public Slider MasterSlider { get; }
        public Label MasterValue { get; }

        public ScrollView Details { get; }
        public VisualElement Categories { get; }

        public DropdownField DeviceField { get; }
        public DropdownField PlaybackField { get; }

        public Toggle MonoToggle { get; }
        public Slider BalanceSlider { get; }
        public Label BalanceValue { get; }
        public Toggle SubtitlesToggle { get; }

        public Button SettingsButton { get; }

        public AudioPanelBinder(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            Root = Bind<VisualElement>(root, "AudioPanel");
            DeviceName = Bind<Label>(root, "AudioDeviceName");

            ExpandButton = Bind<Button>(root, "ExpandAudioButton");

            MasterMuteButton = Bind<Button>(root, "MasterMuteButton");
            MasterIcon = Bind<Image>(root, "MasterVolumeIcon");
            MasterSlider = Bind<Slider>(root, "MasterVolumeSlider");
            MasterValue = Bind<Label>(root, "MasterVolumeValue");

            Details = Bind<ScrollView>(root, "AudioDetails");
            Categories = Bind<VisualElement>(root, "AudioCategoryList");

            DeviceField = Bind<DropdownField>(root, "AudioOutputDevice");
            PlaybackField = Bind<DropdownField>(root, "AudioPlaybackMode");

            MonoToggle = Bind<Toggle>(root, "MonoAudioToggle");
            BalanceSlider = Bind<Slider>(root, "AudioBalanceSlider");
            BalanceValue = Bind<Label>(root, "AudioBalanceValue");
            SubtitlesToggle = Bind<Toggle>(root, "AudioSubtitlesToggle");

            SettingsButton = Bind<Button>(root, "AudioSettingsButton");
        }
    }
}