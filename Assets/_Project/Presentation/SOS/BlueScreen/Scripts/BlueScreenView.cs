using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.BlueScreen
{
    public sealed class BlueScreenView
    {
        private readonly BlueScreenBinder binder;

        public BlueScreenView(BlueScreenBinder binder)
        {
            if (binder == null)
                throw new ArgumentNullException(nameof(binder));

            if (!binder.IsValid)
                throw new ArgumentException(
                    "The blue screen binder is invalid.",
                    nameof(binder));

            this.binder = binder;
        }

        public void Render(BlueScreenViewState state)
        {
            if (state == null)
                return;

            binder.Root.style.display = state.IsVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.ErrorMessage.text = state.MessageText;
            binder.ProgressLabel.text = state.ProgressText;
            binder.StopCodeLabel.text = state.StopCodeText;

            bool hasImage = state.QRImage != null;

            binder.QRCode.style.display = hasImage
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.QRCode.style.backgroundImage = hasImage
                ? new StyleBackground(state.QRImage)
                : new StyleBackground(StyleKeyword.None);
        }
    }
}