using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopBinder : UIBinder
    {
        public VisualElement DocumentRoot { get; }
        public VisualElement Root { get; }
        public Image Wallpaper { get; }
        public VisualElement Taskbar { get; }
        public GroupBox WidgetGroup { get; }
        public GroupBox AppGroup { get; }
        public GroupBox StatusGroup { get; }

        public DesktopBinder(VisualElement documentRoot)
        {
            DocumentRoot = documentRoot;

            if (DocumentRoot == null)
            {
                Debug.LogError(
                    $"[{nameof(DesktopBinder)}] Document root is null."
                );

                IsValid = false;
                return;
            }

            Root = Bind<VisualElement>(DocumentRoot, "DesktopRoot");
            Wallpaper = Bind<Image>(Root, "DesktopWallpaper");
            Taskbar = Bind<VisualElement>(Root, "DesktopTaskbar");

            WidgetGroup = Bind<GroupBox>(Taskbar, "WidgetGroup");
            AppGroup = Bind<GroupBox>(Taskbar, "AppGroup");
            StatusGroup = Bind<GroupBox>(Taskbar, "StatusGroup");
        }
    }
}