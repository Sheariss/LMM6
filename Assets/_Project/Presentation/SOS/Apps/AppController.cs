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

    // Implement this on your adapted WindowController.
    public interface IAppWindow : IDisposable
    {
        bool IsFocused { get; }
        bool IsMinimized { get; }

        // Activate must restore if minimized, then focus and bring forward.
        void Activate();
        void Minimize();

        event Action Closed;
        event Action StateChanged;
    }

    public sealed class AppController : MonoBehaviour
    {
        [Header("App Catalog")]
        [SerializeField] private AppCatalog appCatalog;

        private readonly Dictionary<string, List<AppInstance>>
            instances = new(StringComparer.Ordinal);

        private VisualElement windowLayer;

        // Initializes window behavior only.
        // AppController separately creates the app-specific runtime controller.
        private Func<AppDefinition, TemplateContainer, IAppWindow>
            initializeWindow;

        private Func<string, bool> canAccessApp;

        public event Action<string, int, bool> AppStateChanged;

        // A taskbar/controller can subscribe and display a window chooser.
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
        }

        private long activationCounter;

        public void Initialize(
            VisualElement windowLayer,
            Func<AppDefinition, TemplateContainer, IAppWindow> initializeWindow,
            Func<string, bool> canAccessApp)
        {
            Clear();

            this.windowLayer = windowLayer
                ?? throw new ArgumentNullException(nameof(windowLayer));

            this.initializeWindow = initializeWindow
                ?? throw new ArgumentNullException(nameof(initializeWindow));

            this.canAccessApp = canAccessApp
                ?? throw new ArgumentNullException(nameof(canAccessApp));
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
                // This UXML already contains the window AND app content.
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

                // Position the complete instance within the desktop.
                root.style.position = Position.Absolute;

                int offset = (GetTotalInstanceCount() % 8) * 24;

                root.style.left = 32 + offset;
                root.style.top = 32 + offset;

                root.style.width = app.DefaultWindowSize.x;
                root.style.height = app.DefaultWindowSize.y;

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

                // Initialize this instance's window behavior.
                // This initializer must also apply app.IsResizable.
                window = initializeWindow(app, root);

                if (window == null)
                {
                    throw new InvalidOperationException(
                        "Window initializer returned null.");
                }

                // Instantiate the runtime prefab from this catalog entry.
                contentController =
                    Instantiate(app.RuntimePrefab, transform);

                contentController.gameObject.name =
                    $"{app.AppID}Runtime";

                // Bind app behavior to this instance's content subtree.
                contentController.Initialize(content);

                instance = new AppInstance
                {
                    AppID = app.AppID,
                    Root = root,
                    Window = window,
                    ContentController = contentController
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

                Activate(instance);
            }
            catch (Exception exception)
            {
                if (instance != null)
                {
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

                CleanupRuntime(window, contentController, root);

                Debug.LogException(exception, this);
                PublishState(app.AppID);
            }
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
                    // Destroy the whole runtime prefab instance.
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

        private int GetTotalInstanceCount()
        {
            int count = 0;

            foreach (List<AppInstance> running in instances.Values)
                count += running.Count;

            return count;
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
            Clear();

            windowLayer = null;
            initializeWindow = null;
            canAccessApp = null;
        }
    }
}