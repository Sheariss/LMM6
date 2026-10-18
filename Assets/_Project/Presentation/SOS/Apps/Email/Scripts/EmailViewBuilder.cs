using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailRowState
    {
        public string Id;
        public string Sender;
        public string Subject;
        public string Preview;
        public string Date;

        public bool IsRead;
        public bool IsStarred;
        public bool IsSelected;
        public bool HasAttachments;
    }

    public sealed class EmailAttachmentState
    {
        public string FileId;
        public string Name;
        public string Size;
        public string Error;
        public bool IsAvailable;
    }

    public sealed class EmailReaderMessageState
    {
        public string Id;
        public string Sender;
        public string SenderAddress;
        public string Recipient;
        public string Date;
        public string Preview;
        public string Body;

        public bool IsExpanded;
        public bool IsStarred;

        public List<EmailAttachmentState> Attachments = new();
    }

    public sealed class EmailViewState
    {
        public List<EmailRowState> Rows = new();
        public List<EmailReaderMessageState> ReaderMessages = new();

        public EmailFilter Filter;
        public EmailCategory Category;

        public string Range;
        public string PromotionsPreview;
        public string SocialPreview;
        public string ReaderId;
        public string ReaderSubject;

        public int InboxUnread;
        public int PrimaryUnread;
        public int DraftCount;
        public int SelectedCount;

        public bool AllSelected;
        public bool CanGoBack;
        public bool CanGoForward;
        public bool CanMarkSelectedRead;
        public bool CanMarkSelectedUnread;

        public bool IsReading => !string.IsNullOrEmpty(ReaderId);
    }

    public sealed class EmailReaderElements
    {
        public VisualElement Root;
        public Button Back;
        public ScrollView Scroll;
        public Label Subject;
        public Label Status;
        public VisualElement Messages;
    }

    public sealed class EmailRowElements : IDisposable
    {
        public VisualElement Root { get; }

        private readonly Toggle selection;
        private readonly Button star;
        private readonly Button open;

        private readonly Label sender;
        private readonly Label subject;
        private readonly Label preview;
        private readonly Label date;
        private readonly Image attachmentIcon;

        private readonly Action openHandler;
        private readonly Action starHandler;
        private readonly EventCallback<ChangeEvent<bool>> selectHandler;

        public EmailRowElements(
            Action openAction,
            Action starAction,
            Action<bool> selectAction)
        {
            openHandler = openAction;
            starHandler = starAction;
            selectHandler = evt => selectAction(evt.newValue);

            Root = new VisualElement();
            Root.AddToClassList("mail-row");

            selection = new Toggle();
            selection.AddToClassList("mail-check");

            star = EmailViewBuilder.IconButton(
                "Star conversation", "icon-star", "mail-star");

            open = new Button();
            open.AddToClassList("mail-message");

            sender = EmailViewBuilder.Text("", "mail-sender");
            subject = EmailViewBuilder.Text("", "mail-subject");
            preview = EmailViewBuilder.Text("", "mail-preview");
            date = EmailViewBuilder.Text("", "mail-time");

            VisualElement summary = new()
            {
                pickingMode = PickingMode.Ignore
            };

            summary.AddToClassList("mail-summary");
            summary.Add(subject);
            summary.Add(preview);

            attachmentIcon = EmailViewBuilder.Icon("icon-draft");
            attachmentIcon.AddToClassList("mail-row-attachment");

            open.Add(sender);
            open.Add(summary);
            open.Add(attachmentIcon);
            open.Add(date);

            Root.Add(selection);
            Root.Add(star);
            Root.Add(open);

            open.clicked += openHandler;
            star.clicked += starHandler;
            selection.RegisterValueChangedCallback(selectHandler);
        }

        public void Render(EmailRowState state)
        {
            sender.text = state.Sender;
            subject.text = state.Subject;
            preview.text = state.Preview;
            date.text = state.Date;

            open.tooltip = $"{state.Sender}: {state.Subject}";

            selection.tooltip = "Select conversation";
            selection.SetValueWithoutNotify(state.IsSelected);

            star.tooltip = state.IsStarred
                ? "Remove stars from conversation"
                : "Star latest message";

            Root.EnableInClassList("mail-row--unread", !state.IsRead);
            Root.EnableInClassList("mail-row--selected", state.IsSelected);
            star.EnableInClassList("mail-star--active", state.IsStarred);

            attachmentIcon.style.display = state.HasAttachments
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void Dispose()
        {
            open.clicked -= openHandler;
            star.clicked -= starHandler;
            selection.UnregisterValueChangedCallback(selectHandler);
            Root.RemoveFromHierarchy();
        }
    }

    public sealed class EmailMessageCardElements : IDisposable
    {
        public VisualElement Root { get; }

        private readonly Button summary;
        private readonly Button star;
        private readonly Image arrow;
        private readonly Label sender;
        private readonly Label date;
        private readonly Label preview;
        private readonly VisualElement details;

        private readonly List<Action> cleanup = new();

        public EmailMessageCardElements(
            EmailReaderMessageState state,
            Action toggleExpanded,
            Action toggleStar,
            Action<string> openAttachment)
        {
            Root = new VisualElement();
            Root.AddToClassList("mail-thread-card");

            VisualElement header = new();
            header.AddToClassList("mail-thread-header");

            summary = new Button();
            summary.AddToClassList("mail-thread-summary");

            arrow = EmailViewBuilder.Icon("icon-expand");
            arrow.AddToClassList("mail-thread-chevron");

            VisualElement heading = new()
            {
                pickingMode = PickingMode.Ignore
            };
            heading.AddToClassList("mail-thread-heading");

            sender = EmailViewBuilder.Text(
                state.Sender, "mail-thread-sender");

            preview = EmailViewBuilder.Text(
                state.Preview, "mail-thread-preview");

            date = EmailViewBuilder.Text(
                state.Date, "mail-thread-date");

            heading.Add(sender);
            heading.Add(preview);

            summary.Add(arrow);
            summary.Add(heading);
            summary.Add(date);

            star = EmailViewBuilder.IconButton(
                "Star message", "icon-star", "mail-star");

            header.Add(summary);
            header.Add(star);

            details = new VisualElement();
            details.AddToClassList("mail-thread-details");

            details.Add(EmailViewBuilder.Text(
                "From: " + state.SenderAddress, "mail-reader-meta"));

            details.Add(EmailViewBuilder.Text(
                "To: " + state.Recipient, "mail-reader-meta"));

            details.Add(EmailViewBuilder.Text(
                state.Body, "mail-reader-body"));

            VisualElement attachments = new();
            attachments.AddToClassList("mail-attachments");

            foreach (EmailAttachmentState attachment in state.Attachments)
            {
                string fileId = attachment.FileId;

                Button button = new();
                button.AddToClassList("mail-attachment");

                button.tooltip = attachment.IsAvailable
                    ? $"{attachment.Name}\n{attachment.Size}"
                    : attachment.Error;

                button.Add(EmailViewBuilder.Icon("icon-draft"));

                button.Add(EmailViewBuilder.Text(
                    attachment.Name, "mail-attachment-name"));

                if (!string.IsNullOrEmpty(attachment.Size))
                {
                    button.Add(EmailViewBuilder.Text(
                        attachment.Size, "mail-attachment-size"));
                }

                button.SetEnabled(attachment.IsAvailable);

                Action clicked = () => openAttachment(fileId);
                button.clicked += clicked;
                cleanup.Add(() => button.clicked -= clicked);

                attachments.Add(button);
            }

            if (state.Attachments.Count > 0)
                details.Add(attachments);

            Root.Add(header);
            Root.Add(details);

            summary.clicked += toggleExpanded;
            star.clicked += toggleStar;

            cleanup.Add(() => summary.clicked -= toggleExpanded);
            cleanup.Add(() => star.clicked -= toggleStar);

            Render(state);
        }

        public void Render(EmailReaderMessageState state)
        {
            sender.text = state.Sender;
            date.text = state.Date;
            preview.text = state.Preview;

            details.style.display = state.IsExpanded
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            preview.style.display = state.IsExpanded
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            arrow.EnableInClassList("icon-expand", state.IsExpanded);
            arrow.EnableInClassList("icon-next", !state.IsExpanded);

            summary.tooltip = state.IsExpanded
                ? "Collapse message"
                : "Expand message";

            star.tooltip = state.IsStarred
                ? "Unstar message"
                : "Star message";

            star.EnableInClassList(
                "mail-star--active", state.IsStarred);
        }

        public void Dispose()
        {
            foreach (Action action in cleanup)
                action();

            cleanup.Clear();
            Root.RemoveFromHierarchy();
        }
    }

    public sealed class EmailViewBuilder
    {
        public EmailViewState Build(EmailEngine engine)
        {
            List<EmailConversationData> page =
                engine.GetVisibleConversations(out int total);

            int start = engine.PageIndex * EmailEngine.PageSize;

            EmailViewState state = new()
            {
                Filter = engine.Filter,
                Category = engine.Category,

                Range = total == 0
                    ? "0 of 0"
                    : $"{start + 1}-{start + page.Count} of {total}",

                InboxUnread = engine.GetUnreadInboxCount(),

                PrimaryUnread = engine.GetUnreadInboxCount(
                    EmailCategory.Primary),

                DraftCount = engine.Messages.Count(message =>
                    message.Folder == EmailFolder.Drafts),

                CanGoBack = engine.PageIndex > 0,

                CanGoForward =
                    (engine.PageIndex + 1) * EmailEngine.PageSize < total,

                PromotionsPreview = CategoryPreview(
                    engine.Messages, EmailCategory.Promotions),

                SocialPreview = CategoryPreview(
                    engine.Messages, EmailCategory.Social)
            };

            foreach (EmailConversationData conversation in page)
            {
                bool selected = engine.IsSelected(conversation.Id);

                if (selected)
                {
                    state.SelectedCount++;
                    state.CanMarkSelectedRead |= conversation.CanMarkRead;
                    state.CanMarkSelectedUnread |= conversation.CanMarkUnread;
                }

                state.Rows.Add(new EmailRowState
                {
                    Id = conversation.Id,
                    Sender = Participants(conversation, engine.Account),
                    Subject = Subject(conversation.Subject),
                    Preview = " - " + Preview(conversation.Latest.Body),
                    Date = conversation.Latest.DateLabel,
                    IsRead = !conversation.IsUnread,
                    IsStarred = conversation.IsStarred,
                    IsSelected = selected,
                    HasAttachments = conversation.HasAttachments
                });
            }

            state.AllSelected =
                page.Count > 0 && state.SelectedCount == page.Count;

            EmailConversationData opened = engine.OpenedConversation;

            if (opened == null)
                return state;

            state.ReaderId = opened.Id;
            state.ReaderSubject = Subject(opened.Subject);

            foreach (EmailMessageData message in opened.Messages)
            {
                EmailReaderMessageState messageState = new()
                {
                    Id = message.Id,
                    Sender = Sender(message),
                    SenderAddress = SenderAddress(message),

                    Recipient = string.IsNullOrWhiteSpace(
                        message.RecipientAddress)
                        ? engine.Account.EmailAddress
                        : message.RecipientAddress,

                    Date = message.DateLabel,
                    Preview = Preview(message.Body),
                    Body = message.Body,
                    IsExpanded = engine.IsExpanded(message.Id),
                    IsStarred = message.IsStarred
                };

                if (message.Folder == EmailFolder.Drafts)
                    messageState.Sender = "Draft - " + messageState.Sender;

                foreach (EmailAttachmentData attachment in message.Attachments)
                {
                    EmailResolvedAttachment resolved =
                        engine.AttachmentResolver.Resolve(attachment.FileId);

                    messageState.Attachments.Add(new EmailAttachmentState
                    {
                        FileId = attachment.FileId,
                        Name = resolved.Name,
                        Size = resolved.IsAvailable
                            ? FormatSize(resolved.Size)
                            : string.Empty,
                        Error = resolved.Error,
                        IsAvailable = resolved.IsAvailable
                    });
                }

                state.ReaderMessages.Add(messageState);
            }

            return state;
        }

        private static string Subject(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "(No subject)"
                : value;
        }

        private static string Sender(EmailMessageData message)
        {
            return string.IsNullOrWhiteSpace(message.SenderName)
                ? message.SenderAddress
                : message.SenderName;
        }

        private static string SenderAddress(EmailMessageData message)
        {
            if (string.IsNullOrWhiteSpace(message.SenderName))
                return message.SenderAddress;

            if (string.IsNullOrWhiteSpace(message.SenderAddress))
                return message.SenderName;

            return $"{message.SenderName} <{message.SenderAddress}>";
        }

        private static string Participants(
            EmailConversationData conversation,
            EmailMailboxDefinition account)
        {
            bool outgoingOnly = conversation.Messages.All(message =>
                message.Folder == EmailFolder.Sent ||
                message.Folder == EmailFolder.Drafts);

            IEnumerable<string> names = outgoingOnly
                ? conversation.Messages.Select(message =>
                    message.RecipientAddress)
                : conversation.Messages.Select(message =>
                    string.Equals(
                        message.SenderAddress,
                        account.EmailAddress,
                        StringComparison.OrdinalIgnoreCase)
                        ? "me"
                        : Sender(message));

            string result = string.Join(
                ", ",
                names.Distinct(StringComparer.OrdinalIgnoreCase));

            if (outgoingOnly)
                result = "To: " + result;

            if (conversation.Messages.Count > 1)
                result += $" ({conversation.Messages.Count})";

            return result;
        }

        private static string Preview(string body)
        {
            string result = (body ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ');

            return result.Length > 120
                ? result.Substring(0, 120) + "..."
                : result;
        }

        private static string CategoryPreview(
            IReadOnlyList<EmailMessageData> messages,
            EmailCategory category)
        {
            EmailMessageData latest = messages
                .Where(message =>
                    message.Folder == EmailFolder.Inbox &&
                    message.Category == category)
                .OrderByDescending(message => message.SortOrder)
                .ThenBy(message => message.Id, StringComparer.Ordinal)
                .FirstOrDefault();

            return latest == null
                ? string.Empty
                : $"{Sender(latest)}: {Subject(latest.Subject)}";
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";

            if (bytes < 1024L * 1024L)
                return $"{bytes / 1024d:0.#} KB";

            return $"{bytes / (1024d * 1024d):0.#} MB";
        }

        public static Label Text(string value, string className)
        {
            Label label = new(value ?? string.Empty)
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList(className);
            return label;
        }

        public static Image Icon(string className)
        {
            Image image = new()
            {
                pickingMode = PickingMode.Ignore
            };

            image.AddToClassList("mail-icon");
            image.AddToClassList(className);
            return image;
        }

        public static Button IconButton(
            string tooltip,
            string iconClass,
            string buttonClass = "mail-icon-button")
        {
            Button button = new()
            {
                tooltip = tooltip
            };

            button.AddToClassList(buttonClass);
            button.Add(Icon(iconClass));
            return button;
        }

        public EmailReaderElements CreateReader()
        {
            EmailReaderElements reader = new();

            reader.Root = new VisualElement
            {
                name = "EmailReaderPanel"
            };

            reader.Root.AddToClassList("mail-reader");
            reader.Root.style.display = DisplayStyle.None;

            VisualElement toolbar = new();
            toolbar.AddToClassList("mail-reader-toolbar");

            reader.Back = IconButton(
                "Back to conversations", "icon-previous");

            toolbar.Add(reader.Back);

            reader.Scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden
            };

            reader.Scroll.AddToClassList("mail-reader-scroll");

            reader.Subject = Text("", "mail-reader-subject");
            reader.Status = Text("", "mail-attachment-error");
            reader.Status.style.display = DisplayStyle.None;

            reader.Messages = new VisualElement();

            reader.Scroll.Add(reader.Subject);
            reader.Scroll.Add(reader.Status);
            reader.Scroll.Add(reader.Messages);

            reader.Root.Add(toolbar);
            reader.Root.Add(reader.Scroll);

            return reader;
        }
    }
}