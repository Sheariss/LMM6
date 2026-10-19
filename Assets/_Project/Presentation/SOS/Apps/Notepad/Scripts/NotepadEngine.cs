using System;
using System.Collections.Generic;
using Atlas.SOS.Notepad;

namespace Atlas.Presentation.SOS.Notepad
{
    public sealed class NotepadEngine
    {
        public const int MaximumTextLength = 100000;
        private const int MaximumHistoryEntries = 100;

        private readonly List<string> undo = new();
        private readonly List<string> redo = new();

        public NotepadDocument Document { get; private set; }

        public bool ReadMode { get; private set; }
        public int ZoomPercent { get; private set; } = 100;

        public bool CanEdit =>
            Document != null &&
            !Document.IsReadOnly &&
            !ReadMode;

        public bool CanUndo => CanEdit && undo.Count > 0;
        public bool CanRedo => CanEdit && redo.Count > 0;

        public void Load(NotepadDocument document)
        {
            Document = document?.Clone() ??
                throw new ArgumentNullException(nameof(document));

            Document.Name ??= "Untitled document";
            Document.Text = Normalize(Document.Text);

            if (Document.HasBaseline)
                Document.SavedText = Normalize(Document.SavedText);

            undo.Clear();
            redo.Clear();
            ReadMode = false;
        }

        public void NewDocument()
        {
            Load(new NotepadDocument
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Untitled document",
                Text = string.Empty
            });
        }

        public bool SetText(string text)
        {
            if (!CanEdit)
                return false;

            text = Normalize(text);

            if (text.Length > MaximumTextLength ||
                string.Equals(
                    text,
                    Document.Text,
                    StringComparison.Ordinal))
            {
                return false;
            }

            Push(undo, Document.Text);
            redo.Clear();
            Document.Text = text;

            return true;
        }

        public void SetName(string name)
        {
            if (!CanEdit)
                return;

            name = (name ?? string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();

            if (name.Length > 100)
                name = name.Substring(0, 100);

            Document.Name = string.IsNullOrWhiteSpace(name)
                ? "Untitled document"
                : name;
        }

        public void Undo()
        {
            if (!CanUndo)
                return;

            Push(redo, Document.Text);
            Document.Text = Pop(undo);
        }

        public void Redo()
        {
            if (!CanRedo)
                return;

            Push(undo, Document.Text);
            Document.Text = Pop(redo);
        }

        public void Save()
        {
            if (Document == null || Document.IsReadOnly)
                return;

            Document.SavedName = Document.Name;
            Document.SavedText = Document.Text;
            Document.HasBaseline = true;
            Document.IsSessionSaved = true;
        }

        public void MakeCopy()
        {
            NotepadDocument copy = Document.Clone();

            copy.Id = Guid.NewGuid().ToString("N");
            copy.Name = copy.Name + " copy";
            copy.IsReadOnly = false;
            copy.HasBaseline = false;
            copy.IsSessionSaved = false;
            copy.SavedName = null;
            copy.SavedText = null;

            Load(copy);
        }

        public void ToggleReadMode()
        {
            ReadMode = !ReadMode;
        }

        public void SetZoom(int percentage)
        {
            ZoomPercent = Math.Clamp(percentage, 10, 500);
        }

        public int Find(string query, int start, bool backwards)
        {
            string text = Document.Text;

            if (string.IsNullOrEmpty(query) || text.Length == 0)
                return -1;

            if (backwards)
            {
                int position = start < 0
                    ? text.Length - 1
                    : Math.Min(start, text.Length - 1);

                int index = text.LastIndexOf(
                    query,
                    position,
                    StringComparison.OrdinalIgnoreCase);

                return index >= 0
                    ? index
                    : text.LastIndexOf(
                        query,
                        StringComparison.OrdinalIgnoreCase);
            }

            int forwardStart = Math.Clamp(start, 0, text.Length);

            int next = text.IndexOf(
                query,
                forwardStart,
                StringComparison.OrdinalIgnoreCase);

            return next >= 0
                ? next
                : text.IndexOf(
                    query,
                    StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string text)
        {
            return (text ?? string.Empty)
                .Replace("\r\n", "\n")
                .Replace('\r', '\n');
        }

        private static void Push(List<string> history, string text)
        {
            if (history.Count >= MaximumHistoryEntries)
                history.RemoveAt(0);

            history.Add(text);
        }

        private static string Pop(List<string> history)
        {
            int index = history.Count - 1;
            string text = history[index];

            history.RemoveAt(index);
            return text;
        }
    }
}