using System;
using System.Collections.Generic;
using UnityEngine;

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

    public sealed class EmailComposeDraft
    {
        public string Id;
        public string ThreadId;
        public string SourceMessageId;

        public string To = string.Empty;
        public string Subject = string.Empty;
        public string Body = string.Empty;

        public List<string> AttachmentIds = new();
    }

    public sealed class EmailMessageData
    {
        public string Id { get; }
        public string ThreadId { get; internal set; }
        public string SourceMessageId { get; internal set; }

        public string SenderName { get; }
        public string SenderAddress { get; }
        public Sprite SenderAvatar { get; }
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

        public bool IsArchived { get; internal set; }
        public bool IsSpam { get; internal set; }
        public bool IsTrashed { get; internal set; }
        public bool DeliveryFailed { get; internal set; }

        internal HashSet<string> MutableLabels { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<string> UserLabels => MutableLabels;
        public IReadOnlyList<EmailAttachmentData> Attachments { get; }

        public bool IsReceived =>
            Folder != EmailFolder.Sent &&
            Folder != EmailFolder.Drafts;

        public bool InInbox =>
            IsReceived &&
            !IsArchived &&
            !IsSpam &&
            !IsTrashed;

        public string ConversationKey =>
            Folder == EmailFolder.Drafts
                ? "draft:" + Id
                : string.IsNullOrWhiteSpace(ThreadId)
                    ? "message:" + Id
                    : "thread:" + ThreadId;

        public EmailMessageData(EmailMessageDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            Id = definition.Id;
            ThreadId = (definition.ThreadId ?? string.Empty).Trim();

            SenderName = definition.SenderName ?? string.Empty;
            SenderAddress = definition.SenderAddress ?? string.Empty;
            SenderAvatar = definition.SenderAvatar;
            RecipientAddress = definition.RecipientAddress ?? string.Empty;

            Subject = definition.Subject ?? string.Empty;
            Body = definition.Body ?? string.Empty;
            DateLabel = definition.DateLabel ?? string.Empty;
            SortOrder = definition.SortOrder;

            Folder = definition.Folder;
            Category = definition.Category;
            Label = definition.Label;

            IsRead = definition.InitiallyRead;
            IsStarred = definition.InitiallyStarred;

            IsArchived =
                definition.InitiallyArchived || Folder == EmailFolder.Archive;

            IsSpam =
                definition.InitiallySpam || Folder == EmailFolder.Spam;

            IsTrashed =
                definition.InitiallyTrashed || Folder == EmailFolder.Trash;

            if (Label == EmailLabel.Work)
                MutableLabels.Add("Work");
            else if (Label == EmailLabel.Personal)
                MutableLabels.Add("Personal");

            List<EmailAttachmentData> attachments = new();
            HashSet<string> seen = new(StringComparer.Ordinal);

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

                    string fileId = attachment.FileId.Trim();

                    if (seen.Add(fileId))
                        attachments.Add(new EmailAttachmentData(fileId));
                }
            }

            Attachments = attachments.AsReadOnly();
        }
    }
}