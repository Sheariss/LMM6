using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantViewBuilder
    {
        private readonly VisualTreeAsset assistantTemplate;
        private readonly VisualTreeAsset playerTemplate;

        public AssistantViewBuilder(
            VisualTreeAsset assistantTemplate,
            VisualTreeAsset playerTemplate)
        {
            this.assistantTemplate = assistantTemplate;
            this.playerTemplate = playerTemplate;
        }

        public VisualElement BuildMessage(AssistantMessageData data)
        {
            var template = data.fromPlayer
                ? playerTemplate
                : assistantTemplate;

            var element = template.CloneTree();
            var text = element.Q<Label>("MessageText");

            if (text == null)
                throw new InvalidOperationException(
                    "Message template requires MessageText.");

            text.enableRichText = false;
            text.text = data.text;

            var sender = element.Q<Label>("MessageSender");

            if (sender != null)
                sender.text = data.fromPlayer ? "YOU" : "ATLAS";

            var time = element.Q<Label>("MessageTime");

            if (time != null)
                time.text = data.time;

            return element;
        }

        public Button BuildChoice(string label, Action clicked)
        {
            var button = new Button(clicked)
            {
                text = label
            };

            button.AddToClassList("assistant-choice-button");
            return button;
        }

        public Button BuildConversation(
            string title,
            bool selected,
            Action clicked)
        {
            var button = BuildChoice(title, clicked);

            if (selected)
                button.AddToClassList(
                    "assistant-choice-button--primary");

            return button;
        }

        public VisualElement BuildOffer(
            AssistantOfferDefinition definition,
            bool deferred,
            Action accept,
            Action later,
            Action dismiss)
        {
            var card = new VisualElement();

            card.AddToClassList("assistant-message-bubble");
            card.AddToClassList(
                "assistant-message-bubble--assistant");

            card.style.marginBottom = 8;

            var title = new Label(
                deferred
                    ? $"Saved: {definition.title}"
                    : definition.title);

            title.AddToClassList("assistant-message-sender");
            card.Add(title);

            var description = new Label(definition.description);
            description.enableRichText = false;
            description.AddToClassList("assistant-message-text");
            card.Add(description);

            card.Add(BuildChoice(
                !string.IsNullOrWhiteSpace(definition.invitationConversationId)
                    ? "Open introduction conversation"
                    : deferred ? "Open saved guidance" : "Open guidance",
                accept));

            if (!string.IsNullOrWhiteSpace(definition.invitationConversationId))
                return card;

            if (!deferred)
                card.Add(BuildChoice("Later", later));

            card.Add(BuildChoice("Dismiss", dismiss));

            return card;
        }
    }
}
