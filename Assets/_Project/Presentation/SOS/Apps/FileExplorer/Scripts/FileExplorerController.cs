using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.FileExplorer
{
    public sealed class FileExplorerController : AppContentController
    {
        [Header("File System")]
        [SerializeField] private FileSystemLibrary fileSystemLibrary;
        [SerializeField] private string userId = "guest";

        private FileExplorerBinder binder;
        private FileExplorerView view;
        private FileExplorerViewBuilder viewBuilder;
        private FileExplorerEngine engine;

        private VisualElement menu;
        private VisualElement menuContent;
        private VisualElement menuEventRoot;
        private Button menuAnchor;

        private float menuDesiredHeight;

        private readonly List<Button> menuButtons = new();

        // -------------------- INITIALIZATION --------------------

        protected override void OnInitialize()
        {
            if (fileSystemLibrary == null)
            {
                throw new InvalidOperationException(
                    "FileExplorerController requires a FileSystemLibrary.");
            }

            binder = new FileExplorerBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "File Explorer UI is missing required elements.");
            }

            engine = new FileExplorerEngine(
                fileSystemLibrary,
                userId);

            view = new FileExplorerView(binder);
            viewBuilder = new FileExplorerViewBuilder();

            ConfigureLocationButtons();
            ConfigureUnavailableActions();
            RegisterCallbacks();

            Render();
        }

        protected override void OnShutdown()
        {
            CloseMenu();

            binder = null;
            view = null;
            viewBuilder = null;
            engine = null;
        }

        // -------------------- LOCATION ICONS --------------------

        private void ConfigureLocationButtons()
        {
            FileSystemProfile profile =
                fileSystemLibrary.GetProfile(userId);

            ConfigureLocationButton(
                binder.HomeButton,
                profile?.GetLocation(FileSystemLocation.Home),
                "fe-icon-home");

            ConfigureLocationButton(
                binder.RecentButton,
                profile?.GetLocation(FileSystemLocation.Recent),
                "fe-icon-recent");

            ConfigureLocationButton(
                binder.DesktopButton,
                profile?.GetLocation(FileSystemLocation.Desktop),
                "fe-icon-desktop");

            ConfigureLocationButton(
                binder.DocumentsButton,
                profile?.GetLocation(FileSystemLocation.Documents),
                "fe-icon-documents");

            ConfigureLocationButton(
                binder.PicturesButton,
                profile?.GetLocation(FileSystemLocation.Pictures),
                "fe-icon-pictures");

            ConfigureLocationButton(
                binder.DownloadsButton,
                profile?.GetLocation(FileSystemLocation.Downloads),
                "fe-icon-downloads");

            // FileSystemLocation currently has no Starred entry.
            ConfigureLocationButton(
                binder.StarredButton,
                null,
                "fe-icon-starred");
        }

        private static void ConfigureLocationButton(
            Button button,
            FileLocationDefinition definition,
            string fallbackIconClass)
        {
            string title =
                string.IsNullOrWhiteSpace(definition?.DisplayName)
                    ? button.text
                    : definition.DisplayName;

            button.text = "";

            var icon = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

            icon.AddToClassList("fe-icon");
            icon.AddToClassList("fe-sidebar-icon");
            icon.AddToClassList(fallbackIconClass);

            if (definition?.Icon != null)
            {
                icon.style.backgroundImage =
                    new StyleBackground(definition.Icon);
            }

            var label = new Label(title)
            {
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList("fe-sidebar-label");

            button.Add(icon);
            button.Add(label);

            button.style.display =
                definition == null || definition.Visible
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        private void SetFolderIcon(
            VisualElement element,
            string folderId)
        {
            if (element == null)
                return;

            element.AddToClassList("fe-icon-folder");

            Sprite sprite =
                fileSystemLibrary.GetFolderIcon(
                    userId,
                    folderId);

            if (sprite != null)
            {
                element.style.backgroundImage =
                    new StyleBackground(sprite);
            }
            else
            {
                // Let the USS fallback provide the icon.
                element.style.backgroundImage = StyleKeyword.Null;
            }
        }

        private void UpdatePathIcon()
        {
            VisualElement icon =
                binder.Root.Q<VisualElement>("FE-PathIcon");

            SetFolderIcon(
                icon,
                engine.CurrentDirectory?.Id);
        }

        // -------------------- COMMAND AVAILABILITY --------------------

        private void ConfigureUnavailableActions()
        {
            DisableAction(binder.CutButton, "Cut");
            DisableAction(binder.CopyButton, "Copy");
            DisableAction(binder.PasteButton, "Paste");
            DisableAction(binder.RenameButton, "Rename");
            DisableAction(binder.ShareButton, "Share");
            DisableAction(binder.DeleteButton, "Delete");
        }

        private static void DisableAction(
            Button button,
            string action)
        {
            button.SetEnabled(false);
            button.tooltip = $"{action} — not implemented yet";
        }

        // -------------------- CALLBACKS --------------------

        private void RegisterCallbacks()
        {
            BindClick(
                binder.NewTabButton,
                OnNewTabPressed);

            BindClick(
                binder.BackButton,
                OnBackPressed);

            BindClick(
                binder.ForwardButton,
                OnForwardPressed);

            BindClick(
                binder.UpButton,
                OnUpPressed);

            BindClick(
                binder.RefreshButton,
                Render);

            BindCallback<ChangeEvent<string>>(
                binder.SearchField,
                OnSearchChanged);

            BindClick(
                binder.NewButton,
                () => ToggleMenu(
                    binder.NewButton,
                    BuildNewMenu));

            BindClick(
                binder.SortButton,
                () => ToggleMenu(
                    binder.SortButton,
                    BuildSortMenu));

            BindClick(
                binder.ViewButton,
                () => ToggleMenu(
                    binder.ViewButton,
                    BuildViewMenu));

            BindClick(
                binder.MoreButton,
                () => ToggleMenu(
                    binder.MoreButton,
                    BuildMoreMenu));

            BindClick(
                binder.HomeButton,
                () => NavigateTo(FileExplorerLocation.Home));

            BindClick(
                binder.DesktopButton,
                () => NavigateTo(FileExplorerLocation.Desktop));

            BindClick(
                binder.DocumentsButton,
                () => NavigateTo(FileExplorerLocation.Documents));

            BindClick(
                binder.PicturesButton,
                () => NavigateTo(FileExplorerLocation.Pictures));

            BindClick(
                binder.DownloadsButton,
                () => NavigateTo(FileExplorerLocation.Downloads));

            BindClick(
                binder.RecentButton,
                OnRecentPressed);

            BindClick(
                binder.StarredButton,
                OnStarredPressed);

            BindCallback<ClickEvent>(
                binder.ContentScrollView,
                OnContentBackgroundPressed);

            BindCallback<KeyDownEvent>(
                binder.WindowRoot,
                OnWindowKeyDown,
                TrickleDown.TrickleDown);

            BindCallback<GeometryChangedEvent>(
                binder.WindowRoot,
                OnWindowGeometryChanged);

            BindCallback<DetachFromPanelEvent>(
                binder.WindowRoot,
                OnWindowDetached);
        }

        // -------------------- TABS --------------------

        private void OnNewTabPressed()
        {
            engine.OpenTab();
            Render();
        }

        private void OnTabPressed(int tabId)
        {
            engine.SelectTab(tabId);
            Render();
        }

        private void OnTabClosePressed(int tabId)
        {
            engine.CloseTab(tabId);
            Render();
        }

        private void DecorateTabs(VisualElement tabs)
        {
            int index = 0;

            foreach (VisualElement tabElement in tabs.Children())
            {
                FileExplorerTab tab = engine.Tabs[index++];

                FileExplorerDirectory directory =
                    engine.GetDirectory(tab.CurrentPath);

                // WindowController excludes focusable header elements
                // from window drag handling.
                tabElement.focusable = true;

                Label oldIcon =
                    tabElement.Q<Label>(
                        className: "fe-tab__icon");

                if (oldIcon != null)
                {
                    oldIcon.RemoveFromHierarchy();

                    var icon = new VisualElement
                    {
                        pickingMode = PickingMode.Ignore
                    };

                    icon.AddToClassList("fe-tab__icon");

                    SetFolderIcon(
                        icon,
                        directory?.Id);

                    tabElement.Insert(0, icon);
                }

                Button close =
                    tabElement.Q<Button>(
                        className: "fe-tab__close");

                if (close == null)
                    continue;

                close.text = "";
                close.tooltip = "Close tab";

                var closeIcon = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };

                closeIcon.AddToClassList("fe-icon");
                closeIcon.AddToClassList("fe-icon-close");

                close.Add(closeIcon);
            }
        }

        // -------------------- NAVIGATION --------------------

        private void OnBackPressed()
        {
            engine.GoBack();
            Render();
        }

        private void OnForwardPressed()
        {
            engine.GoForward();
            Render();
        }

        private void OnUpPressed()
        {
            engine.GoUp();
            Render();
        }

        private void OnBreadcrumbPressed(string path)
        {
            engine.Navigate(path);
            Render();
        }

        private void NavigateTo(FileExplorerLocation location)
        {
            engine.NavigateToLocation(location);
            Render();
        }

        private void OnRecentPressed()
        {
            Debug.Log("Recent view not implemented yet.");
        }

        private void OnStarredPressed()
        {
            Debug.Log("Starred view not implemented yet.");
        }

        // -------------------- SEARCH --------------------

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            engine.SetSearchQuery(evt.newValue);
            Render();
        }

        // -------------------- ITEMS --------------------

        private void OnItemPressed(
            FileExplorerItem item,
            bool additive)
        {
            if (item == null)
                return;

            if (additive)
                engine.ToggleItemSelection(item.Id);
            else
                engine.SelectItem(item.Id);

            Render();
        }

        private void OnItemOpened(FileExplorerItem item)
        {
            if (item == null)
                return;

            if (item.IsFolder)
            {
                engine.OpenItem(item);
                Render();
                return;
            }

            OpenFile(item);
        }

        private void OpenFile(FileExplorerItem item)
        {
            Debug.Log($"Open file: {item.Path}");

            // Route file opening through AppController when implemented.
        }

        private void OnContentBackgroundPressed(ClickEvent evt)
        {
            if (evt.target != binder.ContentScrollView &&
                evt.target != binder.ContentContainer)
            {
                return;
            }

            engine.ClearSelection();
            Render();
        }

        private void SelectAll()
        {
            engine.ClearSelection();

            foreach (FileExplorerItem item in engine.GetVisibleItems())
                engine.ToggleItemSelection(item.Id);

            Render();
        }

        private void ClearSelection()
        {
            engine.ClearSelection();
            Render();
        }

        // -------------------- RENDER --------------------

        private void Render()
        {
            if (engine == null ||
                view == null ||
                viewBuilder == null)
            {
                return;
            }

            CloseMenu();

            FileExplorerViewState state =
                viewBuilder.BuildState(engine);

            view.Render(state);
            UpdatePathIcon();

            VisualElement tabs =
                viewBuilder.BuildTabs(
                    engine,
                    OnTabPressed,
                    OnTabClosePressed);

            DecorateTabs(tabs);
            view.SetTabs(tabs);

            VisualElement breadcrumbs =
                viewBuilder.BuildBreadcrumbs(
                    engine,
                    OnBreadcrumbPressed);

            view.SetBreadcrumbs(breadcrumbs);

            VisualElement directory =
                viewBuilder.BuildDirectory(
                    engine,
                    OnItemPressed,
                    OnItemOpened);

            view.SetContent(directory);
        }

        // -------------------- MENU LIFECYCLE --------------------

        private void ToggleMenu(
            Button anchor,
            Action buildMenu)
        {
            if (menu != null && menuAnchor == anchor)
            {
                CloseMenu(true);
                return;
            }

            CloseMenu();

            menuAnchor = anchor;
            menuAnchor.AddToClassList("fe-command-button--open");

            menu = new VisualElement
            {
                focusable = true
            };

            menu.AddToClassList("fe-popover");

            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
                verticalScrollerVisibility = ScrollerVisibility.Auto
            };

            scroll.AddToClassList("fe-popover-scroll");

            menu.Add(scroll);

            menuContent = scroll.contentContainer;
            menuDesiredHeight = 14f;

            menuButtons.Clear();

            buildMenu();

            binder.MenuLayer.Add(menu);
            binder.MenuLayer.BringToFront();

            PositionMenu();

            if (binder.WindowRoot.panel != null)
            {
                menuEventRoot =
                    binder.WindowRoot.panel.visualTree;

                menuEventRoot.RegisterCallback<PointerDownEvent>(
                    OnGlobalPointerDown,
                    TrickleDown.TrickleDown);
            }

            VisualElement openedMenu = menu;

            openedMenu.schedule.Execute(() =>
            {
                if (menu != openedMenu ||
                    openedMenu.panel == null)
                {
                    return;
                }

                if (menuButtons.Count > 0)
                    menuButtons[0].Focus();
                else
                    openedMenu.Focus();
            });
        }

        private void PositionMenu()
        {
            if (menu == null || menuAnchor == null)
                return;

            float width =
                binder.MenuLayer.resolvedStyle.width;

            float height =
                binder.MenuLayer.resolvedStyle.height;

            if (float.IsNaN(width) || width <= 0f)
                width = binder.WindowRoot.worldBound.width;

            if (float.IsNaN(height) || height <= 0f)
                height = binder.WindowRoot.worldBound.height;

            const float padding = 8f;

            float menuWidth = Mathf.Min(
                232f,
                Mathf.Max(1f, width - padding * 2f));

            float menuHeight = Mathf.Min(
                menuDesiredHeight,
                Mathf.Max(1f, height - padding * 2f));

            Vector2 below =
                binder.MenuLayer.WorldToLocal(
                    new Vector2(
                        menuAnchor.worldBound.xMin,
                        menuAnchor.worldBound.yMax));

            Vector2 above =
                binder.MenuLayer.WorldToLocal(
                    new Vector2(
                        menuAnchor.worldBound.xMin,
                        menuAnchor.worldBound.yMin));

            float left = Mathf.Clamp(
                below.x,
                padding,
                Mathf.Max(
                    padding,
                    width - menuWidth - padding));

            float top = below.y + 5f;

            if (top + menuHeight > height - padding)
                top = above.y - menuHeight - 5f;

            top = Mathf.Clamp(
                top,
                padding,
                Mathf.Max(
                    padding,
                    height - menuHeight - padding));

            menu.style.left = left;
            menu.style.top = top;
            menu.style.width = menuWidth;
            menu.style.height = menuHeight;
        }

        private void CloseMenu(bool restoreFocus = false)
        {
            Button previousAnchor = menuAnchor;

            if (menuEventRoot != null)
            {
                menuEventRoot.UnregisterCallback<PointerDownEvent>(
                    OnGlobalPointerDown,
                    TrickleDown.TrickleDown);

                menuEventRoot = null;
            }

            menu?.RemoveFromHierarchy();

            menuAnchor?.RemoveFromClassList(
                "fe-command-button--open");

            menu = null;
            menuContent = null;
            menuAnchor = null;
            menuDesiredHeight = 0f;

            menuButtons.Clear();

            if (restoreFocus &&
                previousAnchor != null &&
                previousAnchor.panel != null &&
                previousAnchor.enabledInHierarchy)
            {
                previousAnchor.Focus();
            }
        }

        private void OnGlobalPointerDown(PointerDownEvent evt)
        {
            if (menu == null)
                return;

            VisualElement target =
                evt.target as VisualElement;

            if (IsWithin(menu, target) ||
                IsWithin(menuAnchor, target))
            {
                return;
            }

            CloseMenu();
        }

        private static bool IsWithin(
            VisualElement container,
            VisualElement target)
        {
            return container != null &&
                   target != null &&
                   (target == container ||
                    container.Contains(target));
        }

        private void OnWindowGeometryChanged(
            GeometryChangedEvent evt)
        {
            CloseMenu();
        }

        private void OnWindowDetached(
            DetachFromPanelEvent evt)
        {
            CloseMenu();
        }

        // -------------------- MENU KEYBOARD --------------------

        private void OnWindowKeyDown(KeyDownEvent evt)
        {
            if (menu == null)
                return;

            if (evt.keyCode == KeyCode.Escape)
            {
                CloseMenu(true);
                evt.StopPropagation();
                return;
            }

            if (menuButtons.Count == 0)
                return;

            int focusedIndex = -1;

            VisualElement target =
                evt.target as VisualElement;

            for (int i = 0; i < menuButtons.Count; i++)
            {
                if (!IsWithin(menuButtons[i], target))
                    continue;

                focusedIndex = i;
                break;
            }

            int nextIndex;

            switch (evt.keyCode)
            {
                case KeyCode.DownArrow:
                    nextIndex =
                        (focusedIndex + 1) % menuButtons.Count;
                    break;

                case KeyCode.UpArrow:
                    nextIndex = focusedIndex <= 0
                        ? menuButtons.Count - 1
                        : focusedIndex - 1;
                    break;

                case KeyCode.Home:
                    nextIndex = 0;
                    break;

                case KeyCode.End:
                    nextIndex = menuButtons.Count - 1;
                    break;

                default:
                    return;
            }

            menuButtons[nextIndex].Focus();
            evt.StopPropagation();
        }

        // -------------------- MENU ROWS --------------------

        private void AddMenuItem(
            string text,
            Action action,
            bool selected = false,
            string unavailableReason = null,
            string iconClass = null)
        {
            var button = new Button();

            button.AddToClassList("fe-popover-item");

            button.EnableInClassList(
                "fe-popover-item--selected",
                selected);

            var check = new Label(selected ? "✓" : "")
            {
                pickingMode = PickingMode.Ignore
            };

            check.AddToClassList("fe-popover-check");
            button.Add(check);

            if (!string.IsNullOrWhiteSpace(iconClass))
            {
                var icon = new VisualElement
                {
                    pickingMode = PickingMode.Ignore
                };

                icon.AddToClassList("fe-icon");
                icon.AddToClassList("fe-popover-icon");
                icon.AddToClassList(iconClass);

                button.Add(icon);
            }

            var label = new Label(text)
            {
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList("fe-popover-label");
            button.Add(label);

            bool available = action != null;

            button.SetEnabled(available);

            if (available)
            {
                button.clicked += () =>
                {
                    CloseMenu(true);
                    action();
                };

                menuButtons.Add(button);
            }
            else
            {
                button.tooltip =
                    unavailableReason ?? "Not implemented yet";
            }

            menuContent.Add(button);
            menuDesiredHeight += 34f;
        }

        private void AddMenuDivider()
        {
            var divider = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

            divider.AddToClassList("fe-popover-divider");

            menuContent.Add(divider);
            menuDesiredHeight += 11f;
        }

        // -------------------- NEW MENU --------------------

        private void BuildNewMenu()
        {
            const string reason =
                "Item creation is not implemented in the file system engine yet.";

            AddMenuItem(
                "Folder",
                null,
                unavailableReason: reason,
                iconClass: "fe-icon-folder");

            AddMenuDivider();

            AddMenuItem(
                "Text document",
                null,
                unavailableReason: reason,
                iconClass: "fe-icon-documents");

            AddMenuItem(
                "Shortcut",
                null,
                unavailableReason: reason,
                iconClass: "fe-icon-shortcut");
        }

        // -------------------- SORT MENU --------------------

        private void BuildSortMenu()
        {
            FileExplorerTab tab = engine.ActiveTab;

            if (tab == null)
                return;

            AddSortItem(
                "Name",
                FileExplorerSortMode.Name);

            AddSortItem(
                "Date modified",
                FileExplorerSortMode.DateModified);

            AddSortItem(
                "Type",
                FileExplorerSortMode.Type);

            AddSortItem(
                "Size",
                FileExplorerSortMode.Size);

            AddMenuDivider();

            AddMenuItem(
                "Ascending",
                () => SetSortDirection(
                    FileExplorerSortDirection.Ascending),
                selected:
                    tab.SortDirection ==
                    FileExplorerSortDirection.Ascending);

            AddMenuItem(
                "Descending",
                () => SetSortDirection(
                    FileExplorerSortDirection.Descending),
                selected:
                    tab.SortDirection ==
                    FileExplorerSortDirection.Descending);
        }

        private void AddSortItem(
            string title,
            FileExplorerSortMode mode)
        {
            AddMenuItem(
                title,
                () =>
                {
                    FileExplorerTab tab = engine.ActiveTab;

                    if (tab == null)
                        return;

                    // Preserve direction when choosing a sort field.
                    // The existing SetSortMode method toggles direction
                    // when selecting the current field.
                    tab.SortMode = mode;

                    Render();
                },
                selected:
                    engine.ActiveTab.SortMode == mode);
        }

        private void SetSortDirection(
            FileExplorerSortDirection direction)
        {
            if (engine.ActiveTab == null)
                return;

            engine.ActiveTab.SortDirection = direction;

            Render();
        }

        // -------------------- VIEW MENU --------------------

        private void BuildViewMenu()
        {
            if (engine.ActiveTab == null)
                return;

            AddViewItem(
                "Large icons",
                FileExplorerViewMode.LargeGrid);

            AddViewItem(
                "Medium icons",
                FileExplorerViewMode.Grid);

            AddViewItem(
                "List",
                FileExplorerViewMode.List);

            AddViewItem(
                "Details",
                FileExplorerViewMode.Details);
        }

        private void AddViewItem(
            string title,
            FileExplorerViewMode mode)
        {
            string iconClass =
                mode == FileExplorerViewMode.Grid ||
                mode == FileExplorerViewMode.LargeGrid
                    ? "fe-icon-grid"
                    : "fe-icon-list";

            AddMenuItem(
                title,
                () =>
                {
                    engine.SetViewMode(mode);
                    Render();
                },
                selected:
                    engine.ActiveTab.ViewMode == mode,
                iconClass: iconClass);
        }

        // -------------------- MORE MENU --------------------

        private void BuildMoreMenu()
        {
            AddMenuItem(
                "New tab",
                OnNewTabPressed,
                iconClass: "fe-icon-new");

            AddMenuItem(
                "Refresh",
                Render,
                iconClass: "fe-icon-refresh");

            AddMenuDivider();

            IReadOnlyList<FileExplorerItem> items =
                engine.GetVisibleItems();

            Action selectAll = null;
            Action clearSelection = null;

            if (items.Count > 0)
                selectAll = SelectAll;

            if (engine.SelectedItemCount > 0)
                clearSelection = ClearSelection;

            AddMenuItem(
                "Select all",
                selectAll,
                unavailableReason:
                    "There are no visible items to select.");

            AddMenuItem(
                "Clear selection",
                clearSelection,
                unavailableReason:
                    "No items are selected.");

            AddMenuDivider();

            AddMenuItem(
                "Filter",
                null,
                unavailableReason:
                    "Filtering is not implemented yet.");
        }
    }
}