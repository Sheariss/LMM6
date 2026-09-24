using Atlas.Presentation.SOS.Windows;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Windows
{
    public sealed class WindowView
    {
        private readonly WindowBinder binder;

        public WindowView(WindowBinder binder)
        {
            this.binder = binder;
        }

        public void SetFocused(bool focused)
        {
            binder.Root.EnableInClassList(
                "window--focused",
                focused);
        }

        public void SetMaximized(bool maximized)
        {
            binder.Root.EnableInClassList(
                "window--maximized",
                maximized);

            binder.MaximizeButton.EnableInClassList(
                "window-button--restore",
                maximized);
        }

        public void Show()
        {
            binder.Root.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            binder.Root.style.display = DisplayStyle.None;
        }
    }
}