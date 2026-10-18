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
        private EmailFilter searchOrigin = EmailFilter.Inbox;
        private string searchOriginLabel;

        public string UserId { get; }
        public EmailMailboxDefinition Account { get; }
        public EmailAttachmentResolver AttachmentResolver { get; }

        public EmailFilter Filter { get; private set; } = EmailFilter.Inbox;
        public EmailCategory Category { get; private set; }
        public string SelectedLabel { get; private set; }
        public string SearchText { get; private set; } = string.Empty;
        public int PageIndex { get; private set; }

        public IReadOnlyList<EmailMessageData> Messages =>
            manager.GetMessages(UserId);

        public IReadOnlyList<string> Labels =>
            manager.GetLabels(UserId);

        public EmailConversationData OpenedConversation =>
            GetConversations().FirstOrDefault(item => item.Id == openedId);

        public EmailEngine(
            EmailManager manager,
            string userId,
            EmailAttachmentResolver resolver)
        {
            this.manager = manager;
            UserId = userId;
            Account = manager.GetDefinition(userId);
            AttachmentResolver = resolver;
        }

        public List<EmailConversationData> GetConversations()
        {
            return Messages
                .GroupBy(message => message.ConversationKey,
                    StringComparer.Ordinal)
                .Select(group =>
                    new EmailConversationData(group.Key, group))
                .ToList();
        }

        public void SetFilter(EmailFilter filter)
        {
            Filter = filter;
            SearchText = string.Empty;
            SelectedLabel = null;
            ResetNavigation();
        }

        public void SetLabel(string label)
        {
            Filter = EmailFilter.CustomLabel;
            SelectedLabel = label;
            SearchText = string.Empty;
            ResetNavigation();
        }

        public void SetCategory(EmailCategory category)
        {
            Category = category;
            Filter = EmailFilter.Inbox;
            SearchText = string.Empty;
            SelectedLabel = null;
            ResetNavigation();
        }

        public void Search(string value)
        {
            value = (value ?? string.Empty).Trim();

            if (SearchText.Length == 0 && value.Length > 0)
            {
                searchOrigin = Filter;
                searchOriginLabel = SelectedLabel;
            }

            SearchText = value;

            if (value.Length > 0)
            {
                Filter = EmailFilter.All;
                SelectedLabel = null;
            }
            else
            {
                Filter = searchOrigin;
                SelectedLabel = searchOriginLabel;
            }

            ResetNavigation();
        }

        private void ResetNavigation()
        {
            PageIndex = 0;
            selected.Clear();
            CloseConversation();
        }

        private bool MatchesFilter(EmailConversationData conversation)
        {
            bool trashed = conversation.Messages.Any(message => message.IsTrashed);
            bool spam = conversation.Messages.Any(message => message.IsSpam);

            if (Filter == EmailFilter.Trash)
                return trashed;

            if (Filter == EmailFilter.Spam)
                return spam && !trashed;

            if (trashed || spam)
                return false;

            return Filter switch
            {
                EmailFilter.Inbox => conversation.Messages.Any(message =>
                    message.InInbox && message.Category == Category),

                EmailFilter.Starred => conversation.IsStarred,

                EmailFilter.Sent => conversation.Messages.Any(message =>
                    message.Folder == EmailFolder.Sent),

                EmailFilter.Drafts =>
                    conversation.Latest.Folder == EmailFolder.Drafts,

                EmailFilter.Archive =>
                    conversation.Messages.Any(message => message.IsArchived) &&
                    !conversation.Messages.Any(message => message.InInbox),

                EmailFilter.Work => conversation.Messages.Any(message =>
                    message.UserLabels.Contains(
                        "Work", StringComparer.OrdinalIgnoreCase)),

                EmailFilter.Personal => conversation.Messages.Any(message =>
                    message.UserLabels.Contains(
                        "Personal", StringComparer.OrdinalIgnoreCase)),

                EmailFilter.CustomLabel => conversation.Messages.Any(message =>
                    message.UserLabels.Contains(
                        SelectedLabel, StringComparer.OrdinalIgnoreCase)),

                _ => true
            };
        }

        private bool Contains(string value)
        {
            return (value ?? string.Empty).IndexOf(
                SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool MatchesSearch(EmailConversationData conversation)
        {
            if (SearchText.Length == 0)
                return true;

            foreach (EmailMessageData message in conversation.Messages)
            {
                if (Contains(message.SenderName) ||
                    Contains(message.SenderAddress) ||
                    Contains(message.RecipientAddress) ||
                    Contains(message.Subject) ||
                    Contains(message.Body) ||
                    Contains(message.Category.ToString()) ||
                    message.UserLabels.Any(Contains))
                {
                    return true;
                }

                if (message.Attachments.Any(attachment =>
                        Contains(AttachmentResolver.Resolve(
                            attachment.FileId).Name)))
                {
                    return true;
                }
            }

            return false;
        }

        public List<EmailConversationData> GetVisibleConversations(out int total)
        {
            List<EmailConversationData> filtered = GetConversations()
                .Where(MatchesFilter)
                .Where(MatchesSearch)
                .OrderByDescending(item => item.Latest.SortOrder)
                .ThenBy(item => item.Id, StringComparer.Ordinal)
                .ToList();

            total = filtered.Count;

            PageIndex = Math.Max(
                0, Math.Min(PageIndex, total == 0 ? 0 : (total - 1) / PageSize));

            List<EmailConversationData> page = filtered
                .Skip(PageIndex * PageSize)
                .Take(PageSize)
                .ToList();

            selected.IntersectWith(page.Select(item => item.Id));
            return page;
        }

        public int GetUnreadInboxCount(EmailCategory? category = null)
        {
            return GetConversations().Count(conversation =>
                conversation.IsUnread &&
                conversation.Messages.Any(message =>
                    message.InInbox &&
                    (!category.HasValue ||
                     message.Category == category.Value)));
        }

        public int GetUnreadLocationCount(EmailFilter location)
        {
            return GetConversations().Count(conversation =>
                conversation.IsUnread &&
                conversation.Messages.Any(message =>
                    location == EmailFilter.Spam
                        ? message.IsSpam && !message.IsTrashed
                        : message.IsTrashed));
        }

        public bool IsSelected(string id) => selected.Contains(id);
        public bool IsExpanded(string id) => expanded.Contains(id);

        public void Select(string id, bool value)
        {
            if (value)
                selected.Add(id);
            else
                selected.Remove(id);
        }

        public void SelectAll(bool value)
        {
            List<EmailConversationData> page =
                GetVisibleConversations(out _);

            selected.Clear();

            if (value)
                selected.UnionWith(page.Select(item => item.Id));
        }

        public void MovePage(int offset)
        {
            PageIndex = Math.Max(0, PageIndex + offset);
            selected.Clear();
            CloseConversation();
        }

        public void OpenConversation(string id)
        {
            EmailConversationData conversation =
                GetConversations().FirstOrDefault(item => item.Id == id);

            if (conversation == null)
                return;

            openedId = id;
            expanded.Clear();

            expanded.UnionWith(conversation.Messages
                .Where(message => message.IsReceived && !message.IsRead)
                .Select(message => message.Id));

            expanded.Add(conversation.Latest.Id);

            manager.SetRead(
                UserId,
                conversation.Messages.Where(message => message.IsReceived)
                    .Select(message => message.Id),
                true);
        }

        public void CloseConversation()
        {
            openedId = null;
            expanded.Clear();
        }

        public void ToggleExpanded(string id)
        {
            if (OpenedConversation?.Messages.Any(message => message.Id == id)
                != true)
                return;

            if (!expanded.Add(id))
                expanded.Remove(id);
        }

        public void ToggleConversationStar(string id)
        {
            EmailConversationData conversation =
                GetConversations().FirstOrDefault(item => item.Id == id);

            if (conversation == null)
                return;

            manager.SetStarred(
                UserId,
                conversation.IsStarred
                    ? conversation.Messages.Where(message => message.IsStarred)
                        .Select(message => message.Id)
                    : new[] { conversation.Latest.Id },
                !conversation.IsStarred);
        }

        public void ToggleMessageStar(string id)
        {
            if (OpenedConversation?.Messages.Any(message => message.Id == id)
                == true)
                manager.ToggleStar(UserId, id);
        }

        public EmailMessageData FindMessage(string id)
        {
            return Messages.FirstOrDefault(message => message.Id == id);
        }

        public string[] TargetMessageIds(bool reader)
        {
            if (reader)
            {
                return OpenedConversation?.Messages
                    .Select(message => message.Id).ToArray()
                    ?? Array.Empty<string>();
            }

            return GetVisibleConversations(out _)
                .Where(item => selected.Contains(item.Id))
                .SelectMany(item => item.Messages)
                .Select(message => message.Id)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        public void MarkSelected(bool value)
        {
            Mark(TargetMessageIds(false), value);
        }

        public void Mark(IEnumerable<string> ids, bool value)
        {
            HashSet<string> requested = new(ids, StringComparer.Ordinal);

            manager.SetRead(
                UserId,
                Messages.Where(message =>
                        requested.Contains(message.Id) && message.IsReceived)
                    .Select(message => message.Id),
                value);
        }

        public bool HasAttachment(string messageId, string fileId)
        {
            return Messages.Any(message =>
                message.Id == messageId &&
                message.Attachments.Any(attachment =>
                    attachment.FileId == fileId));
        }
    }
}