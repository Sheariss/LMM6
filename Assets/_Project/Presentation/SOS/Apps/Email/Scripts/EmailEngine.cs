using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailEngine
    {
        public const int PageSize = 12;

        private readonly EmailManager manager;

        private readonly HashSet<string> selected =
            new(StringComparer.Ordinal);

        private readonly HashSet<string> expanded =
            new(StringComparer.Ordinal);

        private string openedId;

        public string UserId { get; }

        public EmailMailboxDefinition Account { get; }

        public EmailAttachmentResolver AttachmentResolver { get; }

        public EmailFilter Filter { get; private set; } =
            EmailFilter.Inbox;

        public EmailCategory Category { get; private set; } =
            EmailCategory.Primary;

        public string SearchText { get; private set; } =
            string.Empty;

        public int PageIndex { get; private set; }

        public IReadOnlyList<EmailMessageData> Messages =>
            manager.GetMessages(UserId);

        public EmailConversationData OpenedConversation =>
            GetConversations().FirstOrDefault(
                conversation => conversation.Id == openedId);

        public EmailEngine(
            EmailManager manager,
            string userId,
            EmailAttachmentResolver attachmentResolver)
        {
            this.manager = manager ??
                throw new ArgumentNullException(nameof(manager));

            AttachmentResolver = attachmentResolver ??
                throw new ArgumentNullException(
                    nameof(attachmentResolver));

            UserId = userId;
            Account = manager.GetDefinition(userId);

            // Resolve the mailbox during initialization.
            _ = Messages;
        }

        public List<EmailConversationData> GetConversations()
        {
            return Messages
                .GroupBy(
                    message => message.ConversationKey,
                    StringComparer.Ordinal)
                .Select(group =>
                    new EmailConversationData(group.Key, group))
                .ToList();
        }

        public int GetUnreadInboxCount(
            EmailCategory? category = null)
        {
            return GetConversations().Count(conversation =>
                conversation.IsUnread &&
                conversation.Messages.Any(message =>
                    message.Folder == EmailFolder.Inbox &&
                    (!category.HasValue ||
                     message.Category == category.Value)));
        }

        public void SetFilter(EmailFilter filter)
        {
            Filter = filter;
            ResetNavigation();
        }

        public void SetCategory(EmailCategory category)
        {
            Category = category;
            ResetNavigation();
        }

        public void Search(string value)
        {
            SearchText = (value ?? string.Empty).Trim();
            ResetNavigation();
        }

        private void ResetNavigation()
        {
            PageIndex = 0;
            selected.Clear();
            CloseConversation();
        }

        private bool MatchesFilter(
            EmailConversationData conversation)
        {
            return Filter switch
            {
                EmailFilter.Inbox =>
                    conversation.Messages.Any(message =>
                        message.Folder == EmailFolder.Inbox &&
                        message.Category == Category),

                EmailFilter.Starred =>
                    conversation.IsStarred,

                EmailFilter.Sent =>
                    conversation.Messages.Any(message =>
                        message.Folder == EmailFolder.Sent),

                EmailFilter.Drafts =>
                    conversation.Latest.Folder ==
                    EmailFolder.Drafts,

                EmailFilter.Work =>
                    conversation.Messages.Any(message =>
                        message.Label == EmailLabel.Work),

                EmailFilter.Personal =>
                    conversation.Messages.Any(message =>
                        message.Label == EmailLabel.Personal),

                _ => true
            };
        }

        private bool Contains(string value)
        {
            return (value ?? string.Empty).IndexOf(
                SearchText,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool MatchesSearch(
            EmailConversationData conversation)
        {
            if (SearchText.Length == 0)
                return true;

            foreach (EmailMessageData message
                     in conversation.Messages)
            {
                if (Contains(message.SenderName) ||
                    Contains(message.SenderAddress) ||
                    Contains(message.RecipientAddress) ||
                    Contains(message.Subject) ||
                    Contains(message.Body))
                {
                    return true;
                }

                foreach (EmailAttachmentData attachment
                         in message.Attachments)
                {
                    EmailResolvedAttachment resolved =
                        AttachmentResolver.Resolve(
                            attachment.FileId);

                    if (Contains(resolved.Name))
                        return true;
                }
            }

            return false;
        }

        public List<EmailConversationData> GetVisibleConversations(
            out int total)
        {
            List<EmailConversationData> filtered =
                GetConversations()
                    .Where(MatchesFilter)
                    .Where(MatchesSearch)
                    .OrderByDescending(conversation =>
                        conversation.Latest.SortOrder)
                    .ThenBy(
                        conversation => conversation.Id,
                        StringComparer.Ordinal)
                    .ToList();

            total = filtered.Count;

            int maximumPage =
                total == 0 ? 0 : (total - 1) / PageSize;

            PageIndex = Math.Max(
                0,
                Math.Min(PageIndex, maximumPage));

            List<EmailConversationData> page = filtered
                .Skip(PageIndex * PageSize)
                .Take(PageSize)
                .ToList();

            // Bulk selection is restricted to this page.
            selected.IntersectWith(
                page.Select(conversation => conversation.Id));

            return page;
        }

        public bool IsSelected(string conversationId)
        {
            return selected.Contains(conversationId);
        }

        public void Select(
            string conversationId,
            bool value)
        {
            if (value)
                selected.Add(conversationId);
            else
                selected.Remove(conversationId);
        }

        public void SelectAll(bool value)
        {
            List<EmailConversationData> page =
                GetVisibleConversations(out _);

            selected.Clear();

            if (!value)
                return;

            foreach (EmailConversationData conversation in page)
                selected.Add(conversation.Id);
        }

        public void MovePage(int offset)
        {
            PageIndex = Math.Max(0, PageIndex + offset);
            selected.Clear();
            CloseConversation();
        }

        public void OpenConversation(string conversationId)
        {
            EmailConversationData conversation =
                GetConversations().FirstOrDefault(
                    item => item.Id == conversationId);

            if (conversation == null)
                return;

            openedId = conversationId;
            expanded.Clear();

            // Capture unread messages before marking them read.
            foreach (EmailMessageData message
                     in conversation.Messages)
            {
                if (message.IsReceived && !message.IsRead)
                    expanded.Add(message.Id);
            }

            // Always show the newest message expanded.
            expanded.Add(conversation.Latest.Id);

            manager.SetRead(
                UserId,
                conversation.Messages
                    .Where(message => message.IsReceived)
                    .Select(message => message.Id),
                true);
        }

        public void CloseConversation()
        {
            openedId = null;
            expanded.Clear();
        }

        public bool IsExpanded(string messageId)
        {
            return expanded.Contains(messageId);
        }

        public void ToggleExpanded(string messageId)
        {
            EmailConversationData conversation =
                OpenedConversation;

            if (conversation == null ||
                !conversation.Messages.Any(
                    message => message.Id == messageId))
            {
                return;
            }

            if (!expanded.Add(messageId))
                expanded.Remove(messageId);
        }

        public void ToggleConversationStar(
            string conversationId)
        {
            EmailConversationData conversation =
                GetConversations().FirstOrDefault(
                    item => item.Id == conversationId);

            if (conversation == null)
                return;

            if (conversation.IsStarred)
            {
                // Clear every star represented by this row.
                manager.SetStarred(
                    UserId,
                    conversation.Messages
                        .Where(message => message.IsStarred)
                        .Select(message => message.Id),
                    false);
            }
            else
            {
                // A new conversation star belongs to its latest message.
                manager.SetStarred(
                    UserId,
                    new[] { conversation.Latest.Id },
                    true);
            }
        }

        public void ToggleMessageStar(string messageId)
        {
            EmailConversationData conversation =
                OpenedConversation;

            if (conversation == null ||
                !conversation.Messages.Any(
                    message => message.Id == messageId))
            {
                return;
            }

            manager.ToggleStar(UserId, messageId);
        }

        public void MarkSelected(bool isRead)
        {
            string[] ids = GetVisibleConversations(out _)
                .Where(conversation =>
                    selected.Contains(conversation.Id))
                .SelectMany(conversation =>
                    conversation.Messages)
                .Where(message => message.IsReceived)
                .Select(message => message.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            manager.SetRead(UserId, ids, isRead);
        }

        public bool IsAttachmentInOpenedConversation(
            string messageId,
            string fileId)
        {
            EmailConversationData conversation =
                OpenedConversation;

            return conversation != null &&
                conversation.Messages.Any(message =>
                    message.Id == messageId &&
                    message.Attachments.Any(attachment =>
                        attachment.FileId == fileId));
        }
    }
}