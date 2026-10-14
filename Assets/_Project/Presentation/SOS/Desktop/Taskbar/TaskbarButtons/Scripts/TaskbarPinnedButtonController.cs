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

        // Cache every available app's button for this desktop session.
        private readonly Dictionary<string, ButtonInstance> instances =
            new(StringComparer.Ordinal);

        private readonly HashSet<string> pinnedAppIDs =
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

        // Preserve the original API for callers that only supply pinned apps.
        public void Build(IReadOnlyList<AppDefinition> apps)
        {
            Build(apps, apps);
        }

        public void Build(
            IReadOnlyList<AppDefinition> availableApps,
            IReadOnlyList<AppDefinition> pinnedApps)
        {
            if (availableApps == null)
                throw new ArgumentNullException(nameof(availableApps));

            if (pinnedApps == null)
                throw new ArgumentNullException(nameof(pinnedApps));

            Clear();

            try
            {
                // Clone each available app's button exactly once.
                // Unpinned buttons remain detached until the app opens.
                for (int index = 0; index < availableApps.Count; index++)
                {
                    AppDefinition app = availableApps[index];

                    if (app == null ||
                        string.IsNullOrWhiteSpace(app.AppID))
                    {
                        throw new InvalidOperationException(
                            $"Available app at index {index} " +
                            "is missing its AppID.");
                    }

                    if (instances.ContainsKey(app.AppID))
                        continue;

                    CreateButton(app);
                }

                // Attach pinned buttons in the user's configured order.
                for (int index = 0; index < pinnedApps.Count; index++)
                {
                    AppDefinition app = pinnedApps[index];

                    if (app == null ||
                        string.IsNullOrWhiteSpace(app.AppID))
                    {
                        throw new InvalidOperationException(
                            $"Pinned app at index {index} " +
                            "is missing its AppID.");
                    }

                    if (!instances.TryGetValue(
                        app.AppID,
                        out ButtonInstance instance))
                    {
                        throw new InvalidOperationException(
                            $"Pinned app '{app.AppID}' " +
                            "is not in the available app list.");
                    }

                    if (!pinnedAppIDs.Add(app.AppID))
                        continue;

                    container.Add(instance.Binder.InstanceRoot);
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

            string appID = app.AppID;
            Action clickHandler = () => launchApp(appID);

            instances.Add(appID, new ButtonInstance
            {
                Binder = binder,
                View = view,
                ClickHandler = clickHandler
            });

            binder.Button.clicked += clickHandler;

            // Build or SetAppState attaches the cached root when needed.
        }

        public void SetAppState(
            string appID,
            bool isOpen,
            bool isFocused)
        {
            if (string.IsNullOrWhiteSpace(appID) ||
                !instances.TryGetValue(
                    appID,
                    out ButtonInstance instance))
            {
                return;
            }

            instance.View.SetState(isOpen, isFocused);

            bool shouldBeVisible =
                pinnedAppIDs.Contains(appID) || isOpen;

            if (shouldBeVisible)
            {
                // Already attached buttons keep their position.
                // Newly opened unpinned apps append after existing buttons.
                if (instance.Binder.InstanceRoot.parent != container)
                    container.Add(instance.Binder.InstanceRoot);
            }
            else
            {
                // Keep the cached button and its click handler for reuse.
                instance.View.Remove();
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
            pinnedAppIDs.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}