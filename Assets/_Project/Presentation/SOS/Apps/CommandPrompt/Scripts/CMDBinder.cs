using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    public sealed class CMDBinder : UIBinder
    {
        public VisualElement Root { get; }
        public VisualElement Background { get; }
        public ScrollView ScrollView { get; }
        public TextField CommandInput { get; }

        public CMDBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[CMDBinder] Root VisualElement is null.");
                return;
            }

            Root = Bind<VisualElement>(root, "WindowRoot");
            Background = Bind<VisualElement>(root, "CMDBackground");
            ScrollView = Bind<ScrollView>(root, "ScrollView");
            CommandInput = Bind<TextField>(root, "CommandInput");
        }
    }
}