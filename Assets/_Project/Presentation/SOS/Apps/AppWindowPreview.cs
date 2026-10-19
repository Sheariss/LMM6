using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Apps
{
    public sealed class AppWindowPreview
    {
        public string AppID { get; }
        public IAppWindow Window { get; }
        public string Title { get; }
        public Sprite Icon { get; }
        public long WindowNumber { get; }
        public uint Revision { get; }
        public VisualElement Root { get; }

        public bool IsMinimized => Window.IsMinimized;
        public bool IsFocused => Window.IsFocused;

        public AppWindowPreview(
            string appID,
            IAppWindow window,
            string title,
            Sprite icon,
            long windowNumber,
            uint revision,
            VisualElement root)
        {
            AppID = appID;
            Window = window;
            Title = title;
            Icon = icon;
            WindowNumber = windowNumber;
            Revision = revision;
            Root = root;
        }
    }
}