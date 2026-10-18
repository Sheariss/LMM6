using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.SOS.StickyNotes
{
    public sealed class StickyNotesController : AppContentController
    {
        private StickyNotesManager manager;
        private StickyNotesBinder binder;
        private StickyNotesView view;

        private StickyNotesSession session;
        private string selectedId;
        private string query = string.Empty;
        private bool refreshing;

        protected override void OnInitialize()
        {
            manager = StickyNotesManager.Instance;

            if (manager == null || !manager.isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "An active StickyNotesManager is required.");
            }

            binder = new StickyNotesBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "Sticky Notes UI binding failed.");
            }

            view = new StickyNotesView(
                binder,
                new StickyNotesViewBuilder());

            // Resolve context before subscribing to its notifications.
            session = manager.Session;

            manager.Changed += Refresh;
            view.SelectionRequested += SelectNote;
            view.OpenRequested += OpenNote;

            BindClick(binder.NewNote, CreateNote);
            BindClick(binder.OpenNote, OpenContextNote);
            BindClick(binder.DeleteNote, RequestDelete);
            BindClick(binder.CancelDelete, CancelDelete);
            BindClick(binder.ConfirmDelete, ConfirmDelete);

            BindCallback<ChangeEvent<string>>(
                binder.Search,
                OnSearchChanged);

            BindCallback<PointerDownEvent>(
                InstanceRoot,
                OnPointerDown,
                TrickleDown.TrickleDown);

            BindCallback<KeyDownEvent>(
                InstanceRoot,
                OnKeyDown,
                TrickleDown.TrickleDown);

            Refresh();
        }

        private void Refresh()
        {
            if (refreshing || manager == null || view == null)
                return;

            refreshing = true;

            try
            {
                StickyNotesSession current = manager.Session;

                if (!current.Equals(session))
                {
                    session = current;
                    selectedId = null;
                    query = string.Empty;

                    view.Clear();
                    view.SetStatus(string.Empty);
                }

                List<StickyNoteData> all = manager.GetNotes(session);
                List<StickyNoteData> visible =
                    manager.GetNotes(session, query);

                bool selectedVisible = false;

                foreach (StickyNoteData note in visible)
                {
                    if (note.id == selectedId)
                    {
                        selectedVisible = true;
                        break;
                    }
                }

                if (!selectedVisible)
                    selectedId = null;

                view.Render(
                    visible,
                    selectedId,
                    query,
                    all.Count,
                    session.IsValid);
            }
            finally
            {
                refreshing = false;
            }
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            query = evt.newValue ?? string.Empty;
            view.HideOverlays();
            Refresh();
        }

        private void SelectNote(string id)
        {
            selectedId = id;
            Refresh();
        }

        private void OpenNote(string id)
        {
            view.HideOverlays();

            if (!manager.TryOpenNote(session, id, out string error))
                view.SetStatus(error);
            else
                view.SetStatus(string.Empty);
        }

        private void OpenContextNote()
        {
            string id = view.ContextNoteId;

            if (id != null)
                OpenNote(id);
        }

        private void CreateNote()
        {
            if (!manager.TryCreateNote(
                session,
                out StickyNoteData note,
                out string error))
            {
                view.SetStatus(error);
                return;
            }

            query = string.Empty;
            selectedId = note.id;

            view.HideOverlays();
            Refresh();
            OpenNote(note.id);
        }

        private void RequestDelete()
        {
            string id = view.ContextNoteId;

            if (!manager.TryGetNote(session, id, out StickyNoteData note))
            {
                view.HideOverlays();
                view.SetStatus("This note no longer exists.");
                return;
            }

            string title = (note.text ?? string.Empty).Trim();
            int newline = title.IndexOf('\n');

            if (newline >= 0)
                title = title.Substring(0, newline);

            if (title.Length == 0)
                title = "Untitled note";
            else if (title.Length > 60)
                title = title.Substring(0, 59) + "…";

            view.ShowDeleteDialog(id, title);
        }

        private void CancelDelete()
        {
            view.HideDeleteDialog();
            binder.Search.Focus();
        }

        private void ConfirmDelete()
        {
            string id = view.PendingDeleteId;
            view.HideDeleteDialog();

            if (!manager.TryDeleteNote(session, id, out string error))
                view.SetStatus(error);
            else
                view.SetStatus(string.Empty);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            VisualElement target = evt.target as VisualElement;

            if (target != null &&
                (target == binder.ContextMenu ||
                    binder.ContextMenu.Contains(target)))
            {
                return;
            }

            view.HideContextMenu();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape)
                return;

            bool showingOverlay =
                view.ContextNoteId != null ||
                view.PendingDeleteId != null;

            if (!showingOverlay)
                return;

            view.HideOverlays();
            binder.Search.Focus();
            evt.StopPropagation();
        }

        protected override void OnShutdown()
        {
            if (manager != null)
                manager.Changed -= Refresh;

            if (view != null)
            {
                view.SelectionRequested -= SelectNote;
                view.OpenRequested -= OpenNote;
                view.Clear();
            }

            manager = null;
            binder = null;
            view = null;

            session = default;
            selectedId = null;
            query = string.Empty;
            refreshing = false;
        }
    }
}