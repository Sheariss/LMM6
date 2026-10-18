using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Contacts;
using UnityEngine;
using UnityEngine.Serialization;

namespace Atlas.Presentation.SOS.Email
{
    public enum EmailFolder
    {
        Inbox = 0,
        Sent = 1,
        Drafts = 2,
        Archive = 3,
        Spam = 4,
        Trash = 5
    }

    public enum EmailCategory
    {
        Primary = 0,
        Promotions = 1,
        Social = 2
    }

    public enum EmailLabel
    {
        None = 0,
        Work = 1,
        Personal = 2
    }

    public enum EmailFilter
    {
        Inbox = 0,
        Starred = 1,
        Sent = 2,
        Drafts = 3,
        Work = 4,
        Personal = 5,
        All = 6,
        Archive = 7,
        Spam = 8,
        Trash = 9,
        CustomLabel = 10
    }

    public enum EmailDestination
    {
        Inbox = 0,
        Archive = 1,
        Spam = 2,
        Trash = 3
    }

    [CreateAssetMenu(
        fileName = "EmailLibrary",
        menuName = "ATLAS/SOS/Email/Email Library")]
    public sealed class EmailLibrary : ScriptableObject
    {
        [Tooltip(
            "Optional Contacts library used by chain participants. " +
            "Custom participants do not require it.")]
        [SerializeField]
        private ContactLibrary contacts;

        [SerializeField]
        private List<EmailMailboxDefinition> mailboxes = new();

        public IReadOnlyList<EmailMailboxDefinition> Mailboxes => mailboxes;

        public List<EmailMessageDefinition> BuildMessages(
            EmailMailboxDefinition mailbox)
        {
            if (mailbox == null)
                throw new ArgumentNullException(nameof(mailbox));

            List<EmailMessageDefinition> result = new();

            // Kept for incremental migration of existing authored assets.
            if (mailbox.LegacyMessages != null)
            {
                foreach (EmailMessageDefinition message
                         in mailbox.LegacyMessages)
                {
                    if (message == null)
                        throw Invalid(mailbox, "A legacy message is null.");

                    result.Add(message);
                }
            }

            HashSet<string> chainIds = new(StringComparer.Ordinal);

            if (mailbox.Chains != null)
            {
                foreach (EmailChainDefinition chain in mailbox.Chains)
                {
                    if (chain == null ||
                        string.IsNullOrWhiteSpace(chain.Id))
                    {
                        throw Invalid(
                            mailbox, "Every email chain needs an ID.");
                    }

                    string chainId = chain.Id.Trim();

                    if (!chainIds.Add(chainId))
                    {
                        throw Invalid(
                            mailbox, $"Duplicate chain ID '{chainId}'.");
                    }

                    BuildChain(mailbox, chain, chainId, result);
                }
            }

            HashSet<string> messageIds = new(StringComparer.Ordinal);

            foreach (EmailMessageDefinition message in result)
            {
                if (string.IsNullOrWhiteSpace(message.Id) ||
                    !messageIds.Add(message.Id))
                {
                    throw Invalid(
                        mailbox,
                        $"Missing or duplicate message ID '{message.Id}'.");
                }
            }

            return result;
        }

        private void BuildChain(
            EmailMailboxDefinition mailbox,
            EmailChainDefinition chain,
            string chainId,
            List<EmailMessageDefinition> result)
        {
            Dictionary<string, Identity> participants =
                new(StringComparer.OrdinalIgnoreCase)
                {
                    [EmailParticipantDefinition.SelfId] = new Identity
                    {
                        Name = mailbox.DisplayName,
                        Address = mailbox.EmailAddress,
                        Avatar = mailbox.ProfileImage
                    }
                };

            if (chain.Participants != null)
            {
                foreach (EmailParticipantDefinition participant
                         in chain.Participants)
                {
                    if (participant == null ||
                        string.IsNullOrWhiteSpace(participant.Id))
                    {
                        throw Invalid(
                            mailbox,
                            $"Chain '{chainId}' has a participant without an ID.");
                    }

                    string id = participant.Id.Trim();

                    if (participants.ContainsKey(id))
                    {
                        throw Invalid(
                            mailbox,
                            $"Chain '{chainId}' repeats participant '{id}'. " +
                            "'self' is reserved for the mailbox owner.");
                    }

                    participants.Add(
                        id, ResolveParticipant(mailbox, chainId, participant));
                }
            }

            if (chain.Messages == null)
                return;

            foreach (EmailChainMessageDefinition message in chain.Messages)
            {
                if (message == null)
                {
                    throw Invalid(
                        mailbox, $"Chain '{chainId}' contains a null message.");
                }

                string senderId = (message.SenderId ?? string.Empty).Trim();

                if (!participants.TryGetValue(senderId, out Identity sender))
                {
                    throw Invalid(
                        mailbox,
                        $"Message '{message.Id}' in '{chainId}' refers to " +
                        $"unknown sender '{senderId}'.");
                }

                List<string> recipients =
                    message.RecipientIds != null &&
                    message.RecipientIds.Count > 0
                        ? message.RecipientIds
                        : chain.DefaultRecipientIds;

                List<string> addresses = new();
                HashSet<string> seen =
                    new(StringComparer.OrdinalIgnoreCase);

                if (recipients != null)
                {
                    foreach (string recipientId in recipients)
                    {
                        string id = (recipientId ?? string.Empty).Trim();

                        if (!participants.TryGetValue(id, out Identity recipient))
                        {
                            throw Invalid(
                                mailbox,
                                $"Message '{message.Id}' in '{chainId}' refers " +
                                $"to unknown recipient '{id}'.");
                        }

                        if (seen.Add(recipient.Address))
                            addresses.Add(recipient.Address);
                    }
                }

                if (addresses.Count == 0)
                {
                    throw Invalid(
                        mailbox,
                        $"Message '{message.Id}' in '{chainId}' " +
                        "needs at least one recipient.");
                }

                bool outgoing = string.Equals(
                    sender.Address,
                    mailbox.EmailAddress,
                    StringComparison.OrdinalIgnoreCase);

                if (message.IsDraft && !outgoing)
                {
                    throw Invalid(
                        mailbox,
                        $"Draft '{message.Id}' must be sent by the mailbox owner.");
                }

                result.Add(new EmailMessageDefinition
                {
                    Id = message.Id,
                    ThreadId = chainId,
                    SenderName = sender.Name,
                    SenderAddress = sender.Address,
                    SenderAvatar = sender.Avatar,
                    RecipientAddress = string.Join(", ", addresses),

                    Subject = string.IsNullOrWhiteSpace(message.SubjectOverride)
                        ? chain.Subject
                        : message.SubjectOverride,

                    Body = message.Body,
                    DateLabel = message.DateLabel,
                    SortOrder = message.SortOrder,

                    Folder = message.IsDraft
                        ? EmailFolder.Drafts
                        : outgoing
                            ? EmailFolder.Sent
                            : EmailFolder.Inbox,

                    Category = chain.Category,
                    Label = chain.Label,
                    InitiallyRead = outgoing || message.InitiallyRead,
                    InitiallyStarred = message.InitiallyStarred,

                    InitiallyArchived =
                        chain.Location == EmailDestination.Archive,

                    InitiallySpam =
                        chain.Location == EmailDestination.Spam,

                    InitiallyTrashed =
                        chain.Location == EmailDestination.Trash,

                    Attachments = message.Attachments
                });
            }
        }

        private Identity ResolveParticipant(
            EmailMailboxDefinition mailbox,
            string chainId,
            EmailParticipantDefinition participant)
        {
            ContactData contact = null;

            if (!string.IsNullOrWhiteSpace(participant.ContactId))
            {
                contact = contacts != null
                    ? contacts.GetById(participant.ContactId.Trim())
                    : null;

                if (contact == null)
                {
                    throw Invalid(
                        mailbox,
                        $"Participant '{participant.Id}' in '{chainId}' " +
                        $"cannot resolve contact '{participant.ContactId}'. " +
                        "Assign the Contacts library or use a custom identity.");
                }
            }

            string address = participant.EmailAddress;

            if (string.IsNullOrWhiteSpace(address) && contact != null)
            {
                address = participant.UseWorkEmail
                    ? contact.WorkEmail
                    : contact.PrimaryEmail;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                throw Invalid(
                    mailbox,
                    $"Participant '{participant.Id}' in '{chainId}' " +
                    "has no email address.");
            }

            string name = participant.DisplayName;

            if (string.IsNullOrWhiteSpace(name))
                name = contact != null ? contact.DisplayName : address;

            return new Identity
            {
                Name = name.Trim(),
                Address = address.Trim(),
                Avatar = participant.ProfileImage != null
                    ? participant.ProfileImage
                    : contact?.ProfileImage
            };
        }

        private static InvalidOperationException Invalid(
            EmailMailboxDefinition mailbox,
            string message)
        {
            return new InvalidOperationException(
                $"Email mailbox '{mailbox.UserId}': {message}");
        }

        private sealed class Identity
        {
            public string Name;
            public string Address;
            public Sprite Avatar;
        }
    }

    [Serializable]
    public sealed class EmailMailboxDefinition
    {
        public string UserId;
        public string DisplayName;
        public string EmailAddress;
        public string Initials;
        public Sprite ProfileImage;

        public List<string> LabelNames = new();

        [Header("Email Chains")]
        public List<EmailChainDefinition> Chains = new();

        [Header("Legacy Messages — migrate these into Chains")]
        [Tooltip(
            "Preserves your existing flat email catalog. " +
            "Remove a legacy message after recreating it in a chain. " +
            "Do not keep the same message ID in both places.")]
        [FormerlySerializedAs("Messages")]
        public List<EmailMessageDefinition> LegacyMessages = new();
    }

    [Serializable]
    public sealed class EmailChainDefinition
    {
        public string Id;
        public string Subject;

        public EmailCategory Category;
        public EmailLabel Label;
        public EmailDestination Location;

        [Tooltip(
            "'self' automatically refers to the mailbox owner. " +
            "Add other participants here.")]
        public List<EmailParticipantDefinition> Participants = new();

        [Tooltip(
            "Used when a message has no recipient overrides. " +
            "Use participant IDs, such as self, danny, or maya.")]
        public List<string> DefaultRecipientIds = new()
        {
            EmailParticipantDefinition.SelfId
        };

        public List<EmailChainMessageDefinition> Messages = new();
    }

    [Serializable]
    public sealed class EmailParticipantDefinition
    {
        public const string SelfId = "self";

        [Tooltip("An ID local to this chain, for example: danny")]
        public string Id;

        [Tooltip(
            "Optional ContactLibrary contact ID. " +
            "Leave empty for a custom identity.")]
        public string ContactId;

        [Tooltip(
            "When using a contact, choose WorkEmail instead of PrimaryEmail. " +
            "An explicit EmailAddress below overrides either choice.")]
        public bool UseWorkEmail;

        [Header("Custom Identity / Contact Overrides")]
        public string DisplayName;
        public string EmailAddress;
        public Sprite ProfileImage;
    }

    [Serializable]
    public sealed class EmailChainMessageDefinition
    {
        public string Id;

        [Tooltip(
            "Participant ID of the sender. " +
            "Use self for the mailbox owner.")]
        public string SenderId;

        [Tooltip(
            "Leave empty to use the chain's default recipients. " +
            "Otherwise list participant IDs.")]
        public List<string> RecipientIds = new();

        [Tooltip("Leave empty to use the chain's subject.")]
        public string SubjectOverride;

        [TextArea(5, 20)]
        public string Body;

        public string DateLabel;

        [Tooltip(
            "Higher values are newer. " +
            "Use a consistent ordering across the whole mailbox.")]
        public int SortOrder;

        public bool IsDraft;
        public bool InitiallyRead;
        public bool InitiallyStarred;

        public List<EmailAttachmentDefinition> Attachments = new();
    }

    // Flattened definition used by runtime-generated messages and legacy data.
    [Serializable]
    public sealed class EmailMessageDefinition
    {
        public string Id;
        public string ThreadId;

        public string SenderName;
        public string SenderAddress;
        public Sprite SenderAvatar;
        public string RecipientAddress;

        public string Subject;

        [TextArea(5, 20)]
        public string Body;

        public string DateLabel;
        public int SortOrder;

        public EmailFolder Folder;
        public EmailCategory Category;
        public EmailLabel Label;

        public bool InitiallyRead;
        public bool InitiallyStarred;

        public bool InitiallyArchived;
        public bool InitiallySpam;
        public bool InitiallyTrashed;

        public List<EmailAttachmentDefinition> Attachments = new();
    }

    [Serializable]
    public sealed class EmailAttachmentDefinition
    {
        [Tooltip(
            "File ID in this mailbox user's simulated filesystem. " +
            "Use the file ID, not the filename or ContentId.")]
        public string FileId;
    }
}