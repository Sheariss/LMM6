using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantBinder : UIBinder
    {
        public ScrollView MessageScroll { get; }
        public VisualElement MessageList { get; }
        public VisualElement Welcome { get; }
        public VisualElement Choices { get; }
        public VisualElement PromptList { get; }

        public TextField Input { get; }
        public Label Placeholder { get; }
        public Button SendButton { get; }
        public Label Status { get; }

        public AssistantBinder(VisualElement root)
        {
            MessageScroll = Bind<ScrollView>(
                root, "AssistantMessageScrollView");

            MessageList = Bind<VisualElement>(
                root, "AssistantMessageList");

            Welcome = Bind<VisualElement>(
                root, "AssistantWelcome");

            Choices = Bind<VisualElement>(
                root, "AssistantChoices");

            PromptList = Bind<VisualElement>(
                root, "AssistantPromptList");

            Input = Bind<TextField>(
                root, "AssistantMessageInput");

            Placeholder = Bind<Label>(
                root, "AssistantInputPlaceholder");

            SendButton = Bind<Button>(
                root, "AssistantSendButton");

            Status = Bind<Label>(
                root, "AssistantStatusLabel");
        }
    }
}