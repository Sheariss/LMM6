using System;
using System.Collections.Generic;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Notepad
{
    public sealed class NotepadBinder : UIBinder
    {
        public VisualElement Root { get; }
        public VisualElement Content { get; }
        public VisualElement MenuLayer { get; }
        public VisualElement Prompt { get; }

        public TextField Editor { get; }
        public TextField FileName { get; }
        public TextField Search { get; }
        public TextField ZoomPercentage { get; }

        public Toggle AutoSave { get; }
        public Slider ZoomSlider { get; }

        public Label Title { get; }
        public Label SaveState { get; }
        public Label AutoSaveState { get; }
        public Label Status { get; }
        public Label PromptText { get; }
        public Label PageCount { get; }

        public VisualElement SavedBadge { get; }
        public VisualElement UnsavedBadge { get; }

        public List<Button> Buttons { get; }

        public NotepadBinder(VisualElement content)
        {
            Root = FindRoot(content);

            Content = Bind<VisualElement>(Root, "WindowContent");
            MenuLayer = Bind<VisualElement>(Root, "MenuLayer");
            Prompt = Bind<VisualElement>(Root, "Prompt");

            Editor = Bind<TextField>(Root, "Editor");
            FileName = Bind<TextField>(Root, "FileName");
            Search = Bind<TextField>(Root, "DocumentSearch");
            ZoomPercentage = Bind<TextField>(Root, "ZoomPercentage");

            AutoSave = Bind<Toggle>(Root, "AutoSaveToggle");
            ZoomSlider = Bind<Slider>(Root, "ZoomSlider");

            Title = Bind<Label>(Root, "WindowTitle");
            SaveState = Bind<Label>(Root, "SaveState");
            AutoSaveState = Bind<Label>(Root, "AutoSaveState");
            Status = Bind<Label>(Root, "Status");
            PromptText = Bind<Label>(Root, "PromptText");
            PageCount = Bind<Label>(Root, "PageCount");

            SavedBadge = Bind<VisualElement>(Root, "SavedBadge");
            UnsavedBadge = Bind<VisualElement>(Root, "UnsavedBadge");

            Buttons = Root.Query<Button>().ToList();

            if (!IsValid)
            {
                throw new InvalidOperationException(
                    "Notepad is missing required UXML elements.");
            }
        }

        public Button Button(string name)
        {
            return Required<Button>(name);
        }

        public T Required<T>(string name) where T : VisualElement
        {
            T element = Root.Q<T>(name);

            return element ?? throw new InvalidOperationException(
                $"Notepad requires {typeof(T).Name} '{name}'.");
        }

        private static VisualElement FindRoot(VisualElement content)
        {
            for (VisualElement current = content;
                 current != null;
                 current = current.parent)
            {
                if (current.name == "WindowRoot")
                    return current;
            }

            throw new InvalidOperationException(
                "Notepad must initialize inside WindowRoot.");
        }
    }
}