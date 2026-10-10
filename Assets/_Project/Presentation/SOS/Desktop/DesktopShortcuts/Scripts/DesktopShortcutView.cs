using Atlas.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopShortcutView
    {
        private readonly DesktopShortcutBinder binder;

        public DesktopShortcutView(DesktopShortcutBinder binder)
        {
            this.binder = binder;
        }

        public void Populate(AppDefinition app)
        {
            Populate(
                app.DisplayName,
                app.AppIcon96 != null ? app.AppIcon96 : app.AppIcon48,
                isShortcut: true);
        }

        public void Populate(
            string displayName,
            Sprite icon,
            bool isShortcut)
        {
            binder.Label.text = displayName;

            // Clear the template's placeholder background image.
            binder.Icon.style.backgroundImage =
                new StyleBackground(StyleKeyword.None);

            binder.Icon.sprite = icon;
            binder.Icon.scaleMode = ScaleMode.ScaleToFit;

            binder.Button.tooltip = displayName;
            SetShortcutArrowVisible(isShortcut);
        }

        public void SetShortcutArrowVisible(bool visible)
        {
            binder.Arrow.style.display =
                visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void SetAppearance(Vector2 iconSize, float labelFontSize)
        {
            binder.Icon.style.width = Mathf.Max(1f, iconSize.x);
            binder.Icon.style.height = Mathf.Max(1f, iconSize.y);
            binder.Icon.style.flexShrink = 0f;

            binder.Label.style.fontSize = Mathf.Max(1f, labelFontSize);
        }

        public void Remove()
        {
            // Remove the entire clone, including its template wrapper.
            binder.InstanceRoot.RemoveFromHierarchy();
        }
    }
}