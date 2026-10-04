using System;
using System.Collections.Generic;
using Atlas.SOS.FileSystem;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerViewBuilder
    {
        public FileExplorerViewState BuildState(
            FileExplorerEngine engine)
        {
            if (engine == null)
                return new FileExplorerViewState();

            FileExplorerDirectory directory =
                engine.CurrentDirectory;

            FileExplorerTab tab =
                engine.ActiveTab;

            IReadOnlyList<FileExplorerItem> items =
                engine.GetVisibleItems();

            return new FileExplorerViewState
            {
                CanGoBack =
                    engine.CanGoBack,

                CanGoForward =
                    engine.CanGoForward,

                CanGoUp =
                    engine.CanGoUp,

                DirectoryTitle =
                    directory?.Name ?? "Files",

                DirectorySubtitle =
                    BuildDirectorySubtitle(
                        items.Count,
                        tab?.SearchQuery),

                SortStatus =
                    BuildSortStatus(tab),

                SearchText =
                    tab?.SearchQuery ?? "",

                SelectedLocation =
                    engine.GetCurrentLocation(),

                ViewMode =
                    tab?.ViewMode ??
                    FileExplorerViewMode.Grid,

                IsEmpty =
                    items.Count == 0,

                ItemCount =
                    items.Count,

                SelectedItemCount =
                    engine.SelectedItemCount,

                CanCreateNew =
                    directory != null &&
                    !directory.IsReadOnly,

                CanSort =
                    items.Count > 0,

                CanChangeView =
                    true,

                CanFilter =
                    true,

                CanRefresh =
                    true
            };
        }

        // -------------------- TABS --------------------
        public VisualElement BuildTabs(
            FileExplorerEngine engine,
            Action<int> onTabPressed,
            Action<int> onTabClosePressed)
        {
            VisualElement root =
                new();

            if (engine == null)
                return root;

            foreach (FileExplorerTab tab
                     in engine.Tabs)
            {
                root.Add(
                    BuildTab(
                        tab,
                        tab == engine.ActiveTab,
                        onTabPressed,
                        onTabClosePressed));
            }

            return root;
        }

        private VisualElement BuildTab(
            FileExplorerTab tab,
            bool active,
            Action<int> onTabPressed,
            Action<int> onTabClosePressed)
        {
            VisualElement root =
                new();

            root.AddToClassList(
                "fe-tab");

            if (active)
            {
                root.AddToClassList(
                    "fe-tab--active");
            }

            Label icon =
                new("▣");

            icon.AddToClassList(
                "fe-tab__icon");

            Label title =
                new(
                    BuildTabTitle(tab));

            title.AddToClassList(
                "fe-tab__label");

            Button close =
                new()
                {
                    text = "×"
                };

            close.AddToClassList(
                "fe-tab__close");

            root.Add(icon);
            root.Add(title);
            root.Add(close);

            root.RegisterCallback<ClickEvent>(
                evt =>
                {
                    onTabPressed?.Invoke(
                        tab.Id);
                });

            close.RegisterCallback<ClickEvent>(
                evt =>
                {
                    evt.StopPropagation();

                    onTabClosePressed?.Invoke(
                        tab.Id);
                });

            return root;
        }

        // -------------------- BREADCRUMBS --------------------
        public VisualElement BuildBreadcrumbs(
            FileExplorerEngine engine,
            Action<string> onBreadcrumbPressed)
        {
            VisualElement root =
                new();

            if (engine?.ActiveTab == null)
                return root;

            string path =
                engine.ActiveTab.CurrentPath;

            string[] segments =
                path.Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

            string currentPath = "";

            for (int i = 0;
                 i < segments.Length;
                 i++)
            {
                currentPath +=
                    "/" + segments[i];

                string capturedPath =
                    currentPath;

                Button breadcrumb =
                    new()
                    {
                        text =
                            FormatSegment(
                                segments[i])
                    };

                breadcrumb.AddToClassList(
                    "fe-breadcrumb");

                if (i ==
                    segments.Length - 1)
                {
                    breadcrumb.AddToClassList(
                        "fe-breadcrumb--current");
                }

                breadcrumb.clicked +=
                    () =>
                    {
                        onBreadcrumbPressed?.Invoke(
                            capturedPath);
                    };

                root.Add(
                    breadcrumb);

                if (i <
                    segments.Length - 1)
                {
                    Label separator =
                        new("›");

                    separator.AddToClassList(
                        "fe-breadcrumb-separator");

                    root.Add(
                        separator);
                }
            }

            return root;
        }

        // -------------------- DIRECTORY --------------------
        public VisualElement BuildDirectory(
            FileExplorerEngine engine,
            Action<FileExplorerItem, bool> onItemPressed,
            Action<FileExplorerItem> onItemOpened)
        {
            VisualElement root =
                new();

            if (engine?.ActiveTab == null)
                return root;

            IReadOnlyList<FileExplorerItem> items =
                engine.GetVisibleItems();

            FileExplorerViewMode mode =
                engine.ActiveTab.ViewMode;

            if (mode ==
                FileExplorerViewMode.Details)
            {
                root.Add(
                    BuildDetailsHeader());
            }

            foreach (FileExplorerItem item
                     in items)
            {
                VisualElement element =
                    mode switch
                    {
                        FileExplorerViewMode.List =>
                            BuildListItem(
                                item,
                                engine,
                                onItemPressed,
                                onItemOpened),

                        FileExplorerViewMode.Details =>
                            BuildDetailsItem(
                                item,
                                engine,
                                onItemPressed,
                                onItemOpened),

                        _ =>
                            BuildGridItem(
                                item,
                                engine,
                                onItemPressed,
                                onItemOpened)
                    };

                root.Add(
                    element);
            }

            return root;
        }

        private VisualElement BuildGridItem(
            FileExplorerItem item,
            FileExplorerEngine engine,
            Action<FileExplorerItem, bool> onPressed,
            Action<FileExplorerItem> onOpened)
        {
            VisualElement root =
                new();

            root.AddToClassList(
                "fe-file-card");

            if (item.IsFolder)
            {
                root.AddToClassList(
                    "fe-file-card--folder");
            }

            if (engine.IsSelected(item.Id))
            {
                root.AddToClassList(
                    "fe-file-card--selected");
            }

            VisualElement preview =
                new();

            preview.AddToClassList(
                "fe-file-card__preview");

            Label icon =
                new(
                    GetItemIcon(item));

            icon.AddToClassList(
                "fe-file-card__icon");

            Label name =
                new(item.Name);

            name.AddToClassList(
                "fe-file-card__name");

            Label metadata =
                new(
                    item.IsFolder
                        ? "Folder"
                        : GetFileTypeLabel(
                            item.FileType));

            metadata.AddToClassList(
                "fe-file-card__metadata");

            preview.Add(icon);

            root.Add(preview);
            root.Add(name);
            root.Add(metadata);

            RegisterItemCallbacks(
                root,
                item,
                onPressed,
                onOpened);

            return root;
        }

        private VisualElement BuildListItem(
            FileExplorerItem item,
            FileExplorerEngine engine,
            Action<FileExplorerItem, bool> onPressed,
            Action<FileExplorerItem> onOpened)
        {
            VisualElement root =
                new();

            root.AddToClassList(
                "fe-file-row");

            if (engine.IsSelected(item.Id))
            {
                root.AddToClassList(
                    "fe-file-row--selected");
            }

            Label icon =
                new(
                    GetItemIcon(item));

            icon.AddToClassList(
                "fe-file-row__icon");

            Label name =
                new(item.Name);

            name.AddToClassList(
                "fe-file-row__name");

            Label metadata =
                new(
                    item.IsFolder
                        ? "Folder"
                        : GetFileTypeLabel(
                            item.FileType));

            metadata.AddToClassList(
                "fe-file-row__metadata");

            root.Add(icon);
            root.Add(name);
            root.Add(metadata);

            RegisterItemCallbacks(
                root,
                item,
                onPressed,
                onOpened);

            return root;
        }

        private VisualElement BuildDetailsHeader()
        {
            VisualElement root =
                new();

            root.AddToClassList(
                "fe-details-header");

            root.Add(
                BuildDetailsHeaderLabel(
                    "Name",
                    "fe-details-name"));

            root.Add(
                BuildDetailsHeaderLabel(
                    "Date modified",
                    "fe-details-column--date"));

            root.Add(
                BuildDetailsHeaderLabel(
                    "Type",
                    "fe-details-column--type"));

            root.Add(
                BuildDetailsHeaderLabel(
                    "Size",
                    "fe-details-column--size"));

            return root;
        }

        private static Label BuildDetailsHeaderLabel(
            string text,
            string className)
        {
            Label label =
                new(text);

            label.AddToClassList(
                "fe-details-header__column");

            label.AddToClassList(
                className);

            return label;
        }

        private VisualElement BuildDetailsItem(
            FileExplorerItem item,
            FileExplorerEngine engine,
            Action<FileExplorerItem, bool> onPressed,
            Action<FileExplorerItem> onOpened)
        {
            VisualElement root =
                new();

            root.AddToClassList(
                "fe-details-row");

            if (engine.IsSelected(item.Id))
            {
                root.AddToClassList(
                    "fe-details-row--selected");
            }

            VisualElement nameContainer =
                new();

            nameContainer.AddToClassList(
                "fe-details-name");

            Label icon =
                new(
                    GetItemIcon(item));

            icon.AddToClassList(
                "fe-details-icon");

            Label name =
                new(item.Name);

            name.AddToClassList(
                "fe-details-text");

            nameContainer.Add(icon);
            nameContainer.Add(name);

            Label date =
                BuildDetailsValue(
                    item.ModifiedDate,
                    "fe-details-column--date");

            Label type =
                BuildDetailsValue(
                    item.IsFolder
                        ? "Folder"
                        : GetFileTypeLabel(
                            item.FileType),
                    "fe-details-column--type");

            Label size =
                BuildDetailsValue(
                    item.IsFolder
                        ? ""
                        : FormatSize(
                            item.Size),
                    "fe-details-column--size");

            root.Add(nameContainer);
            root.Add(date);
            root.Add(type);
            root.Add(size);

            RegisterItemCallbacks(
                root,
                item,
                onPressed,
                onOpened);

            return root;
        }

        private static Label BuildDetailsValue(
            string text,
            string className)
        {
            Label label =
                new(text);

            label.AddToClassList(
                "fe-details-text");

            label.AddToClassList(
                className);

            return label;
        }

        private static void RegisterItemCallbacks(
            VisualElement element,
            FileExplorerItem item,
            Action<FileExplorerItem, bool> onPressed,
            Action<FileExplorerItem> onOpened)
        {
            element.RegisterCallback<ClickEvent>(
                evt =>
                {
                    if (evt.clickCount >= 2)
                    {
                        onOpened?.Invoke(
                            item);

                        evt.StopPropagation();

                        return;
                    }

                    onPressed?.Invoke(
                        item,
                        evt.ctrlKey);

                    evt.StopPropagation();
                });
        }

        private static string BuildDirectorySubtitle(
            int itemCount,
            string searchQuery)
        {
            string count =
                $"{itemCount} {(itemCount == 1 ? "item" : "items")}";

            if (string.IsNullOrWhiteSpace(
                    searchQuery))
            {
                return count;
            }

            return
                $"{count} matching \"{searchQuery}\"";
        }

        private static string BuildSortStatus(
            FileExplorerTab tab)
        {
            if (tab == null)
                return "";

            string mode =
                tab.SortMode switch
                {
                    FileExplorerSortMode.DateModified =>
                        "Date modified",

                    FileExplorerSortMode.Type =>
                        "Type",

                    FileExplorerSortMode.Size =>
                        "Size",

                    _ =>
                        "Name"
                };

            string direction =
                tab.SortDirection ==
                FileExplorerSortDirection.Ascending
                    ? "Ascending"
                    : "Descending";

            return
                $"{mode} • {direction}";
        }

        private static string BuildTabTitle(
            FileExplorerTab tab)
        {
            if (tab == null ||
                string.IsNullOrWhiteSpace(
                    tab.CurrentPath))
            {
                return "Files";
            }

            string[] segments =
                tab.CurrentPath.Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                return "Files";

            return FormatSegment(
                segments[^1]);
        }

        private static string FormatSegment(
            string segment)
        {
            if (string.IsNullOrWhiteSpace(
                    segment))
            {
                return "Files";
            }

            segment =
                segment.Replace(
                    "-",
                    " ");

            return
                char.ToUpper(segment[0]) +
                segment.Substring(1);
        }

        private static string GetItemIcon(
            FileExplorerItem item)
        {
            if (item.IsFolder)
                return "▰";

            return item.FileType switch
            {
                FileSystemFileType.Image => "▧",
                FileSystemFileType.PDF => "▤",
                FileSystemFileType.Text => "≡",
                FileSystemFileType.Archive => "▥",
                FileSystemFileType.Shortcut => "↗",
                _ => "□"
            };
        }

        private static string GetFileTypeLabel(
            FileSystemFileType type)
        {
            return type switch
            {
                FileSystemFileType.Text =>
                    "Text Document",

                FileSystemFileType.Image =>
                    "Image",

                FileSystemFileType.PDF =>
                    "PDF Document",

                FileSystemFileType.Archive =>
                    "Archive",

                FileSystemFileType.Shortcut =>
                    "Shortcut",

                _ =>
                    "File"
            };
        }

        private static string FormatSize(
            long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";

            if (bytes < 1024 * 1024)
            {
                return
                    $"{bytes / 1024f:0.#} KB";
            }

            return
                $"{bytes / (1024f * 1024f):0.#} MB";
        }
    }
}