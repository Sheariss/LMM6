using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using Atlas.SOS.Authentication;
using UnityEngine;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailManager : MonoBehaviour
    {
        public static EmailManager Instance { get; private set; }

        [SerializeField] private EmailLibrary emailLibrary;

        [SerializeField, Min(0)]
        private float deliveryFailureDelay = 1.5f;

        private sealed class Mailbox
        {
            public readonly List<EmailMessageData> Messages = new();

            public readonly List<string> Labels = new()
            {
                "Work",
                "Personal"
            };
        }

        private sealed class PendingFailure
        {
            public string UserId;
            public Mailbox Mailbox;
            public string SentId;
            public double Due;
        }

        private readonly Dictionary<string, EmailMailboxDefinition>
            definitions = new(StringComparer.Ordinal);

        private readonly Dictionary<string, List<EmailMessageDefinition>>
            authoredMessages = new(StringComparer.Ordinal);

        private readonly Dictionary<string, Mailbox>
            mailboxes = new(StringComparer.Ordinal);

        private readonly List<PendingFailure> pending = new();

        public event Action<string> MailboxChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            if (emailLibrary == null)
            {
                throw new InvalidOperationException(
                    "EmailManager requires an EmailLibrary.");
            }

            foreach (EmailMailboxDefinition definition in emailLibrary.Mailboxes)
            {
                if (definition == null ||
                    string.IsNullOrWhiteSpace(definition.UserId) ||
                    definitions.ContainsKey(definition.UserId))
                {
                    throw new InvalidOperationException(
                        "Email mailbox IDs must be present and unique.");
                }

                if (string.IsNullOrWhiteSpace(definition.EmailAddress))
                {
                    throw new InvalidOperationException(
                        $"Mailbox '{definition.UserId}' needs an email address.");
                }

                List<EmailMessageDefinition> messages =
                    emailLibrary.BuildMessages(definition);

                definitions.Add(definition.UserId, definition);
                authoredMessages.Add(definition.UserId, messages);
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            pending.Clear();

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            for (int index = pending.Count - 1; index >= 0; index--)
            {
                PendingFailure item = pending[index];

                if (Time.unscaledTimeAsDouble < item.Due)
                    continue;

                pending.RemoveAt(index);

                if (!mailboxes.TryGetValue(item.UserId, out Mailbox box) ||
                    !ReferenceEquals(box, item.Mailbox))
                {
                    continue;
                }

                EmailMessageData sent =
                    box.Messages.Find(message => message.Id == item.SentId);

                if (sent == null)
                    continue;

                sent.DeliveryFailed = true;

                EmailMailboxDefinition account = definitions[item.UserId];

                string address = account.EmailAddress ?? string.Empty;
                int at = address.LastIndexOf('@');

                string domain = at >= 0 && at < address.Length - 1
                    ? address.Substring(at + 1)
                    : "mail.invalid";

                EmailMessageDefinition failure = new()
                {
                    Id = "failure-" + Guid.NewGuid().ToString("N"),
                    SenderName = "Mail Delivery Subsystem",
                    SenderAddress = "postmaster@" + domain,
                    RecipientAddress = account.EmailAddress,
                    Subject = "Delivery failed: " + DisplaySubject(sent.Subject),

                    Body =
                        $"Your message to {sent.RecipientAddress} " +
                        "could not be delivered because the recipient " +
                        "address does not exist.\n\n" +
                        $"Original subject: {DisplaySubject(sent.Subject)}\n\n" +
                        "Check the recipient address and try again.",

                    DateLabel = "Just now",
                    SortOrder = NextOrder(box),
                    Folder = EmailFolder.Inbox,
                    Category = EmailCategory.Primary,
                    InitiallyRead = false
                };

                box.Messages.Add(new EmailMessageData(failure));
                MailboxChanged?.Invoke(item.UserId);
            }
        }

        private static string DisplaySubject(string subject)
        {
            return string.IsNullOrWhiteSpace(subject)
                ? "(No subject)"
                : subject;
        }

        private static int NextOrder(Mailbox box)
        {
            int maximum = box.Messages.Count == 0
                ? 0
                : box.Messages.Max(message => message.SortOrder);

            return checked(maximum + 1);
        }

        private void RequireCurrentUser(string userId)
        {
            if (!string.Equals(
                    AuthenticationManager.Instance?.CurrentUser?.UserId,
                    userId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Email requires the currently signed-in user.");
            }
        }

        public EmailMailboxDefinition GetDefinition(string userId)
        {
            RequireCurrentUser(userId);

            if (!definitions.TryGetValue(userId, out var definition))
            {
                throw new InvalidOperationException(
                    $"No mailbox exists for '{userId}'.");
            }

            return definition;
        }

        private Mailbox GetMailbox(string userId)
        {
            EmailMailboxDefinition definition = GetDefinition(userId);

            if (mailboxes.TryGetValue(userId, out Mailbox existing))
                return existing;

            Mailbox box = new();

            foreach (EmailMessageDefinition message in authoredMessages[userId])
                box.Messages.Add(new EmailMessageData(message));

            if (definition.LabelNames != null)
            {
                foreach (string label in definition.LabelNames)
                {
                    if (!string.IsNullOrWhiteSpace(label) &&
                        !box.Labels.Contains(
                            label.Trim(),
                            StringComparer.OrdinalIgnoreCase))
                    {
                        box.Labels.Add(label.Trim());
                    }
                }
            }

            mailboxes.Add(userId, box);
            return box;
        }

        public IReadOnlyList<EmailMessageData> GetMessages(string userId)
        {
            return GetMailbox(userId).Messages.AsReadOnly();
        }

        public IReadOnlyList<string> GetLabels(string userId)
        {
            return GetMailbox(userId).Labels.AsReadOnly();
        }

        public void SetRead(
            string userId,
            IEnumerable<string> ids,
            bool value)
        {
            SetFlag(userId, ids, value, true);
        }

        public void SetStarred(
            string userId,
            IEnumerable<string> ids,
            bool value)
        {
            SetFlag(userId, ids, value, false);
        }

        private void SetFlag(
            string userId,
            IEnumerable<string> ids,
            bool value,
            bool read)
        {
            HashSet<string> selected = new(
                ids ?? Array.Empty<string>(),
                StringComparer.Ordinal);

            bool changed = false;

            foreach (EmailMessageData message in GetMailbox(userId).Messages)
            {
                if (!selected.Contains(message.Id))
                    continue;

                if (read)
                {
                    if (message.IsRead == value)
                        continue;

                    message.IsRead = value;
                }
                else
                {
                    if (message.IsStarred == value)
                        continue;

                    message.IsStarred = value;
                }

                changed = true;
            }

            if (changed)
                MailboxChanged?.Invoke(userId);
        }

        public void ToggleStar(string userId, string messageId)
        {
            EmailMessageData message = GetMailbox(userId).Messages
                .Find(item => item.Id == messageId);

            if (message != null)
                SetStarred(userId, new[] { messageId }, !message.IsStarred);
        }

        public void Move(
            string userId,
            IEnumerable<string> ids,
            EmailDestination destination)
        {
            HashSet<string> selected = new(
                ids ?? Array.Empty<string>(),
                StringComparer.Ordinal);

            foreach (EmailMessageData message in GetMailbox(userId).Messages)
            {
                if (!selected.Contains(message.Id))
                    continue;

                message.IsArchived = destination == EmailDestination.Archive;
                message.IsSpam = destination == EmailDestination.Spam;
                message.IsTrashed = destination == EmailDestination.Trash;
            }

            MailboxChanged?.Invoke(userId);
        }

        public bool TryCreateLabel(
            string userId,
            string name,
            out string error)
        {
            name = (name ?? string.Empty).Trim();
            Mailbox box = GetMailbox(userId);

            if (name.Length == 0 ||
                name.Length > 40 ||
                name.Any(char.IsControl))
            {
                error = "Enter a label name between 1 and 40 characters.";
                return false;
            }

            if (box.Labels.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                error = "A label with that name already exists.";
                return false;
            }

            box.Labels.Add(name);
            error = null;

            MailboxChanged?.Invoke(userId);
            return true;
        }

        public void AssignLabel(
            string userId,
            IEnumerable<string> ids,
            string label,
            bool value)
        {
            Mailbox box = GetMailbox(userId);

            string canonical = box.Labels.FirstOrDefault(name =>
                string.Equals(
                    name, label, StringComparison.OrdinalIgnoreCase));

            if (canonical == null)
                return;

            HashSet<string> selected = new(
                ids ?? Array.Empty<string>(),
                StringComparer.Ordinal);

            foreach (EmailMessageData message in box.Messages)
            {
                if (!selected.Contains(message.Id))
                    continue;

                if (value)
                    message.MutableLabels.Add(canonical);
                else
                    message.MutableLabels.Remove(canonical);
            }

            MailboxChanged?.Invoke(userId);
        }

        public EmailComposeDraft EditDraft(string userId, string id)
        {
            EmailMessageData message = GetMailbox(userId).Messages
                .Find(item =>
                    item.Id == id &&
                    item.Folder == EmailFolder.Drafts);

            if (message == null)
                return null;

            return new EmailComposeDraft
            {
                Id = message.Id,
                ThreadId = message.ThreadId,
                SourceMessageId = message.SourceMessageId,
                To = message.RecipientAddress,
                Subject = message.Subject,
                Body = message.Body,

                AttachmentIds = message.Attachments
                    .Select(attachment => attachment.FileId)
                    .ToList()
            };
        }

        public void SaveDraft(string userId, EmailComposeDraft draft)
        {
            if (draft == null)
                return;

            Mailbox box = GetMailbox(userId);

            box.Messages.RemoveAll(message =>
                message.Id == draft.Id &&
                message.Folder == EmailFolder.Drafts);

            if (string.IsNullOrWhiteSpace(draft.To) &&
                string.IsNullOrWhiteSpace(draft.Subject) &&
                string.IsNullOrWhiteSpace(draft.Body) &&
                (draft.AttachmentIds == null ||
                 draft.AttachmentIds.Count == 0))
            {
                MailboxChanged?.Invoke(userId);
                return;
            }

            if (string.IsNullOrEmpty(draft.Id))
                draft.Id = "draft-" + Guid.NewGuid().ToString("N");

            EmailMessageData messageData =
                CreateOutgoing(userId, draft, EmailFolder.Drafts, box);

            messageData.SourceMessageId = draft.SourceMessageId;
            box.Messages.Add(messageData);

            MailboxChanged?.Invoke(userId);
        }

        public void RemoveDraft(string userId, string id)
        {
            Mailbox box = GetMailbox(userId);

            box.Messages.RemoveAll(message =>
                message.Id == id &&
                message.Folder == EmailFolder.Drafts);

            MailboxChanged?.Invoke(userId);
        }

        public bool TrySend(
            string userId,
            EmailComposeDraft draft,
            out string sentId,
            out string error)
        {
            sentId = null;
            error = null;

            if (draft == null)
            {
                error = "No email draft was provided.";
                return false;
            }

            Mailbox box = GetMailbox(userId);
            string recipient = (draft.To ?? string.Empty).Trim();

            if (!IsValidRecipient(recipient))
            {
                error = "Enter one valid recipient email address.";
                return false;
            }

            draft.To = recipient;

            EmailMessageData source = box.Messages.Find(message =>
                message.Id == draft.SourceMessageId);

            if (source != null)
            {
                if (string.IsNullOrWhiteSpace(source.ThreadId))
                    source.ThreadId = "reply-" + Guid.NewGuid().ToString("N");

                draft.ThreadId = source.ThreadId;
            }

            box.Messages.RemoveAll(message =>
                message.Id == draft.Id &&
                message.Folder == EmailFolder.Drafts);

            EmailComposeDraft outgoing = new()
            {
                Id = "sent-" + Guid.NewGuid().ToString("N"),
                ThreadId = draft.ThreadId,
                To = draft.To,
                Subject = draft.Subject,
                Body = draft.Body,

                AttachmentIds = draft.AttachmentIds != null
                    ? new List<string>(draft.AttachmentIds)
                    : new List<string>()
            };

            EmailMessageData sent =
                CreateOutgoing(userId, outgoing, EmailFolder.Sent, box);

            sent.SourceMessageId = draft.SourceMessageId;

            if (source != null)
                sent.MutableLabels.UnionWith(source.UserLabels);

            box.Messages.Add(sent);
            sentId = sent.Id;

            pending.Add(new PendingFailure
            {
                UserId = userId,
                Mailbox = box,
                SentId = sent.Id,
                Due = Time.unscaledTimeAsDouble + deliveryFailureDelay
            });

            MailboxChanged?.Invoke(userId);
            return true;
        }

        private static bool IsValidRecipient(string recipient)
        {
            if (string.IsNullOrWhiteSpace(recipient))
                return false;

            try
            {
                MailAddress address = new(recipient);

                return string.Equals(
                           address.Address,
                           recipient,
                           StringComparison.OrdinalIgnoreCase) &&
                       address.Host.Contains(".");
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private EmailMessageData CreateOutgoing(
            string userId,
            EmailComposeDraft draft,
            EmailFolder folder,
            Mailbox box)
        {
            EmailMailboxDefinition account = definitions[userId];

            return new EmailMessageData(new EmailMessageDefinition
            {
                Id = draft.Id,
                ThreadId = draft.ThreadId,
                SenderName = account.DisplayName,
                SenderAddress = account.EmailAddress,
                SenderAvatar = account.ProfileImage,
                RecipientAddress = draft.To,
                Subject = draft.Subject,
                Body = draft.Body,
                DateLabel = "Just now",
                SortOrder = NextOrder(box),
                Folder = folder,
                Category = EmailCategory.Primary,
                InitiallyRead = true,

                Attachments = (draft.AttachmentIds ?? new List<string>())
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.Ordinal)
                    .Select(id => new EmailAttachmentDefinition
                    {
                        FileId = id
                    })
                    .ToList()
            });
        }

        public void ResetSession()
        {
            pending.Clear();
            mailboxes.Clear();
            MailboxChanged?.Invoke(null);
        }
    }
}