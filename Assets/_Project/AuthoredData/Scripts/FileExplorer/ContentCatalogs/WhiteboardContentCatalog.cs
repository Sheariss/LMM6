using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.FileSystem
{
    [Serializable]
    public sealed class WhiteboardContentEntry
    {
        [SerializeField] private string contentId;
        [SerializeField]
        private WhiteboardDocument document =
            new WhiteboardDocument();

        public string ContentId => contentId;
        public WhiteboardDocument Document => document;
    }

    [CreateAssetMenu(
        fileName = "WhiteboardContentCatalog",
        menuName = "ATLAS/SOS/Content Catalogs/Whiteboard Content Catalog")]
    public sealed class WhiteboardContentCatalog : ScriptableObject
    {
        public const string StartHereId = "whiteboard.start-here";
        public const string TutorialId = "whiteboard.tutorial";

        [SerializeField]
        private List<WhiteboardContentEntry> entries =
            new List<WhiteboardContentEntry>();

        public bool TryResolve(
            string contentId,
            out WhiteboardDocument document,
            out string error)
        {
            document = null;
            error = null;

            if (string.IsNullOrWhiteSpace(contentId))
            {
                error = "The whiteboard file has no content ID.";
                return false;
            }

            WhiteboardContentEntry match = null;

            foreach (WhiteboardContentEntry entry in entries)
            {
                if (entry == null ||
                    !string.Equals(
                        entry.ContentId,
                        contentId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (match != null)
                {
                    error =
                        $"Whiteboard content ID '{contentId}' is duplicated.";
                    return false;
                }

                match = entry;
            }

            if (match != null)
            {
                if (!Validate(match.Document, out error))
                    return false;

                // Runtime edits must never modify the authored definition.
                document = match.Document.Clone();
                document.Id = Guid.NewGuid().ToString("N");
                return true;
            }

            return TryCreateBuiltIn(contentId, out document, out error);
        }

        public static bool TryCreateBuiltIn(
            string contentId,
            out WhiteboardDocument document,
            out string error)
        {
            document = null;
            error = null;

            if (contentId == StartHereId)
            {
                document = CreateStartHere();
                return true;
            }

            if (contentId == TutorialId)
            {
                document = CreateTutorial();
                return true;
            }

            error = $"Whiteboard content '{contentId}' was not found.";
            return false;
        }

        private static bool Validate(
            WhiteboardDocument document,
            out string error)
        {
            error = null;

            if (document == null || document.Items == null)
            {
                error = "The whiteboard definition is missing.";
                return false;
            }

            HashSet<string> ids = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (WhiteboardItem item in document.Items)
            {
                if (item == null ||
                    string.IsNullOrWhiteSpace(item.Id) ||
                    !ids.Add(item.Id))
                {
                    error =
                        "Whiteboard items require unique, non-empty IDs.";
                    return false;
                }

                if (item.Points == null ||
                    item.Start == null ||
                    item.End == null ||
                    !Finite(item.Position) ||
                    !Finite(item.Size) ||
                    item.Size.x < 0f ||
                    item.Size.y < 0f)
                {
                    error = $"Whiteboard item '{item.Id}' is invalid.";
                    return false;
                }

                foreach (Vector2 point in item.Points)
                {
                    if (!Finite(point))
                    {
                        error = $"Stroke '{item.Id}' has an invalid point.";
                        return false;
                    }
                }

                if (!Finite(item.Start.Position) ||
                    !Finite(item.End.Position) ||
                    !Finite(item.Start.Anchor) ||
                    !Finite(item.End.Anchor))
                {
                    error = $"Connection '{item.Id}' is invalid.";
                    return false;
                }
            }

            foreach (WhiteboardItem item in document.Items)
            {
                if (!item.IsConnection)
                    continue;

                if (!ValidEndpoint(document, item.Start) ||
                    !ValidEndpoint(document, item.End))
                {
                    error =
                        $"Connection '{item.Id}' references an invalid item.";
                    return false;
                }
            }

            if (float.IsNaN(document.GridSize) ||
                float.IsInfinity(document.GridSize) ||
                document.GridSize <= 0f)
            {
                error = "The whiteboard grid size must be positive.";
                return false;
            }

            return true;
        }

        private static bool ValidEndpoint(
            WhiteboardDocument document,
            WhiteboardEndpoint endpoint)
        {
            return string.IsNullOrEmpty(endpoint.ItemId) ||
                   document.Find(endpoint.ItemId)?.CanAttach == true;
        }

        private static bool Finite(Vector2 value)
        {
            return !float.IsNaN(value.x) &&
                   !float.IsNaN(value.y) &&
                   !float.IsInfinity(value.x) &&
                   !float.IsInfinity(value.y);
        }

        private static WhiteboardDocument CreateStartHere()
        {
            WhiteboardDocument board = new WhiteboardDocument
            {
                Title = "Start Here"
            };

            Add(board, WhiteboardItemKind.Text,
                "Your space to think",
                new Vector2(0f, 0f),
                new Vector2(720f, 80f),
                Color.clear, 36f);

            Add(board, WhiteboardItemKind.Text,
                "Collect ideas, connect them, and make a plan.",
                new Vector2(0f, 85f),
                new Vector2(720f, 70f),
                Color.clear);

            WhiteboardItem ideas = Add(
                board, WhiteboardItemKind.StickyNote,
                "IDEAS\n\nWhat would you like to explore?",
                new Vector2(0f, 200f),
                new Vector2(210f, 190f),
                new Color(1f, 0.87f, 0.48f));

            WhiteboardItem plan = Add(
                board, WhiteboardItemKind.StickyNote,
                "NEXT STEP\n\nTurn one idea into an action.",
                new Vector2(360f, 200f),
                new Vector2(210f, 190f),
                new Color(0.6f, 0.88f, 0.75f));

            board.Items.Add(new WhiteboardItem
            {
                Kind = WhiteboardItemKind.Connector,
                Color = new Color(0.65f, 0.8f, 1f),
                Start = new WhiteboardEndpoint
                {
                    ItemId = ideas.Id,
                    Anchor = new Vector2(1f, 0.5f)
                },
                End = new WhiteboardEndpoint
                {
                    ItemId = plan.Id,
                    Anchor = new Vector2(0f, 0.5f)
                }
            });

            Add(board, WhiteboardItemKind.Text,
                "Open Help for the practice board.\n" +
                "Changes stay available for this play session.",
                new Vector2(0f, 450f),
                new Vector2(720f, 100f),
                Color.clear);

            return board;
        }

        private static WhiteboardDocument CreateTutorial()
        {
            WhiteboardDocument board = new WhiteboardDocument
            {
                Title = "Whiteboard Tutorial"
            };

            Add(board, WhiteboardItemKind.Text,
                "Try the whiteboard",
                Vector2.zero,
                new Vector2(1000f, 80f),
                Color.clear, 36f);

            string[] lessons =
            {
                "1. NAVIGATE\n\nV: select\nH: hand tool\n" +
                "Hold Space or middle-drag to pan.\nScroll to zoom.",

                "2. CREATE\n\nN: sticky note\nT: text\n" +
                "Use Shapes for rectangles, ellipses, lines, and connectors.",

                "3. EDIT\n\nSelect an object.\n" +
                "Edit its text, size, or color in the toolbar.\nDrag to move.",

                "4. CONNECT\n\nChoose Connector.\n" +
                "Click one note, then another.\nMove a note: the line follows.",

                "5. DRAW AND ORGANIZE\n\nP: freehand drawing\n" +
                "F: add a frame\nUse Frames to jump to a section.",

                "6. EXPERIMENT\n\nCtrl+Z: undo\nCtrl+Y: redo\n" +
                "Delete: remove selection\nEsc: cancel or return to select."
            };

            for (int i = 0; i < lessons.Length; i++)
            {
                Add(board, WhiteboardItemKind.StickyNote,
                    lessons[i],
                    new Vector2((i % 3) * 330f, 130f + (i / 3) * 300f),
                    new Vector2(290f, 250f),
                    i % 2 == 0
                        ? new Color(1f, 0.87f, 0.48f)
                        : new Color(0.65f, 0.82f, 1f),
                    18f);
            }

            return board;
        }

        private static WhiteboardItem Add(
            WhiteboardDocument board,
            WhiteboardItemKind kind,
            string text,
            Vector2 position,
            Vector2 size,
            Color color,
            float fontSize = 20f)
        {
            WhiteboardItem item = new WhiteboardItem
            {
                Kind = kind,
                Text = text,
                Position = position,
                Size = size,
                Color = color,
                FontSize = fontSize,
                TextColor = kind == WhiteboardItemKind.Text
                    ? Color.white
                    : new Color(0.12f, 0.13f, 0.16f)
            };

            board.Items.Add(item);
            return item;
        }
    }
}