using System;
using System.Collections.Generic;
using Atlas.SOS.Apps;
using Atlas.SOS.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Widgets
{
    public sealed class WidgetController : MonoBehaviour
    {
        [Header("Catalogs")]
        [SerializeField] private WidgetCatalog widgetCatalog;
        [SerializeField] private AppCatalog appCatalog;

        [Header("App Data")]
        [SerializeField]
        private List<WidgetDataBinding> appDataBindings = new();

        [Header("Panel")]
        [SerializeField] private VisualTreeAsset panelTemplate;

        [Header("Initial Layout")]
        [SerializeField] private List<string> defaultWidgetIDs = new();

        private readonly Dictionary<string, WidgetInstance> instances =
            new(StringComparer.Ordinal);

        private TemplateContainer panelHost;
        private Button toggleButton;

        private WidgetBinder binder;
        private WidgetEngine engine;
        private WidgetView view;
        private WidgetContentViewBuilder contentBuilder;

        private Action<string> launchApp;
        private bool initialized;
        private bool visible;

        public bool IsInitialized => initialized;

        public event Action<string, int> WidgetStateChanged;

        private sealed class WidgetInstance
        {
            public WidgetContext Context;
            public WidgetFrame Frame;
            public Action OpenHandler;
        }

        public void Initialize(
            VisualElement parent,
            Button toggleButton,
            Func<string, bool> canAccessApp,
            Action<string> launchApp)
        {
            Shutdown();

            if (!isActiveAndEnabled)
            {
                throw new InvalidOperationException(
                    "WidgetController must be active.");
            }

            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            if (toggleButton == null)
                throw new ArgumentNullException(nameof(toggleButton));

            if (canAccessApp == null)
                throw new ArgumentNullException(nameof(canAccessApp));

            if (launchApp == null)
                throw new ArgumentNullException(nameof(launchApp));

            if (panelTemplate == null)
            {
                throw new InvalidOperationException(
                    "Widget panel template is missing.");
            }

            try
            {
                engine = new WidgetEngine(
                    widgetCatalog,
                    appCatalog,
                    canAccessApp);

                contentBuilder = new WidgetContentViewBuilder(
                    appDataBindings);

                this.launchApp = launchApp;

                panelHost = panelTemplate.CloneTree();
                panelHost.name = "WidgetsPanelHost";
                panelHost.pickingMode = PickingMode.Ignore;

                panelHost.style.position = Position.Absolute;
                panelHost.style.left = 12;
                panelHost.style.right = 12;
                panelHost.style.top = 12;
                panelHost.style.bottom = 12;

                parent.Add(panelHost);

                binder = new WidgetBinder(panelHost);

                if (!binder.IsValid)
                {
                    throw new InvalidOperationException(
                        "Widget panel is missing required elements.");
                }

                view = new WidgetView(
                    binder,
                    new WidgetViewBuilder());

                this.toggleButton = toggleButton;

                this.toggleButton.clicked += Toggle;
                binder.CloseButton.clicked += Hide;
                binder.AddButton.clicked += TogglePicker;
                binder.ConfirmAddButton.clicked += AddSelected;

                binder.LayoutField.RegisterValueChangedCallback(
                    OnLayoutChanged);

                binder.Root.RegisterCallback<KeyDownEvent>(
                    OnKeyDown);

                initialized = true;

                Refresh();
                Hide();

                if (defaultWidgetIDs != null)
                {
                    foreach (string widgetID in defaultWidgetIDs)
                    {
                        if (!TryAdd(widgetID, out _, out string error))
                        {
                            Debug.LogWarning(
                                $"[WidgetController] {error}",
                                this);
                        }
                    }
                }
            }
            catch
            {
                Shutdown();
                throw;
            }
        }

        public void Toggle()
        {
            if (visible)
                Hide();
            else
                Show();
        }

        public void Show()
        {
            if (!initialized)
                return;

            Refresh();

            visible = true;
            panelHost.style.display = DisplayStyle.Flex;
            panelHost.BringToFront();

            binder.CloseButton.Focus();
        }

        public void Hide()
        {
            visible = false;

            if (panelHost != null)
                panelHost.style.display = DisplayStyle.None;

            view?.ShowPicker(false);
        }

        public bool TryAdd(
            string widgetID,
            out string instanceID,
            out string error)
        {
            instanceID = null;

            if (!initialized)
            {
                error = "The widget system is not initialized.";
                return false;
            }

            if (!engine.TryAdd(
                    widgetID,
                    out WidgetContext context,
                    out error))
            {
                return false;
            }

            var instance = new WidgetInstance
            {
                Context = context
            };

            try
            {
                Sprite icon = context.Definition.Icon;

                if (icon == null &&
                    appCatalog.TryGetApp(
                        context.Definition.AssociatedAppID,
                        out AppDefinition app))
                {
                    icon = app.AppIcon48;
                }

                instance.Frame = view.BuildFrame(context, icon);

                contentBuilder.Populate(
                    context,
                    instance.Frame.Content);

                instance.OpenHandler =
                    () => OpenAssociatedApp(context.InstanceID);

                instance.Frame.OpenButton.clicked +=
                    instance.OpenHandler;

                view.Attach(instance.Frame);
                instances.Add(context.InstanceID, instance);
            }
            catch (Exception exception)
            {
                instances.Remove(context.InstanceID);
                engine.Remove(context.InstanceID);

                Release(instance);

                error = exception.Message;
                Debug.LogException(exception, this);

                Refresh();
                return false;
            }

            instanceID = context.InstanceID;
            error = null;

            Refresh();
            PublishState(context.Definition.WidgetID);

            return true;
        }

        public bool Remove(string instanceID)
        {
            if (!initialized ||
                string.IsNullOrEmpty(instanceID) ||
                !instances.TryGetValue(
                    instanceID,
                    out WidgetInstance instance))
            {
                return false;
            }

            instances.Remove(instanceID);
            engine.Remove(instanceID);

            Release(instance);
            Refresh();

            PublishState(instance.Context.Definition.WidgetID);

            return true;
        }

        public void Clear()
        {
            var released =
                new List<WidgetInstance>(instances.Values);

            var widgetIDs =
                new HashSet<string>(StringComparer.Ordinal);

            instances.Clear();
            engine?.Clear();

            foreach (WidgetInstance instance in released)
            {
                widgetIDs.Add(
                    instance.Context.Definition.WidgetID);

                Release(instance);
            }

            if (initialized)
                Refresh();

            foreach (string widgetID in widgetIDs)
                PublishState(widgetID);
        }

        public void Shutdown()
        {
            initialized = false;
            Hide();

            if (toggleButton != null)
                toggleButton.clicked -= Toggle;

            if (binder != null)
            {
                if (binder.CloseButton != null)
                    binder.CloseButton.clicked -= Hide;

                if (binder.AddButton != null)
                    binder.AddButton.clicked -= TogglePicker;

                if (binder.ConfirmAddButton != null)
                    binder.ConfirmAddButton.clicked -= AddSelected;

                if (binder.LayoutField != null)
                {
                    binder.LayoutField.UnregisterValueChangedCallback(
                        OnLayoutChanged);
                }

                if (binder.Root != null)
                {
                    binder.Root.UnregisterCallback<KeyDownEvent>(
                        OnKeyDown);
                }
            }

            Clear();

            view?.Clear();
            panelHost?.RemoveFromHierarchy();

            panelHost = null;
            toggleButton = null;
            binder = null;
            engine = null;
            view = null;
            contentBuilder = null;
            launchApp = null;
        }

        private void TogglePicker()
        {
            if (!initialized)
                return;

            Refresh();
            view.ShowPicker(!view.PickerVisible);
        }

        private void AddSelected()
        {
            if (!initialized)
                return;

            WidgetDefinition definition =
                view.SelectedDefinition;

            if (definition == null)
                return;

            if (TryAdd(
                    definition.WidgetID,
                    out _,
                    out string error))
            {
                view.SetStatus(null);
                view.ShowPicker(false);
            }
            else
            {
                view.SetStatus(error);
            }
        }

        private void OpenAssociatedApp(string instanceID)
        {
            if (!initialized ||
                !engine.TryGetInstance(
                    instanceID,
                    out WidgetContext context) ||
                !engine.CanAccess(context.Definition))
            {
                return;
            }

            launchApp(context.Definition.AssociatedAppID);
            Hide();
        }

        private void OnLayoutChanged(ChangeEvent<string> evt)
        {
            if (initialized)
                RefreshSelection();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (!initialized ||
                evt.keyCode != KeyCode.Escape)
            {
                return;
            }

            if (view.PickerVisible)
                view.ShowPicker(false);
            else
                Hide();

            evt.StopPropagation();
        }

        private void Refresh()
        {
            if (view == null || engine == null)
                return;

            view.SetCatalog(
                engine.GetAvailableDefinitions());

            view.RefreshInstances(
                engine.Instances);

            RefreshSelection();
        }

        private void RefreshSelection()
        {
            WidgetDefinition definition =
                view.SelectedDefinition;

            string reason = null;

            bool canAdd = definition != null &&
                engine.CanAdd(
                    definition.WidgetID,
                    out reason);

            view.SetSelection(canAdd, reason);
        }

        private void PublishState(string widgetID)
        {
            Action<string, int> handlers =
                WidgetStateChanged;

            if (handlers == null)
                return;

            int count = engine?.GetCount(widgetID) ?? 0;

            foreach (
                Action<string, int> handler
                in handlers.GetInvocationList())
            {
                try
                {
                    handler(widgetID, count);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }
        }

        private static void Release(WidgetInstance instance)
        {
            if (instance.Frame != null &&
                instance.OpenHandler != null)
            {
                instance.Frame.OpenButton.clicked -=
                    instance.OpenHandler;
            }

            instance.Frame?.Root.RemoveFromHierarchy();
        }

        private void OnDisable()
        {
            Shutdown();
        }
    }
}