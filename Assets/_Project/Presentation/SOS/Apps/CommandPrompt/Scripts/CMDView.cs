using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    public sealed class CMDView
    {
        private readonly CMDBinder binder;

        public CMDView(CMDBinder binder)
        {
            this.binder = binder;
        }

        // -------------------- RENDER --------------------
        public void Render(CMDViewState state)
        {
            if (state == null)
                return;

            SetOutput(state.OutputEntries);
            SetPrompt(state.Prompt);

            SetColors(
                state.BackgroundColor,
                state.ForegroundColor);
        }

        // -------------------- OUTPUT --------------------
        private void SetOutput(IReadOnlyList<string> entries)
        {
            if (binder.ScrollView == null)
                return;

            binder.ScrollView.Clear();

            if (entries == null)
                return;

            foreach (string entry in entries)
            {
                Label label = new Label(entry);

                label.AddToClassList("spacing-reset");
                label.AddToClassList("cmd-output");

                binder.ScrollView.Add(label);
            }

            ScrollToBottom();
        }

        // -------------------- PROMPT --------------------
        private void SetPrompt(string prompt)
        {
            if (binder.CommandPrompt == null)
                return;

            binder.CommandPrompt.text =
                prompt ?? string.Empty;
        }

        // -------------------- COLORS --------------------
        private void SetColors(
            Color backgroundColor,
            Color foregroundColor)
        {
            if (binder.Background != null)
            {
                binder.Background.style.backgroundColor = backgroundColor;
            }

            if (binder.ScrollView != null)
            {
                binder.ScrollView.style.color = foregroundColor;
            }

            if (binder.CommandPrompt != null)
            {
                binder.CommandPrompt.style.color = foregroundColor;
            }

            if (binder.CommandInput != null)
            {
                binder.CommandInput.style.color = foregroundColor;
            }
        }

        // -------------------- INPUT --------------------
        public void ClearInput()
        {
            binder.CommandInput?.SetValueWithoutNotify(
                string.Empty);
        }

        // -------------------- FOCUS --------------------
        public void FocusInput()
        {
            binder.CommandInput?.Focus();
        }

        // -------------------- SCROLL --------------------
        private void ScrollToBottom()
        {
            if (binder.ScrollView == null)
                return;

            binder.ScrollView.schedule.Execute(() =>
            {
                binder.ScrollView.scrollOffset =
                    new Vector2(
                        0,
                        binder.ScrollView.contentContainer.layout.height);
            });
        }
    }
}