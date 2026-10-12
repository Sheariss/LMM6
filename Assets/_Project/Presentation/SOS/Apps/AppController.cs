using System;
using System.Collections.Generic;
using Atlas.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Apps
{
    public enum AppLaunchIntent
    {
        Launch,           // Desktop shortcut or Start menu app entry.
        TaskbarActivate,  // Ordinary taskbar click.
        NewWindow         // Explicit new-window action.
    }

    public interface IAppWindow : IDisposable
    {
        bool IsFocused { get; }
        bool IsMinimized { get; }

        // Restore if minimized, then focus and bring forward.
        void Activate();
        void Minimize();

        event Action Closed;
        event Action StateChanged;
    }

    public sealed class AppController : MonoBehaviour
    {
        private const float CascadeOffset = 24f;
        private const int MaximumCascadeSlots = 8;

        [Header("App Catalog")]
        [SerializeField] private AppCatalog appCatalog;

        private readonly Dictionary<string, List<AppInstance>>
            instances = new(StringComparer.Ordinal);

        private VisualElement windowLayer;

        // Initializes window behavior only.
        // AppController creates the app-specific runtime controller.
        private Func<AppDefinition, TemplateContainer, IAppWindow>
            initializeWindow;

        private Func<string, bool> canAccessApp;

        private long activationCounter;
        private long spawnCounter;

        public event Action<string, int, bool> AppStateChanged;

        public event Action<string, IReadOnlyList<IAppWindow>>
            WindowSelectionRequested;

        private sealed class AppInstance
        {
            public string AppID;
            public TemplateContainer Root;
            public IAppWindow Window;
            public AppContentController ContentController;

            public Action CloseHandler;
            public Action StateHandler;
            public long ActivationOrder;

            public Vector2 InitialWindowSize;
            public long SpawnOrder;
            public bool PlacementPending;
        }

        public void Initialize(
            VisualElement windowLayer,
            Func<AppDefinition, TemplateContainer, IAppWindow> initializeWindow,
            Func<string, bool> canAccessApp)
        {
            if (windowLayer == null)
                throw new ArgumentNullException(nameof(windowLayer));

            if (initializeWindow == null)
                throw new ArgumentNullException(nameof(initializeWindow));

            if (canAccessApp == null)
                throw new ArgumentNullException(nameof(canAccessApp));

            if (this.windowLayer != null)
            {
                this.windowLayer.UnregisterCallback<GeometryChangedEvent>(
                    OnWindowLayerGeometryChanged);
            }

            Clear();

            this.windowLayer = windowLayer;
            this.initializeWindow = initializeWindow;
            this.canAccessApp = canAccessApp;

            this.windowLayer.RegisterCallback<GeometryChangedEvent>(
                OnWindowLayerGeometryChanged);
        }

        public void Launch(string appID)
        {
            RequestApp(appID, AppLaunchIntent.Launch);
        }

        public void ActivateFromTaskbar(string appID)
        {
            RequestApp(appID, AppLaunchIntent.TaskbarActivate);
        }

        public void OpenNewWindow(string appID)
        {
            RequestApp(appID, AppLaunchIntent.NewWindow);
        }

        public void RequestApp(string appID, AppLaunchIntent intent)
        {
            if (windowLayer == null ||
                initializeWindow == null ||
                canAccessApp == null ||
                appCatalog == null ||
                string.IsNullOrWhiteSpace(appID))
            {
                return;
            }

            if (!canAccessApp(appID) ||
                !appCatalog.TryGetApp(appID, out AppDefinition app))
            {
                return;
            }

            instances.TryGetValue(appID, out List<AppInstance> running);

            if (running == null || running.Count == 0)
            {
                Spawn(app);
                return;
            }

            if (intent != AppLaunchIntent.TaskbarActivate)
            {
                if (app.AllowMultipleInstances)
                    Spawn(app);
                else
                    Activate(GetMostRecent(running));

                return;
            }

            if (running.Count > 1 &&
                WindowSelectionRequested != null)
            {
                var windows = new List<IAppWindow>(running.Count);

                foreach (AppInstance instance in running)
                    windows.Add(instance.Window);

                WindowSelectionRequested.Invoke(
                    appID, windows.AsReadOnly());

                return;
            }

            AppInstance target = GetMostRecent(running);

            if (running.Count == 1 &&
                target.Window.IsFocused &&
                !target.Window.IsMinimized)
            {
                target.Window.Minimize();
            }
            else
            {
                Activate(target);
            }
        }

        private void Spawn(AppDefinition app)
        {
            if (app.AppUXML == null || app.RuntimePrefab == null)
            {
                Debug.LogError(
                    $"[AppController] App '{app.AppID}' is missing " +
                    "its UXML or runtime prefab.",
                    this);

                return;
            }

            TemplateContainer root = null;
            IAppWindow window = null;
            AppContentController contentController = null;
            AppInstance instance = null;

            try
            {
                // This UXML contains the window and its app content.
                root = app.AppUXML.CloneTree();

                VisualElement shell =
                    root.Q<VisualElement>("WindowRoot");

                VisualElement content =
                    root.Q<VisualElement>("WindowContent");

                Label title =
                    root.Q<Label>("WindowTitle");

                VisualElement icon =
                    root.Q<VisualElement>("WindowIcon");

                if (shell == null ||
                    content == null ||
                    title == null ||
                    icon == null)
                {
                    throw new InvalidOperationException(
                        $"App '{app.AppID}' UXML is missing " +
                        "required window elements.");
                }

                // Use the effective initial size for centering.
                // Minimum dimensions can exceed the configured default.
                Vector2 initialSize = new(
                    Mathf.Max(
                        1f,
                        Mathf.Max(
                            app.DefaultWindowSize.x,
                            app.MinimumWindowSize.x)),
                    Mathf.Max(
                        1f,
                        Mathf.Max(
                            app.DefaultWindowSize.y,
                            app.MinimumWindowSize.y)));

                root.style.position = Position.Absolute;

                // Temporary position until desktop geometry is available.
                root.style.left = 0;
                root.style.top = 0;

                root.style.width = initialSize.x;
                root.style.height = initialSize.y;

                root.style.minWidth = app.MinimumWindowSize.x;
                root.style.minHeight = app.MinimumWindowSize.y;

                shell.style.width = Length.Percent(100);
                shell.style.height = Length.Percent(100);

                title.text = app.DisplayName;

                if (app.AppIcon48 != null)
                {
                    icon.style.backgroundImage =
                        new StyleBackground(app.AppIcon48);
                }

                windowLayer.Add(root);

                // This initializer must also apply app.IsResizable.
                window = initializeWindow(app, root);

                if (window == null)
                {
                    throw new InvalidOperationException(
                        "Window initializer returned null.");
                }

                contentController =
                    Instantiate(app.RuntimePrefab, transform);

                contentController.gameObject.name =
                    $"{app.AppID}Runtime";

                contentController.Initialize(content);

                instance = new AppInstance
                {
                    AppID = app.AppID,
                    Root = root,
                    Window = window,
                    ContentController = contentController,
                    InitialWindowSize = initialSize,
                    SpawnOrder = spawnCounter,
                    PlacementPending = true
                };

                instance.CloseHandler =
                    () => Remove(instance);

                instance.StateHandler =
                    () => OnWindowStateChanged(instance);

                window.Closed += instance.CloseHandler;
                window.StateChanged += instance.StateHandler;

                if (!instances.TryGetValue(
                        app.AppID, out List<AppInstance> running))
                {
                    running = new List<AppInstance>();
                    instances.Add(app.AppID, running);
                }

                running.Add(instance);
                spawnCounter++;

                // Position immediately when the desktop is already laid out.
                // Otherwise the geometry callback completes placement.
                TryPlaceWindow(instance);

                Activate(instance);
            }
            catch (Exception exception)
            {
                if (instance != null)
                {
                    instance.PlacementPending = false;

                    window.Closed -= instance.CloseHandler;
                    window.StateChanged -= instance.StateHandler;

                    if (instances.TryGetValue(
                            app.AppID, out List<AppInstance> running))
                    {
                        running.Remove(instance);

                        if (running.Count == 0)
                            instances.Remove(app.AppID);
                    }
                }

                if (instances.Count == 0)
                    spawnCounter = 0;

                CleanupRuntime(window, contentController, root);

                Debug.LogException(exception, this);
                PublishState(app.AppID);
            }
        }

        private void TryPlaceWindow(AppInstance instance)
        {
            if (!instance.PlacementPending ||
                windowLayer == null ||
                instance.Root.parent != windowLayer)
            {
                return;
            }

            Rect bounds = windowLayer.contentRect;

            if (!IsValidDimension(bounds.width) ||
                !IsValidDimension(bounds.height))
            {
                return;
            }

            float width = instance.InitialWindowSize.x;
            float height = instance.InitialWindowSize.y;

            // Oversized windows start at the desktop's top-left.
            float centeredLeft =
                Mathf.Max(0f, (bounds.width - width) * 0.5f);

            float centeredTop =
                Mathf.Max(0f, (bounds.height - height) * 0.5f);

            // Determine how many diagonal steps fit after centering.
            float availableHorizontalOffset =
                Mathf.Max(0f, bounds.width - width - centeredLeft);

            float availableVerticalOffset =
                Mathf.Max(0f, bounds.height - height - centeredTop);

            float availableOffset = Mathf.Min(
                availableHorizontalOffset,
                availableVerticalOffset);

            // Limit before converting to an integer.
            float maximumOffset =
                (MaximumCascadeSlots - 1) * CascadeOffset;

            int cascadeSlots = 1 + Mathf.FloorToInt(
                Mathf.Min(availableOffset, maximumOffset) /
                CascadeOffset);

            int cascadeStep =
                (int)(instance.SpawnOrder % cascadeSlots);

            float offset = cascadeStep * CascadeOffset;

            instance.Root.style.left =
                bounds.xMin + centeredLeft + offset;

            instance.Root.style.top =
                bounds.yMin + centeredTop + offset;

            instance.PlacementPending = false;
        }

        private static bool IsValidDimension(float value)
        {
            return value > 0f &&
                   !float.IsNaN(value) &&
                   !float.IsInfinity(value);
        }

        private void OnWindowLayerGeometryChanged(
            GeometryChangedEvent evt)
        {
            // Only complete initial placement.
            // Already placed or manually moved windows stay where they are.
            var pending = new List<AppInstance>();

            foreach (List<AppInstance> running in instances.Values)
            {
                foreach (AppInstance instance in running)
                {
                    if (instance.PlacementPending)
                        pending.Add(instance);
                }
            }

            foreach (AppInstance instance in pending)
                TryPlaceWindow(instance);
        }

        private void Activate(AppInstance instance)
        {
            instance.ActivationOrder = ++activationCounter;
            instance.Root.BringToFront();
            instance.Window.Activate();

            PublishAllStates();
        }

        private void OnWindowStateChanged(AppInstance instance)
        {
            if (instance.Window.IsFocused &&
                !instance.Window.IsMinimized)
            {
                instance.ActivationOrder = ++activationCounter;
                instance.Root.BringToFront();
            }

            PublishAllStates();
        }

        private static AppInstance GetMostRecent(
            List<AppInstance> running)
        {
            AppInstance result = running[0];

            foreach (AppInstance instance in running)
            {
                if (instance.ActivationOrder > result.ActivationOrder)
                    result = instance;
            }

            return result;
        }

        private void Remove(AppInstance instance)
        {
            if (!instances.TryGetValue(
                    instance.AppID, out List<AppInstance> running) ||
                !running.Remove(instance))
            {
                return;
            }

            if (running.Count == 0)
                instances.Remove(instance.AppID);

            // The next window starts centered after all windows close.
            if (instances.Count == 0)
                spawnCounter = 0;

            try
            {
                Release(instance);
            }
            finally
            {
                PublishState(instance.AppID);
            }
        }

        private static void Release(AppInstance instance)
        {
            instance.PlacementPending = false;

            instance.Window.Closed -= instance.CloseHandler;
            instance.Window.StateChanged -= instance.StateHandler;

            CleanupRuntime(
                instance.Window,
                instance.ContentController,
                instance.Root);
        }

        private static void CleanupRuntime(
            IAppWindow window,
            AppContentController contentController,
            TemplateContainer root)
        {
            // Release app callbacks before removing its UI.
            if (contentController != null)
            {
                try
                {
                    contentController.Shutdown();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    UnityEngine.Object.Destroy(
                        contentController.gameObject);
                }
            }

            try
            {
                window?.Dispose();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                root?.RemoveFromHierarchy();
            }
        }

        private void PublishAllStates()
        {
            // Snapshot keys because subscribers may close windows.
            var appIDs = new List<string>(instances.Keys);

            foreach (string appID in appIDs)
                PublishState(appID);
        }

        private void PublishState(string appID)
        {
            int count = 0;
            bool focused = false;

            if (instances.TryGetValue(
                    appID, out List<AppInstance> running))
            {
                count = running.Count;

                foreach (AppInstance instance in running)
                {
                    focused |= instance.Window.IsFocused &&
                               !instance.Window.IsMinimized;
                }
            }

            AppStateChanged?.Invoke(appID, count, focused);
        }

        public void Clear()
        {
            var released = new List<AppInstance>();
            var appIDs = new List<string>(instances.Keys);

            foreach (List<AppInstance> running in instances.Values)
                released.AddRange(running);

            instances.Clear();

            spawnCounter = 0;
            activationCounter = 0;

            foreach (AppInstance instance in released)
            {
                try
                {
                    Release(instance);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            foreach (string appID in appIDs)
                AppStateChanged?.Invoke(appID, 0, false);
        }

        private void OnDisable()
        {
            if (windowLayer != null)
            {
                windowLayer.UnregisterCallback<GeometryChangedEvent>(
                    OnWindowLayerGeometryChanged);
            }

            Clear();

            windowLayer = null;
            initializeWindow = null;
            canAccessApp = null;
        }
    }
}