using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Messages
{
    public sealed class MessagesViewBuilder
    {
        private readonly MessagesView view;

        private readonly VisualTreeAsset incomingMessageTemplate;
        private readonly VisualTreeAsset outgoingMessageTemplate;
        private readonly VisualTreeAsset typingIndicatorTemplate;

        private Sprite activeContactAvatar;
        private string activeContactName;

        public MessagesViewBuilder(
            MessagesView view,
            VisualTreeAsset incomingMessageTemplate,
            VisualTreeAsset outgoingMessageTemplate,
            VisualTreeAsset typingIndicatorTemplate)
        {
            this.view = view;
            this.incomingMessageTemplate = incomingMessageTemplate;
            this.outgoingMessageTemplate = outgoingMessageTemplate;
            this.typingIndicatorTemplate = typingIndicatorTemplate;
        }

        public void BuildConversationList(
            IReadOnlyList<MessageConversationData> conversations,
            System.Action<string> onSelected)
        {
            view.ClearConversationList();

            if (conversations == null)
                return;

            foreach (MessageConversationData conversation in conversations)
            {
                if (conversation == null)
                    continue;

                VisualElement item =
                    BuildConversationItem(conversation, onSelected);

                view.ConversationContainer.Add(item);
            }
        }

        public void BuildConversation(
            MessageConversationData conversation)
        {
            view.ClearMessageHistory();

            activeContactAvatar = conversation?.ContactAvatar;
            activeContactName = conversation?.ContactName;

            if (conversation == null)
            {
                view.ShowEmptyState();
                return;
            }

            view.ShowConversation();

            view.SetContact(
                conversation.ContactName,
                conversation.Status,
                conversation.ContactAvatar);

            foreach (MessageData message in conversation.Messages)
                AddMessage(message);

            view.ScrollToBottom();
        }

        public void AddMessage(MessageData message)
        {
            if (message == null)
                return;

            VisualTreeAsset template = message.IsOutgoing
                ? outgoingMessageTemplate
                : incomingMessageTemplate;

            if (template == null)
                return;

            TemplateContainer instance = template.CloneTree();

            instance.style.flexShrink = 0;

            Label sender =
                instance.Q<Label>("MessageSender");

            Label text =
                instance.Q<Label>("MessageText");

            Label time =
                instance.Q<Label>("MessageTime");

            Label read =
                instance.Q<Label>("MessageReadIcon");

            VisualElement avatar =
                instance.Q<VisualElement>("MessageAvatar");

            if (sender != null)
            {
                sender.text = GetSenderName(
                    message,
                    activeContactName);
            }

            if (text != null)
                text.text = message.Text;

            if (time != null)
                time.text = message.Timestamp;

            if (read != null)
            {
                read.text = GetReadIcon(message.ReadState);

                read.EnableInClassList(
                    "messages-message-read--read",
                    message.ReadState == MessageReadState.Read);
            }

            if (avatar != null)
            {
                bool hasPhoto = activeContactAvatar != null;

                avatar.EnableInClassList(
                    "messages-message-avatar--has-photo",
                    hasPhoto);

                if (hasPhoto)
                {
                    avatar.style.backgroundImage =
                        new StyleBackground(activeContactAvatar);
                }
                else
                {
                    avatar.style.backgroundImage = StyleKeyword.Null;
                }
            }

            view.MessageContainer.Add(instance);
            view.ScrollToBottom();
        }

        private VisualElement BuildConversationItem(
            MessageConversationData conversation,
            System.Action<string> onSelected)
        {
            VisualElement root = new();
            root.AddToClassList("messages-conversation-item");

            VisualElement avatar = new();
            avatar.AddToClassList("messages-conversation-avatar");

            if (conversation.ContactAvatar != null)
            {
                avatar.style.backgroundImage =
                    new StyleBackground(conversation.ContactAvatar);
            }

            VisualElement content = new();
            content.AddToClassList("messages-conversation-content");

            VisualElement top = new();
            top.AddToClassList("messages-conversation-top");

            Label name = new(conversation.ContactName);
            name.AddToClassList("messages-conversation-name");

            MessageData lastMessage =
                conversation.GetLastMessage();

            Label time = new(
                lastMessage != null
                    ? lastMessage.Timestamp
                    : string.Empty);

            time.AddToClassList("messages-conversation-time");

            top.Add(name);
            top.Add(time);

            VisualElement bottom = new();
            bottom.AddToClassList("messages-conversation-bottom");

            Label preview = new(
                GetPreviewText(
                    lastMessage,
                    conversation.ContactName));

            preview.AddToClassList("messages-conversation-preview");

            bottom.Add(preview);

            int unreadCount =
                conversation.GetUnreadCount();

            if (unreadCount > 0)
            {
                Label badge = new(unreadCount.ToString());
                badge.AddToClassList("messages-unread-badge");
                bottom.Add(badge);
            }

            content.Add(top);
            content.Add(bottom);

            root.Add(avatar);
            root.Add(content);

            root.RegisterCallback<ClickEvent>(_ =>
                onSelected?.Invoke(conversation.Id));

            return root;
        }

        private static string GetSenderName(
            MessageData message,
            string contactName)
        {
            if (message.IsOutgoing)
                return "You";

            if (!string.IsNullOrWhiteSpace(message.SenderName))
                return message.SenderName;

            return string.IsNullOrWhiteSpace(contactName)
                ? "Unknown"
                : contactName;
        }

        private static string GetPreviewText(
            MessageData message,
            string contactName)
        {
            if (message == null)
                return "No messages yet";

            string sender =
                GetSenderName(message, contactName);

            string text = (message.Text ?? string.Empty)
                .Replace("\r\n", " ")
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            return $"{sender}: {text}";
        }

        private static string GetReadIcon(MessageReadState state)
        {
            return state switch
            {
                MessageReadState.Sent => "✓",
                MessageReadState.Delivered => "✓✓",
                MessageReadState.Read => "✓✓",
                _ => string.Empty
            };
        }
    }
}