using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.Credits
{
    public sealed class CreditsView
    {
        private readonly CreditsBinder binder;
        private Action fastForwardStarted;
        private Action fastForwardEnded;
        private Action skipPressed;

        public VisualElement Content => binder.Content;

        public CreditsView(CreditsBinder binder)
        {
            this.binder = binder;
        }

        public void RegisterCallbacks(Action onFastForwardStarted, Action onFastForwardEnded, Action onSkipPressed)
        {
            fastForwardStarted = onFastForwardStarted;
            fastForwardEnded = onFastForwardEnded;
            skipPressed = onSkipPressed;
            binder.ScrollView.RegisterCallback<KeyDownEvent>(OnKeyDown);
            binder.ScrollView.RegisterCallback<KeyUpEvent>(OnKeyUp);
            binder.ScrollView.focusable = true;
            binder.ScrollView.Focus();
        }

        public void UnregisterCallbacks()
        {
            binder.ScrollView.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            binder.ScrollView.UnregisterCallback<KeyUpEvent>(OnKeyUp);
            fastForwardStarted = null;
            fastForwardEnded = null;
            skipPressed = null;
        }

        public void ScrollToTop()
        {
            binder.ScrollView.scrollOffset = Vector2.zero;
        }

        public void Scroll(float amount)
        {
            Vector2 offset = binder.ScrollView.scrollOffset;
            offset.y += amount;
            binder.ScrollView.scrollOffset = offset;
        }

        public bool HasReachedEnd()
        {
            float viewportHeight = binder.ScrollView.contentViewport.layout.height;
            float contentHeight = binder.ScrollView.contentContainer.layout.height;

            if (contentHeight <= viewportHeight)
                return false;

            float maxScroll = contentHeight - viewportHeight;
            return binder.ScrollView.scrollOffset.y >= maxScroll - 1f;
        }

        public void SetFastForwarding(bool active, float multiplier)
        {
            binder.FastForwardIndicator.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
            binder.FastForwardValue.text = $"×{multiplier:0.#}";
            binder.Controls.EnableInClassList("credits-controls--fast-forward", active);
        }

        public void ShowControls()
        {
            binder.Controls.style.display = DisplayStyle.Flex;
        }

        public void HideControls()
        {
            binder.Controls.style.display = DisplayStyle.None;
        }

        public void Focus()
        {
            binder.ScrollView.Focus();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return)
            {
                if (!evt.repeat)
                    fastForwardStarted?.Invoke();

                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Escape)
            {
                skipPressed?.Invoke();
                evt.StopPropagation();
            }
        }

        private void OnKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode != KeyCode.Space && evt.keyCode != KeyCode.Return)
                return;

            fastForwardEnded?.Invoke();
            evt.StopPropagation();
        }
    }
}