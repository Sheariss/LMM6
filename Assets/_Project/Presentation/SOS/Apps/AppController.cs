using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.FileExplorer;
using Atlas.SOS.Apps;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Apps
{
    public enum AppLaunchIntent
    {
        Launch,
        TaskbarActivate,
        NewWindow
    }

    public interface IAppWindow : IDisposable
    {
        bool IsFocused { get; }
        bool IsMinimized { get; }

        void Activate();
        void Minimize();
        void Close();

        event Action Closed;
        event Action StateChanged;
    }

    public sealed class AppController : MonoBehaviour, IFileAppLauncher
    {
        private const float CascadeOffset = 24f;
        private const int MaximumCascadeSlots = 8;

        [Header("App Catalog")]
        [SerializeField] private AppCatalog appCatalog;

        [Header("File Opening")]
        [SerializeField] private FileAssociationCatalog fileAssociations;

        private FileOpenRouter fileOpenRouter;

        private readonly Dictionary<string, List<AppInstance>>
            instances = new(StringComparer.Ordinal);

        private VisualElement windowLayer;

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

            if (appCatalog == null)
            {
                throw new InvalidOperationException(
                    "AppController requires an AppCatalog.");
            }

            if (fileAssociations == null)
            {
                throw new InvalidOperationException(
                    "AppController requires a FileAssociationCatalog.");
            }

            if (this.windowLayer != null)
            {
                this.windowLayer.UnregisterCallback<GeometryChangedEvent>(
                    OnWindowLayerGeometryChanged);
            }

            Clear();

            this.windowLayer = windowLayer;
            this.initializeWindow = initializeWindow;
            this.canAccessApp = canAccessApp;

            fileOpenRouter = new FileOpenRouter(fileAssociations, this);

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
                List<IAppWindow> windows = new(running.Count);

                foreach (AppInstance instance in running)
                    windows.Add(instance.Window);

                WindowSelectionRequested.Invoke(
                    appID,
                    windows.AsReadOnly());

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

        public IReadOnlyList<AppWindowPreview> GetWindowPreviews(string appID)
        {
            var result = new List<AppWindowPreview>();

            if (windowLayer == null ||
                canAccessApp == null ||
                appCatalog == null ||
                string.IsNullOrWhiteSpace(appID) ||
                !canAccessApp(appID) ||
                !appCatalog.TryGetApp(appID, out AppDefinition app) ||
                !instances.TryGetValue(appID, out List<AppInstance> running))
            {
                return result;
            }

            Sprite icon = app.AppIcon48 != null
                ? app.AppIcon48
                : app.AppIcon96;

            foreach (AppInstance instance in running)
            {
                string title = instance.ContentController != null
                    ? instance.ContentController.PreviewTitle
                    : null;

                if (string.IsNullOrWhiteSpace(title))
                    title = instance.Root.Q<Label>("WindowTitle")?.text;

                if (string.IsNullOrWhiteSpace(title))
                    title = app.DisplayName;

                uint revision = instance.ContentController != null
                    ? instance.ContentController.PreviewRevision
                    : 0;

                result.Add(new AppWindowPreview(
                    appID,
                    instance.Window,
                    title,
                    icon,
                    instance.SpawnOrder + 1,
                    revision,
                    instance.Root));
            }

            return result;
        }

        public IReadOnlyList<AppWindowPreview> GetAllWindowPreviews()
        {
            var result = new List<AppWindowPreview>();
            var appIDs = new List<string>(instances.Keys);

            foreach (string appID in appIDs)
                result.AddRange(GetWindowPreviews(appID));

            return result;
        }

        public bool TryActivateWindow(string appID, IAppWindow window)
        {
            if (!TryFindWindow(appID, window, out AppInstance instance))
                return false;

            Activate(instance);
            return true;
        }

        public bool TryCloseWindow(string appID, IAppWindow window)
        {
            if (!TryFindWindow(appID, window, out AppInstance instance))
                return false;

            instance.Window.Close();
            return true;
        }

        private bool TryFindWindow(
            string appID,
            IAppWindow window,
            out AppInstance instance)
        {
            instance = null;

            if (windowLayer == null ||
                canAccessApp == null ||
                window == null ||
                string.IsNullOrWhiteSpace(appID) ||
                !canAccessApp(appID) ||
                !instances.TryGetValue(appID, out List<AppInstance> running))
            {
                return false;
            }

            foreach (AppInstance candidate in running)
            {
                if (!ReferenceEquals(candidate.Window, window))
                    continue;

                instance = candidate;
                return true;
            }

            return false;
        }

        public bool TryOpenFile(
            string appId,
            FileOpenRequest request,
            out string error)
        {
            error = null;

            if (windowLayer == null ||
                initializeWindow == null ||
                canAccessApp == null ||
                appCatalog == null)
            {
                error = "The app system is not initialized.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(appId))
            {
                error = "No destination app ID was provided.";
                return false;
            }

            if (request?.File == null)
            {
                error = "No valid file-open request was provided.";
                return false;
            }

            if (!canAccessApp(appId))
            {
                error = $"The current user cannot access app '{appId}'.";
                return false;
            }

            if (!appCatalog.TryGetApp(appId, out AppDefinition app))
            {
                error = $"App '{appId}' does not exist in the app catalog.";
                return false;
            }

            if (app.RuntimePrefab == null)
            {
                error = $"App '{appId}' has no runtime prefab.";
                return false;
            }

            if (app.RuntimePrefab is not IFileContentReceiver)
            {
                error = $"App '{appId}' cannot receive filesystem content.";
                return false;
            }

            instances.TryGetValue(appId, out List<AppInstance> running);

            bool createWindow =
                app.AllowMultipleInstances ||
                running == null ||
                running.Count == 0;

            if (createWindow)
                return Spawn(app, request, out error) != null;

            AppInstance target = GetMostRecent(running);

            if (target.ContentController is not IFileContentReceiver receiver)
            {
                error = $"The running app '{appId}' cannot receive files.";
                return false;
            }

            try
            {
                if (!receiver.TryOpenFile(request, out error))
                {
                    if (string.IsNullOrWhiteSpace(error))
                    {
                        error =
                            $"App '{appId}' could not open " +
                            $"'{request.File.Name}'.";
                    }

                    return false;
                }

                Activate(target);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                Debug.LogException(exception, this);
                return false;
            }
        }

        private void Spawn(AppDefinition app)
        {
            Spawn(app, null, out _);
        }

        private AppInstance Spawn(
            AppDefinition app,
            FileOpenRequest request,
            out string error)
        {
            error = null;

            if (app == null)
            {
                error = "No app definition was provided.";
                Debug.LogError($"[AppController] {error}", this);
                return null;
            }

            if (app.AppUXML == null || app.RuntimePrefab == null)
            {
                error =
                    $"App '{app.AppID}' is missing its UXML " +
                    "or runtime prefab.";

                Debug.LogError($"[AppController] {error}", this);
                return null;
            }

            TemplateContainer root = null;
            IAppWindow window = null;
            AppContentController contentController = null;
            AppInstance instance = null;

            try
            {
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
                root.style.left = 0f;
                root.style.top = 0f;

                root.style.width = initialSize.x;
                root.style.height = initialSize.y;

                root.style.minWidth = app.MinimumWindowSize.x;
                root.style.minHeight = app.MinimumWindowSize.y;

                shell.style.width = Length.Percent(100f);
                shell.style.height = Length.Percent(100f);

                title.text = app.DisplayName;

                if (app.AppIcon48 != null)
                {
                    icon.style.backgroundImage =
                        new StyleBackground(app.AppIcon48);
                }

                windowLayer.Add(root);

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

                if (contentController is FileExplorerController explorer)
                {
                    if (fileOpenRouter == null)
                    {
                        throw new InvalidOperationException(
                            "The file-open router has not been initialized.");
                    }

                    explorer.ConfigureFileOpening(fileOpenRouter);
                }

                contentController.Initialize(content);

                if (request != null)
                {
                    if (contentController is not IFileContentReceiver receiver)
                    {
                        throw new InvalidOperationException(
                            $"App '{app.AppID}' cannot receive " +
                            "filesystem content.");
                    }

                    if (!receiver.TryOpenFile(
                        request,
                        out string openError))
                    {
                        throw new InvalidOperationException(
                            string.IsNullOrWhiteSpace(openError)
                                ? $"App '{app.AppID}' could not open " +
                                  $"'{request.File.Name}'."
                                : openError);
                    }
                }

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
                    app.AppID,
                    out List<AppInstance> running))
                {
                    running = new List<AppInstance>();
                    instances.Add(app.AppID, running);
                }

                running.Add(instance);
                spawnCounter++;

                TryPlaceWindow(instance);
                Activate(instance);

                return instance;
            }
            catch (Exception exception)
            {
                error = exception.Message;

                if (instance != null)
                {
                    instance.PlacementPending = false;

                    window.Closed -= instance.CloseHandler;
                    window.StateChanged -= instance.StateHandler;

                    if (instances.TryGetValue(
                        app.AppID,
                        out List<AppInstance> running))
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

                return null;
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

            float centeredLeft =
                Mathf.Max(0f, (bounds.width - width) * 0.5f);

            float centeredTop =
                Mathf.Max(0f, (bounds.height - height) * 0.5f);

            float availableHorizontalOffset =
                Mathf.Max(0f, bounds.width - width - centeredLeft);

            float availableVerticalOffset =
                Mathf.Max(0f, bounds.height - height - centeredTop);

            float availableOffset = Mathf.Min(
                availableHorizontalOffset,
                availableVerticalOffset);

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
            List<AppInstance> pending = new();

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
                instance.AppID,
                out List<AppInstance> running) ||
                !running.Remove(instance))
            {
                return;
            }

            if (running.Count == 0)
                instances.Remove(instance.AppID);

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
            List<string> appIDs = new(instances.Keys);

            foreach (string appID in appIDs)
                PublishState(appID);
        }

        private void PublishState(string appID)
        {
            int count = 0;
            bool focused = false;

            if (instances.TryGetValue(
                appID,
                out List<AppInstance> running))
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
            List<AppInstance> released = new();
            List<string> appIDs = new(instances.Keys);

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

            fileOpenRouter = null;
            windowLayer = null;
            initializeWindow = null;
            canAccessApp = null;
        }
    }
}