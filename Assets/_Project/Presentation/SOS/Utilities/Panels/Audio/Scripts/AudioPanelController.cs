using System;
using System.Collections.Generic;
using Atlas.Core.Accessibility;
using Atlas.Core.Settings;
using Atlas.Presentation.SOS.Panels;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Audio
{
    public sealed class AudioPanelController : PanelRuntime
    {
        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelTemplate;

        [Header("Settings Destination")]
        [SerializeField] private string settingsAppId = "control-panel";

        private readonly List<Action> unsubscribe = new();

        private AudioPanelEngine engine;
        private AudioPanelViewBuilder builder;
        private AudioPanelView view;

        private Func<string, bool> canAccessApp;
        private Action<string> launchApp;

        private bool initialized;

        public override VisualElement Surface => view?.Binder.Root;

        public override void Initialize(
            VisualElement parent,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "AudioPanelController must be active.");
            }

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            this.launchApp = launchApp ??
                throw new ArgumentNullException(nameof(launchApp));

            try
            {
                engine = new AudioPanelEngine(
                    SettingsManager.Instance,
                    AccessibilityManager.Instance);

                builder = new AudioPanelViewBuilder();
                view = builder.Build(panelTemplate, engine);

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

            engine.SetExpanded(false);

            view.Binder.Details.scrollOffset = Vector2.zero;

            Render();
            view.Show();
        }

        public override void Hide()
        {
            view?.Hide();
        }

        public override void Shutdown()
        {
            initialized = false;

            Hide();

            foreach (Action remove in unsubscribe)
                remove();

            unsubscribe.Clear();

            engine?.Dispose();
            view?.Detach();

            engine = null;
            builder = null;
            view = null;

            canAccessApp = null;
            launchApp = null;
        }

        private void RegisterEvents()
        {
            AudioPanelBinder binder = view.Binder;

            BindClick(binder.ExpandButton, ToggleExpanded);
            BindClick(binder.SettingsButton, OpenSettings);

            foreach (var entry in view.Rows)
            {
                string id = entry.Key;
                AudioVolumeRow row = entry.Value;

                BindClick(
                    row.MuteButton,
                    () => engine.ToggleMute(id));

                BindChange<float>(
                    row.Slider,
                    value => engine.SetVolume(id, value));
            }

            BindChange<string>(
                binder.DeviceField,
                _ => SelectOption("device", binder.DeviceField));

            BindChange<string>(
                binder.PlaybackField,
                _ => SelectOption(
                    "playback-mode",
                    binder.PlaybackField));

            BindChange<bool>(
                binder.MonoToggle,
                engine.SetMonoAudio);

            BindChange<float>(
                binder.BalanceSlider,
                engine.SetBalance);

            BindChange<bool>(
                binder.SubtitlesToggle,
                engine.SetSubtitles);

            binder.Root.RegisterCallback<KeyDownEvent>(OnKeyDown);

            unsubscribe.Add(() =>
                binder.Root.UnregisterCallback<KeyDownEvent>(
                    OnKeyDown));

            engine.Changed += Render;
            unsubscribe.Add(() => engine.Changed -= Render);
        }

        private void BindClick(Button button, Action action)
        {
            Action handler = () =>
            {
                if (initialized)
                    action();
            };

            button.clicked += handler;
            unsubscribe.Add(() => button.clicked -= handler);
        }

        private void BindChange<T>(
            BaseField<T> field,
            Action<T> action)
        {
            EventCallback<ChangeEvent<T>> handler = evt =>
            {
                if (initialized)
                    action(evt.newValue);
            };

            field.RegisterValueChangedCallback(handler);

            unsubscribe.Add(() =>
                field.UnregisterValueChangedCallback(handler));
        }

        private void ToggleExpanded()
        {
            engine.SetExpanded(!engine.Expanded);
        }

        private void SelectOption(
            string settingId,
            DropdownField field)
        {
            string optionId = view.GetSelectedOptionId(field);

            if (optionId != null)
                engine.SetOption(settingId, optionId);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!initialized ||
                evt.keyCode != KeyCode.Escape ||
                !engine.Expanded)
            {
                return;
            }

            engine.SetExpanded(false);

            view.Binder.ExpandButton.Focus();

            evt.StopPropagation();
        }

        private bool CanOpenSettings()
        {
            return !string.IsNullOrWhiteSpace(settingsAppId) &&
                   canAccessApp != null &&
                   canAccessApp(settingsAppId);
        }

        private void OpenSettings()
        {
            if (!CanOpenSettings())
                return;

            launchApp(settingsAppId);

            if (initialized)
                RequestClose();
        }

        private void Render()
        {
            if (!initialized || view == null)
                return;

            view.Render(builder.BuildState(engine));
            view.SetSettingsAvailable(CanOpenSettings());
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}