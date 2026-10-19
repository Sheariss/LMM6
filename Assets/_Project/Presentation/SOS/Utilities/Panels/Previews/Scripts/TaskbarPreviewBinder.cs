using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarPreviewBinder : UIBinder
    {
        public VisualElement Root { get; }
        public VisualElement Cards { get; }

        public TaskbarPreviewBinder(VisualElement instanceRoot)
        {
            if (instanceRoot == null)
                throw new ArgumentNullException(nameof(instanceRoot));

            Root = Bind<VisualElement>(
                instanceRoot,
                "TaskbarPreviewRoot");

            Cards = Bind<VisualElement>(
                instanceRoot,
                "TaskbarPreviewCards");
        }
    }
}