using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.Presentation.SOS.Apps;
using Atlas.Presentation.SOS.Contacts;
using Atlas.SOS.Authentication;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailController : AppContentController
    {
        [Header("Attachments")]
        [SerializeField] private FileSystemLibrary fileSystemLibrary;
        [SerializeField] private FileAssociationCatalog fileAssociations;

        [Header("Identity")]
        [SerializeField] private ContactLibrary contacts;
        [SerializeField] private Sprite defaultAvatar;
        [SerializeField] private Sprite filledStar;

        private EmailManager manager;
        private EmailBinder binder;
        private EmailEngine engine;
        private EmailView view;
        private EmailViewBuilder builder;
        private EmailAttachmentResolver resolver;
        private FileOpenRouter router;

        private string userId;
        private bool executing;

        protected override void OnInitialize()
        {
            manager = EmailManager.Instance;

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "EmailManager is not available.");
            }

            userId = AuthenticationManager.Instance?.CurrentUser?.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "Email requires a signed-in user.");
            }

            if (fileSystemLibrary == null || fileAssociations == null)
            {
                throw new InvalidOperationException(
                    "Assign Email's filesystem library and file associations.");
            }

            AppController launcher = GetComponentInParent<AppController>();

            if (launcher == null)
            {
                throw new InvalidOperationException(
                    "Email must be launched by AppController.");
            }

            router = new FileOpenRouter(fileAssociations, launcher);
            resolver = new EmailAttachmentResolver(fileSystemLibrary, userId);
            engine = new EmailEngine(manager, userId, resolver);

            InstanceRoot.style.height = StyleKeyword.Auto;
            InstanceRoot.style.flexGrow = 1;
            InstanceRoot.style.flexShrink = 1;
            InstanceRoot.style.flexBasis = 0;
            InstanceRoot.style.minHeight = 0;

            binder = new EmailBinder(InstanceRoot);

            builder = new EmailViewBuilder(
                fileAssociations, contacts, defaultAvatar, filledStar);

            view = new EmailView(binder, builder);

            BindClick(
                binder.Compose,
                () => view.OpenComposer(new EmailComposeDraft()));

            BindClick(binder.Menu, view.ToggleSidebar);
            BindClick(binder.MoreFoldersButton, view.ToggleMore);
            BindClick(binder.AddLabel, view.OpenCreateLabel);

            BindClick(binder.ClearSearch, () =>
            {
                Execute(() => engine.Search(""));
                binder.SearchField.Focus();
            });

            BindCallback<ChangeEvent<string>>(
                binder.SearchField,
                evt => Execute(() => engine.Search(evt.newValue)));

            BindCallback<ChangeEvent<bool>>(
                binder.SelectAllToggle,
                evt => Execute(() => engine.SelectAll(evt.newValue)));

            BindCallback<GeometryChangedEvent>(binder.Root, evt =>
            {
                view.CloseActionsMenu(false);
                view.SetCompact(evt.newRect.width < 1000f);
            });

            foreach (var pair in binder.Navigation)
            {
                EmailFilter filter = pair.Key;

                BindClick(
                    pair.Value.Button,
                    () => Execute(() => engine.SetFilter(filter)));
            }

            foreach (var pair in binder.Categories)
            {
                EmailCategory category = pair.Key;

                BindClick(
                    pair.Value,
                    () => Execute(() => engine.SetCategory(category)));
            }

            BindClick(
                binder.Actions["previous"],
                () => Execute(() => engine.MovePage(-1)));

            BindClick(
                binder.Actions["next"],
                () => Execute(() => engine.MovePage(1)));

            BindClick(binder.Actions["refresh"], Render);

            BindClick(binder.Actions["more"], () =>
                view.ToggleActionsMenu(value =>
                    Execute(() => engine.MarkSelected(value))));

            BindAction(binder.Actions, "archive", false);
            BindAction(binder.Actions, "delete", false);
            BindAction(binder.Actions, "unread", false);
            BindAction(binder.Actions, "move", false);
            BindAction(binder.Actions, "labels", false);

            BindClick(
                view.Reader.Back,
                () => Execute(engine.CloseConversation));

            foreach (string action in view.Reader.Actions.Keys)
                BindAction(view.Reader.Actions, action, true);

            view.OpenRequested += Open;

            view.StarRequested += id =>
                Execute(() => engine.ToggleConversationStar(id));

            view.SelectionChanged += (id, value) =>
                Execute(() => engine.Select(id, value));

            view.ExpansionRequested += id =>
                Execute(() => engine.ToggleExpanded(id));

            view.MessageStarRequested += id =>
                Execute(() => engine.ToggleMessageStar(id));

            view.LabelSelected += label =>
                Execute(() => engine.SetLabel(label));

            view.ReplyRequested += id => ComposeFrom(id, false);
            view.ForwardRequested += id => ComposeFrom(id, true);

            view.MessageMenuRequested += id => view.OpenChoices(
                "Message actions",
                new (string, Action)[]
                {
                    ("Reply", () => ComposeFrom(id, false)),
                    ("Forward", () => ComposeFrom(id, true)),
                    ("Mark unread", () => Execute(() =>
                    {
                        engine.Mark(new[] { id }, false);
                        engine.CloseConversation();
                    }))
                });

            view.AttachmentRequested += OpenAttachment;
            view.SendRequested += Send;
            view.SaveDraftRequested += SaveDraft;

            view.DiscardDraftRequested += draft =>
            {
                view.CloseDialog();
                Execute(() => manager.RemoveDraft(userId, draft.Id));
            };

            view.CreateLabelRequested += name =>
            {
                if (!manager.TryCreateLabel(userId, name, out string error))
                {
                    view.SetModalError(error);
                    return;
                }

                view.CloseDialog();
                Render();
            };

            manager.MailboxChanged += OnMailboxChanged;
            Render();
        }

        private void BindAction(
            Dictionary<string, Button> buttons,
            string action,
            bool reader)
        {
            BindClick(buttons[action], () => HandleAction(action, reader));
        }

        private void HandleAction(string action, bool reader)
        {
            string[] ids = engine.TargetMessageIds(reader);

            if (ids.Length == 0)
                return;

            if (action == "move")
            {
                view.OpenChoices(
                    "Move to",
                    Enum.GetValues(typeof(EmailDestination))
                        .Cast<EmailDestination>()
                        .Select(destination =>
                            (destination.ToString(),
                             (Action)(() => Move(ids, destination, reader)))));

                return;
            }

            if (action == "labels")
            {
                EmailMessageData[] targets = engine.Messages
                    .Where(message => ids.Contains(message.Id))
                    .ToArray();

                view.OpenLabels(
                    engine.Labels,

                    label => targets.All(message =>
                        message.UserLabels.Contains(
                            label, StringComparer.OrdinalIgnoreCase)),

                    (label, value) => Execute(() =>
                        manager.AssignLabel(userId, ids, label, value)));

                return;
            }

            if (action == "unread")
            {
                Execute(() =>
                {
                    engine.Mark(ids, false);

                    if (reader)
                        engine.CloseConversation();
                });

                return;
            }

            EmailDestination destination = action switch
            {
                "archive" => EmailDestination.Archive,
                "spam" => EmailDestination.Spam,
                _ => EmailDestination.Trash
            };

            Move(ids, destination, reader);
        }

        private void Move(
            string[] ids,
            EmailDestination destination,
            bool reader)
        {
            Execute(() =>
            {
                manager.Move(userId, ids, destination);

                if (reader)
                    engine.CloseConversation();
            });

            view.ShowToast(destination == EmailDestination.Trash
                ? "Moved to Trash"
                : "Moved to " + destination);
        }

        private void Open(string id)
        {
            EmailConversationData conversation = engine.GetConversations()
                .FirstOrDefault(item => item.Id == id);

            if (conversation?.Latest.Folder == EmailFolder.Drafts)
            {
                EmailComposeDraft draft =
                    manager.EditDraft(userId, conversation.Latest.Id);

                if (draft != null)
                    view.OpenComposer(draft);

                return;
            }

            Execute(() => engine.OpenConversation(id));
        }

        private void ComposeFrom(string id, bool forward)
        {
            EmailMessageData source = engine.FindMessage(id);

            if (source == null)
                return;

            string prefix = forward ? "Fwd: " : "Re: ";

            string subject = source.Subject.StartsWith(
                prefix, StringComparison.OrdinalIgnoreCase)
                ? source.Subject
                : prefix + source.Subject;

            bool ownMessage = string.Equals(
                source.SenderAddress,
                engine.Account.EmailAddress,
                StringComparison.OrdinalIgnoreCase);

            view.OpenComposer(new EmailComposeDraft
            {
                // A forward has no reply relationship, even if its subject changes.
                SourceMessageId = forward ? null : source.Id,
                ThreadId = forward ? null : source.ThreadId,

                To = forward
                    ? ""
                    : ownMessage
                        ? source.RecipientAddress
                        : source.SenderAddress,

                Subject = subject,

                Body = forward
                    ? "---------- Forwarded message ----------\n" +
                      $"From: {source.SenderName} <{source.SenderAddress}>\n" +
                      $"To: {source.RecipientAddress}\n" +
                      $"Subject: {source.Subject}\n\n{source.Body}"
                    : "",

                AttachmentIds = forward
                    ? source.Attachments
                        .Select(attachment => attachment.FileId)
                        .ToList()
                    : new List<string>()
            });
        }

        private void Send(EmailComposeDraft draft)
        {
            bool sent = false;
            string error = null;

            Execute(() =>
            {
                sent = manager.TrySend(
                    userId, draft, out _, out error);

                if (sent)
                    view.CloseDialog();
            });

            if (!sent)
                view.SetModalError(error);
            else
                view.ShowToast("Message sent");
        }

        private void SaveDraft(EmailComposeDraft draft)
        {
            view.CloseDialog();
            Execute(() => manager.SaveDraft(userId, draft));
            view.ShowToast("Draft saved");
        }

        private void OpenAttachment(string messageId, string fileId)
        {
            if (!engine.HasAttachment(messageId, fileId))
                return;

            EmailResolvedAttachment attachment = resolver.Resolve(fileId);

            if (!attachment.IsAvailable)
            {
                view.ShowAttachmentError(attachment.Error);
                view.ShowToast(attachment.Error);
                return;
            }

            if (!router.TryOpen(attachment.Request, out string error))
            {
                view.ShowAttachmentError(error);
                view.ShowToast(error);
                return;
            }

            view.ShowAttachmentError(null);
        }

        private void Execute(Action action)
        {
            executing = true;

            try
            {
                action();
            }
            finally
            {
                executing = false;
            }

            Render();
        }

        private void OnMailboxChanged(string changedUser)
        {
            if (!executing &&
                (changedUser == null || changedUser == userId))
            {
                Render();
            }
        }

        private void Render()
        {
            if (view == null || engine == null || builder == null)
                return;

            view.Render(builder.Build(engine));
        }

        protected override void OnShutdown()
        {
            if (manager != null)
                manager.MailboxChanged -= OnMailboxChanged;

            if (view?.ActiveDraft != null &&
                manager != null &&
                AuthenticationManager.Instance?.CurrentUser?.UserId == userId)
            {
                manager.SaveDraft(userId, view.ActiveDraft);
            }

            view?.Dispose();

            view = null;
            engine = null;
            binder = null;
            builder = null;
            manager = null;
            resolver = null;
            router = null;
            userId = null;
        }
    }
}