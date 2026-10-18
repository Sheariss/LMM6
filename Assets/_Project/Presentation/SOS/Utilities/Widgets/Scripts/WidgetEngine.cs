using System;
using System.Collections.Generic;
using Atlas.SOS.Apps;
using Atlas.SOS.Widgets;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetEngine
    {
        private readonly WidgetCatalog catalog;
        private readonly Func<string, bool> canAccessApp;

        private readonly List<WidgetContext> instances = new();
        private readonly IReadOnlyList<WidgetContext> readOnlyInstances;

        public IReadOnlyList<WidgetContext> Instances => readOnlyInstances;

        public WidgetEngine(
            WidgetCatalog catalog,
            AppCatalog appCatalog,
            Func<string, bool> canAccessApp)
        {
            this.catalog = catalog ??
                throw new ArgumentNullException(nameof(catalog));

            if (appCatalog == null)
                throw new ArgumentNullException(nameof(appCatalog));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            readOnlyInstances = instances.AsReadOnly();

            if (catalog.Widgets == null)
                throw new InvalidOperationException("Widget list is missing.");

            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (WidgetDefinition definition in catalog.Widgets)
            {
                if (definition == null)
                    throw new InvalidOperationException(
                        "The widget catalog contains an empty entry.");

                if (string.IsNullOrWhiteSpace(definition.WidgetID) ||
                    !ids.Add(definition.WidgetID))
                {
                    throw new InvalidOperationException(
                        "Widget IDs must be nonempty and unique.");
                }

                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                {
                    throw new InvalidOperationException(
                        $"Widget '{definition.WidgetID}' needs a display name.");
                }

                if (definition.WidgetUXML == null)
                {
                    throw new InvalidOperationException(
                        $"Widget '{definition.WidgetID}' needs its UXML.");
                }

                if (!ValidDimension(definition.Size.x) ||
                    !ValidDimension(definition.Size.y))
                {
                    throw new InvalidOperationException(
                        $"Widget '{definition.WidgetID}' has an invalid size.");
                }

                if (!definition.HasAssociatedApp ||
                    !appCatalog.TryGetApp(definition.AssociatedAppID, out _))
                {
                    throw new InvalidOperationException(
                        $"Widget '{definition.WidgetID}' needs a valid " +
                        "associated app ID.");
                }
            }
        }

        public List<WidgetDefinition> GetAvailableDefinitions()
        {
            var result = new List<WidgetDefinition>();

            foreach (WidgetDefinition definition in catalog.Widgets)
            {
                if (CanAccess(definition))
                    result.Add(definition);
            }

            result.Sort((a, b) =>
            {
                int result = string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase);

                return result != 0
                    ? result
                    : string.CompareOrdinal(a.WidgetID, b.WidgetID);
            });

            return result;
        }

        public bool CanAccess(WidgetDefinition definition)
        {
            return definition != null &&
                definition.HasAssociatedApp &&
                canAccessApp(definition.AssociatedAppID);
        }

        public bool CanAdd(string widgetID, out string error)
        {
            if (!catalog.TryGetWidget(widgetID, out WidgetDefinition definition))
            {
                error = $"Widget '{widgetID}' was not found.";
                return false;
            }

            if (!CanAccess(definition))
            {
                error = "This widget is unavailable to the current user.";
                return false;
            }

            if (!definition.AllowMultipleInstances &&
                GetCount(widgetID) > 0)
            {
                error = "This layout is already added.";
                return false;
            }

            error = null;
            return true;
        }

        public bool TryAdd(
            string widgetID,
            out WidgetContext context,
            out string error)
        {
            context = null;

            if (!CanAdd(widgetID, out error))
                return false;

            catalog.TryGetWidget(widgetID, out WidgetDefinition definition);

            context = new WidgetContext(definition);
            instances.Add(context);

            return true;
        }

        public bool TryGetInstance(
            string instanceID,
            out WidgetContext context)
        {
            foreach (WidgetContext instance in instances)
            {
                if (instance.InstanceID == instanceID)
                {
                    context = instance;
                    return true;
                }
            }

            context = null;
            return false;
        }

        public bool Remove(string instanceID)
        {
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i].InstanceID != instanceID)
                    continue;

                instances.RemoveAt(i);
                return true;
            }

            return false;
        }

        public int GetCount(string widgetID)
        {
            int count = 0;

            foreach (WidgetContext instance in instances)
            {
                if (instance.Definition.WidgetID == widgetID)
                    count++;
            }

            return count;
        }

        public void Clear()
        {
            instances.Clear();
        }

        private static bool ValidDimension(float value)
        {
            return value > 0f &&
                !float.IsNaN(value) &&
                !float.IsInfinity(value);
        }
    }
}