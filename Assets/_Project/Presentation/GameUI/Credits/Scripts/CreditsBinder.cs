using UnityEngine.UIElements;

namespace Atlas.Presentation.Credits
{
    public sealed class CreditsBinder : UIBinder
    {
        public ScrollView ScrollView { get; }
        public VisualElement Content { get; }
        public VisualElement Controls { get; }
        public VisualElement FastForwardKey { get; }
        public Label FastForwardKeyLabel { get; }
        public Label FastForwardControlLabel { get; }
        public VisualElement SkipKey { get; }
        public Label SkipKeyLabel { get; }
        public Label SkipControlLabel { get; }
        public VisualElement FastForwardIndicator { get; }
        public VisualElement FastForwardGlyph { get; }
        public Label FastForwardValue { get; }

        public CreditsBinder(VisualElement root)
        {
            ScrollView = Bind<ScrollView>(root, "Credits-ScrollView");
            Content = Bind<VisualElement>(root, "Credits-Content");
            Controls = Bind<VisualElement>(root, "Credits-Controls");
            FastForwardKey = Bind<VisualElement>(root, "Credits-FastForwardKey");
            FastForwardKeyLabel = Bind<Label>(root, "Credits-FastForwardKeyLabel");
            FastForwardControlLabel = Bind<Label>(root, "Credits-FastForwardControlLabel");
            SkipKey = Bind<VisualElement>(root, "Credits-SkipKey");
            SkipKeyLabel = Bind<Label>(root, "Credits-SkipKeyLabel");
            SkipControlLabel = Bind<Label>(root, "Credits-SkipControlLabel");
            FastForwardIndicator = Bind<VisualElement>(root, "Credits-FastForwardIndicator");
            FastForwardGlyph = Bind<VisualElement>(root, "Credits-FastForwardGlyph");
            FastForwardValue = Bind<Label>(root, "Credits-FastForwardValue");
        }
    }
}