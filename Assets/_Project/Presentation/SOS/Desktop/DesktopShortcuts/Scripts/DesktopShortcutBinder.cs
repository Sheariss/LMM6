using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopShortcutBinder : UIBinder
    {
        public VisualElement InstanceRoot { get; }
        public VisualElement Root { get; }
        public Button Button { get; }
        public Image Icon { get; }
        public VisualElement Arrow { get; }
        public Label Label { get; }

        public DesktopShortcutBinder(VisualElement instanceRoot)
        {
            InstanceRoot = instanceRoot;

            if (InstanceRoot == null)
            {
                IsValid = false;
                return;
            }

            Root = Bind<VisualElement>(
                InstanceRoot, "DesktopShortcutRoot");

            Button = Bind<Button>(Root, "ShortcutButton");
            Icon = Bind<Image>(Button, "ShortcutIcon");
            Arrow = Bind<VisualElement>(Icon, "ShortcutArrow");
            Label = Bind<Label>(Button, "ShortcutLabel");
        }
    }
}