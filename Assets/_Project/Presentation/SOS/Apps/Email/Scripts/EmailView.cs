using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailView : IDisposable
    {
        private readonly EmailBinder binder;
        private readonly EmailViewBuilder builder;
        private readonly EmailActionsMenu actionsMenu;

        private readonly Dictionary<string, EmailRendered<EmailRowState>> rows =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string,
            EmailRendered<EmailReaderMessageState>> cards =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string, EmailNavigationElements> labels =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<Action> cleanup = new();
        private readonly List<Action> modalCleanup = new();

        private readonly Label empty;
        private readonly Label toast;

        private VisualElement modal;
        private Label modalError;
        private TextField composeTo;
        private TextField composeSubject;
        private TextField composeBody;

        private EmailComposeDraft draft;
        private EmailViewState current;
        private string readerId;

        private bool rail;
        private bool moreOpen;
        private bool disposed;

        private IVisualElementScheduledItem toastTimer;

        public EmailReaderElements Reader { get; }
        public EmailComposeDraft ActiveDraft => draft;

        public event Action<string> OpenRequested;
        public event Action<string> StarRequested;
        public event Action<string, bool> SelectionChanged;
        public event Action<string> ExpansionRequested;
        public event Action<string> MessageStarRequested;
        public event Action<string> ReplyRequested;
        public event Action<string> ForwardRequested;
        public event Action<string> MessageMenuRequested;
        public event Action<string, string> AttachmentRequested;
        public event Action<string> LabelSelected;

        public event Action<EmailComposeDraft> SendRequested;
        public event Action<EmailComposeDraft> SaveDraftRequested;
        public event Action<EmailComposeDraft> DiscardDraftRequested;
        public event Action<string> CreateLabelRequested;

        public EmailView(EmailBinder binder, EmailViewBuilder builder)
        {
            this.binder = binder;
            this.builder = builder;

            binder.MessageList.Clear();

            empty = EmailViewBuilder.Text(
                "No conversations match this view.", "mail-empty");

            Reader = builder.CreateReader();
            binder.InboxPanel.Add(Reader.Root);

            toast = EmailViewBuilder.Text("", "mail-toast");
            toast.style.display = DisplayStyle.None;
            binder.Root.Add(toast);

            actionsMenu = new EmailActionsMenu(
                binder.Root, binder.Actions["more"]);

            binder.SearchField.textEdition.placeholder = "Search mail";
            binder.SearchField.textEdition.hidePlaceholderOnFocus = true;

            SetMore(false);
        }

        private static void Show(VisualElement element, bool visible)
        {
            element.style.display =
                visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ToggleSidebar()
        {
            rail = !rail;
            binder.Root.EnableInClassList("mail--rail", rail);
            actionsMenu.Close(false);
        }

        public void SetCompact(bool value)
        {
            binder.Root.EnableInClassList("mail--compact", value);
        }

        public void ToggleMore() => SetMore(!moreOpen);

        private void SetMore(bool value)
        {
            moreOpen = value;
            binder.Root.EnableInClassList("mail--more-open", value);
            binder.MoreText.text = value ? "Less" : "More";
        }

        public void CloseActionsMenu(bool focus)
        {
            actionsMenu.Close(focus);
        }

        public void ToggleActionsMenu(Action<bool> action)
        {
            if (current == null)
                return;

            actionsMenu.Toggle(
                current.CanMarkSelectedRead,
                current.CanMarkSelectedUnread,
                action);
        }

        public void ShowToast(string text)
        {
            toastTimer?.Pause();

            toast.text = text;
            Show(toast, true);

            toastTimer = binder.Root.schedule.Execute(() =>
            {
                if (!disposed)
                    Show(toast, false);
            }).StartingIn(2500);
        }

        public void ShowAttachmentError(string error)
        {
            Reader.Status.text = error ?? string.Empty;
            Show(Reader.Status, !string.IsNullOrWhiteSpace(error));
        }

        public void Render(EmailViewState state)
        {
            if (disposed)
                return;

            current = state;
            actionsMenu.Close(false);

            binder.SearchField.SetValueWithoutNotify(state.SearchText);
            Show(binder.ClearSearch, state.SearchText.Length > 0);

            Button account = binder.Root.Q<Button>("AccountButton");
            account.text = state.AccountInitials;
            account.tooltip = state.AccountDescription;

            foreach (var pair in binder.Navigation)
            {
                bool active = pair.Key == state.Filter;

                pair.Value.Button.EnableInClassList(
                    "mail-nav--active", active);

                int count = pair.Key switch
                {
                    EmailFilter.Inbox => state.InboxUnread,
                    EmailFilter.Drafts => state.DraftCount,
                    EmailFilter.Spam => state.SpamUnread,
                    EmailFilter.Trash => state.TrashUnread,
                    _ => 0
                };

                pair.Value.Count.text = count > 0 ? count.ToString() : "";

                pair.Value.Button.EnableInClassList(
                    "has-unread", count > 0);
            }

            foreach (var pair in binder.Categories)
            {
                pair.Value.EnableInClassList(
                    "mail-tab--active", pair.Key == state.Category);
            }

            RenderLabels(state);

            binder.SelectAllToggle.SetValueWithoutNotify(state.AllSelected);
            binder.SelectAllToggle.SetEnabled(state.Rows.Count > 0);

            binder.Range.text = state.Range;

            binder.Actions["previous"].SetEnabled(state.CanGoBack);
            binder.Actions["next"].SetEnabled(state.CanGoForward);

            foreach (string action in new[]
                     {
                         "archive", "delete", "unread", "move", "labels", "more"
                     })
            {
                binder.Actions[action].SetEnabled(state.SelectedCount > 0);
            }

            Show(binder.Toolbar, !state.IsReading);
            Show(binder.Tabs,
                !state.IsReading && state.Filter == EmailFilter.Inbox);

            Show(binder.MessageScroll, !state.IsReading);
            Show(Reader.Root, state.IsReading);

            RenderRows(state.Rows);

            if (!state.IsReading)
            {
                ClearCards();
                readerId = null;
                ShowAttachmentError(null);
                return;
            }

            Reader.Subject.text = state.ReaderSubject;

            if (readerId != state.ReaderId)
            {
                ClearCards();
                readerId = state.ReaderId;
                Reader.Scroll.scrollOffset = Vector2.zero;
                ShowAttachmentError(null);
            }

            RenderCards(state.ReaderMessages);
        }

        private void RenderLabels(EmailViewState state)
        {
            foreach (string name in state.Labels.Where(name =>
                         !string.Equals(name, "Work",
                             StringComparison.OrdinalIgnoreCase) &&
                         !string.Equals(name, "Personal",
                             StringComparison.OrdinalIgnoreCase)))
            {
                if (!labels.TryGetValue(name, out var row))
                {
                    string captured = name;

                    row = EmailBinder.CreateNavigation(name, "icon-label");
                    labels.Add(name, row);
                    binder.CustomLabels.Add(row.Button);

                    EmailViewBuilder.Wire(
                        row.Button,
                        () => LabelSelected?.Invoke(captured),
                        cleanup);
                }

                row.Button.EnableInClassList(
                    "mail-nav--active",
                    state.Filter == EmailFilter.CustomLabel &&
                    string.Equals(
                        state.SelectedLabel,
                        name,
                        StringComparison.OrdinalIgnoreCase));
            }
        }

        private void RenderRows(List<EmailRowState> states)
        {
            empty.RemoveFromHierarchy();

            HashSet<string> visible =
                new(states.Select(state => state.Id), StringComparer.Ordinal);

            foreach (var pair in rows)
            {
                if (!visible.Contains(pair.Key))
                    pair.Value.Root.RemoveFromHierarchy();
            }

            for (int index = 0; index < states.Count; index++)
            {
                EmailRowState state = states[index];
                string id = state.Id;

                if (!rows.TryGetValue(id, out var row))
                {
                    row = builder.CreateRow(
                        () => OpenRequested?.Invoke(id),
                        () => StarRequested?.Invoke(id),
                        value => SelectionChanged?.Invoke(id, value),
                        (messageId, fileId) =>
                            AttachmentRequested?.Invoke(messageId, fileId));

                    rows.Add(id, row);
                }

                row.Render(state);
                Place(binder.MessageList, row.Root, index);
            }

            if (states.Count == 0)
                binder.MessageList.Add(empty);
        }

        private void RenderCards(List<EmailReaderMessageState> states)
        {
            HashSet<string> visible =
                new(states.Select(state => state.Id), StringComparer.Ordinal);

            foreach (string id in cards.Keys.ToArray())
            {
                if (visible.Contains(id))
                    continue;

                cards[id].Dispose();
                cards.Remove(id);
            }

            for (int index = 0; index < states.Count; index++)
            {
                EmailReaderMessageState state = states[index];
                string id = state.Id;

                if (!cards.TryGetValue(id, out var card))
                {
                    card = builder.CreateCard(
                        state,
                        () => ExpansionRequested?.Invoke(id),
                        () => MessageStarRequested?.Invoke(id),
                        () => ReplyRequested?.Invoke(id),
                        () => ForwardRequested?.Invoke(id),
                        () => MessageMenuRequested?.Invoke(id),
                        (messageId, fileId) =>
                            AttachmentRequested?.Invoke(messageId, fileId));

                    cards.Add(id, card);
                }

                card.Render(state);
                Place(Reader.Messages, card.Root, index);
            }
        }

        private static void Place(
            VisualElement parent,
            VisualElement child,
            int index)
        {
            if (child.parent == parent && parent.IndexOf(child) == index)
                return;

            child.RemoveFromHierarchy();
            parent.Insert(index, child);
        }

        private void ClearCards()
        {
            foreach (var card in cards.Values)
                card.Dispose();

            cards.Clear();
        }

        private VisualElement OpenDialog(string title, bool composer = false)
        {
            CloseDialog();

            modal = EmailViewBuilder.Box("mail-modal");
            VisualElement panel = EmailViewBuilder.Box(
                composer ? "mail-dialog mail-composer" : "mail-dialog");

            // Box accepts one class name, so add these separately.
            panel.ClearClassList();
            panel.AddToClassList("mail-dialog");

            if (composer)
                panel.AddToClassList("mail-composer");

            VisualElement header = EmailViewBuilder.Box("mail-dialog-header");
            header.Add(EmailViewBuilder.Text(title, "mail-dialog-title"));

            Button close = EmailViewBuilder.IconButton("Close", "icon-close");

            EmailViewBuilder.Wire(close, () =>
            {
                if (draft != null)
                    SaveDraftRequested?.Invoke(draft);
                else
                    CloseDialog();
            }, modalCleanup);

            header.Add(close);
            panel.Add(header);

            modalError = EmailViewBuilder.Text("", "mail-error");
            modalError.style.display = DisplayStyle.None;
            panel.Add(modalError);

            modal.Add(panel);
            binder.Root.Add(modal);
            modal.BringToFront();

            return panel;
        }

        public void SetModalError(string error)
        {
            if (modalError == null)
                return;

            modalError.text = error ?? string.Empty;
            Show(modalError, !string.IsNullOrWhiteSpace(error));
        }

        public void CloseDialog()
        {
            foreach (Action action in modalCleanup)
                action();

            modalCleanup.Clear();
            modal?.RemoveFromHierarchy();

            modal = null;
            modalError = null;
            composeTo = null;
            composeSubject = null;
            composeBody = null;
            draft = null;
        }

        public void OpenComposer(EmailComposeDraft value)
        {
            VisualElement panel = OpenDialog("New message", true);
            draft = value;

            composeTo = new TextField("To")
            {
                value = draft.To,
                maxLength = 254
            };

            composeSubject = new TextField("Subject")
            {
                value = draft.Subject,
                maxLength = 200
            };

            composeBody = new TextField
            {
                value = draft.Body,
                multiline = true,
                maxLength = 16000
            };

            composeTo.AddToClassList("mail-field");
            composeSubject.AddToClassList("mail-field");
            composeBody.AddToClassList("mail-compose-body");

            composeTo.textEdition.placeholder = "Recipient email address";
            composeTo.textEdition.hidePlaceholderOnFocus = true;

            BindDraftField(composeTo, text => draft.To = text);
            BindDraftField(composeSubject, text => draft.Subject = text);
            BindDraftField(composeBody, text => draft.Body = text);

            panel.Add(composeTo);
            panel.Add(composeSubject);
            panel.Add(composeBody);

            if (draft.AttachmentIds.Count > 0)
            {
                panel.Add(EmailViewBuilder.Text(
                    $"{draft.AttachmentIds.Count} forwarded attachment(s)",
                    "mail-reader-meta"));
            }

            VisualElement footer = EmailViewBuilder.Box("mail-dialog-footer");

            Button send = EmailViewBuilder.TextButton(
                "Send", "icon-sent", "mail-primary-button");

            Button save = new() { text = "Save and close" };
            save.AddToClassList("mail-pill-button");

            Button discard = EmailViewBuilder.IconButton(
                "Discard draft", "icon-delete");

            EmailViewBuilder.Wire(send,
                () => SendRequested?.Invoke(draft), modalCleanup);

            EmailViewBuilder.Wire(save,
                () => SaveDraftRequested?.Invoke(draft), modalCleanup);

            EmailViewBuilder.Wire(discard,
                () => DiscardDraftRequested?.Invoke(draft), modalCleanup);

            footer.Add(send);
            footer.Add(save);
            footer.Add(discard);

            panel.Add(footer);
            composeTo.Focus();
        }

        private void BindDraftField(
            TextField field,
            Action<string> changed)
        {
            EventCallback<ChangeEvent<string>> callback =
                evt => changed(evt.newValue);

            field.RegisterValueChangedCallback(callback);

            modalCleanup.Add(() =>
                field.UnregisterValueChangedCallback(callback));
        }

        public void OpenCreateLabel()
        {
            VisualElement panel = OpenDialog("New label");

            TextField name = new("Label name")
            {
                maxLength = 40
            };

            name.AddToClassList("mail-field");
            panel.Add(name);

            VisualElement footer = EmailViewBuilder.Box("mail-dialog-footer");

            Button cancel = new() { text = "Cancel" };
            cancel.AddToClassList("mail-pill-button");

            Button create = new() { text = "Create" };
            create.AddToClassList("mail-primary-button");

            EmailViewBuilder.Wire(cancel, CloseDialog, modalCleanup);

            EmailViewBuilder.Wire(create,
                () => CreateLabelRequested?.Invoke(name.value),
                modalCleanup);

            footer.Add(cancel);
            footer.Add(create);
            panel.Add(footer);

            name.Focus();
        }

        public void OpenChoices(
            string title,
            IEnumerable<(string Text, Action Action)> choices)
        {
            VisualElement panel = OpenDialog(title);

            foreach (var choice in choices)
            {
                Button button = new() { text = choice.Text };
                button.AddToClassList("mail-choice");

                Action action = choice.Action;

                EmailViewBuilder.Wire(button, () =>
                {
                    CloseDialog();
                    action();
                }, modalCleanup);

                panel.Add(button);
            }
        }

        public void OpenLabels(
            IEnumerable<string> names,
            Func<string, bool> allHave,
            Action<string, bool> assign)
        {
            VisualElement panel = OpenDialog("Labels");

            foreach (string name in names)
            {
                string label = name;

                Toggle toggle = new(label)
                {
                    value = allHave(label)
                };

                toggle.labelElement.enableRichText = false;
                toggle.AddToClassList("mail-label-toggle");

                EventCallback<ChangeEvent<bool>> changed =
                    evt => assign(label, evt.newValue);

                toggle.RegisterValueChangedCallback(changed);

                modalCleanup.Add(() =>
                    toggle.UnregisterValueChangedCallback(changed));

                panel.Add(toggle);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            toastTimer?.Pause();

            CloseDialog();
            actionsMenu.Dispose();
            ClearCards();

            foreach (var row in rows.Values)
                row.Dispose();

            foreach (Action action in cleanup)
                action();

            rows.Clear();
            cleanup.Clear();

            Reader.Root.RemoveFromHierarchy();
            toast.RemoveFromHierarchy();

            OpenRequested = null;
            StarRequested = null;
            SelectionChanged = null;
            ExpansionRequested = null;
            MessageStarRequested = null;
            ReplyRequested = null;
            ForwardRequested = null;
            MessageMenuRequested = null;
            AttachmentRequested = null;
            LabelSelected = null;
            SendRequested = null;
            SaveDraftRequested = null;
            DiscardDraftRequested = null;
            CreateLabelRequested = null;
        }
    }
}