using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.SOS.StickyNotes
{
    public sealed class StickyNotesBinder : UIBinder
    {
        // Keep Root scoped to WindowContent.
        // The view uses it for content queries and popup positioning.
        public VisualElement Root { get; }

        public VisualElement WindowRoot { get; }

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

            WindowRoot = FindWindowRoot(root);

            if (WindowRoot == null)
            {
                throw new InvalidOperationException(
                    "Sticky Notes could not find its enclosing WindowRoot.");
            }

            VisualElement header = Bind<VisualElement>(
                WindowRoot,
                "WindowHeader");

            if (header != null)
            {
                NewNote = Bind<Button>(
                    header,
                    "new-note-button");

                Settings = Bind<Button>(
                    header,
                    "settings-button");
            }

            Search = Bind<TextField>(
                Root,
                "search-field");

            SearchHint = Bind<Label>(
                Root,
                "search-placeholder");

            NotesList = Bind<ScrollView>(
                Root,
                "notes-list");

            Empty = Bind<Label>(
                Root,
                "notes-empty");

            Count = Bind<Label>(
                Root,
                "notes-count");

            Status = Bind<Label>(
                Root,
                "notes-status");

            ContextMenu = Bind<VisualElement>(
                Root,
                "note-context-menu");

            OpenNote = Bind<Button>(
                Root,
                "open-note-button");

            DeleteNote = Bind<Button>(
                Root,
                "delete-note-button");

            DeleteOverlay = Bind<VisualElement>(
                Root,
                "delete-overlay");

            DeleteMessage = Bind<Label>(
                Root,
                "delete-message");

            CancelDelete = Bind<Button>(
                Root,
                "cancel-delete-button");

            ConfirmDelete = Bind<Button>(
                Root,
                "confirm-delete-button");
        }

        private static VisualElement FindWindowRoot(
            VisualElement element)
        {
            for (VisualElement current = element;
                 current != null;
                 current = current.parent)
            {
                if (current.name == "WindowRoot")
                    return current;
            }

            return null;
        }
    }
}