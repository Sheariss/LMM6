using System;
using System.Collections.Generic;

namespace Atlas.SOS.StickyNotes
{
    public sealed class StickyNotesEngine
    {
        private readonly StickyNotesUserData data;

        public StickyNotesEngine(
            StickyNotesUserData data,
            StickyNotesLibrary library)
        {
            this.data = data ??
                throw new ArgumentNullException(nameof(data));

            if (library == null)
                throw new ArgumentNullException(nameof(library));

            data.notes ??= new List<StickyNoteData>();

            if (!data.initialized)
            {
                Seed(library);
                data.initialized = true;
            }

            Normalize();
        }

        private void Seed(StickyNotesLibrary library)
        {
            data.notes.Clear();

            foreach (StickyNoteSeed seed in library.Notes)
            {
                if (seed == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(seed.userId) &&
                    seed.userId != data.userId)
                {
                    continue;
                }

                StickyNoteData note = new()
                {
                    id = string.IsNullOrWhiteSpace(seed.id)
                        ? Guid.NewGuid().ToString("N")
                        : seed.id,

                    text = seed.text ?? string.Empty,
                    color = seed.color,
                    stamp = seed.stamp ?? string.Empty,
                    spans = CopySpans(
                        seed.text ?? string.Empty,
                        seed.spans)
                };

                data.notes.Add(note);
            }
        }

        private void Normalize()
        {
            HashSet<string> ids = new(StringComparer.Ordinal);

            for (int i = 0; i < data.notes.Count;)
            {
                StickyNoteData note = data.notes[i];

                if (note == null)
                {
                    data.notes.RemoveAt(i);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(note.id) ||
                    !ids.Add(note.id))
                {
                    note.id = Guid.NewGuid().ToString("N");
                    ids.Add(note.id);
                }

                note.text ??= string.Empty;
                note.stamp ??= string.Empty;

                if (!Enum.IsDefined(typeof(StickyNoteColor), note.color))
                    note.color = StickyNoteColor.Yellow;

                note.spans = CopySpans(note.text, note.spans);
                i++;
            }
        }

        public List<StickyNoteData> GetNotes(string query)
        {
            query = (query ?? string.Empty).Trim();

            List<StickyNoteData> result = new();

            foreach (StickyNoteData note in data.notes)
            {
                if (query.Length > 0 &&
                    note.text.IndexOf(
                        query,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                result.Add(note.Copy());
            }

            return result;
        }

        public bool TryGetNote(string id, out StickyNoteData note)
        {
            StickyNoteData stored = Find(id);
            note = stored?.Copy();

            return note != null;
        }

        public StickyNoteData CreateNote()
        {
            StickyNoteData note = new()
            {
                id = Guid.NewGuid().ToString("N"),
                color = StickyNoteColor.Yellow,
                stamp = "New"
            };

            data.notes.Insert(0, note);

            return note.Copy();
        }

        public bool DeleteNote(string id)
        {
            StickyNoteData note = Find(id);
            return note != null && data.notes.Remove(note);
        }

        public bool SetColor(
            string id,
            StickyNoteColor color)
        {
            StickyNoteData note = Find(id);

            if (note == null ||
                !Enum.IsDefined(typeof(StickyNoteColor), color))
            {
                return false;
            }

            note.color = color;
            return true;
        }

        public bool UpdateDocument(
            string id,
            string text,
            IReadOnlyList<StickyNoteSpan> spans,
            string stamp)
        {
            StickyNoteData note = Find(id);

            if (note == null)
                return false;

            note.text = text ?? string.Empty;
            note.spans = CopySpans(note.text, spans);
            note.stamp = stamp ?? "Edited";

            return true;
        }

        private StickyNoteData Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (StickyNoteData note in data.notes)
            {
                if (note.id == id)
                    return note;
            }

            return null;
        }

        private static List<StickyNoteSpan> CopySpans(
            string text,
            IReadOnlyList<StickyNoteSpan> source)
        {
            List<StickyNoteSpan> result = new();

            if (source == null)
                return result;

            const StickyNoteTextStyle supported =
                StickyNoteTextStyle.Bold |
                StickyNoteTextStyle.Italic |
                StickyNoteTextStyle.Underline |
                StickyNoteTextStyle.Strikethrough;

            foreach (StickyNoteSpan span in source)
            {
                if (span == null ||
                    span.start < 0 ||
                    span.start >= text.Length ||
                    span.length <= 0)
                {
                    continue;
                }

                StickyNoteTextStyle style = span.style & supported;

                if (style == StickyNoteTextStyle.None)
                    continue;

                result.Add(new StickyNoteSpan
                {
                    start = span.start,
                    length = Math.Min(
                        span.length,
                        text.Length - span.start),
                    style = style
                });
            }

            return result;
        }
    }
}