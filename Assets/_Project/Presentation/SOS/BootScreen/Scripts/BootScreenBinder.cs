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


        // -------------------- CONSTRUCTOR --------------------
        public BootScreenBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[BootScreenBinder] Root VisualElement is null.");
                return;
            }

            Root = Bind<VisualElement>(root, "BootScreenRoot");

            LogoPlaceholder = Bind<Image>(root, "BootLogoPlaceholder");
            BootTitle = Bind<Label>(root, "BootTitle");
            FillProgress = Bind<VisualElement>(root, "BootProgressFillBar");
            BootDescription = Bind<Label>(root, "BootDescription");
        }
    }
}