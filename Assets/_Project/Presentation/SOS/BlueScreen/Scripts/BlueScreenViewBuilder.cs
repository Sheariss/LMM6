using UnityEngine;

namespace Atlas.Presentation.SOS.BlueScreen
{
    public sealed class BlueScreenViewState
    {
        public bool IsVisible { get; }
        public string MessageText { get; }
        public string ProgressText { get; }
        public string StopCodeText { get; }
        public Texture2D QRImage { get; }

        public BlueScreenViewState(
            bool isVisible,
            string messageText,
            string progressText,
            string stopCodeText,
            Texture2D qrImage)
        {
            IsVisible = isVisible;
            MessageText = messageText;
            ProgressText = progressText;
            StopCodeText = stopCodeText;
            QRImage = qrImage;
        }
    }

        public sealed class BlueScreenViewBuilder
    {


        public BlueScreenViewState Build(
            BlueScreenEngine engine,
            Texture2D qrImage = null)
        {
            if (engine == null)
            {
                return new BlueScreenViewState(
                    false,
                    BlueScreenEngine.DefaultMessage,
                    "0% complete",
                    $"Stop code: {BlueScreenEngine.DefaultStopCode}",
                    null);
            }

            return new BlueScreenViewState(
                isVisible: engine.IsVisible,
                messageText: engine.Message,
                progressText: $"{engine.Progress}% complete",
                stopCodeText: $"Stop code: {engine.StopCode}",
                qrImage: qrImage);
        }
    }
}