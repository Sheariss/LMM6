using Atlas.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopBinder : UIBinder
    {
        public VisualElement DocumentRoot { get; }
        public VisualElement Root { get; }
        public VisualElement Workspace { get; }

        public Image Wallpaper { get; }

        public VisualElement ShortcutLayer { get; }
        public VisualElement ShortcutGrid { get; }
        public VisualElement WindowLayer { get; }

        public TemplateContainer Taskbar { get; }

        public DesktopBinder(VisualElement documentRoot)
        {
            DocumentRoot = documentRoot;

            if (DocumentRoot == null)
            {
                Debug.LogError(
                    $"[{nameof(DesktopBinder)}] Document root is null.");

                IsValid = false;
                return;
            }

            Root = Bind<VisualElement>(
                DocumentRoot, "DesktopRoot");

            Workspace = Bind<VisualElement>(
                Root, "DesktopWorkspace");

            Wallpaper = Bind<Image>(
                Workspace, "BackgroundLayer");

            ShortcutLayer = Bind<VisualElement>(
                Workspace, "ShortcutLayer");

            ShortcutGrid = Bind<VisualElement>(
                ShortcutLayer, "ShortcutGrid");

            WindowLayer = Bind<VisualElement>(
                Workspace, "WindowLayer");

            Taskbar = Bind<TemplateContainer>(
                Root, "Taskbar");
        }
    }
}