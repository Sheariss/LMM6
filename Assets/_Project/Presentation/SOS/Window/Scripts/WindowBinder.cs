using UnityEngine;
using UnityEngine.UIElements;
using Atlas.Utils;

namespace Atlas.Presentation.SOS.Windows
{
    public sealed class WindowBinder : UIBinder
    {
        public VisualElement Root { get; }

        public VisualElement TitleBar { get; }

        public Image Icon { get; }
        public Label Title { get; }

        public Button MinimizeButton { get; }
        public Button MaximizeButton { get; }
        public Button CloseButton { get; }

        public VisualElement Content { get; }

        public WindowBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[WindowBinder] Root VisualElement is null.");
                return;
            }

            Root = Bind<VisualElement>(root, "WindowRoot");

            TitleBar = Bind<VisualElement>(root, "WindowTitleBar");

            Icon = Bind<Image>(root, "WindowIcon");
            Title = Bind<Label>(root, "WindowTitle");

            MinimizeButton = Bind<Button>(root, "WindowMinimizeButton");
            MaximizeButton = Bind<Button>(root, "WindowMaximizeButton");
            CloseButton = Bind<Button>(root, "WindowCloseButton");

            Content = Bind<VisualElement>(root, "WindowContent");
        }
    }
}