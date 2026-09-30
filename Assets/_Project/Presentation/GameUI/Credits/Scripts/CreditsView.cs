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

        private float scrollPosition;
        private bool isFastForwardKeyHeld;

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

            binder.Root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            binder.Root.RegisterCallback<KeyUpEvent>(OnKeyUp);

            binder.Root.focusable = true;
            binder.Root.Focus();
        }

        public void UnregisterCallbacks()
        {
            binder.Root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            binder.Root.UnregisterCallback<KeyUpEvent>(OnKeyUp);

            fastForwardStarted = null;
            fastForwardEnded = null;
            skipPressed = null;
            isFastForwardKeyHeld = false;
        }

        public void InitializePosition()
        {
            float viewportHeight = binder.Viewport.resolvedStyle.height;

            scrollPosition = viewportHeight;
            ApplyPosition();
        }

        public void Scroll(float amount)
        {
            scrollPosition -= amount;
            ApplyPosition();
        }

        public bool HasReachedEnd()
        {
            float contentHeight = binder.Content.resolvedStyle.height;

            if (contentHeight <= 0f)
                return false;

            return scrollPosition <= -contentHeight;
        }

        public void SetFastForwarding(bool active, float multiplier)
        {
            binder.FastForwardIndicator.style.display = active
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.FastForwardValue.text = $"×{multiplier:0.#}";

            binder.FastForwardKey.EnableInClassList(
                "credits-key--active",
                active
            );
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
            binder.Root.Focus();
        }

        private void ApplyPosition()
        {
            binder.Content.transform.position = new Vector3(
                0f,
                scrollPosition,
                0f
            );
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return)
            {
                if (!isFastForwardKeyHeld)
                {
                    isFastForwardKeyHeld = true;
                    fastForwardStarted?.Invoke();
                }

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

            if (isFastForwardKeyHeld)
            {
                isFastForwardKeyHeld = false;
                fastForwardEnded?.Invoke();
            }

            evt.StopPropagation();
        }
    }
}