using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantView
    {
        private readonly AssistantBinder binder;
        private readonly VisualTreeAsset assistantTemplate;
        private readonly VisualTreeAsset playerTemplate;

        private VisualElement lastMessage;
        private bool disposed;

        public AssistantView(
            AssistantBinder binder,
            VisualTreeAsset assistantTemplate,
            VisualTreeAsset playerTemplate)
        {
            this.binder = binder;
            this.assistantTemplate = assistantTemplate;
            this.playerTemplate = playerTemplate;

            binder.MessageScroll.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.MessageList.RegisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);
        }

        public Button AddPrompt(string text)
        {
            var button = new Button
            {
                text = text,
                tooltip = text
            };

            button.AddToClassList("assistant-prompt-button");
            binder.PromptList.Add(button);

            return button;
        }

        public void RefreshComposer()
        {
            bool empty = string.IsNullOrEmpty(binder.Input.value);

            binder.Placeholder.style.display =
                empty ? DisplayStyle.Flex : DisplayStyle.None;

            binder.SendButton.SetEnabled(
                !string.IsNullOrWhiteSpace(binder.Input.value));
        }

        public void ClearInput()
        {
            binder.Input.SetValueWithoutNotify(string.Empty);
            RefreshComposer();
        }

        public void AddMessage(string text, bool fromPlayer)
        {
            VisualTreeAsset template =
                fromPlayer ? playerTemplate : assistantTemplate;

            TemplateContainer message = template.CloneTree();
            Label messageText = message.Q<Label>("MessageText");

            if (messageText == null)
            {
                throw new InvalidOperationException(
                    "Assistant message template needs a MessageText label.");
            }

            // Player input should appear literally.
            messageText.enableRichText = false;
            messageText.text = text;

            Label sender = message.Q<Label>("MessageSender");
            if (sender != null)
                sender.text = "ATLAS";

            Label time = message.Q<Label>("MessageTime");
            if (time != null)
                time.text = DateTime.Now.ToString("HH:mm");

            binder.Welcome.style.display = DisplayStyle.None;
            binder.MessageList.Add(message);

            lastMessage = message;
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Wait until layout has measured the newly added message.
            if (!disposed && lastMessage?.panel != null)
                binder.MessageScroll.ScrollTo(lastMessage);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            binder.MessageScroll.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.MessageList.UnregisterCallback<GeometryChangedEvent>(
                OnGeometryChanged);

            binder.PromptList.Clear();
            binder.MessageList.Clear();

            lastMessage = null;
        }
    }
}