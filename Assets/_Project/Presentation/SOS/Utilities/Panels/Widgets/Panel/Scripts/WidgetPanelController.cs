using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Panels;
using Atlas.SOS.Apps;
using Atlas.SOS.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetPanelController : PanelRuntime
    {
        [Header("Catalogs")]
        [SerializeField] private WidgetCatalog widgetCatalog;
        [SerializeField] private AppCatalog appCatalog;

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelTemplate;

        [Header("Initial Pinned Widgets")]
        [SerializeField]
        private List<string> defaultWidgetIDs = new();

        [Header("Shared App Data")]
        [SerializeField]
        private List<WidgetDataBinding> appDataBindings = new();

        private readonly List<WidgetController> widgets = new();

        private WidgetPanelEngine engine;
        private WidgetPanelView view;
        private WidgetContentViewBuilder contentBuilder;

        private Func<string, bool> canAccessApp;
        private Action<string> launchApp;

        private bool initialized;

        public override VisualElement Surface => view?.Binder.Root;

        public override void Initialize(
            VisualElement parent,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "WidgetPanelController must be active.");
            }

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            this.canAccessApp = canAccessApp ??
                throw new ArgumentNullException(nameof(canAccessApp));

            this.launchApp = launchApp ??
                throw new ArgumentNullException(nameof(launchApp));

            try
            {
                engine = new WidgetPanelEngine(
                    widgetCatalog,
                    appCatalog,
                    defaultWidgetIDs,
                    canAccessApp);

                contentBuilder = new WidgetContentViewBuilder(
                    appDataBindings);

                view = new WidgetPanelViewBuilder().Build(panelTemplate);

                parent.Add(view.Host);

                WidgetPanelBinder binder = view.Binder;

                binder.DiscoverButton.clicked += ShowDiscover;
                binder.WidgetsButton.clicked += ShowWidgets;
                binder.AddButton.clicked += OpenPicker;
                binder.ClosePickerButton.clicked += ClosePicker;
                binder.PinButton.clicked += ToggleSelectedPin;

                binder.Root.RegisterCallback<KeyDownEvent>(
                    OnKeyDown);

                initialized = true;

                view.SetState(0, 0, 0);
                view.RenderNavigation(engine);
                view.Hide();
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public override void Show()
        {
            if (!initialized)
            {
                throw new InvalidOperationException(
                    "The widget panel is not initialized.");
            }

            engine.ResetNavigation();

            RebuildWidgets();

            view.RenderNavigation(engine);
            view.Show();
        }

        public override void Hide()
        {
            engine?.ClosePicker();

            if (view != null)
            {
                if (engine != null)
                    view.RenderNavigation(engine);

                view.Hide();
            }
        }

        private void ShowDiscover()
        {
            if (!initialized)
                return;

            engine.Navigate(WidgetPanelPage.Discover);
            view.RenderNavigation(engine);
        }

        private void ShowWidgets()
        {
            if (!initialized)
                return;

            engine.Navigate(WidgetPanelPage.Widgets);
            view.RenderNavigation(engine);
        }

        private void OpenPicker()
        {
            if (!initialized)
                return;

            engine.OpenPicker();

            view.RenderNavigation(engine);
            view.RenderCatalog(engine, SelectWidget);

            view.Binder.ClosePickerButton.Focus();
        }

        private void ClosePicker()
        {
            if (!initialized)
                return;

            engine.ClosePicker();
            view.RenderNavigation(engine);
            view.Binder.AddButton.Focus();
        }

        private void SelectWidget(string widgetID)
        {
            if (!initialized)
                return;

            engine.Select(widgetID);
            view.RenderCatalog(engine, SelectWidget);

            // Selection rebuilds the catalog list.
            view.Binder.PinButton.Focus();
        }

        private void ToggleSelectedPin()
        {
            if (!initialized || !engine.ToggleSelectedPin())
                return;

            RebuildWidgets();
            view.RenderSelection(engine);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!initialized ||
                evt.keyCode != KeyCode.Escape ||
                !engine.PickerOpen)
            {
                return;
            }

            ClosePicker();
            evt.StopPropagation();
        }

        private void RebuildWidgets()
        {
            ClearWidgets();

            int failedCount = 0;
            float minimumWidth = 0;

            foreach (WidgetContext context in engine.GetVisiblePlacements())
            {
                WidgetController widget = null;

                try
                {
                    widget = new WidgetController(
                        context,
                        engine.GetIcon(context.Definition),
                        canAccessApp,
                        LaunchAssociatedApp,
                        contentBuilder);

                    view.Attach(widget.Root);
                    widgets.Add(widget);

                    minimumWidth = Mathf.Max(
                        minimumWidth,
                        context.Definition.Size.x + 12);
                }
                catch (Exception exception)
                {
                    widget?.Dispose();
                    failedCount++;

                    Debug.LogError(
                        $"[WidgetPanelController] Failed to create " +
                        $"'{context.Definition.WidgetID}'.",
                        this);

                    Debug.LogException(exception, this);
                }
            }

            view.SetState(
                widgets.Count,
                failedCount,
                minimumWidth);
        }

        private void ClearWidgets()
        {
            foreach (WidgetController widget in widgets)
                widget.Dispose();

            widgets.Clear();
            view?.ClearWidgets();
        }

        private void LaunchAssociatedApp(string appID)
        {
            if (!initialized ||
                string.IsNullOrWhiteSpace(appID) ||
                !canAccessApp(appID))
            {
                return;
            }

            launchApp(appID);

            if (initialized)
                RequestClose();
        }

        public override void Shutdown()
        {
            initialized = false;

            Hide();

            if (view != null)
            {
                WidgetPanelBinder binder = view.Binder;

                binder.DiscoverButton.clicked -= ShowDiscover;
                binder.WidgetsButton.clicked -= ShowWidgets;
                binder.AddButton.clicked -= OpenPicker;
                binder.ClosePickerButton.clicked -= ClosePicker;
                binder.PinButton.clicked -= ToggleSelectedPin;

                binder.Root.UnregisterCallback<KeyDownEvent>(
                    OnKeyDown);
            }

            ClearWidgets();
            view?.Detach();

            view = null;
            engine = null;
            contentBuilder = null;
            canAccessApp = null;
            launchApp = null;
        }

        private void OnDisable()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}