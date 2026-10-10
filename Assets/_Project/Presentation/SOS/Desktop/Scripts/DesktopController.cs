using System;
using System.Collections.Generic;
using Atlas.SOS.Apps;
using Atlas.SOS.Authentication;
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

        [Header("Shortcuts")]
        [SerializeField] private VisualTreeAsset shortcutTemplate;
        [SerializeField]
        private Vector2 shortcutSize =
            new Vector2(128f, 128f);
        [SerializeField]
        private Vector2 shortcutGap =
            new Vector2(16f, 16f);

        private DesktopBinder binder;
        private DesktopView view;

        private DesktopShortcutGridView gridView;
        private DesktopShortcutGridController gridController;
        private DesktopShortcutController shortcutController;

        private readonly List<AppDefinition> availableApps = new();
        private readonly List<AppDefinition> pinnedApps = new();

        public UserInfo ActiveUser { get; private set; }
        public bool IsPrepared { get; private set; }

        public IReadOnlyList<AppDefinition> AvailableApps =>
            availableApps;

        public IReadOnlyList<AppDefinition> PinnedApps =>
            pinnedApps;

        public bool IsPreparedForCurrentUser =>
            IsPrepared &&
            ValidateUser(ActiveUser, out _) &&
            IsDocumentCurrent();

        public event Action<string> AppLaunchRequested;

        private void OnEnable()
        {
            // Bind and hide the shell. Do not populate it yet.
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

            if (appCatalog == null || shortcutTemplate == null)
            {
                error = "The app catalog or shortcut template is missing.";
                return false;
            }

            if (user.Wallpaper == null)
            {
                error = "This user does not have a desktop wallpaper.";
                return false;
            }

            try
            {
                var allowedIDs = new HashSet<string>(
                    user.AvailableAppIDs,
                    StringComparer.Ordinal);

                availableApps.AddRange(
                    ResolveApps(user.AvailableAppIDs, allowedIDs));

                List<AppDefinition> shortcutApps =
                    ResolveApps(user.DesktopShortcutAppIDs, allowedIDs);

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
                    RequestAppLaunch);

                shortcutController.Build(shortcutApps);

                // Future taskbar controller:
                // taskbarController.Build(pinnedApps);
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

            // DesktopManager decides when to reveal it.
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
            error = null;
            return true;
        }

        public void Hide()
        {
            view?.Hide();
            HideCurrentDocumentRoot();
        }

        public void Clear()
        {
            IsPrepared = false;
            Hide();

            // Release click handlers before removing grid instances.
            shortcutController?.Dispose();
            shortcutController = null;

            gridController?.Dispose();
            gridController = null;

            gridView?.Dispose();
            gridView = null;

            view?.ClearWallpaper();

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
                error = "A desktop UIDocument was not found in the parent hierarchy.";
                return false;
            }

            if (!desktopDocument.isActiveAndEnabled)
            {
                error = "The desktop UIDocument is disabled.";
                return false;
            }

            VisualElement documentRoot = desktopDocument.rootVisualElement;
            VisualElement desktopRoot =
                documentRoot?.Q<VisualElement>("DesktopRoot");

            if (desktopRoot == null)
            {
                error = "The UIDocument does not contain DesktopRoot.";
                return false;
            }

            // Keep the desktop hidden while binding and preparing its contents.
            desktopRoot.SetEnabled(false);
            desktopRoot.style.visibility = Visibility.Hidden;

            binder = new DesktopBinder(documentRoot);

            if (!binder.IsValid)
            {
                error = "The desktop visual tree is missing required elements.";
                return false;
            }

            view = new DesktopView(binder);
            view.Hide();

            // These background layers should not intercept shortcut input.
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
                   binder.DocumentRoot ==
                       desktopDocument.rootVisualElement &&
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
            var addedIDs = new HashSet<string>(StringComparer.Ordinal);

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

                if (!appCatalog.TryGetApp(appID, out AppDefinition app))
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

        private void RequestAppLaunch(string appID)
        {
            if (!IsPreparedForCurrentUser)
            {
                Clear();
                return;
            }

            if (!ActiveUser.CanAccessApp(appID) ||
                !appCatalog.TryGetApp(appID, out _))
            {
                return;
            }

            // Connect AppController's launch method to this event.
            AppLaunchRequested?.Invoke(appID);
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
    }
}