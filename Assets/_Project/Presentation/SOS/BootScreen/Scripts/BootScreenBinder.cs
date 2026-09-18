using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.SOS.BootScreen
{
    public class BootScreenBinder : UIBinder
    {
        // -------------------- UI ELEMENTS --------------------
        public VisualElement Root { get; }

        public Image LogoPlaceholder { get; }
        public Label BootTitle { get; }
        public VisualElement FillProgress { get; }
        public Label BootDescription { get; }

        // -------------------- STATE --------------------
        public bool IsValid { get; }

        // -------------------- CONSTRUCTOR --------------------
        public BootScreenBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[BootScreenBinder] Root VisualElement is null.");
                IsValid = false;
                return;
            }

            Root = Bind<VisualElement>(root, "BootScreenRoot");

            LogoPlaceholder = Bind<Image>(root, "BootLogoPlaceholder");
            BootTitle = Bind<Label>(root, "BootTitle");
            FillProgress = Bind<VisualElement>(root, "BootProgressBarFill");
            BootDescription = Bind<Label>(root, "Description");
        }
    }
}