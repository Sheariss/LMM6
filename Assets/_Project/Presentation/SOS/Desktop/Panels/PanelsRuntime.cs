using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Panels
{
    public enum DesktopPanelID
    {
        Widgets,
        Network,
        DateTime,
        Notifications
    }

    public abstract class PanelRuntime : MonoBehaviour
    {
        public abstract VisualElement Surface { get; }

        public event Action<PanelRuntime> CloseRequested;

        public abstract void Initialize(
            VisualElement parent,
            Func<string, bool> canAccessApp,
            Action<string> launchApp);

        public abstract void Show();
        public abstract void Hide();
        public abstract void Shutdown();

        // Override if a panel creates additional popup surfaces.
        public virtual bool Contains(VisualElement target)
        {
            return target != null &&
                   Surface != null &&
                   (target == Surface || Surface.Contains(target));
        }

        protected void RequestClose()
        {
            CloseRequested?.Invoke(this);
        }
    }
}