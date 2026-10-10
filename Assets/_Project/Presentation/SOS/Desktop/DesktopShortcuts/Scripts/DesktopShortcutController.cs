using System;
using System.Collections.Generic;
using Atlas.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopShortcutController : IDisposable
    {
        private readonly VisualTreeAsset template;
        private readonly DesktopShortcutGridController grid;
        private readonly Action<string> launchApp;

        private readonly Vector2 iconSize;
        private readonly float labelFontSize;

        private readonly List<ShortcutInstance> instances = new();

        private sealed class ShortcutInstance
        {
            public string AppID;
            public DesktopShortcutBinder Binder;
            public Action ClickHandler;
        }

        public DesktopShortcutController(
            VisualTreeAsset template,
            DesktopShortcutGridController grid,
            Action<string> launchApp,
            Vector2 iconSize,
            float labelFontSize)
        {
            this.template = template != null
                ? template
                : throw new ArgumentNullException(nameof(template));

            this.grid = grid
                ?? throw new ArgumentNullException(nameof(grid));

            this.launchApp = launchApp
                ?? throw new ArgumentNullException(nameof(launchApp));

            this.iconSize = iconSize;
            this.labelFontSize = labelFontSize;
        }

        public void Build(IReadOnlyList<AppDefinition> apps)
        {
            if (apps == null)
                throw new ArgumentNullException(nameof(apps));

            Clear();

            var addedIDs = new HashSet<string>(StringComparer.Ordinal);

            try
            {
                for (int appIndex = 0; appIndex < apps.Count; appIndex++)
                {
                    AppDefinition app = apps[appIndex];

                    if (app == null || string.IsNullOrWhiteSpace(app.AppID))
                    {
                        throw new InvalidOperationException(
                            $"Shortcut definition at index {appIndex} " +
                            "is missing its AppID.");
                    }

                    if (!addedIDs.Add(app.AppID))
                        continue;

                    CreateShortcut(app);
                }
                Debug.Log(
                $"[DesktopShortcutController] Built {instances.Count} shortcuts; " +
                $"{grid.PendingCount} waiting for grid space.");
            }
            catch
            {
                Clear();
                throw;
            }
        }

        private void CreateShortcut(AppDefinition app)
        {
            TemplateContainer root = template.CloneTree();
            var binder = new DesktopShortcutBinder(root);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "Desktop shortcut template is missing required elements.");
            }

            var view = new DesktopShortcutView(binder);
            view.Populate(app);
            view.SetAppearance(iconSize, labelFontSize);

            string appID = app.AppID;
            Action clickHandler = () => launchApp(appID);

            var instance = new ShortcutInstance
            {
                AppID = appID,
                Binder = binder,
                ClickHandler = clickHandler
            };

            instances.Add(instance);
            binder.Button.clicked += clickHandler;

            grid.Add(appID, binder.InstanceRoot);
        }

        public void Clear()
        {
            foreach (ShortcutInstance instance in instances)
            {
                instance.Binder.Button.clicked -= instance.ClickHandler;
                grid.Remove(instance.AppID);
            }

            instances.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}