using System;
using System.Collections.Generic;
using Atlas.SOS.Apps;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarPinnedButtonController : IDisposable
    {
        private readonly VisualTreeAsset template;
        private readonly VisualElement container;
        private readonly Action<string> launchApp;

        private readonly Dictionary<string, ButtonInstance> instances =
            new(StringComparer.Ordinal);

        private sealed class ButtonInstance
        {
            public TaskbarPinnedButtonBinder Binder;
            public TaskbarPinnedButtonView View;
            public Action ClickHandler;
        }

        public TaskbarPinnedButtonController(
            VisualTreeAsset template,
            VisualElement container,
            Action<string> launchApp)
        {
            this.template = template != null
                ? template
                : throw new ArgumentNullException(nameof(template));

            this.container = container
                ?? throw new ArgumentNullException(nameof(container));

            this.launchApp = launchApp
                ?? throw new ArgumentNullException(nameof(launchApp));
        }

        public void Build(IReadOnlyList<AppDefinition> apps)
        {
            if (apps == null)
                throw new ArgumentNullException(nameof(apps));

            Clear();

            try
            {
                for (int index = 0; index < apps.Count; index++)
                {
                    AppDefinition app = apps[index];

                    if (app == null ||
                        string.IsNullOrWhiteSpace(app.AppID))
                    {
                        throw new InvalidOperationException(
                            $"Pinned app at index {index} " +
                            "is missing its AppID.");
                    }

                    if (instances.ContainsKey(app.AppID))
                        continue;

                    CreateButton(app);
                }
            }
            catch
            {
                Clear();
                throw;
            }
        }

        private void CreateButton(AppDefinition app)
        {
            TemplateContainer root = template.CloneTree();

            root.style.flexGrow = 0;
            root.style.flexShrink = 0;

            var binder = new TaskbarPinnedButtonBinder(root);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "Pinned button template is missing required elements.");
            }

            var view = new TaskbarPinnedButtonView(binder);
            view.Populate(app);
            view.SetState(false, false);

            string appID = app.AppID;
            Action clickHandler = () => launchApp(appID);

            instances.Add(appID, new ButtonInstance
            {
                Binder = binder,
                View = view,
                ClickHandler = clickHandler
            });

            binder.Button.clicked += clickHandler;
            container.Add(root);
        }

        public void SetAppState(
            string appID,
            bool isOpen,
            bool isFocused)
        {
            if (string.IsNullOrWhiteSpace(appID))
                return;

            if (instances.TryGetValue(appID, out ButtonInstance instance))
            {
                instance.View.SetState(isOpen, isFocused);
            }
        }

        public void Clear()
        {
            foreach (ButtonInstance instance in instances.Values)
            {
                instance.Binder.Button.clicked -= instance.ClickHandler;
                instance.View.Remove();
            }

            instances.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}