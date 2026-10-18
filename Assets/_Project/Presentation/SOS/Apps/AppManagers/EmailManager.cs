using System;
using System.Collections.Generic;
using Atlas.SOS.Authentication;
using UnityEngine;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailManager : MonoBehaviour
    {
        public static EmailManager Instance { get; private set; }

        [SerializeField]
        private EmailLibrary emailLibrary;

        private readonly Dictionary<string, EmailMailboxDefinition>
            definitions = new(StringComparer.Ordinal);

        private readonly Dictionary<string, IReadOnlyList<EmailMessageData>>
            mailboxes = new(StringComparer.Ordinal);

        public event Action<string> MailboxChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            ValidateLibrary();
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void ValidateLibrary()
        {
            if (emailLibrary == null)
            {
                throw new InvalidOperationException(
                    "EmailManager requires an EmailLibrary.");
            }

            definitions.Clear();

            foreach (EmailMailboxDefinition mailbox
                     in emailLibrary.Mailboxes)
            {
                if (mailbox == null ||
                    string.IsNullOrWhiteSpace(mailbox.UserId))
                {
                    throw new InvalidOperationException(
                        "Every email mailbox requires a user ID.");
                }

                if (definitions.ContainsKey(mailbox.UserId))
                {
                    throw new InvalidOperationException(
                        $"Duplicate mailbox: {mailbox.UserId}");
                }

                if (mailbox.Messages == null)
                {
                    throw new InvalidOperationException(
                        $"Mailbox '{mailbox.UserId}' has no message list.");
                }

                HashSet<string> ids = new(StringComparer.Ordinal);

                foreach (EmailMessageDefinition message
                         in mailbox.Messages)
                {
                    if (message == null ||
                        string.IsNullOrWhiteSpace(message.Id) ||
                        !ids.Add(message.Id))
                    {
                        throw new InvalidOperationException(
                            $"Mailbox '{mailbox.UserId}' contains " +
                            "a missing or duplicate message ID.");
                    }

                    if (message.Attachments == null)
                        continue;

                    HashSet<string> attachmentIds =
                        new(StringComparer.Ordinal);

                    foreach (EmailAttachmentDefinition attachment
                             in message.Attachments)
                    {
                        if (attachment == null ||
                            string.IsNullOrWhiteSpace(attachment.FileId) ||
                            !attachmentIds.Add(attachment.FileId))
                        {
                            throw new InvalidOperationException(
                                $"Email '{message.Id}' contains " +
                                "a missing or duplicate attachment ID.");
                        }
                    }
                }

                definitions.Add(mailbox.UserId, mailbox);
            }
        }

        private void RequireCurrentUser(string userId)
        {
            string currentId =
                AuthenticationManager.Instance?.CurrentUser?.UserId;

            if (string.IsNullOrWhiteSpace(userId) ||
                !string.Equals(
                    currentId,
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
                    $"No mailbox is authored for '{userId}'.");
            }

            return definition;
        }

        public IReadOnlyList<EmailMessageData> GetMessages(string userId)
        {
            RequireCurrentUser(userId);

            if (mailboxes.TryGetValue(userId, out var existing))
                return existing;

            List<EmailMessageData> messages = new();

            foreach (EmailMessageDefinition definition
                     in GetDefinition(userId).Messages)
            {
                messages.Add(new EmailMessageData(definition));
            }

            IReadOnlyList<EmailMessageData> result =
                messages.AsReadOnly();

            mailboxes.Add(userId, result);
            return result;
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
            IEnumerable<string> messageIds,
            bool value,
            bool readFlag)
        {
            if (messageIds == null)
                throw new ArgumentNullException(nameof(messageIds));

            HashSet<string> ids =
                new(messageIds, StringComparer.Ordinal);

            bool changed = false;

            foreach (EmailMessageData message in GetMessages(userId))
            {
                if (!ids.Contains(message.Id))
                    continue;

                if (readFlag)
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
            foreach (EmailMessageData message in GetMessages(userId))
            {
                if (message.Id != messageId)
                    continue;

                SetStarred(
                    userId,
                    new[] { messageId },
                    !message.IsStarred);

                return;
            }
        }

        public void ResetSession()
        {
            mailboxes.Clear();
            MailboxChanged?.Invoke(null);
        }
    }
}