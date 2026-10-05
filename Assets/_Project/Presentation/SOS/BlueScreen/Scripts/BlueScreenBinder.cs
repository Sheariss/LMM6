using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.BlueScreen
{
    public sealed class BlueScreenBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Label ErrorMessage { get; }
        public Label ProgressLabel { get; }
        public Label StopCodeLabel { get; }
        public VisualElement QRCode { get; }

        public BlueScreenBinder(VisualElement root)
        {
            if (root == null)
            {
                IsValid = false;
                Debug.LogError("[BlueScreenBinder] VE root is null.");
                return;
            }

            Root = Bind<VisualElement>(root, "BlueScreenRoot");
            ErrorMessage = Bind<Label>(Root, "ErrorMessage");
            ProgressLabel = Bind<Label>(Root, "ProgressLabel");
            StopCodeLabel = Bind<Label>(Root, "StopCodeLabel");
            QRCode = Bind<VisualElement>(Root, "QRCode");
        }
    }
}