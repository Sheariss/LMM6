using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.SOS.StickyNotes
{
    public sealed class StickyNotesBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Button NewNote { get; }
        public Button Settings { get; }

        public TextField Search { get; }
        public Label SearchHint { get; }

        public ScrollView NotesList { get; }
        public Label Empty { get; }
        public Label Count { get; }
        public Label Status { get; }

        public VisualElement ContextMenu { get; }
        public Button OpenNote { get; }
        public Button DeleteNote { get; }

        public VisualElement DeleteOverlay { get; }
        public Label DeleteMessage { get; }
        public Button CancelDelete { get; }
        public Button ConfirmDelete { get; }

        public StickyNotesBinder(VisualElement root)
        {
            Root = root ??
                throw new ArgumentNullException(nameof(root));

            NewNote = Bind<Button>(root, "new-note-button");
            Settings = Bind<Button>(root, "settings-button");

            Search = Bind<TextField>(root, "search-field");
            SearchHint = Bind<Label>(root, "search-placeholder");

            NotesList = Bind<ScrollView>(root, "notes-list");
            Empty = Bind<Label>(root, "notes-empty");
            Count = Bind<Label>(root, "notes-count");
            Status = Bind<Label>(root, "notes-status");

            ContextMenu = Bind<VisualElement>(root, "note-context-menu");
            OpenNote = Bind<Button>(root, "open-note-button");
            DeleteNote = Bind<Button>(root, "delete-note-button");

            DeleteOverlay = Bind<VisualElement>(root, "delete-overlay");
            DeleteMessage = Bind<Label>(root, "delete-message");
            CancelDelete = Bind<Button>(root, "cancel-delete-button");
            ConfirmDelete = Bind<Button>(root, "confirm-delete-button");
        }
    }
}