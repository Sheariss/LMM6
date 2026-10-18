using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.SOS.StickyNotes
{
    public enum StickyNoteColor
    {
        Yellow,
        Green,
        Pink,
        Purple,
        Blue,
        Gray,
        Charcoal
    }

    [Flags]
    public enum StickyNoteTextStyle
    {
        None = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikethrough = 8
    }

    [Serializable]
    public sealed class StickyNoteSpan
    {
        public int start;
        public int length;
        public StickyNoteTextStyle style;

        public StickyNoteSpan Copy()
        {
            return new StickyNoteSpan
            {
                start = start,
                length = length,
                style = style
            };
        }
    }

    [Serializable]
    public sealed class StickyNoteData
    {
        public string id;
        public string text = string.Empty;
        public StickyNoteColor color;

        // Authored/session-time label. No dependency on the real-world clock.
        public string stamp = string.Empty;

        // Offsets use C# string indices: UTF-16 code units.
        public List<StickyNoteSpan> spans = new();

        public StickyNoteData Copy()
        {
            StickyNoteData copy = new()
            {
                id = id,
                text = text,
                color = color,
                stamp = stamp
            };

            if (spans != null)
            {
                foreach (StickyNoteSpan span in spans)
                {
                    if (span != null)
                        copy.spans.Add(span.Copy());
                }
            }

            return copy;
        }
    }

    [Serializable]
    public sealed class StickyNotesUserData
    {
        public string userId;
        public bool initialized;
        public List<StickyNoteData> notes = new();
    }

    [Serializable]
    public sealed class StickyNotesSaveData
    {
        public List<StickyNotesUserData> users = new();
    }

    public readonly struct StickyNotesSession :
        IEquatable<StickyNotesSession>
    {
        public string UserId { get; }
        public long Generation { get; }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(UserId) &&
            Generation > 0;

        internal StickyNotesSession(string userId, long generation)
        {
            UserId = userId;
            Generation = generation;
        }

        public bool Equals(StickyNotesSession other)
        {
            return Generation == other.Generation &&
                string.Equals(
                    UserId,
                    other.UserId,
                    StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is StickyNotesSession other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((UserId?.GetHashCode() ?? 0) * 397) ^
                    Generation.GetHashCode();
            }
        }
    }

    public interface IStickyNotesWidgetHost
    {
        // Open an existing note, or focus its existing widget.
        bool TryOpenNote(
            StickyNotesSession session,
            string noteId,
            out string error);

        // Close the widget without deleting its stored note.
        void CloseNote(StickyNotesSession session, string noteId);

        // Called when changing user/save or shutting down the manager.
        void CloseAllNotes();
    }

    public readonly struct StickyNotePalette
    {
        public Color Body { get; }
        public Color Header { get; }
        public Color Text { get; }

        public StickyNotePalette(
            Color body,
            Color header,
            Color text)
        {
            Body = body;
            Header = header;
            Text = text;
        }
    }

    public static class StickyNotesPalette
    {
        public static StickyNotePalette Get(StickyNoteColor color)
        {
            Color darkText = new Color32(48, 46, 40, 255);

            return color switch
            {
                StickyNoteColor.Green => new StickyNotePalette(
                    new Color32(218, 241, 201, 255),
                    new Color32(183, 224, 157, 255),
                    darkText),

                StickyNoteColor.Pink => new StickyNotePalette(
                    new Color32(250, 216, 232, 255),
                    new Color32(243, 183, 212, 255),
                    darkText),

                StickyNoteColor.Purple => new StickyNotePalette(
                    new Color32(233, 221, 250, 255),
                    new Color32(209, 184, 240, 255),
                    darkText),

                StickyNoteColor.Blue => new StickyNotePalette(
                    new Color32(215, 237, 252, 255),
                    new Color32(177, 218, 247, 255),
                    darkText),

                StickyNoteColor.Gray => new StickyNotePalette(
                    new Color32(238, 238, 238, 255),
                    new Color32(216, 216, 216, 255),
                    darkText),

                StickyNoteColor.Charcoal => new StickyNotePalette(
                    new Color32(69, 69, 73, 255),
                    new Color32(49, 49, 53, 255),
                    new Color32(246, 246, 246, 255)),

                _ => new StickyNotePalette(
                    new Color32(255, 247, 199, 255),
                    new Color32(255, 234, 135, 255),
                    darkText)
            };
        }
    }
}