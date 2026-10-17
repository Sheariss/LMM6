using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Atlas.Presentation.SOS.FileExplorer;
using Atlas.SOS.Apps;
using Atlas.SOS.FileSystem;
using UnityEngine;
using static Atlas.AuthoredData.Users.UserLibrarySO;

namespace Atlas.Presentation.SOS.Desktop
{
    public enum StartMenuAppView
    {
        Category,
        Grid,
        List
    }

    public sealed class StartMenuDocument
    {
        public FileExplorerItem Item { get; }
        public FileOpenRequest Request { get; }
        public Sprite Icon { get; }
        public DateTime ModifiedUtc { get; }

        public StartMenuDocument(
            FileExplorerItem item,
            FileOpenRequest request,
            Sprite icon,
            DateTime modifiedUtc)
        {
            Item = item;
            Request = request;
            Icon = icon;
            ModifiedUtc = modifiedUtc;
        }
    }

    public sealed class StartMenuEngine
    {
        public const int CollapsedPinnedCount = 12;
        public const int RecentPreviewCount = 6;

        private readonly AppCatalog appCatalog;
        private readonly FileSystemLibrary fileSystemLibrary;
        private readonly FileAssociationCatalog associations;

        private readonly List<AppDefinition> apps = new();
        private readonly List<AppDefinition> pinnedApps = new();
        private readonly List<StartMenuDocument> documents = new();

        public UserInfo User { get; }

        public IReadOnlyList<AppDefinition> Apps => apps;
        public IReadOnlyList<AppDefinition> PinnedApps => pinnedApps;
        public IReadOnlyList<StartMenuDocument> Documents => documents;

        public StartMenuAppView ViewMode { get; set; } =
            StartMenuAppView.Category;

        public AppCategory? ExpandedCategory { get; set; }
        public bool PinnedExpanded { get; set; }
        public bool ShowingRecent { get; set; }
        public string Query { get; private set; } = string.Empty;

        public bool IsSearching => Query.Length > 0;

        public StartMenuEngine(
            UserInfo user,
            AppCatalog appCatalog,
            FileSystemLibrary fileSystemLibrary,
            FileAssociationCatalog associations)
        {
            User = user ?? throw new ArgumentNullException(nameof(user));

            this.appCatalog = appCatalog != null
                ? appCatalog
                : throw new ArgumentNullException(nameof(appCatalog));

            this.fileSystemLibrary = fileSystemLibrary != null
                ? fileSystemLibrary
                : throw new ArgumentNullException(nameof(fileSystemLibrary));

            this.associations = associations != null
                ? associations
                : throw new ArgumentNullException(nameof(associations));
        }

        public void SetQuery(string query)
        {
            Query = (query ?? string.Empty).Trim();
        }

        public IEnumerable<AppDefinition> GetSearchResults()
        {
            return apps.Where(app =>
                (app.DisplayName ?? string.Empty).IndexOf(
                    Query,
                    StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public AppDefinition FindApp(string appID)
        {
            return apps.FirstOrDefault(app =>
                string.Equals(app.AppID, appID, StringComparison.Ordinal));
        }

        public StartMenuDocument FindDocument(string path)
        {
            return documents.FirstOrDefault(document =>
                string.Equals(
                    document.Item.Path,
                    path,
                    StringComparison.OrdinalIgnoreCase));
        }

        public void Refresh()
        {
            apps.Clear();
            pinnedApps.Clear();
            documents.Clear();

            var availableByID = new Dictionary<string, AppDefinition>(
                StringComparer.Ordinal);

            foreach (string appID in
                     User.AvailableAppIDs ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(appID) ||
                    availableByID.ContainsKey(appID) ||
                    !appCatalog.TryGetApp(appID, out AppDefinition app))
                {
                    continue;
                }

                availableByID.Add(appID, app);
                apps.Add(app);
            }

            apps.Sort((first, second) =>
            {
                int nameOrder = StringComparer.OrdinalIgnoreCase.Compare(
                    first.DisplayName,
                    second.DisplayName);

                return nameOrder != 0
                    ? nameOrder
                    : StringComparer.Ordinal.Compare(
                        first.AppID,
                        second.AppID);
            });

            var seenPins = new HashSet<string>(StringComparer.Ordinal);

            foreach (string appID in
                     User.StartPinnedAppIDs ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(appID) ||
                    !seenPins.Add(appID) ||
                    !availableByID.TryGetValue(
                        appID,
                        out AppDefinition app))
                {
                    continue;
                }

                pinnedApps.Add(app);
            }

            FileSystemProfile profile =
                fileSystemLibrary.GetProfile(User.UserId);

            if (profile == null)
                return;

            Dictionary<string, FileExplorerDirectory> directories =
                new FileSystemRuntimeBuilder().Build(profile);

            var seenPaths = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (FileExplorerDirectory directory in directories.Values)
            {
                List<FileSystemFileDefinition> relatedFiles = directory.Items
                    .Where(item => !item.IsFolder &&
                        item.FileDefinition != null)
                    .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(item => item.FileDefinition)
                    .ToList();

                foreach (FileExplorerItem item in directory.Items)
                {
                    if (item.IsFolder ||
                        item.FileDefinition == null ||
                        item.FileDefinition.Hidden ||
                        string.IsNullOrWhiteSpace(item.Id) ||
                        string.IsNullOrWhiteSpace(item.Path) ||
                        !seenPaths.Add(item.Path))
                    {
                        continue;
                    }

                    if (!associations.TryGetAppId(
                            item.FileType,
                            out string appID,
                            out _) ||
                        !availableByID.TryGetValue(
                            appID,
                            out AppDefinition app) ||
                        app.RuntimePrefab == null ||
                        app.RuntimePrefab is not IFileContentReceiver)
                    {
                        continue;
                    }

                    var request = new FileOpenRequest(
                        User.UserId,
                        item.Path,
                        item.FileDefinition,
                        relatedFiles);

                    Sprite icon = item.FileDefinition.PreviewImage;

                    if (icon == null)
                    {
                        icon = app.AppIcon48 != null
                            ? app.AppIcon48
                            : app.AppIcon96;
                    }

                    documents.Add(new StartMenuDocument(
                        item,
                        request,
                        icon,
                        ParseDate(item.ModifiedDate)));
                }
            }

            documents.Sort((first, second) =>
            {
                int dateOrder = second.ModifiedUtc.CompareTo(
                    first.ModifiedUtc);

                if (dateOrder != 0)
                    return dateOrder;

                int nameOrder = StringComparer.OrdinalIgnoreCase.Compare(
                    first.Item.Name,
                    second.Item.Name);

                return nameOrder != 0
                    ? nameOrder
                    : StringComparer.OrdinalIgnoreCase.Compare(
                        first.Item.Path,
                        second.Item.Path);
            });
        }

        public static string CategoryName(AppCategory category)
        {
            return category switch
            {
                AppCategory.DeveloperTools => "Developer Tools",
                AppCategory.UtilitiesAndTools => "Utilities & Tools",
                _ => category.ToString()
            };
        }

        private static DateTime ParseDate(string value)
        {
            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces |
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out DateTime parsed)
                    ? parsed
                    : DateTime.MinValue;
        }
    }
}