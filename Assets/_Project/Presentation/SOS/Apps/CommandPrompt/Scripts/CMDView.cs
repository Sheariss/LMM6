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

            SetOutput(state.OutputLines);
            SetColors(
                state.BackgroundColor,
                state.ForegroundColor);
        }

        // -------------------- OUTPUT --------------------
        private void SetOutput(
            System.Collections.Generic.IReadOnlyList<string> lines)
        {
            if (binder.ScrollView == null)
                return;

            binder.ScrollView.Clear();

            if (lines == null)
                return;

            foreach (string line in lines)
            {
                Label label = new Label(line);

                label.AddToClassList("cmd-output");

                binder.ScrollView.Add(label);
            }

            ScrollToBottom();
        }

        // -------------------- COLORS --------------------
        private void SetColors(
            Color backgroundColor,
            Color foregroundColor)
        {
            if (binder.Background != null)
            {
                binder.Background.style.backgroundColor =
                    backgroundColor;
            }

            if (binder.ScrollView != null)
            {
                binder.ScrollView.style.color =
                    foregroundColor;
            }

            if (binder.CommandInput != null)
            {
                binder.CommandInput.style.color =
                    foregroundColor;
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