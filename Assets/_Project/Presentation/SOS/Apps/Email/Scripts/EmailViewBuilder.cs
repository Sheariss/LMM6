using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Presentation.SOS.Contacts;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailAttachmentState
    {
        public string MessageId;
        public string FileId;
        public string Name;
        public string Size;
        public string Error;
        public bool Available;
        public Sprite Icon;
        public Texture2D Fallback;
    }

    public sealed class EmailRowState
    {
        public string Id;
        public string Sender;
        public string Subject;
        public string Preview;
        public string Date;

        public bool Read;
        public bool Starred;
        public bool Selected;

        public List<EmailAttachmentState> Attachments = new();
    }

    public sealed class EmailReaderMessageState
    {
        public string Id;
        public string Sender;
        public string Address;
        public string Recipient;
        public string Date;
        public string Preview;
        public string Body;
        public string Status;

        public bool Expanded;
        public bool Starred;

        public Sprite Avatar;
        public List<EmailAttachmentState> Attachments = new();
    }

    public sealed class EmailViewState
    {
        public string AccountInitials;
        public string AccountDescription;
        public string SearchText;
        public string SelectedLabel;
        public string Range;
        public string ReaderId;
        public string ReaderSubject;

        public EmailFilter Filter;
        public EmailCategory Category;

        public int InboxUnread;
        public int SpamUnread;
        public int TrashUnread;
        public int DraftCount;
        public int SelectedCount;

        public bool AllSelected;
        public bool CanGoBack;
        public bool CanGoForward;
        public bool CanMarkSelectedRead;
        public bool CanMarkSelectedUnread;

        public List<string> Labels = new();
        public List<EmailRowState> Rows = new();
        public List<EmailReaderMessageState> ReaderMessages = new();

        public bool IsReading => ReaderId != null;
    }

    public sealed class EmailReaderElements
    {
        public VisualElement Root;
        public Button Back;
        public Dictionary<string, Button> Actions = new();
        public ScrollView Scroll;
        public Label Subject;
        public Label Status;
        public VisualElement Messages;
    }

    public sealed class EmailRendered<T> : IDisposable
    {
        public VisualElement Root { get; }

        private readonly Action<T> render;
        private readonly List<Action> cleanup;

        public EmailRendered(
            VisualElement root,
            Action<T> render,
            List<Action> cleanup)
        {
            Root = root;
            this.render = render;
            this.cleanup = cleanup;
        }

        public void Render(T state)
        {
            render(state);
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
        private readonly FileAssociationCatalog fileAssociations;
        private readonly ContactLibrary contacts;
        private readonly Sprite defaultAvatar;
        private readonly Sprite filledStar;

        public EmailViewBuilder(
            FileAssociationCatalog fileAssociations,
            ContactLibrary contacts,
            Sprite defaultAvatar,
            Sprite filledStar)
        {
            this.fileAssociations = fileAssociations;
            this.contacts = contacts;
            this.defaultAvatar = defaultAvatar;
            this.filledStar = filledStar;
        }

        public EmailViewState Build(EmailEngine engine)
        {
            List<EmailConversationData> page =
                engine.GetVisibleConversations(out int total);

            int start = engine.PageIndex * EmailEngine.PageSize;

            EmailViewState state = new()
            {
                AccountInitials = engine.Account.Initials,

                AccountDescription =
                    $"{engine.Account.DisplayName}\n{engine.Account.EmailAddress}",

                Filter = engine.Filter,
                Category = engine.Category,
                SearchText = engine.SearchText,
                SelectedLabel = engine.SelectedLabel,

                Range = total == 0
                    ? "0 of 0"
                    : $"{start + 1}-{start + page.Count} of {total}",

                InboxUnread = engine.GetUnreadInboxCount(),
                SpamUnread = engine.GetUnreadLocationCount(EmailFilter.Spam),
                TrashUnread = engine.GetUnreadLocationCount(EmailFilter.Trash),

                DraftCount = engine.Messages.Count(message =>
                    message.Folder == EmailFolder.Drafts &&
                    !message.IsTrashed &&
                    !message.IsSpam),

                CanGoBack = engine.PageIndex > 0,
                CanGoForward = start + page.Count < total,
                Labels = engine.Labels.ToList()
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

                string participants = string.Join(", ",
                    conversation.Messages
                        .Select(message =>
                            string.Equals(
                                message.SenderAddress,
                                engine.Account.EmailAddress,
                                StringComparison.OrdinalIgnoreCase)
                                ? "me"
                                : Sender(message))
                        .Distinct(StringComparer.OrdinalIgnoreCase));

                if (conversation.Messages.Count > 1)
                    participants += $" ({conversation.Messages.Count})";

                EmailRowState row = new()
                {
                    Id = conversation.Id,

                    Sender = conversation.Latest.Folder == EmailFolder.Drafts
                        ? "Draft"
                        : participants,

                    Subject = Subject(conversation.Subject),
                    Preview = " - " + Preview(conversation.Latest.Body),
                    Date = conversation.Latest.DateLabel,
                    Read = !conversation.IsUnread,
                    Starred = conversation.IsStarred,
                    Selected = selected
                };

                HashSet<string> seen = new(StringComparer.Ordinal);

                foreach (EmailMessageData message in conversation.Messages)
                {
                    foreach (EmailAttachmentState attachment
                             in Attachments(message, engine.AttachmentResolver))
                    {
                        if (seen.Add(attachment.FileId))
                            row.Attachments.Add(attachment);
                    }
                }

                state.Rows.Add(row);
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
                state.ReaderMessages.Add(new EmailReaderMessageState
                {
                    Id = message.Id,
                    Sender = Sender(message),
                    Address = message.SenderAddress,
                    Recipient = message.RecipientAddress,
                    Date = message.DateLabel,
                    Body = message.Body,
                    Preview = Preview(message.Body),
                    Expanded = engine.IsExpanded(message.Id),
                    Starred = message.IsStarred,

                    Avatar = message.SenderAvatar != null
                        ? message.SenderAvatar
                        : FindAvatar(message.SenderAddress),

                    Status = message.DeliveryFailed
                        ? "Delivery failed"
                        : message.Folder == EmailFolder.Sent
                            ? "Sent"
                            : string.Empty,

                    Attachments = Attachments(
                        message, engine.AttachmentResolver)
                });
            }

            return state;
        }

        private Sprite FindAvatar(string address)
        {
            if (contacts != null && !string.IsNullOrWhiteSpace(address))
            {
                foreach (ContactData contact in contacts.Contacts)
                {
                    if (contact == null)
                        continue;

                    if (string.Equals(
                            contact.PersonalEmail,
                            address,
                            StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(
                            contact.WorkEmail,
                            address,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return contact.ProfileImage != null
                            ? contact.ProfileImage
                            : defaultAvatar;
                    }
                }
            }

            return defaultAvatar;
        }

        private List<EmailAttachmentState> Attachments(
            EmailMessageData message,
            EmailAttachmentResolver resolver)
        {
            List<EmailAttachmentState> result = new();

            foreach (EmailAttachmentData attachment in message.Attachments)
            {
                EmailResolvedAttachment resolved =
                    resolver.Resolve(attachment.FileId);

                FileSystemFileType type =
                    resolved.Request?.File?.FileType ??
                    FileSystemFileType.Unknown;

                result.Add(new EmailAttachmentState
                {
                    MessageId = message.Id,
                    FileId = attachment.FileId,
                    Name = resolved.Name,
                    Size = resolved.AvailableSize(),
                    Available = resolved.IsAvailable,
                    Error = resolved.Error,

                    Icon = fileAssociations != null
                        ? fileAssociations.GetIcon(type)
                        : null,

                    Fallback = Resources.Load<Texture2D>(
                        type == FileSystemFileType.PDF
                            ? "AppIcons/Pdf Viewer/icons8-pdf-48"
                            : "AppIcons/Document/icons8-document-48")
                });
            }

            return result;
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

        private static string Preview(string value)
        {
            string text = (value ?? string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ');

            return text.Length > 120
                ? text.Substring(0, 120) + "..."
                : text;
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

        public static VisualElement Box(string className)
        {
            VisualElement element = new();
            element.AddToClassList(className);
            return element;
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
            string icon,
            string buttonClass = "mail-icon-button")
        {
            Button button = new()
            {
                tooltip = tooltip
            };

            button.AddToClassList(buttonClass);
            button.Add(Icon(icon));
            return button;
        }

        public static Button TextButton(
            string text,
            string icon,
            string className)
        {
            Button button = IconButton(text, icon, className);
            button.Add(Text(text, "mail-button-text"));
            return button;
        }

        public static void Wire(
            Button button,
            Action action,
            List<Action> cleanup)
        {
            button.clicked += action;
            cleanup.Add(() => button.clicked -= action);
        }

        private void SetStar(Button button, bool value)
        {
            button.EnableInClassList("mail-star--active", value);

            Image image = button.Q<Image>();
            image.EnableInClassList("icon-star-filled", value);

            if (value && filledStar != null)
                image.style.backgroundImage = new StyleBackground(filledStar);
            else
                image.style.backgroundImage = StyleKeyword.Null;

            button.tooltip = value ? "Unstar" : "Star";
        }

        public Button AttachmentChip(
            EmailAttachmentState attachment,
            Action<string, string> open,
            List<Action> cleanup)
        {
            Button button = new()
            {
                tooltip = attachment.Available
                    ? $"{attachment.Name}\n{attachment.Size}"
                    : attachment.Error
            };

            button.AddToClassList("mail-attachment");

            Image image = new()
            {
                sprite = attachment.Icon,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };

            if (attachment.Icon == null)
                image.image = attachment.Fallback;

            image.AddToClassList("mail-file-icon");

            button.Add(image);
            button.Add(Text(attachment.Name, "mail-attachment-name"));

            if (!string.IsNullOrEmpty(attachment.Size))
                button.Add(Text(attachment.Size, "mail-attachment-size"));

            button.SetEnabled(attachment.Available);

            Wire(
                button,
                () => open(attachment.MessageId, attachment.FileId),
                cleanup);

            return button;
        }

        public EmailRendered<EmailRowState> CreateRow(
            Action open,
            Action starAction,
            Action<bool> select,
            Action<string, string> attachmentAction)
        {
            List<Action> cleanup = new();
            List<Action> chipCleanup = new();

            VisualElement root = Box("mail-row");

            Toggle selection = new();
            selection.AddToClassList("mail-check");

            Button star = IconButton("Star", "icon-star", "mail-star");

            VisualElement content = Box("mail-row-content");

            Button message = new();
            message.AddToClassList("mail-message");

            Label sender = Text("", "mail-sender");
            Label subject = Text("", "mail-subject");
            Label preview = Text("", "mail-preview");
            Label date = Text("", "mail-time");

            VisualElement summary = Box("mail-summary");
            summary.Add(subject);
            summary.Add(preview);

            message.Add(sender);
            message.Add(summary);
            message.Add(date);

            VisualElement chips = Box("mail-list-attachments");

            content.Add(message);
            content.Add(chips);

            root.Add(selection);
            root.Add(star);
            root.Add(content);

            Wire(message, open, cleanup);
            Wire(star, starAction, cleanup);

            EventCallback<ChangeEvent<bool>> changed =
                evt => select(evt.newValue);

            selection.RegisterValueChangedCallback(changed);

            cleanup.Add(() =>
                selection.UnregisterValueChangedCallback(changed));

            cleanup.Add(() =>
            {
                foreach (Action action in chipCleanup)
                    action();

                chipCleanup.Clear();
            });

            string chipSignature = null;

            return new EmailRendered<EmailRowState>(
                root,
                state =>
                {
                    sender.text = state.Sender;
                    subject.text = state.Subject;
                    preview.text = state.Preview;
                    date.text = state.Date;

                    message.tooltip = state.Subject;
                    selection.SetValueWithoutNotify(state.Selected);

                    root.EnableInClassList("mail-row--unread", !state.Read);
                    root.EnableInClassList("mail-row--selected", state.Selected);

                    root.EnableInClassList(
                        "mail-row--attachments",
                        state.Attachments.Count > 0);

                    SetStar(star, state.Starred);

                    string signature = string.Join("|",
                        state.Attachments.Select(item =>
                            item.MessageId + ":" + item.FileId + ":" +
                            item.Name + ":" + item.Size + ":" +
                            item.Available + ":" + item.Error));

                    if (signature == chipSignature)
                        return;

                    foreach (Action action in chipCleanup)
                        action();

                    chipCleanup.Clear();
                    chips.Clear();

                    foreach (EmailAttachmentState attachment in state.Attachments)
                    {
                        chips.Add(AttachmentChip(
                            attachment, attachmentAction, chipCleanup));
                    }

                    chipSignature = signature;
                },
                cleanup);
        }

        public EmailRendered<EmailReaderMessageState> CreateCard(
            EmailReaderMessageState initial,
            Action expand,
            Action starAction,
            Action reply,
            Action forward,
            Action more,
            Action<string, string> attachmentAction)
        {
            List<Action> cleanup = new();

            VisualElement root = Box("mail-thread-card");
            VisualElement header = Box("mail-thread-header");

            Image avatar = new()
            {
                scaleMode = ScaleMode.ScaleAndCrop,
                pickingMode = PickingMode.Ignore
            };

            avatar.AddToClassList("mail-avatar");
            ApplyAvatar(avatar, initial.Avatar);

            Button summary = new();
            summary.AddToClassList("mail-thread-summary");

            VisualElement identity = Box("mail-thread-identity");

            Label sender = Text(initial.Sender, "mail-thread-sender");
            Label address = Text(initial.Address, "mail-thread-address");
            Label preview = Text(initial.Preview, "mail-thread-preview");
            Label date = Text(initial.Date, "mail-thread-date");

            identity.Add(sender);
            identity.Add(address);
            identity.Add(preview);

            summary.Add(identity);
            summary.Add(date);

            Button star = IconButton("Star", "icon-star", "mail-star");
            Button replySmall = IconButton("Reply", "icon-reply");
            Button moreButton = IconButton("More message actions", "icon-more");

            header.Add(avatar);
            header.Add(summary);
            header.Add(star);
            header.Add(replySmall);
            header.Add(moreButton);

            VisualElement details = Box("mail-thread-details");

            Label recipient = Text(
                "To: " + initial.Recipient, "mail-reader-meta");

            Label status = Text(initial.Status, "mail-reader-meta");
            Label body = Text(initial.Body, "mail-reader-body");

            details.Add(recipient);
            details.Add(status);
            details.Add(body);

            VisualElement attachments = Box("mail-attachments");

            foreach (EmailAttachmentState attachment in initial.Attachments)
            {
                attachments.Add(AttachmentChip(
                    attachment, attachmentAction, cleanup));
            }

            details.Add(attachments);

            VisualElement footer = Box("mail-message-actions");

            Button replyButton = TextButton(
                "Reply", "icon-reply", "mail-pill-button");

            Button forwardButton = TextButton(
                "Forward", "icon-forward", "mail-pill-button");

            footer.Add(replyButton);
            footer.Add(forwardButton);

            details.Add(footer);
            root.Add(header);
            root.Add(details);

            Wire(summary, expand, cleanup);
            Wire(star, starAction, cleanup);
            Wire(replySmall, reply, cleanup);
            Wire(replyButton, reply, cleanup);
            Wire(forwardButton, forward, cleanup);
            Wire(moreButton, more, cleanup);

            return new EmailRendered<EmailReaderMessageState>(
                root,
                state =>
                {
                    sender.text = state.Sender;
                    address.text = state.Address;
                    preview.text = state.Preview;
                    date.text = state.Date;
                    recipient.text = "To: " + state.Recipient;
                    body.text = state.Body;
                    status.text = state.Status;

                    status.style.display = string.IsNullOrEmpty(state.Status)
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;

                    details.style.display = state.Expanded
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;

                    preview.style.display = state.Expanded
                        ? DisplayStyle.None
                        : DisplayStyle.Flex;

                    summary.tooltip = state.Expanded
                        ? "Collapse message"
                        : "Expand message";

                    ApplyAvatar(avatar, state.Avatar);
                    SetStar(star, state.Starred);
                },
                cleanup);
        }

        private static void ApplyAvatar(Image image, Sprite sprite)
        {
            if (sprite != null)
            {
                image.sprite = sprite;
                return;
            }

            image.image = Resources.Load<Texture2D>(
                "Icons/Apps/Contacts/Person/" +
                "person_32dp_FFFFFF_FILL0_wght400_GRAD0_opsz40");
        }

        public EmailReaderElements CreateReader()
        {
            EmailReaderElements reader = new()
            {
                Root = Box("mail-reader"),
                Back = IconButton("Back", "icon-previous"),
                Scroll = new ScrollView(ScrollViewMode.Vertical),
                Subject = Text("", "mail-reader-subject"),
                Status = Text("", "mail-error"),
                Messages = new VisualElement()
            };

            VisualElement toolbar = Box("mail-reader-toolbar");
            toolbar.Add(reader.Back);

            foreach (string action in new[]
                     {
                         "archive", "spam", "delete",
                         "unread", "move", "labels"
                     })
            {
                Button button = IconButton(action, "icon-" + action);

                reader.Actions.Add(action, button);
                toolbar.Add(button);
            }

            reader.Scroll.horizontalScrollerVisibility =
                ScrollerVisibility.Hidden;

            reader.Scroll.AddToClassList("mail-reader-scroll");
            reader.Scroll.Add(reader.Subject);
            reader.Scroll.Add(reader.Status);
            reader.Scroll.Add(reader.Messages);

            reader.Root.Add(toolbar);
            reader.Root.Add(reader.Scroll);

            reader.Root.style.display = DisplayStyle.None;
            reader.Status.style.display = DisplayStyle.None;

            return reader;
        }
    }

    internal static class EmailAttachmentFormatting
    {
        public static string AvailableSize(
            this EmailResolvedAttachment attachment)
        {
            if (!attachment.IsAvailable)
                return string.Empty;

            long bytes = attachment.Size;

            if (bytes < 1024)
                return $"{bytes} B";

            if (bytes < 1024L * 1024L)
                return $"{bytes / 1024d:0.#} KB";

            return $"{bytes / (1024d * 1024d):0.#} MB";
        }
    }
}