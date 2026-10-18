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

        private readonly Dictionary<string, EmailRowElements> rows =
            new(StringComparer.Ordinal);

        private readonly Dictionary<string, EmailMessageCardElements> cards =
            new(StringComparer.Ordinal);

        private readonly Label emptyLabel;
        private readonly Label promotionsPreview;
        private readonly Label socialPreview;
        private readonly EmailActionsMenu actionsMenu;

        private EmailViewState currentState;
        private string displayedConversationId;
        private bool disposed;

        public EmailReaderElements Reader { get; }

        public event Action<string> OpenRequested;
        public event Action<string> StarRequested;
        public event Action<string, bool> SelectionChanged;
        public event Action<string> MessageExpansionRequested;
        public event Action<string> MessageStarRequested;
        public event Action<string, string> AttachmentRequested;

        public EmailView(
            EmailBinder binder,
            EmailViewBuilder builder,
            EmailMailboxDefinition account)
        {
            this.binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            if (account == null)
                throw new ArgumentNullException(nameof(account));

            binder.Root.style.position = Position.Relative;
            binder.MessageList.Clear();

            emptyLabel = EmailViewBuilder.Text(
                "No conversations match this view.",
                "mail-empty");

            promotionsPreview =
                binder.Root.Q<Label>("PromotionsPreviewLabel");

            socialPreview =
                binder.Root.Q<Label>("SocialPreviewLabel");

            Reader = builder.CreateReader();

            binder.InboxPanel.Insert(
                binder.InboxPanel.IndexOf(binder.MessageScroll) + 1,
                Reader.Root);

            actionsMenu = new EmailActionsMenu(
                binder.Root,
                binder.Button("MoreActionsButton"));

            Label allMail = binder.Navigation[EmailFilter.All]
                .Q<Label>(className: "mail-nav-label");

            if (allMail != null)
                allMail.text = "All mail";

            Button accountButton = binder.Button("AccountButton");

            accountButton.text =
                account.Initials ?? string.Empty;

            accountButton.tooltip =
                $"{account.DisplayName}\n{account.EmailAddress}";

            accountButton.SetEnabled(false);

            Disable(
                "ComposeButton",
                "Composing is not available.");

            Disable(
                "SnoozedButton",
                "Snoozing is not available.");

            Disable(
                "AddLabelButton",
                "Labels are authored.");

            Disable(
                "SearchOptionsButton",
                "Search includes messages and attachment filenames.");

            Disable(
                "SettingsButton",
                "Mail settings are not available.");

            Disable(
                "HelpButton",
                "Mail help is not available.");

            binder.Button("RefreshButton").tooltip =
                "Refresh the current view; no new mail arrives.";

            binder.StorageLabel.text = "Authored mailbox";

            binder.SyncStatus.text =
                "Changes retained for this session";
        }

        private void Disable(string name, string tooltip)
        {
            Button button = binder.Button(name);

            button.tooltip = tooltip;
            button.SetEnabled(false);
        }

        private static void Show(
            VisualElement element,
            bool visible)
        {
            if (element == null)
                return;

            element.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        public void SetSidebarVisible(bool visible)
        {
            if (disposed)
                return;

            actionsMenu.Close(false);
            Show(binder.Sidebar, visible);
        }

        public void SetCompact(bool compact)
        {
            if (disposed)
                return;

            binder.Root.EnableInClassList(
                "mail--compact",
                compact);
        }

        public void CloseActionsMenu(bool restoreFocus)
        {
            actionsMenu.Close(restoreFocus);
        }

        public void ToggleActionsMenu(Action<bool> markSelected)
        {
            if (disposed)
                return;

            if (markSelected == null)
            {
                throw new ArgumentNullException(
                    nameof(markSelected));
            }

            if (currentState == null ||
                currentState.IsReading ||
                currentState.SelectedCount == 0)
            {
                return;
            }

            actionsMenu.Toggle(
                currentState.CanMarkSelectedRead,
                currentState.CanMarkSelectedUnread,
                markSelected);
        }

        public void ShowAttachmentError(string error)
        {
            if (disposed)
                return;

            Reader.Status.text = error ?? string.Empty;

            Show(
                Reader.Status,
                !string.IsNullOrWhiteSpace(error));
        }

        public void Render(EmailViewState state)
        {
            if (disposed)
                return;

            if (state == null)
                throw new ArgumentNullException(nameof(state));

            actionsMenu.Close(false);
            currentState = state;

            foreach (var pair in binder.Navigation)
            {
                pair.Value.EnableInClassList(
                    "mail-nav--active",
                    pair.Key == state.Filter);
            }

            foreach (var pair in binder.Categories)
            {
                pair.Value.EnableInClassList(
                    "mail-tab--active",
                    pair.Key == state.Category);
            }

            binder.InboxCount.text =
                state.InboxUnread.ToString();

            binder.DraftsCount.text =
                state.DraftCount.ToString();

            binder.MessageRange.text = state.Range;

            if (binder.PrimaryBadge != null)
            {
                binder.PrimaryBadge.text =
                    $"{state.PrimaryUnread} new";

                Show(
                    binder.PrimaryBadge,
                    state.PrimaryUnread > 0);
            }

            SetPreview(
                promotionsPreview,
                state.PromotionsPreview);

            SetPreview(
                socialPreview,
                state.SocialPreview);

            binder.SelectAllToggle.SetValueWithoutNotify(
                state.AllSelected);

            binder.SelectAllToggle.SetEnabled(
                state.Rows.Count > 0);

            binder.SelectAllToggle.tooltip =
                $"Select this page ({state.SelectedCount} selected)";

            binder.Button("PreviousPageButton")
                .SetEnabled(state.CanGoBack);

            binder.Button("NextPageButton")
                .SetEnabled(state.CanGoForward);

            Button more = binder.Button("MoreActionsButton");

            more.SetEnabled(
                state.SelectedCount > 0 &&
                (state.CanMarkSelectedRead ||
                 state.CanMarkSelectedUnread));

            more.tooltip =
                $"Actions for {state.SelectedCount} selected conversations";

            Show(binder.Toolbar, !state.IsReading);

            Show(
                binder.Tabs,
                !state.IsReading &&
                state.Filter == EmailFilter.Inbox);

            Show(binder.MessageScroll, !state.IsReading);
            Show(Reader.Root, state.IsReading);

            RenderRows(state.Rows);

            if (!state.IsReading)
            {
                ClearCards();

                displayedConversationId = null;
                ShowAttachmentError(null);

                return;
            }

            Reader.Subject.text = state.ReaderSubject;

            if (displayedConversationId != state.ReaderId)
            {
                ClearCards();

                displayedConversationId = state.ReaderId;

                Reader.Scroll.scrollOffset = Vector2.zero;

                ShowAttachmentError(null);
            }

            RenderCards(state.ReaderMessages);
        }

        private static void SetPreview(
            Label label,
            string text)
        {
            if (label == null)
                return;

            label.enableRichText = false;
            label.text = text ?? string.Empty;

            Show(label, !string.IsNullOrWhiteSpace(text));
        }

        private void RenderRows(
            IReadOnlyList<EmailRowState> states)
        {
            emptyLabel.RemoveFromHierarchy();

            HashSet<string> visible = new(
                states.Select(state => state.Id),
                StringComparer.Ordinal);

            foreach (var pair in rows)
            {
                if (!visible.Contains(pair.Key))
                    pair.Value.Root.RemoveFromHierarchy();
            }

            for (int index = 0; index < states.Count; index++)
            {
                EmailRowState state = states[index];
                string id = state.Id;

                if (!rows.TryGetValue(
                        id,
                        out EmailRowElements row))
                {
                    row = new EmailRowElements(
                        () => OpenRequested?.Invoke(id),
                        () => StarRequested?.Invoke(id),
                        value => SelectionChanged?.Invoke(id, value));

                    rows.Add(id, row);
                }

                row.Render(state);

                if (row.Root.parent != binder.MessageList ||
                    binder.MessageList.IndexOf(row.Root) != index)
                {
                    row.Root.RemoveFromHierarchy();

                    binder.MessageList.Insert(
                        index,
                        row.Root);
                }
            }

            if (states.Count == 0)
                binder.MessageList.Add(emptyLabel);
        }

        private void RenderCards(
            IReadOnlyList<EmailReaderMessageState> states)
        {
            HashSet<string> visible = new(
                states.Select(state => state.Id),
                StringComparer.Ordinal);

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

                if (!cards.TryGetValue(
                        id,
                        out EmailMessageCardElements card))
                {
                    card = new EmailMessageCardElements(
                        state,
                        () => MessageExpansionRequested?.Invoke(id),
                        () => MessageStarRequested?.Invoke(id),
                        fileId =>
                            AttachmentRequested?.Invoke(id, fileId));

                    cards.Add(id, card);
                }

                card.Render(state);

                if (card.Root.parent != Reader.Messages ||
                    Reader.Messages.IndexOf(card.Root) != index)
                {
                    card.Root.RemoveFromHierarchy();

                    Reader.Messages.Insert(
                        index,
                        card.Root);
                }
            }
        }

        private void ClearCards()
        {
            foreach (EmailMessageCardElements card in cards.Values)
                card.Dispose();

            cards.Clear();
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            actionsMenu.Dispose();
            ClearCards();

            foreach (EmailRowElements row in rows.Values)
                row.Dispose();

            rows.Clear();

            emptyLabel.RemoveFromHierarchy();
            Reader.Root.RemoveFromHierarchy();

            currentState = null;

            OpenRequested = null;
            StarRequested = null;
            SelectionChanged = null;
            MessageExpansionRequested = null;
            MessageStarRequested = null;
            AttachmentRequested = null;
        }
    }
}