using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public abstract class WidgetContentController : AppContentController
    {
        protected WidgetContext Context { get; private set; }

        public void InitializeWidget(
            VisualElement instanceRoot,
            WidgetContext context)
        {
            if (InstanceRoot != null || Context != null)
            {
                throw new InvalidOperationException(
                    "This widget is already initialized.");
            }

            Context = context ??
                throw new ArgumentNullException(nameof(context));

            try
            {
                Initialize(instanceRoot);
            }
            catch
            {
                Context = null;
                throw;
            }
        }

        protected sealed override void OnInitialize()
        {
            if (Context == null)
            {
                throw new InvalidOperationException(
                    "Initialize widgets through InitializeWidget.");
            }

            OnWidgetInitialize();
        }

        protected sealed override void OnShutdown()
        {
            try
            {
                OnWidgetShutdown();
            }
            finally
            {
                Context = null;
            }
        }

        protected abstract void OnWidgetInitialize();

        protected virtual void OnWidgetShutdown()
        {
        }
    }
}