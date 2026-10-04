using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Options
{
    public sealed class OptionsShellView
    {
        private readonly OptionsShellBinder binder;
        private Action backPressed;
        private Action resetPressed;
        private Action applyPressed;

        public VisualElement NavigationContainer => binder.NavigationContainer;
        public VisualElement OptionsContainer => binder.OptionsContainer;

        public OptionsShellView(OptionsShellBinder binder)
        {
            this.binder = binder;
        }

        public void RegisterCallbacks(Action onBackPressed, Action onResetPressed, Action onApplyPressed)
        {
            backPressed = onBackPressed;
            resetPressed = onResetPressed;
            applyPressed = onApplyPressed;
            binder.BackButton.clicked += OnBackPressed;
            binder.ResetButton.clicked += OnResetPressed;
            binder.ApplyButton.clicked += OnApplyPressed;
        }

        public void UnregisterCallbacks()
        {
            binder.BackButton.clicked -= OnBackPressed;
            binder.ResetButton.clicked -= OnResetPressed;
            binder.ApplyButton.clicked -= OnApplyPressed;
            backPressed = null;
            resetPressed = null;
            applyPressed = null;
        }

        public void SetHeader(string title, string description)
        {
            binder.TitleLabel.text = title;
            binder.DescriptionLabel.text = description;
        }

        public void SetSectionHeader(string title, string description)
        {
            binder.SectionTitleLabel.text = title;
            binder.SectionDescriptionLabel.text = description;
        }

        public void ClearNavigation()
        {
            binder.NavigationContainer.Clear();
        }

        public void ClearOptions()
        {
            binder.OptionsContainer.Clear();
        }

        public void SetApplyEnabled(bool enabled)
        {
            binder.ApplyButton.SetEnabled(enabled);
        }

        public void SetResetEnabled(bool enabled)
        {
            binder.ResetButton.SetEnabled(enabled);
        }

        public void Show()
        {
            binder.Root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            binder.Root.style.display = DisplayStyle.None;
        }

        private void OnBackPressed()
        {
            backPressed?.Invoke();
        }

        private void OnResetPressed()
        {
            resetPressed?.Invoke();
        }

        private void OnApplyPressed()
        {
            applyPressed?.Invoke();
        }
    }
}