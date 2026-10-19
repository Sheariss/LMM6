using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetController : IDisposable
    {
        private readonly WidgetEngine engine;
        private readonly WidgetContentViewBuilder contentBuilder;
        private readonly Action<string> launchApp;
        private readonly Sprite icon;

        private WidgetView view;
        private bool disposed;

        public WidgetContext Context => engine.Context;
        public VisualElement Root => view?.Root;

        public WidgetController(
            WidgetContext context,
            Sprite icon,
            Func<string, bool> canAccessApp,
            Action<string> launchApp,
            WidgetContentViewBuilder contentBuilder)
        {
            this.icon = icon;

            this.launchApp = launchApp ??
                throw new ArgumentNullException(nameof(launchApp));

            this.contentBuilder = contentBuilder ??
                throw new ArgumentNullException(nameof(contentBuilder));

            engine = new WidgetEngine(context, canAccessApp);

            try
            {
                view = new WidgetViewBuilder().Build(context);

                Refresh();

                view.Binder.Root.clicked += OnPressed;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Refresh()
        {
            if (disposed)
                return;

            view.Render(
                Context.Definition,
                icon,
                engine.CanOpen);

            contentBuilder.Populate(
                Context,
                view.Content);
        }

        private void OnPressed()
        {
            if (disposed ||
                !engine.TryGetAppID(out string appID))
            {
                return;
            }

            launchApp(appID);
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            if (view != null)
            {
                view.Binder.Root.clicked -= OnPressed;
                view.Detach();
                view = null;
            }
        }
    }
}