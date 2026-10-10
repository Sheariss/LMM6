using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopView
    {
        private readonly DesktopBinder binder;

        public DesktopView(DesktopBinder binder)
        {
            this.binder = binder
                ?? throw new ArgumentNullException(nameof(binder));
        }

        public void SetWallpaper(Sprite wallpaper)
        {
            // Remove the wallpaper authored in the template.
            binder.Wallpaper.style.backgroundImage =
                new StyleBackground(StyleKeyword.None);

            binder.Wallpaper.sprite = wallpaper;
            binder.Wallpaper.scaleMode = ScaleMode.ScaleAndCrop;
        }

        public void Show()
        {
            binder.Root.style.visibility = Visibility.Visible;
            binder.Root.SetEnabled(true);
        }

        public void Hide()
        {
            if (binder.Root == null)
                return;

            binder.Root.SetEnabled(false);
            binder.Root.style.visibility = Visibility.Hidden;
        }

        public void ClearWallpaper()
        {
            if (binder.Wallpaper == null)
                return;

            binder.Wallpaper.sprite = null;

            binder.Wallpaper.style.backgroundImage =
                new StyleBackground(StyleKeyword.None);
        }
    }
}