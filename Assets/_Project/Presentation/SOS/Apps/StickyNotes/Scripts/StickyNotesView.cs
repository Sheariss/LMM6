using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.SOS.StickyNotes
{
    public sealed class StickyNotesView
    {
        private const float ContextMenuWidth = 174f;
        private const float ContextMenuHeight = 86f;

        private readonly StickyNotesBinder binder;
        private readonly StickyNotesViewBuilder builder;

        private readonly VisualElement emptyState;
        private readonly VisualElement emptyAddBadge;

        private readonly Dictionary<string, StickyNotesViewBuilder.Card>
            cards = new(StringComparer.Ordinal);

        public event Action<string> SelectionRequested;
        public event Action<string> OpenRequested;

        public string ContextNoteId { get; private set; }
        public string PendingDeleteId { get; private set; }

        public StickyNotesView(
            StickyNotesBinder binder,
            StickyNotesViewBuilder builder)
        {
            this.binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.builder = builder ??
                throw new ArgumentNullException(nameof(builder));

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "StickyNotesView requires a valid binder.");
            }

            emptyState = binder.Root.Q<VisualElement>(
                "notes-empty-state");

            if (emptyState == null)
            {
                throw new InvalidOperationException(
                    "Sticky Notes UXML is missing 'notes-empty-state'.");
            }

            emptyAddBadge = emptyState.Q<VisualElement>(
                className: "sn-empty-add-badge");

            ConfigureSearch();
            ConfigureList();
            ConfigureUnavailableActions();

            emptyState.pickingMode = PickingMode.Ignore;
            binder.Empty.pickingMode = PickingMode.Ignore;
            binder.Empty.enableRichText = false;
            binder.DeleteMessage.enableRichText = false;
            binder.Status.enableRichText = false;

            HideOverlays();
        }

        private void ConfigureSearch()
        {
            binder.Search.multiline = false;
            binder.Search.isDelayed = false;

            binder.Search.textEdition.placeholder =
                "Search your notes";

            binder.Search.textEdition.hidePlaceholderOnFocus = true;

            // Retained for compatibility with the existing binder.
            // The TextField supplies the visible placeholder.
            binder.SearchHint.text = string.Empty;
            binder.SearchHint.pickingMode = PickingMode.Ignore;
            binder.SearchHint.style.display = DisplayStyle.None;
        }

        private void ConfigureList()
        {
            binder.NotesList.horizontalScrollerVisibility =
                ScrollerVisibility.Hidden;

            binder.NotesList.verticalScrollerVisibility =
                ScrollerVisibility.Auto;
        }

        private void ConfigureUnavailableActions()
        {
            binder.Settings.SetEnabled(false);
            binder.Settings.tooltip =
                "No additional Sticky Notes settings in this slice.";
        }

        public void Render(
            IReadOnlyList<StickyNoteData> notes,
            string selectedId,
            string query,
            int total,
            bool available)
        {
            if (notes == null)
                throw new ArgumentNullException(nameof(notes));

            query ??= string.Empty;

            HashSet<string> wanted = new(StringComparer.Ordinal);

            foreach (StickyNoteData note in notes)
                wanted.Add(note.id);

            RemoveUnusedCards(wanted);
            RenderCards(notes, selectedId);

            UpdateEmptyState(
                notes.Count,
                total,
                query,
                available);

            UpdateFooter(
                notes.Count,
                total,
                query);

            binder.NewNote.SetEnabled(available);
            binder.Search.SetEnabled(available);

            // Avoid resetting selection/caret when the value already matches.
            if (binder.Search.value != query)
                binder.Search.SetValueWithoutNotify(query);

            if (ContextNoteId != null &&
                !wanted.Contains(ContextNoteId))
            {
                HideContextMenu();
            }

            if (PendingDeleteId != null &&
                !wanted.Contains(PendingDeleteId))
            {
                HideDeleteDialog();
            }

            if (!available)
                HideOverlays();
        }

        private void RemoveUnusedCards(HashSet<string> wanted)
        {
            List<string> removed = new();

            foreach (var pair in cards)
            {
                if (wanted.Contains(pair.Key))
                    continue;

                pair.Value.Root.RemoveFromHierarchy();
                removed.Add(pair.Key);
            }

            foreach (string id in removed)
                cards.Remove(id);
        }

        private void RenderCards(
            IReadOnlyList<StickyNoteData> notes,
            string selectedId)
        {
            VisualElement container =
                binder.NotesList.contentContainer;

            for (int i = 0; i < notes.Count; i++)
            {
                StickyNoteData note = notes[i];

                if (!cards.TryGetValue(
                    note.id,
                    out StickyNotesViewBuilder.Card card))
                {
                    card = builder.BuildCard(
                        note.id,
                        SelectNote,
                        OpenNote,
                        ShowContextMenu);

                    cards.Add(note.id, card);
                }

                builder.UpdateCard(
                    card,
                    note,
                    note.id == selectedId);

                // Preserve existing rows, focus, and scroll position
                // whenever their order has not changed.
                if (card.Root.parent != container ||
                    container.IndexOf(card.Root) != i)
                {
                    card.Root.RemoveFromHierarchy();
                    container.Insert(i, card.Root);
                }
            }
        }

        private void UpdateEmptyState(
            int visibleCount,
            int total,
            string query,
            bool available)
        {
            bool isEmpty = visibleCount == 0;
            bool isSearching = !string.IsNullOrWhiteSpace(query);

            emptyState.style.display = isEmpty
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.Empty.style.display = DisplayStyle.Flex;

            binder.Empty.text = !available
                ? "Sign in to view your notes."
                : total == 0
                    ? "No notes yet. Use + to create one."
                    : "No notes match your search.";

            // The add badge belongs to an empty notebook,
            // rather than a search with no matches.
            if (emptyAddBadge != null)
            {
                emptyAddBadge.style.display =
                    available && total == 0 && !isSearching
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
            }
        }

        private void UpdateFooter(
            int visibleCount,
            int total,
            string query)
        {
            binder.Count.text = string.IsNullOrWhiteSpace(query)
                ? $"{total} note{(total == 1 ? "" : "s")}"
                : $"{visibleCount} of {total} notes";
        }

        private void SelectNote(string id)
        {
            SelectionRequested?.Invoke(id);
        }

        private void OpenNote(string id)
        {
            HideContextMenu();
            OpenRequested?.Invoke(id);
        }

        private void ShowContextMenu(
            string id,
            Vector2 worldPosition)
        {
            HideDeleteDialog();

            SelectionRequested?.Invoke(id);

            // Selection may refresh the view.
            if (!cards.ContainsKey(id))
                return;

            ContextNoteId = id;

            Vector2 local =
                binder.Root.WorldToLocal(worldPosition);

            float width = binder.Root.resolvedStyle.width;
            float height = binder.Root.resolvedStyle.height;

            if (float.IsNaN(width) || float.IsInfinity(width))
                width = ContextMenuWidth;

            if (float.IsNaN(height) || float.IsInfinity(height))
                height = ContextMenuHeight;

            binder.ContextMenu.style.left = Mathf.Clamp(
                local.x,
                0f,
                Mathf.Max(0f, width - ContextMenuWidth));

            binder.ContextMenu.style.top = Mathf.Clamp(
                local.y,
                0f,
                Mathf.Max(0f, height - ContextMenuHeight));

            binder.ContextMenu.style.display = DisplayStyle.Flex;
            binder.ContextMenu.BringToFront();

            binder.OpenNote.Focus();
        }

        public void HideContextMenu()
        {
            ContextNoteId = null;
            binder.ContextMenu.style.display = DisplayStyle.None;
        }

        public void ShowDeleteDialog(
            string id,
            string title)
        {
            HideContextMenu();

            PendingDeleteId = id;

            binder.DeleteMessage.text =
                $"Delete “{title}”?\n" +
                "This also closes its desktop note.";

            binder.DeleteOverlay.style.display = DisplayStyle.Flex;
            binder.DeleteOverlay.BringToFront();

            binder.CancelDelete.Focus();
        }

        public void HideDeleteDialog()
        {
            PendingDeleteId = null;
            binder.DeleteOverlay.style.display = DisplayStyle.None;
        }

        public void HideOverlays()
        {
            HideContextMenu();
            HideDeleteDialog();
        }

        public void SetStatus(string message)
        {
            binder.Status.text = message ?? string.Empty;
        }

        public void Clear()
        {
            HideOverlays();

            binder.NotesList.Clear();
            cards.Clear();

            emptyState.style.display = DisplayStyle.None;

            binder.Empty.text = string.Empty;
            binder.Count.text = string.Empty;
            binder.Status.text = string.Empty;
        }
    }
}