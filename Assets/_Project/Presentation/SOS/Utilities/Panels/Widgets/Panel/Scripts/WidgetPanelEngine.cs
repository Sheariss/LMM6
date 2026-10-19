using System;
using System.Collections.Generic;
using Atlas.SOS.Apps;
using Atlas.SOS.Widgets;
using UnityEngine;

namespace Atlas.Presentation.SOS.Widgets
{
    public enum WidgetPanelPage
    {
        Discover,
        Widgets
    }

    public sealed class WidgetPanelEngine
    {
        private readonly WidgetCatalog catalog;
        private readonly AppCatalog appCatalog;
        private readonly Func<string, bool> canAccessApp;

        private readonly List<WidgetContext> placements = new();

        public WidgetPanelPage Page { get; private set; } =
            WidgetPanelPage.Discover;

        public bool PickerOpen { get; private set; }
        public WidgetDefinition SelectedDefinition { get; private set; }

        public WidgetPanelEngine(
            WidgetCatalog widgetCatalog,
            AppCatalog appCatalog,
            IReadOnlyList<string> widgetIDs,
            Func<string, bool> canAccessApp)
        {
            catalog = widgetCatalog ??
                throw new ArgumentNullException(nameof(widgetCatalog));

            this.appCatalog = appCatalog ??
                throw new ArgumentNullException(nameof(appCatalog));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            if (widgetIDs == null)
                throw new ArgumentNullException(nameof(widgetIDs));

            ValidateCatalog();

            foreach (string widgetID in widgetIDs)
            {
                if (!catalog.TryGetWidget(
                        widgetID,
                        out WidgetDefinition definition))
                {
                    throw new InvalidOperationException(
                        $"Panel placement references missing widget '{widgetID}'.");
                }

                if (!definition.AllowMultipleInstances &&
                    GetCount(widgetID) > 0)
                {
                    throw new InvalidOperationException(
                        $"Widget '{widgetID}' cannot be placed more than once.");
                }

                placements.Add(new WidgetContext(definition));
            }
        }

        public void ResetNavigation()
        {
            Page = WidgetPanelPage.Discover;
            PickerOpen = false;
        }

        public void Navigate(WidgetPanelPage page)
        {
            Page = page;
            PickerOpen = false;
        }

        public void OpenPicker()
        {
            Page = WidgetPanelPage.Widgets;
            PickerOpen = true;

            List<WidgetDefinition> available = GetAvailableDefinitions();

            if (SelectedDefinition != null &&
                available.Contains(SelectedDefinition))
            {
                return;
            }

            SelectedDefinition =
                available.Count > 0 ? available[0] : null;
        }

        public void ClosePicker()
        {
            PickerOpen = false;
        }

        public void Select(string widgetID)
        {
            if (catalog.TryGetWidget(
                    widgetID,
                    out WidgetDefinition definition) &&
                CanAccess(definition))
            {
                SelectedDefinition = definition;
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
                int comparison = string.Compare(
                    a.DisplayName,
                    b.DisplayName,
                    StringComparison.OrdinalIgnoreCase);

                return comparison != 0
                    ? comparison
                    : string.CompareOrdinal(a.WidgetID, b.WidgetID);
            });

            return result;
        }

        public List<WidgetContext> GetVisiblePlacements()
        {
            var result = new List<WidgetContext>();

            foreach (WidgetContext placement in placements)
            {
                if (CanAccess(placement.Definition))
                    result.Add(placement);
            }

            return result;
        }

        public int GetCount(string widgetID)
        {
            int count = 0;

            foreach (WidgetContext placement in placements)
            {
                if (placement.Definition.WidgetID == widgetID)
                    count++;
            }

            return count;
        }

        public bool ToggleSelectedPin()
        {
            WidgetDefinition definition = SelectedDefinition;

            if (definition == null || !CanAccess(definition))
                return false;

            if (GetCount(definition.WidgetID) > 0)
            {
                placements.RemoveAll(
                    placement =>
                        placement.Definition.WidgetID ==
                        definition.WidgetID);
            }
            else
            {
                placements.Add(new WidgetContext(definition));
            }

            return true;
        }

        public Sprite GetIcon(WidgetDefinition definition)
        {
            if (definition.Icon != null)
                return definition.Icon;

            if (definition.HasAssociatedApp &&
                appCatalog.TryGetApp(
                    definition.AssociatedAppID,
                    out AppDefinition app))
            {
                return app.AppIcon48;
            }

            return null;
        }

        private bool CanAccess(WidgetDefinition definition)
        {
            return definition != null &&
                   (!definition.HasAssociatedApp ||
                    canAccessApp(definition.AssociatedAppID));
        }

        private void ValidateCatalog()
        {
            if (catalog.Widgets == null)
            {
                throw new InvalidOperationException(
                    "The widget catalog list is missing.");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (WidgetDefinition definition in catalog.Widgets)
            {
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        "The widget catalog contains an empty entry.");
                }

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
                        $"Widget '{definition.WidgetID}' needs UXML.");
                }

                if (!ValidDimension(definition.Size.x) ||
                    !ValidDimension(definition.Size.y))
                {
                    throw new InvalidOperationException(
                        $"Widget '{definition.WidgetID}' has an invalid size.");
                }

                if (definition.HasAssociatedApp &&
                    !appCatalog.TryGetApp(
                        definition.AssociatedAppID,
                        out _))
                {
                    throw new InvalidOperationException(
                        $"Widget '{definition.WidgetID}' references " +
                        $"missing app '{definition.AssociatedAppID}'.");
                }
            }
        }

        private static bool ValidDimension(float value)
        {
            return value > 0 &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }
    }
}