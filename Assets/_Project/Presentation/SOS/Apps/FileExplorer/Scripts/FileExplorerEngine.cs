using System;
using System.Collections.Generic;
using System.Linq;
using Atlas.SOS.FileSystem;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerEngine
    {
        private readonly Dictionary<string, FileExplorerDirectory> directories =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<FileExplorerTab> tabs = new();
        private readonly HashSet<string> selectedItemIds = new();

        private readonly FileSystemProfile profile;

        private int nextTabId = 1;
        private int activeTabIndex;

        private string defaultPath = "";

        public IReadOnlyList<FileExplorerTab> Tabs =>
            tabs;

        public FileExplorerTab ActiveTab =>
            tabs.Count > 0 &&
            activeTabIndex >= 0 &&
            activeTabIndex < tabs.Count
                ? tabs[activeTabIndex]
                : null;

        public FileExplorerDirectory CurrentDirectory =>
            ActiveTab != null &&
            directories.TryGetValue(
                ActiveTab.CurrentPath,
                out FileExplorerDirectory directory)
                ? directory
                : null;

        public bool CanGoBack =>
            ActiveTab?.CanGoBack ?? false;

        public bool CanGoForward =>
            ActiveTab?.CanGoForward ?? false;

        public bool CanGoUp =>
            CurrentDirectory != null &&
            !string.IsNullOrWhiteSpace(
                CurrentDirectory.ParentPath);

        public int SelectedItemCount =>
            selectedItemIds.Count;

        public FileExplorerEngine(
            FileSystemLibrary library,
            string userId)
        {
            profile =
                library?.GetProfile(userId);

            if (profile == null)
                return;

            FileSystemRuntimeBuilder builder =
                new();

            Dictionary<string, FileExplorerDirectory> runtimeDirectories =
                builder.Build(profile);

            foreach (KeyValuePair<string, FileExplorerDirectory> pair
                     in runtimeDirectories)
            {
                directories[pair.Key] =
                    pair.Value;
            }

            defaultPath =
                ResolveFolderPath(
                    profile.HomeFolderId);

            if (string.IsNullOrWhiteSpace(defaultPath))
            {
                defaultPath =
                    directories.Keys.FirstOrDefault() ?? "";
            }

            if (!string.IsNullOrWhiteSpace(defaultPath))
                OpenTab(defaultPath);
        }

        // -------------------- TABS --------------------
        public void OpenTab()
        {
            OpenTab(defaultPath);
        }

        public void OpenTab(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                path = defaultPath;

            if (!directories.ContainsKey(path))
                path = defaultPath;

            if (string.IsNullOrWhiteSpace(path))
                return;

            FileExplorerTab tab =
                new(
                    nextTabId++,
                    path);

            tabs.Add(tab);

            activeTabIndex =
                tabs.Count - 1;

            ClearSelection();
        }

        public void CloseTab(int tabId)
        {
            int index =
                tabs.FindIndex(
                    tab => tab.Id == tabId);

            if (index < 0)
                return;

            if (tabs.Count == 1)
            {
                tabs.Clear();

                activeTabIndex = 0;

                OpenTab();

                return;
            }

            tabs.RemoveAt(index);

            if (activeTabIndex >= tabs.Count)
                activeTabIndex = tabs.Count - 1;
            else if (index < activeTabIndex)
                activeTabIndex--;

            ClearSelection();
        }

        public void SelectTab(int tabId)
        {
            int index =
                tabs.FindIndex(
                    tab => tab.Id == tabId);

            if (index < 0)
                return;

            activeTabIndex = index;

            ClearSelection();
        }

        // -------------------- NAVIGATION --------------------
        public void Navigate(string path)
        {
            if (ActiveTab == null)
                return;

            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!directories.ContainsKey(path))
                return;

            ActiveTab.Navigate(path);

            ClearSelection();
        }

        public void GoBack()
        {
            ActiveTab?.GoBack();

            ClearSelection();
        }

        public void GoForward()
        {
            ActiveTab?.GoForward();

            ClearSelection();
        }

        public void GoUp()
        {
            if (!CanGoUp)
                return;

            Navigate(
                CurrentDirectory.ParentPath);
        }

        public void NavigateToLocation(
            FileExplorerLocation location)
        {
            if (profile == null)
                return;

            if (location == FileExplorerLocation.Home)
            {
                Navigate(defaultPath);

                return;
            }

            FileSystemLocation systemLocation =
                location switch
                {
                    FileExplorerLocation.Desktop =>
                        FileSystemLocation.Desktop,

                    FileExplorerLocation.Documents =>
                        FileSystemLocation.Documents,

                    FileExplorerLocation.Pictures =>
                        FileSystemLocation.Pictures,

                    FileExplorerLocation.Downloads =>
                        FileSystemLocation.Downloads,

                    _ =>
                        FileSystemLocation.None
                };

            if (systemLocation ==
                FileSystemLocation.None)
            {
                return;
            }

            FileLocationDefinition definition =
                profile.GetLocation(
                    systemLocation);

            if (definition == null ||
                !definition.Visible)
            {
                return;
            }

            string path =
                ResolveFolderPath(
                    definition.FolderId);

            if (string.IsNullOrWhiteSpace(path))
                return;

            Navigate(path);
        }

        // -------------------- SEARCH --------------------
        public void SetSearchQuery(string query)
        {
            if (ActiveTab == null)
                return;

            ActiveTab.SearchQuery =
                query?.Trim() ?? "";

            ClearSelection();
        }

        // -------------------- VIEW --------------------
        public void SetViewMode(
            FileExplorerViewMode mode)
        {
            if (ActiveTab == null)
                return;

            ActiveTab.ViewMode = mode;
        }

        public void CycleViewMode()
        {
            if (ActiveTab == null)
                return;

            ActiveTab.ViewMode =
                ActiveTab.ViewMode switch
                {
                    FileExplorerViewMode.LargeGrid =>
                        FileExplorerViewMode.Grid,

                    FileExplorerViewMode.Grid =>
                        FileExplorerViewMode.List,

                    FileExplorerViewMode.List =>
                        FileExplorerViewMode.Details,

                    FileExplorerViewMode.Details =>
                        FileExplorerViewMode.LargeGrid,

                    _ =>
                        FileExplorerViewMode.Grid
                };
        }

        // -------------------- SORT --------------------
        public void SetSortMode(
            FileExplorerSortMode mode)
        {
            if (ActiveTab == null)
                return;

            if (ActiveTab.SortMode == mode)
            {
                ActiveTab.SortDirection =
                    ActiveTab.SortDirection ==
                    FileExplorerSortDirection.Ascending
                        ? FileExplorerSortDirection.Descending
                        : FileExplorerSortDirection.Ascending;

                return;
            }

            ActiveTab.SortMode = mode;

            ActiveTab.SortDirection =
                FileExplorerSortDirection.Ascending;
        }

        // -------------------- CONTENT --------------------
        public IReadOnlyList<FileExplorerItem> GetVisibleItems()
        {
            if (CurrentDirectory == null ||
                ActiveTab == null)
            {
                return Array.Empty<FileExplorerItem>();
            }

            IEnumerable<FileExplorerItem> query =
                CurrentDirectory.Items;

            if (!string.IsNullOrWhiteSpace(
                    ActiveTab.SearchQuery))
            {
                query =
                    query.Where(
                        item =>
                            item.Name.Contains(
                                ActiveTab.SearchQuery,
                                StringComparison.OrdinalIgnoreCase));
            }

            return ApplySorting(query).ToList();
        }

        private IEnumerable<FileExplorerItem> ApplySorting(
            IEnumerable<FileExplorerItem> items)
        {
            if (ActiveTab == null)
                return items;

            IEnumerable<FileExplorerItem> folders =
                items.Where(
                    item => item.IsFolder);

            IEnumerable<FileExplorerItem> files =
                items.Where(
                    item => !item.IsFolder);

            folders =
                SortItems(folders);

            files =
                SortItems(files);

            return folders.Concat(files);
        }

        private IEnumerable<FileExplorerItem> SortItems(
            IEnumerable<FileExplorerItem> items)
        {
            bool ascending =
                ActiveTab.SortDirection ==
                FileExplorerSortDirection.Ascending;

            return ActiveTab.SortMode switch
            {
                FileExplorerSortMode.DateModified =>
                    ascending
                        ? items.OrderBy(
                            item => item.ModifiedDate,
                            StringComparer.OrdinalIgnoreCase)
                        : items.OrderByDescending(
                            item => item.ModifiedDate,
                            StringComparer.OrdinalIgnoreCase),

                FileExplorerSortMode.Type =>
                    ascending
                        ? items.OrderBy(
                            item => item.FileType)
                        : items.OrderByDescending(
                            item => item.FileType),

                FileExplorerSortMode.Size =>
                    ascending
                        ? items.OrderBy(
                            item => item.Size)
                        : items.OrderByDescending(
                            item => item.Size),

                _ =>
                    ascending
                        ? items.OrderBy(
                            item => item.Name,
                            StringComparer.OrdinalIgnoreCase)
                        : items.OrderByDescending(
                            item => item.Name,
                            StringComparer.OrdinalIgnoreCase)
            };
        }

        public void OpenItem(
            FileExplorerItem item)
        {
            if (item == null)
                return;

            if (!item.IsFolder)
                return;

            Navigate(item.Path);
        }

        // -------------------- SELECTION --------------------
        public void SelectItem(
            string itemId,
            bool additive = false)
        {
            if (!additive)
                selectedItemIds.Clear();

            if (!string.IsNullOrWhiteSpace(itemId))
            {
                selectedItemIds.Add(itemId);
            }
        }

        public void ToggleItemSelection(
            string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            if (!selectedItemIds.Add(itemId))
            {
                selectedItemIds.Remove(itemId);
            }
        }

        public bool IsSelected(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            return selectedItemIds.Contains(itemId);
        }

        public void ClearSelection()
        {
            selectedItemIds.Clear();
        }

        // -------------------- LOCATION --------------------
        public FileExplorerLocation GetCurrentLocation()
        {
            if (profile == null ||
                CurrentDirectory == null)
            {
                return FileExplorerLocation.None;
            }

            if (string.Equals(
                CurrentDirectory.Id,
                profile.HomeFolderId,
                StringComparison.OrdinalIgnoreCase))
            {
                return FileExplorerLocation.Home;
            }

            foreach (FileLocationDefinition location
                     in profile.Locations)
            {
                if (location == null ||
                    !location.Visible)
                {
                    continue;
                }

                if (!string.Equals(
                        location.FolderId,
                        CurrentDirectory.Id,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return location.Location switch
                {
                    FileSystemLocation.Desktop =>
                        FileExplorerLocation.Desktop,

                    FileSystemLocation.Documents =>
                        FileExplorerLocation.Documents,

                    FileSystemLocation.Pictures =>
                        FileExplorerLocation.Pictures,

                    FileSystemLocation.Downloads =>
                        FileExplorerLocation.Downloads,

                    _ =>
                        FileExplorerLocation.None
                };
            }

            return FileExplorerLocation.None;
        }

        // -------------------- LOOKUP --------------------
        private string ResolveFolderPath(
            string folderId)
        {
            if (string.IsNullOrWhiteSpace(folderId))
                return "";

            foreach (FileExplorerDirectory directory
                     in directories.Values)
            {
                if (string.Equals(
                    directory.Id,
                    folderId,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return directory.Path;
                }
            }

            return "";
        }
    }
}