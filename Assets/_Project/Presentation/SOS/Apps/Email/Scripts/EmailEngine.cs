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

        private string openedMessageId;

        public string UserId { get; }
        public EmailMailboxDefinition Account { get; }

        public EmailFilter Filter { get; private set; } =
            EmailFilter.Inbox;

        public EmailCategory Category { get; private set; } =
            EmailCategory.Primary;

        public string SearchText { get; private set; } = string.Empty;
        public int PageIndex { get; private set; }

        public IReadOnlyList<EmailMessageData> Messages =>
            manager.GetMessages(UserId);

        public EmailMessageData OpenedMessage =>
            Messages.FirstOrDefault(message =>
                message.Id == openedMessageId);

        public EmailEngine(EmailManager manager, string userId)
        {
            this.manager = manager ??
                throw new ArgumentNullException(nameof(manager));

            UserId = userId;
            Account = manager.GetDefinition(userId);

            // Resolve the mailbox now so configuration errors fail
            // during app initialization.
            _ = Messages;
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
            openedMessageId = null;
            selected.Clear();
        }

        private bool MatchesFilter(EmailMessageData message)
        {
            return Filter switch
            {
                EmailFilter.Inbox =>
                    message.Folder == EmailFolder.Inbox &&
                    message.Category == Category,

                EmailFilter.Starred => message.IsStarred,
                EmailFilter.Sent =>
                    message.Folder == EmailFolder.Sent,
                EmailFilter.Drafts =>
                    message.Folder == EmailFolder.Drafts,
                EmailFilter.Work =>
                    message.Label == EmailLabel.Work,
                EmailFilter.Personal =>
                    message.Label == EmailLabel.Personal,
                _ => true
            };
        }

        private bool Contains(string text)
        {
            return text.IndexOf(
                SearchText,
                StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool MatchesSearch(EmailMessageData message)
        {
            return SearchText.Length == 0 ||
                Contains(message.SenderName) ||
                Contains(message.SenderAddress) ||
                Contains(message.RecipientAddress) ||
                Contains(message.Subject) ||
                Contains(message.Body);
        }

        public List<EmailMessageData> GetVisibleMessages(out int total)
        {
            List<EmailMessageData> filtered = Messages
                .Where(MatchesFilter)
                .Where(MatchesSearch)
                .OrderByDescending(message => message.SortOrder)
                .ThenBy(message => message.Id, StringComparer.Ordinal)
                .ToList();

            total = filtered.Count;

            int maximumPage =
                total == 0 ? 0 : (total - 1) / PageSize;

            PageIndex = Math.Max(0, Math.Min(PageIndex, maximumPage));

            List<EmailMessageData> page = filtered
                .Skip(PageIndex * PageSize)
                .Take(PageSize)
                .ToList();

            // Bulk actions apply only to the current visible page.
            selected.IntersectWith(page.Select(message => message.Id));

            return page;
        }

        public bool IsSelected(string id) => selected.Contains(id);

        public void Select(string id, bool value)
        {
            if (value)
                selected.Add(id);
            else
                selected.Remove(id);
        }

        public void SelectAll(bool value)
        {
            List<EmailMessageData> page = GetVisibleMessages(out _);

            selected.Clear();

            if (value)
            {
                foreach (EmailMessageData message in page)
                    selected.Add(message.Id);
            }
        }

        public void MovePage(int offset)
        {
            PageIndex = Math.Max(0, PageIndex + offset);
            selected.Clear();
            openedMessageId = null;
        }

        public void Open(string id)
        {
            if (!Messages.Any(message => message.Id == id))
                return;

            openedMessageId = id;
            manager.SetRead(UserId, new[] { id }, true);
        }

        public void CloseMessage()
        {
            openedMessageId = null;
        }

        public void ToggleStar(string id)
        {
            manager.ToggleStar(UserId, id);
        }

        public void MarkSelected(bool isRead)
        {
            // Normalize the current page before applying a bulk action.
            GetVisibleMessages(out _);
            manager.SetRead(UserId, selected.ToArray(), isRead);
        }
    }
}