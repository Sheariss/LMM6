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

            string normalized = search.Trim();

            return manager.Conversations
                .Where(x =>
                    x.ContactName.Contains(
                        normalized,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
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