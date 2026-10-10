using System;
using Atlas.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarPinnedButtonView
    {
        private readonly TaskbarPinnedButtonBinder binder;

        public TaskbarPinnedButtonView(
            TaskbarPinnedButtonBinder binder)
        {
            this.binder = binder
                ?? throw new ArgumentNullException(nameof(binder));
        }

        public void Populate(AppDefinition app)
        {
            binder.Icon.style.backgroundImage =
                new StyleBackground(StyleKeyword.None);

            binder.Icon.sprite =
                app.AppIcon48 != null
                    ? app.AppIcon48
                    : app.AppIcon96;

            binder.Icon.scaleMode = ScaleMode.ScaleToFit;
            binder.Button.tooltip = app.DisplayName;

            SetState(false, false);
        }

        public void SetState(bool isOpen, bool isFocused)
        {
            binder.Button.EnableInClassList("is-open", isOpen);
            binder.Button.EnableInClassList(
                "is-focused", isOpen && isFocused);
        }

        public void Remove()
        {
            binder.InstanceRoot.RemoveFromHierarchy();
        }
    }
}