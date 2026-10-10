using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarPinnedButtonBinder : UIBinder
    {
        public VisualElement InstanceRoot { get; }
        public VisualElement Root { get; }
        public Button Button { get; }
        public Image Icon { get; }
        public VisualElement WindowDot { get; }

        public TaskbarPinnedButtonBinder(VisualElement instanceRoot)
        {
            InstanceRoot = instanceRoot;

            if (InstanceRoot == null)
            {
                IsValid = false;
                return;
            }

            Root = Bind<VisualElement>(
                InstanceRoot, "PinnedIconRoot");

            Button = Bind<Button>(Root, "PinnedButton");
            Icon = Bind<Image>(Button, "AppIcon");
            WindowDot = Bind<VisualElement>(Button, "WindowDot");
        }
    }
}