using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using Atlas.Presentation.SOS.Panels;
using Atlas.Presentation.SOS.Taskbar;
using Atlas.Presentation.SOS.Windows;
using Atlas.SOS.Apps;
using Atlas.SOS.Authentication;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.Desktop
{
    public sealed class DesktopController : MonoBehaviour
    {
        [Header("Desktop")]
        private UIDocument desktopDocument;
        [SerializeField] private AppCatalog appCatalog;

        [Header("App Launcher")]
        [SerializeField] private AppController appController;

        [Header("Shortcuts")]
        [SerializeField] private VisualTreeAsset shortcutTemplate;

        [SerializeField]
        private Vector2 shortcutSize = new(128f, 128f);

        [SerializeField]
        private Vector2 shortcutGap = new(16f, 16f);

        [SerializeField]
        private Vector2 shortcutIconSize = new(48f, 48f);

        [SerializeField, Min(1f)]
        private float shortcutLabelFontSize = 14f;

        [Header("Taskbar")]
        [SerializeField] private VisualTreeAsset pinnedButtonTemplate;

        [Header("Taskbar Previews")]
        [SerializeField] private VisualTreeAsset taskbarPreviewTemplate;

        [SerializeField]
        private WindowPreviewCaptureService previewCaptureService;

        [Header("Start Menu")]
        [SerializeField] private VisualTreeAsset startMenuTemplate;
        [SerializeField] private FileSystemLibrary fileSystemLibrary;
        [SerializeField] private FileAssociationCatalog fileAssociationCatalog;

        [Header("Panels")]
        [SerializeField] private PanelController panelController;

        private DesktopBinder binder;
        private DesktopView view;

        private DesktopShortcutGridView gridView;
        private DesktopShortcutGridController gridController;
        private DesktopShortcutController shortcutController;
        private TaskbarPinnedButtonController pinnedButtonController;

        private readonly List<AppDefinition> availableApps = new();
        private readonly List<AppDefinition> pinnedApps = new();

        private TemplateContainer startMenuHost;
        private StartMenuController startMenuController;

        private VisualElement previewLayer;
        private TaskbarPreviewController taskbarPreviewController;

        private bool desktopVisible;

        public UserInfo ActiveUser { get; private set; }
        public bool IsPrepared { get; private set; }

        public IReadOnlyList<AppDefinition> AvailableApps => availableApps;
        public IReadOnlyList<AppDefinition> PinnedApps => pinnedApps;

        public bool IsPreparedForCurrentUser =>
            IsPrepared &&
            ValidateUser(ActiveUser, out _) &&
            IsDocumentCurrent();

        private void OnEnable()
        {
            if (!TryBindDocument(out string error))
                Debug.LogError($"[DesktopController] {error}", this);
        }

        private void OnDisable()
        {
            Clear();
        }

        public bool Prepare(UserInfo user, out string error)
        {
            Clear();

            if (!isActiveAndEnabled)
            {
                error = "The desktop controller is not active.";
                return false;
            }

            if (!TryBindDocument(out error))
                return false;

            if (!ValidateUser(user, out error))
                return false;

            if (appCatalog == null ||
                shortcutTemplate == null ||
                pinnedButtonTemplate == null ||
                taskbarPreviewTemplate == null ||
                startMenuTemplate == null ||
                fileSystemLibrary == null ||
                fileAssociationCatalog == null ||
                appController == null ||
                !appController.isActiveAndEnabled ||
                panelController == null ||
                !panelController.isActiveAndEnabled ||
                previewCaptureService == null ||
                !previewCaptureService.isActiveAndEnabled)
            {
                error =
                    "A required catalog, UI template, app controller, " +
                    "panel controller, or preview capture service is missing.";

                return false;
            }

            if (user.Wallpaper == null)
            {
                error = "This user does not have a desktop wallpaper.";
                return false;
            }

            try
            {
                appController.Initialize(
                    binder.WindowLayer,
                    InitializeAppWindow,
                    appID => IsPreparedForCurrentUser &&
                        ActiveUser.CanAccessApp(appID));

                appController.AppStateChanged += OnAppStateChanged;

                var allowedIDs = new HashSet<string>(
                    user.AvailableAppIDs,
                    StringComparer.Ordinal);

                availableApps.AddRange(
                    ResolveApps(user.AvailableAppIDs, allowedIDs));

                List<AppDefinition> shortcutApps = ResolveApps(
                    user.DesktopShortcutAppIDs,
                    allowedIDs);

                pinnedApps.AddRange(
                    ResolveApps(user.PinnedAppIDs, allowedIDs));

                view.SetWallpaper(user.Wallpaper);

                gridView = new DesktopShortcutGridView(
                    binder.ShortcutGrid,
                    shortcutSize,
                    shortcutGap);

                gridController =
                    new DesktopShortcutGridController(gridView);

                shortcutController = new DesktopShortcutController(
                    shortcutTemplate,
                    gridController,
                    RequestAppLaunch,
                    shortcutIconSize,
                    shortcutLabelFontSize);

                shortcutController.Build(shortcutApps);

                var taskbarBinder = new TaskbarBinder(binder.Taskbar);

                if (!taskbarBinder.IsValid)
                {
                    throw new InvalidOperationException(
                        "The taskbar visual tree is missing required elements.");
                }

                pinnedButtonController = new TaskbarPinnedButtonController(
                    pinnedButtonTemplate,
                    taskbarBinder.AppGroup,
                    RequestTaskbarActivation);

                pinnedButtonController.Build(
                    availableApps,
                    pinnedApps);

                panelController.Initialize(
                    binder.DocumentRoot,
                    binder.PanelLayer,
                    () => IsPreparedForCurrentUser,
                    appID => IsPreparedForCurrentUser &&
                        ActiveUser.CanAccessApp(appID),
                    RequestAppLaunch);

                panelController.BindButton(
                    DesktopPanelID.Widgets,
                    taskbarBinder.WidgetsButton);

                panelController.BindButton(
                    DesktopPanelID.Network,
                    taskbarBinder.NetworkButton);

                panelController.BindButton(
                    DesktopPanelID.Audio,
                    taskbarBinder.VolumeButton);

                panelController.BindButton(
                    DesktopPanelID.DateTime,
                    taskbarBinder.DateTimeButton);

                startMenuHost = startMenuTemplate.CloneTree();
                startMenuHost.name = "StartMenuHost";
                startMenuHost.pickingMode = PickingMode.Ignore;

                startMenuHost.style.position = Position.Absolute;
                startMenuHost.style.left = 0;
                startMenuHost.style.right = 0;
                startMenuHost.style.top = 0;
                startMenuHost.style.bottom = 0;
                startMenuHost.style.alignItems = Align.Center;

                binder.Workspace.Add(startMenuHost);
                startMenuHost.BringToFront();

                var startMenuBinder = new StartMenuBinder(startMenuHost);

                var startMenuEngine = new StartMenuEngine(
                    user,
                    appCatalog,
                    fileSystemLibrary,
                    fileAssociationCatalog);

                var startMenuView = new StartMenuView(
                    startMenuBinder,
                    new StartMenuViewBuilder());

                var startMenuRouter = new FileOpenRouter(
                    fileAssociationCatalog,
                    appController);

                startMenuController = new StartMenuController(
                    startMenuBinder,
                    startMenuEngine,
                    startMenuView,
                    startMenuRouter,
                    binder.DocumentRoot,
                    taskbarBinder.StartButton,
                    () => IsPreparedForCurrentUser,
                    RequestAppLaunch);

                if (desktopDocument.panelSettings == null ||
                    desktopDocument.panelSettings.targetTexture != null ||
                    desktopDocument.panelSettings.targetDisplay != 0)
                {
                    throw new InvalidOperationException(
                        "Preview capture requires a screen-space desktop " +
                        "on display 0.");
                }

                previewLayer = new VisualElement
                {
                    name = "PreviewLayer",
                    pickingMode = PickingMode.Ignore
                };

                previewLayer.style.position = Position.Absolute;
                previewLayer.style.left = 0;
                previewLayer.style.right = 0;
                previewLayer.style.top = 0;
                previewLayer.style.bottom = 0;

                binder.PanelLayer.Add(previewLayer);

                taskbarPreviewController = new TaskbarPreviewController(
                    appController,
                    previewCaptureService,
                    binder.DocumentRoot,
                    previewLayer,
                    taskbarPreviewTemplate,
                    pinnedButtonController,
                    availableApps,
                    () => IsPreparedForCurrentUser &&
                        desktopVisible &&
                        !panelController.HasOpenPanel &&
                        !(startMenuController?.IsOpen ?? false));

                panelController.PanelOpening +=
                    taskbarPreviewController.Hide;

                previewCaptureService.Initialize(
                    appController,
                    () => IsPreparedForCurrentUser &&
                        desktopVisible &&
                        !panelController.HasOpenPanel &&
                        !(startMenuController?.IsOpen ?? false) &&
                        !(taskbarPreviewController?.IsVisible ?? false));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Clear();

                error = "The desktop UI could not be prepared.";
                return false;
            }

            if (!ValidateUser(user, out error))
            {
                Clear();
                return false;
            }

            if (!IsDocumentCurrent())
            {
                Clear();
                error = "The desktop document changed during preparation.";
                return false;
            }

            ActiveUser = user;
            IsPrepared = true;
            error = null;

            return true;
        }

        public bool Show(UserInfo user, out string error)
        {
            if (!IsPrepared || !ReferenceEquals(ActiveUser, user))
            {
                error = "The desktop is not prepared for this user.";
                Hide();
                return false;
            }

            if (!ValidateUser(user, out error))
            {
                Clear();
                return false;
            }

            if (!IsDocumentCurrent())
            {
                Clear();
                error = "The desktop document is no longer current.";
                return false;
            }

            view.Show();
            desktopVisible = true;

            error = null;
            return true;
        }

        public void Hide()
        {
            desktopVisible = false;
            taskbarPreviewController?.Hide();

            panelController?.Close();
            startMenuController?.Close();
            view?.Hide();

            HideCurrentDocumentRoot();
        }

        public void Clear()
        {
            desktopVisible = false;

            if (taskbarPreviewController != null)
            {
                if (panelController != null)
                {
                    panelController.PanelOpening -=
                        taskbarPreviewController.Hide;
                }

                taskbarPreviewController.Dispose();
                taskbarPreviewController = null;
            }

            previewCaptureService?.Shutdown();

            previewLayer?.RemoveFromHierarchy();
            previewLayer = null;

            panelController?.Shutdown();

            startMenuController?.Dispose();
            startMenuController = null;

            startMenuHost?.RemoveFromHierarchy();
            startMenuHost = null;

            if (appController != null)
            {
                appController.AppStateChanged -= OnAppStateChanged;
                appController.Clear();
            }

            IsPrepared = false;
            Hide();

            shortcutController?.Dispose();
            shortcutController = null;

            gridController?.Dispose();
            gridController = null;

            gridView?.Dispose();
            gridView = null;

            view?.ClearWallpaper();

            pinnedButtonController?.Dispose();
            pinnedButtonController = null;

            availableApps.Clear();
            pinnedApps.Clear();

            ActiveUser = null;
            view = null;
            binder = null;
        }

        private bool TryBindDocument(out string error)
        {
            view?.Hide();

            binder = null;
            view = null;

            desktopDocument = GetComponentInParent<UIDocument>();

            if (desktopDocument == null)
            {
                error =
                    "A desktop UIDocument was not found " +
                    "in the parent hierarchy.";

                return false;
            }

            if (!desktopDocument.isActiveAndEnabled)
            {
                error = "The desktop UIDocument is disabled.";
                return false;
            }

            VisualElement documentRoot =
                desktopDocument.rootVisualElement;

            VisualElement desktopRoot =
                documentRoot?.Q<VisualElement>("DesktopRoot");

            if (desktopRoot == null)
            {
                error = "The UIDocument does not contain DesktopRoot.";
                return false;
            }

            desktopRoot.SetEnabled(false);
            desktopRoot.style.visibility = Visibility.Hidden;

            binder = new DesktopBinder(documentRoot);

            if (!binder.IsValid)
            {
                error =
                    "The desktop visual tree is missing required elements.";

                return false;
            }

            view = new DesktopView(binder);
            view.Hide();

            binder.Wallpaper.pickingMode = PickingMode.Ignore;
            binder.WindowLayer.pickingMode = PickingMode.Ignore;

            error = null;
            return true;
        }

        private bool IsDocumentCurrent()
        {
            return desktopDocument != null &&
                desktopDocument.isActiveAndEnabled &&
                binder != null &&
                binder.IsValid &&
                binder.DocumentRoot == desktopDocument.rootVisualElement &&
                binder.Root ==
                    desktopDocument.rootVisualElement
                        .Q<VisualElement>("DesktopRoot") &&
                binder.Root.panel != null;
        }

        private static bool ValidateUser(
            UserInfo user,
            out string error)
        {
            AuthenticationManager authentication =
                AuthenticationManager.Instance;

            if (authentication == null ||
                !authentication.IsSignedIn ||
                user == null ||
                !ReferenceEquals(authentication.CurrentUser, user))
            {
                error = "The authenticated user changed or signed out.";
                return false;
            }

            if (authentication.UserLibrary == null ||
                !ReferenceEquals(
                    authentication.UserLibrary.GetUser(user.UserId),
                    user))
            {
                error = "The current user is missing from the user library.";
                return false;
            }

            error = null;
            return true;
        }

        private List<AppDefinition> ResolveApps(
            IReadOnlyList<string> requestedIDs,
            HashSet<string> allowedIDs)
        {
            var result = new List<AppDefinition>();

            var addedIDs = new HashSet<string>(
                StringComparer.Ordinal);

            foreach (string appID in requestedIDs)
            {
                if (string.IsNullOrWhiteSpace(appID) ||
                    !addedIDs.Add(appID))
                {
                    continue;
                }

                if (!allowedIDs.Contains(appID))
                {
                    Debug.LogWarning(
                        $"[DesktopController] App '{appID}' is listed " +
                        "for placement but is unavailable to this user.",
                        this);

                    continue;
                }

                if (!appCatalog.TryGetApp(
                    appID,
                    out AppDefinition app))
                {
                    Debug.LogWarning(
                        $"[DesktopController] App '{appID}' " +
                        "was not found in the app catalog.",
                        this);

                    continue;
                }

                result.Add(app);
            }

            return result;
        }

        private IAppWindow InitializeAppWindow(
            AppDefinition app,
            TemplateContainer root)
        {
            var windowObject = new GameObject(
                $"{app.AppID}Window");

            windowObject.transform.SetParent(
                appController.transform,
                false);

            var window = windowObject.AddComponent<WindowController>();

            try
            {
                window.Initialize(root, app.IsResizable);
                return window;
            }
            catch
            {
                Destroy(windowObject);
                throw;
            }
        }

        private bool CanRequestApp(string appID)
        {
            if (!IsPreparedForCurrentUser)
            {
                Clear();
                return false;
            }

            return !string.IsNullOrWhiteSpace(appID) &&
                ActiveUser.CanAccessApp(appID) &&
                appCatalog.TryGetApp(appID, out _);
        }

        private void RequestAppLaunch(string appID)
        {
            if (!CanRequestApp(appID))
                return;

            appController.Launch(appID);
        }

        private void RequestTaskbarActivation(string appID)
        {
            if (!CanRequestApp(appID))
                return;

            appController.ActivateFromTaskbar(appID);
        }

        private void HideCurrentDocumentRoot()
        {
            VisualElement root = desktopDocument != null
                ? desktopDocument.rootVisualElement
                    .Q<VisualElement>("DesktopRoot")
                : null;

            if (root == null)
                return;

            root.SetEnabled(false);
            root.style.visibility = Visibility.Hidden;
        }

        private void OnAppStateChanged(
            string appID,
            int instanceCount,
            bool isFocused)
        {
            SetTaskbarAppState(
                appID,
                instanceCount > 0,
                isFocused);
        }

        public void SetTaskbarAppState(
            string appID,
            bool isOpen,
            bool isFocused)
        {
            if (!IsPreparedForCurrentUser)
                return;

            pinnedButtonController?.SetAppState(
                appID,
                isOpen,
                isFocused);
        }

        public void SetPinnedAppState(
            string appID,
            bool isOpen,
            bool isFocused)
        {
            SetTaskbarAppState(
                appID,
                isOpen,
                isFocused);
        }
    }
}