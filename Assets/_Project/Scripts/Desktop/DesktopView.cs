using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopView
    {
        private readonly DesktopBinder binder;

        public DesktopView(DesktopBinder binder)
        {
            this.binder = binder;
        }

        public void Prepare(Sprite wallpaper)
        {
            Hide();

            binder.Wallpaper.style.backgroundImage =
                new StyleBackground(StyleKeyword.None);

            binder.Wallpaper.sprite = wallpaper;
            binder.Wallpaper.scaleMode = ScaleMode.ScaleAndCrop;

            // The first version contains only the desktop shell.
            binder.WidgetGroup.Clear();
            binder.AppGroup.Clear();
            binder.StatusGroup.Clear();
        }

        public void Show()
        {
            binder.Root.style.visibility = Visibility.Visible;
            binder.Root.SetEnabled(true);
        }

        public void Hide()
        {
            // Also works when other required elements are missing.
            if (binder.Root == null)
                return;

            binder.Root.SetEnabled(false);
            binder.Root.style.visibility = Visibility.Hidden;
        }

        public void Clear()
        {
            Hide();

            if (binder.Wallpaper != null)
            {
                binder.Wallpaper.sprite = null;
                binder.Wallpaper.style.backgroundImage =
                    new StyleBackground(StyleKeyword.None);
            }

            binder.WidgetGroup?.Clear();
            binder.AppGroup?.Clear();
            binder.StatusGroup?.Clear();
        }
    }
}