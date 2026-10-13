using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Presentation.SOS.Messages
{
    public sealed class MessagesEngine
    {
        private readonly MessagesManager manager;

        private MessageConversationData activeConversation;

        public MessageConversationData ActiveConversation =>
            activeConversation;

        public MessagesEngine(MessagesManager manager)
        {
            this.manager = manager;
        }

        public IReadOnlyList<MessageConversationData> GetConversations()
        {
            return manager.Conversations;
        }

        public IReadOnlyList<MessageConversationData> SearchConversations(
            string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return manager.Conversations;

            string query = search.Trim();

            return manager.Conversations
                .Where(conversation =>
                    MatchesConversation(conversation, query))
                .ToList();
        }

        private static bool MatchesConversation(
            MessageConversationData conversation,
            string query)
        {
            if (conversation == null)
                return false;

            if (ContainsQuery(conversation.ContactName, query))
                return true;

            return conversation.Messages != null &&
                conversation.Messages.Any(message =>
                    message != null &&
                    (ContainsQuery(message.SenderName, query) ||
                     ContainsQuery(message.Text, query)));
        }

        private static bool ContainsQuery(string value, string query)
        {
            return !string.IsNullOrEmpty(value) &&
                value.IndexOf(
                    query,
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public MessageConversationData SelectConversation(string id)
        {
            activeConversation = manager.GetConversation(id);

            if (activeConversation != null)
                manager.MarkConversationRead(activeConversation.Id);

            return activeConversation;
        }

        public MessageData SendMessage(string text)
        {
            if (activeConversation == null)
                return null;

            if (string.IsNullOrWhiteSpace(text))
                return null;

            MessageData message = new(
                Guid.NewGuid().ToString(),
                "You",
                text.Trim(),
                DateTime.Now.ToString("h:mm tt"),
                MessageDirection.Outgoing,
                MessageReadState.Sent);

            manager.AddOutgoingMessage(
                activeConversation.Id,
                message);

            return message;
        }
    }
}