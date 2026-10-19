using System;

namespace Atlas.SOS.Notepad
{
    [Serializable]
    public sealed class NotepadDocument
    {
        public string Id;
        public string Name;
        public string Text;
        public string SourcePath;

        public bool IsReadOnly;
        public bool HasBaseline;
        public bool IsSessionSaved;

        public string SavedName;
        public string SavedText;

        public bool IsDirty =>
            !HasBaseline ||
            !string.Equals(Name, SavedName, StringComparison.Ordinal) ||
            !string.Equals(Text, SavedText, StringComparison.Ordinal);

        public NotepadDocument Clone()
        {
            return (NotepadDocument)MemberwiseClone();
        }
    }
}