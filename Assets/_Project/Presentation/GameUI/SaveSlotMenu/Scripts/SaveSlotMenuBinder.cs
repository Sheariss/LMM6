using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.GameUI.SaveSlotMenu
{
    public sealed class SaveSlotMenuBinder
    {
        // -------------------- UI ELEMENTS --------------------
        public VisualElement Root { get; }
        public Label TitleLabel { get; }
        public Label InstructionLabel { get; }
        public Button BackButton { get; }
        public SaveSlotBinding[] Slots { get; }

        // -------------------- STATE --------------------
        public bool IsValid { get; }

        // -------------------- CONSTRUCTOR --------------------
        public SaveSlotMenuBinder(VisualElement root)
        {
            if (root == null)
            {
                Debug.LogError("[SaveSlotMenuBinder] Root VisualElement is null.");
                IsValid = false;
                return;
            }

            Root = root.Q<VisualElement>("SaveSlotMenuRoot");
            TitleLabel = root.Q<Label>("SSM-Title");
            InstructionLabel = root.Q<Label>("SSM-Instruction");
            BackButton = root.Q<Button>("SSM-BackButton");

            Slots = new[]
            {
                CreateSlotBinding(root, 1),
                CreateSlotBinding(root, 2),
                CreateSlotBinding(root, 3),
                CreateSlotBinding(root, 4),
                CreateSlotBinding(root, 5),
                CreateSlotBinding(root, 6)
            };

            IsValid = VerifyBindings();
        }

        // -------------------- SLOT BINDING --------------------
        private static SaveSlotBinding CreateSlotBinding(VisualElement root, int slotNumber)
        {
            Button button = root.Q<Button>($"SaveSlotButton{slotNumber:0}");

            if (button == null)
            {
                Debug.LogError($"[SaveSlotMenuBinder] Missing required element: 'SaveSlotButton{slotNumber:0}'.");
                return null;
            }
            return new SaveSlotBinding(slotNumber, button);
        }

        // -------------------- VERIFICATION --------------------
        private bool VerifyBindings()
        {
            bool isValid = true;

            if (Root == null)
            {
                Debug.LogError("[SaveSlotMenuBinder] Missing required element: 'SSM-Root'.");
                isValid = false;
            }

            if (TitleLabel == null)
            {
                Debug.LogError("[SaveSlotMenuBinder] Missing required element: 'SSM-TitleLabel'.");
                isValid = false;
            }

            if (InstructionLabel == null)
            {
                Debug.LogError("[SaveSlotMenuBinder] Missing required element: 'SSM-InstructionLabel'.");
                isValid = false;
            }

            if (BackButton == null)
            {
                Debug.LogError("[SaveSlotMenuBinder] Missing required element: 'SSM-BackButton'.");
                isValid = false;
            }

            for (int i = 0; i < Slots.Length; i++)
            {
                SaveSlotBinding slot = Slots[i];

                if (slot == null || !slot.IsValid)
                {
                    isValid = false;
                }
            }

            if (isValid)
            {
                Debug.Log("[SaveSlotMenuBinder] All required UI bindings verified.");
            }
            return isValid;
        }

        // -------------------- ACTION BINDING --------------------
        public void BindActions(Action<int> onSlotPressed, Action onBackPressed)
        {
            if (!IsValid)
            {
                return;
            }

            for (int i = 0; i < Slots.Length; i++)
            {
                int slotNumber = Slots[i].SlotNumber;
                Slots[i].Button.clicked += () => onSlotPressed(slotNumber);
            }
            BackButton.clicked += onBackPressed;
        }

        // -------------------- SLOT BINDING --------------------
        public sealed class SaveSlotBinding
        {
            // -------------------- STATE --------------------
            public int SlotNumber { get; }
            public bool IsValid { get; }

            // -------------------- ELEMENTS --------------------
            public Button Button { get; }
            public Image StateIcon { get; }
            public Label TitleLabel { get; }
            public Label ChapterLabel { get; }
            public Label DateLabel { get; }
            public Label ProgressLabel { get; }

            // -------------------- CONSTRUCTOR --------------------
            public SaveSlotBinding(int slotNumber, Button button)
            {
                SlotNumber = slotNumber;
                Button = button;
                StateIcon = button.Q<Image>("SaveSlotStateIcon");
                TitleLabel = button.Q<Label>("SaveSlotTitle");
                ChapterLabel = button.Q<Label>("SaveSlotChapter");
                DateLabel = button.Q<Label>("SaveSlotDate");
                ProgressLabel = button.Q<Label>("SaveSlotProgress");

                IsValid = VerifyBindings();
            }

            // -------------------- VERIFICATION --------------------
            private bool VerifyBindings()
            {
                bool isValid = true;

                if (StateIcon == null)
                {
                    Debug.LogError($"[SaveSlotMenuBinder] Slot {SlotNumber:00} is missing 'SaveSlotStateIcon'.");
                    isValid = false;
                }

                if (TitleLabel == null)
                {
                    Debug.LogError($"[SaveSlotMenuBinder] Slot {SlotNumber:00} is missing 'SaveSlotTitle'.");
                    isValid = false;
                }

                if (ChapterLabel == null)
                {
                    Debug.LogError($"[SaveSlotMenuBinder] Slot {SlotNumber:00} is missing 'SaveSlotChapter'.");
                    isValid = false;
                }

                if (DateLabel == null)
                {
                    Debug.LogError($"[SaveSlotMenuBinder] Slot {SlotNumber:00} is missing 'SaveSlotDate'.");
                    isValid = false;
                }

                if (ProgressLabel == null)
                {
                    Debug.LogError($"[SaveSlotMenuBinder] Slot {SlotNumber:00} is missing 'SaveSlotProgress'.");
                    isValid = false;
                }
                return isValid;
            }
        }
    }
}