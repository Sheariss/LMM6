using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.SaveSlotMenu
{
    public sealed class SaveSlotMenuView
    {
        // -------------------- DEPENDENCIES --------------------
        private readonly SaveSlotMenuBinder binder;

        // -------------------- CONSTRUCTOR --------------------
        public SaveSlotMenuView(SaveSlotMenuBinder binder)
        {
            this.binder = binder;
        }

        // -------------------- VISIBILITY --------------------
        public void Show()
        {
            binder.Root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            binder.Root.style.display = DisplayStyle.None;
        }

        // -------------------- VIEW --------------------
        public void Apply(ViewState state)
        {
            binder.TitleLabel.text = state.Title;
            binder.InstructionLabel.text = state.Instruction;

            for (int i = 0; i < binder.Slots.Length; i++)
            {
                ApplySlot(binder.Slots[i], state.Slots[i]);
            }
        }

        // -------------------- SLOT VIEW --------------------
        private static void ApplySlot(
            SaveSlotMenuBinder.SaveSlotBinding binding,
            SlotViewState state)
        {
            binding.Button.SetEnabled(state.Enabled);
            binding.TitleLabel.text = state.Title;
            binding.ChapterLabel.text = state.Chapter;
            binding.DateLabel.text = state.Date;
            binding.ProgressLabel.text = state.Progress;
            binding.ProgressLabel.style.visibility = state.ShowProgress ? Visibility.Visible : Visibility.Hidden;

            ApplySlotStateClasses(binding, state);
        }

        // -------------------- SLOT STATE STYLE --------------------
        private static void ApplySlotStateClasses(
            SaveSlotMenuBinder.SaveSlotBinding binding,
            SlotViewState state)
        {
            binding.Button.EnableInClassList("ssm-save-slot--empty", !state.HasSave);
            binding.Button.EnableInClassList("ssm-save-slot--occupied", state.HasSave);
            binding.StateIcon.EnableInClassList("ssm-save-slot-icon--empty", !state.HasSave);
            binding.StateIcon.EnableInClassList("ssm-save-slot-icon--occupied", state.HasSave);
        }

        // -------------------- VIEW STATE --------------------
        public sealed class ViewState
        {
            public string Title { get; set; }
            public string Instruction { get; set; }
            public SlotViewState[] Slots { get; set; }
        }

        // -------------------- SLOT VIEW STATE --------------------
        public sealed class SlotViewState
        {
            public int SlotNumber { get; set; }
            public bool HasSave { get; set; }
            public bool Enabled { get; set; }
            public string Title { get; set; }
            public string Chapter { get; set; }
            public string Date { get; set; }
            public string Progress { get; set; }
            public bool ShowProgress { get; set; }
        }
    }
}