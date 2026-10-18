using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Email
{
    public enum EmailFolder
    {
        Inbox,
        Sent,
        Drafts,
        Archive
    }

    public enum EmailCategory
    {
        Primary,
        Promotions,
        Social
    }

    public enum EmailLabel
    {
        None,
        Work,
        Personal
    }

    public enum EmailFilter
    {
        Inbox,
        Starred,
        Sent,
        Drafts,
        Work,
        Personal,
        All
    }

    [CreateAssetMenu(
        fileName = "EmailLibrary",
        menuName = "ATLAS/SOS/Email/Email Library")]
    public sealed class EmailLibrary : ScriptableObject
    {
        [SerializeField]
        private List<EmailMailboxDefinition> mailboxes = new();

        public IReadOnlyList<EmailMailboxDefinition> Mailboxes =>
            mailboxes;
    }

    [Serializable]
    public sealed class EmailMailboxDefinition
    {
        public string UserId;
        public string DisplayName;
        public string EmailAddress;
        public string Initials;

        public List<EmailMessageDefinition> Messages = new();
    }

    [Serializable]
    public sealed class EmailMessageDefinition
    {
        public string Id;

        [Tooltip(
            "Messages sharing this ID form a conversation. " +
            "Leave empty for standalone mail.")]
        public string ThreadId;

        public string SenderName;
        public string SenderAddress;
        public string RecipientAddress;
        public string Subject;

        [TextArea(5, 20)]
        public string Body;

        public string DateLabel;

        [Tooltip("Higher values are newer.")]
        public int SortOrder;

        public EmailFolder Folder;
        public EmailCategory Category;
        public EmailLabel Label;

        public bool InitiallyRead;
        public bool InitiallyStarred;

        public List<EmailAttachmentDefinition> Attachments = new();
    }

    [Serializable]
    public sealed class EmailAttachmentDefinition
    {
        [Tooltip(
            "File ID in the mailbox user's simulated filesystem. " +
            "Use the file ID, not its filename or ContentId.")]
        public string FileId;
    }
}