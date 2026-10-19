using Atlas.SOS.Notepad;

namespace Atlas.Presentation.SOS.Notepad
{
    public sealed class NotepadViewState
    {
        public string Name;
        public string Text;
        public string SaveState;
        public string Statistics;
        public string Message;

        public bool IsDirty;
        public bool CanEdit;
        public bool CanSave;
        public bool CanUndo;
        public bool CanRedo;
        public bool ReadMode;
        public bool AutoSave;

        public int ZoomPercent;
    }

    public sealed class NotepadViewBuilder
    {
        public NotepadViewState Build(
            NotepadEngine engine,
            bool autoSave,
            string message)
        {
            NotepadDocument document = engine.Document;

            int words = 0;
            bool insideWord = false;

            foreach (char character in document.Text)
            {
                if (char.IsWhiteSpace(character))
                {
                    insideWord = false;
                }
                else if (!insideWord)
                {
                    insideWord = true;
                    words++;
                }
            }

            string saveState = document.IsDirty
                ? "Unsaved changes"
                : document.IsSessionSaved
                    ? "Saved for this session"
                    : "Authored file";

            if (document.IsReadOnly)
                saveState += " · Read only";

            return new NotepadViewState
            {
                Name = document.Name,
                Text = document.Text,
                SaveState = saveState,
                Statistics = $"{words} words · {document.Text.Length} characters",
                Message = message ?? string.Empty,

                IsDirty = document.IsDirty,
                CanEdit = engine.CanEdit,
                CanSave = !document.IsReadOnly,
                CanUndo = engine.CanUndo,
                CanRedo = engine.CanRedo,
                ReadMode = engine.ReadMode,
                AutoSave = autoSave,

                ZoomPercent = engine.ZoomPercent
            };
        }
    }
}