using System.Collections.Generic;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailAttachmentData
    {
        public string FileId { get; }

        public EmailAttachmentData(string fileId)
        {
            FileId = fileId ?? string.Empty;
        }
    }

    public sealed class EmailMessageData
    {
        public string Id { get; }
        public string ThreadId { get; }

        public string SenderName { get; }
        public string SenderAddress { get; }
        public string RecipientAddress { get; }

        public string Subject { get; }
        public string Body { get; }
        public string DateLabel { get; }

        public int SortOrder { get; }

        public EmailFolder Folder { get; }
        public EmailCategory Category { get; }
        public EmailLabel Label { get; }

        public bool IsRead { get; internal set; }
        public bool IsStarred { get; internal set; }

        public IReadOnlyList<EmailAttachmentData> Attachments { get; }

        // Drafts remain separate from delivered conversations.
        public string ConversationKey =>
            Folder == EmailFolder.Drafts
                ? "draft:" + Id
                : string.IsNullOrWhiteSpace(ThreadId)
                    ? "message:" + Id
                    : "thread:" + ThreadId;

        public bool IsReceived =>
            Folder == EmailFolder.Inbox ||
            Folder == EmailFolder.Archive;

        public EmailMessageData(EmailMessageDefinition definition)
        {
            Id = definition.Id;
            ThreadId = (definition.ThreadId ?? string.Empty).Trim();

            SenderName = definition.SenderName ?? string.Empty;
            SenderAddress = definition.SenderAddress ?? string.Empty;

            RecipientAddress =
                definition.RecipientAddress ?? string.Empty;

            Subject = definition.Subject ?? string.Empty;
            Body = definition.Body ?? string.Empty;
            DateLabel = definition.DateLabel ?? string.Empty;

            SortOrder = definition.SortOrder;

            Folder = definition.Folder;
            Category = definition.Category;
            Label = definition.Label;

            IsRead = definition.InitiallyRead;
            IsStarred = definition.InitiallyStarred;

            List<EmailAttachmentData> attachments = new();

            if (definition.Attachments != null)
            {
                foreach (EmailAttachmentDefinition attachment
                         in definition.Attachments)
                {
                    if (attachment == null ||
                        string.IsNullOrWhiteSpace(attachment.FileId))
                    {
                        continue;
                    }

                    attachments.Add(
                        new EmailAttachmentData(attachment.FileId));
                }
            }

            Attachments = attachments.AsReadOnly();
        }
    }
}