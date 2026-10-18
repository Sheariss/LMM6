using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailConversationData
    {
        public string Id { get; }
        public IReadOnlyList<EmailMessageData> Messages { get; }

        public EmailMessageData First => Messages[0];

        public EmailMessageData Latest =>
            Messages[Messages.Count - 1];

        public string Subject => First.Subject;

        public bool IsUnread =>
            Messages.Any(message =>
                message.IsReceived && !message.IsRead);

        public bool IsStarred =>
            Messages.Any(message => message.IsStarred);

        public bool HasAttachments =>
            Messages.Any(message => message.Attachments.Count > 0);

        public bool CanMarkRead =>
            Messages.Any(message =>
                message.IsReceived && !message.IsRead);

        public bool CanMarkUnread =>
            Messages.Any(message =>
                message.IsReceived && message.IsRead);

        public EmailConversationData(
            string id,
            IEnumerable<EmailMessageData> messages)
        {
            Id = id;

            List<EmailMessageData> ordered = messages
                .OrderBy(message => message.SortOrder)
                .ThenBy(message => message.Id, StringComparer.Ordinal)
                .ToList();

            if (ordered.Count == 0)
            {
                throw new ArgumentException(
                    "A conversation requires at least one message.",
                    nameof(messages));
            }

            Messages = ordered.AsReadOnly();
        }
    }
}