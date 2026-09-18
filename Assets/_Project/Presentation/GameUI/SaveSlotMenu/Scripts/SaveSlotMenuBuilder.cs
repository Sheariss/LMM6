using Atlas.Core.Persistence;

namespace Atlas.Presentation.GameUI.SaveSlotMenu
{
    public sealed class SaveSlotMenuBuilder
    {
        // -------------------- CONSTANTS --------------------

        private const int SlotCount = 6;

        // -------------------- DEPENDENCIES --------------------

        private readonly SaveManager saveManager;

        // -------------------- CONSTRUCTOR --------------------

        public SaveSlotMenuBuilder(
            SaveManager saveManager)
        {
            this.saveManager = saveManager;
        }

        // -------------------- BUILD --------------------

        public SaveSlotMenuView.ViewState Build(
            SaveSlotMenuMode mode)
        {
            SaveSlotMenuView.SlotViewState[] slots =
                new SaveSlotMenuView.SlotViewState[SlotCount];

            for (int i = 0; i < SlotCount; i++)
            {
                int slotNumber = i + 1;

                slots[i] =
                    BuildSlot(
                        slotNumber,
                        mode
                    );
            }

            return new SaveSlotMenuView.ViewState
            {
                Title = "Save Slots",
                Instruction = GetInstruction(mode),
                Slots = slots
            };
        }

        // -------------------- SLOT BUILDING --------------------

        private SaveSlotMenuView.SlotViewState BuildSlot(
            int slotNumber,
            SaveSlotMenuMode mode)
        {
            bool hasSave =
                saveManager.HasSave(
                    slotNumber
                );

            bool enabled =
                IsSlotEnabled(
                    mode,
                    hasSave
                );

            return new SaveSlotMenuView.SlotViewState
            {
                SlotNumber = slotNumber,

                HasSave = hasSave,

                Enabled = enabled,

                Title =
                    BuildSlotTitle(
                        slotNumber
                    ),

                Chapter =
                    BuildSlotChapter(
                        slotNumber,
                        hasSave
                    ),

                Date =
                    BuildSlotDate(
                        slotNumber,
                        hasSave
                    ),

                Progress =
                    BuildSlotProgress(
                        slotNumber,
                        hasSave
                    ),

                ShowProgress = hasSave
            };
        }

        // -------------------- SLOT STATE --------------------

        private static bool IsSlotEnabled(
            SaveSlotMenuMode mode,
            bool hasSave)
        {
            return mode switch
            {
                SaveSlotMenuMode.NewGame =>
                    true,

                SaveSlotMenuMode.Continue =>
                    true,

                _ =>
                    false
            };
        }

        // -------------------- TEXT --------------------

        private static string BuildSlotTitle(
            int slotNumber)
        {
            return $"Save Slot {slotNumber:00}";
        }

        private static string BuildSlotChapter(
            int slotNumber,
            bool hasSave)
        {
            if (!hasSave)
            {
                return "Empty Slot";
            }

            // TODO:
            // Query SaveManager for SaveMetadata.
            //
            // Example:
            // Chapter 2: The Ashford File

            return "Chapter 0: Placeholder Chapter";
        }

        private static string BuildSlotDate(
            int slotNumber,
            bool hasSave)
        {
            if (!hasSave)
            {
                string text = "No saved game data.";
                return text;
            }

            // TODO:
            // Query SaveMetadata for last saved date/time.

            return "Last Saved: --";
        }

        private static string BuildSlotProgress(
            int slotNumber,
            bool hasSave)
        {
            if (!hasSave)
            {
                return string.Empty;
            }

            // TODO:
            // Query SaveMetadata / ProgressionSaveData.

            return "Progress: --";
        }

        private static string GetInstruction(
            SaveSlotMenuMode mode)
        {
            return mode switch
            {
                SaveSlotMenuMode.NewGame =>
                    "Select a save slot for the new investigation.",

                SaveSlotMenuMode.Continue =>
                    "Select an investigation to resume, or choose an empty slot to begin a new investigation.",

                _ =>
                    string.Empty
            };
        }
    }
}