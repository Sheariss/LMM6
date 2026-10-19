using System;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetEngine
    {
        private readonly Func<string, bool> canAccessApp;

        public WidgetContext Context { get; }

        public bool CanOpen =>
            Context.Definition.HasAssociatedApp &&
            canAccessApp(Context.Definition.AssociatedAppID);

        public WidgetEngine(
            WidgetContext context,
            Func<string, bool> canAccessApp)
        {
            Context = context ??
                throw new ArgumentNullException(nameof(context));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));
        }

        public bool TryGetAppID(out string appID)
        {
            appID = null;

            if (!CanOpen)
                return false;

            appID = Context.Definition.AssociatedAppID;
            return true;
        }
    }
}