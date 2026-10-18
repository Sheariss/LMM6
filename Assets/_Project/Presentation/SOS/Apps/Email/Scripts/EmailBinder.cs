using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailNavigationElements
    {
        public Button Button;
        public Label Text;
        public Label Count;
    }

    public sealed class EmailBinder
    {
        public VisualElement Root { get; }
        public VisualElement Sidebar { get; }
        public VisualElement MoreFolders { get; }
        public VisualElement CustomLabels { get; }
        public VisualElement InboxPanel { get; }
        public VisualElement Toolbar { get; }
        public VisualElement Tabs { get; }
        public VisualElement MessageList { get; }

        public ScrollView MessageScroll { get; }
        public TextField SearchField { get; }
        public Toggle SelectAllToggle { get; }

        public Button Compose { get; }
        public Button Menu { get; }
        public Button ClearSearch { get; }
        public Button MoreFoldersButton { get; }
        public Button AddLabel { get; }

        public Label MoreText { get; }
        public Label Range { get; }

        public Dictionary<EmailFilter, EmailNavigationElements> Navigation
        { get; } = new();

        public Dictionary<EmailCategory, Button> Categories
        { get; } = new();

        public Dictionary<string, Button> Actions { get; } = new();

        public EmailBinder(VisualElement host)
        {
            Root = Require<VisualElement>(host, "MailRoot");
            Sidebar = Require<VisualElement>(Root, "MailSidebar");
            InboxPanel = Require<VisualElement>(Root, "InboxPanel");
            Toolbar = Require<VisualElement>(Root, "ListToolbar");
            Tabs = Require<VisualElement>(Root, "MailTabs");
            MessageList = Require<VisualElement>(Root, "MessageList");
            MessageScroll = Require<ScrollView>(Root, "MessageScroll");

            SearchField = Require<TextField>(Root, "SearchField");
            SelectAllToggle = Require<Toggle>(Root, "SelectAllToggle");

            Compose = Require<Button>(Root, "ComposeButton");
            Menu = Require<Button>(Root, "MenuButton");
            ClearSearch = Require<Button>(Root, "ClearSearchButton");
            Range = Require<Label>(Root, "MessageRangeLabel");

            VisualElement navigation =
                Require<VisualElement>(Root, "Navigation");

            AddNavigation(navigation, EmailFilter.Inbox, "Inbox", "icon-mail");
            AddNavigation(navigation, EmailFilter.Starred, "Starred", "icon-star");
            AddNavigation(navigation, EmailFilter.Sent, "Sent", "icon-sent");
            AddNavigation(navigation, EmailFilter.Drafts, "Drafts", "icon-draft");

            MoreFoldersButton = EmailViewBuilder.IconButton(
                "More folders", "icon-expand", "mail-nav");

            MoreFoldersButton.name = "MoreFoldersButton";

            MoreText = EmailViewBuilder.Text("More", "mail-nav-label");
            MoreFoldersButton.Add(MoreText);
            navigation.Add(MoreFoldersButton);

            MoreFolders = new VisualElement();
            MoreFolders.AddToClassList("mail-more-folders");
            navigation.Add(MoreFolders);

            AddNavigation(MoreFolders, EmailFilter.All, "All mail", "icon-mail");
            AddNavigation(MoreFolders, EmailFilter.Archive, "Archive", "icon-archive");
            AddNavigation(MoreFolders, EmailFilter.Spam, "Spam", "icon-spam");
            AddNavigation(MoreFolders, EmailFilter.Trash, "Trash", "icon-delete");

            VisualElement heading = new();
            heading.AddToClassList("mail-labels-heading");

            heading.Add(EmailViewBuilder.Text("Labels", "mail-labels-title"));

            AddLabel = EmailViewBuilder.IconButton(
                "Create label", "icon-add");

            heading.Add(AddLabel);
            navigation.Add(heading);

            AddNavigation(navigation, EmailFilter.Work, "Work", "icon-label");
            AddNavigation(navigation, EmailFilter.Personal, "Personal", "icon-label");

            CustomLabels = new VisualElement();
            navigation.Add(CustomLabels);

            int index = 1;

            foreach (string action in new[]
                     {
                         "archive", "delete", "unread",
                         "move", "labels", "more"
                     })
            {
                Button button = EmailViewBuilder.IconButton(
                    action,
                    "icon-" + action);

                Actions.Add(action, button);
                Toolbar.Insert(index++, button);
            }

            Actions.Add("refresh", Require<Button>(Root, "RefreshButton"));
            Actions.Add("previous", Require<Button>(Root, "PreviousPageButton"));
            Actions.Add("next", Require<Button>(Root, "NextPageButton"));

            foreach (EmailCategory category in
                     Enum.GetValues(typeof(EmailCategory)))
            {
                Button button = EmailViewBuilder.IconButton(
                    category.ToString(),
                    category == EmailCategory.Social
                        ? "icon-social"
                        : category == EmailCategory.Promotions
                            ? "icon-label"
                            : "icon-mail",
                    "mail-tab");

                button.Add(EmailViewBuilder.Text(
                    category.ToString(), "mail-tab-title"));

                Categories.Add(category, button);
                Tabs.Add(button);
            }
        }

        private void AddNavigation(
            VisualElement parent,
            EmailFilter filter,
            string text,
            string icon)
        {
            EmailNavigationElements row = CreateNavigation(text, icon);
            Navigation.Add(filter, row);
            parent.Add(row.Button);
        }

        public static EmailNavigationElements CreateNavigation(
            string text,
            string icon)
        {
            EmailNavigationElements row = new()
            {
                Button = EmailViewBuilder.IconButton(
                    text, icon, "mail-nav"),

                Text = EmailViewBuilder.Text(text, "mail-nav-label"),
                Count = EmailViewBuilder.Text("", "mail-count")
            };

            VisualElement dot = new()
            {
                pickingMode = PickingMode.Ignore
            };

            dot.AddToClassList("mail-nav-dot");

            row.Button.Add(row.Text);
            row.Button.Add(row.Count);
            row.Button.Add(dot);

            return row;
        }

        private static T Require<T>(
            VisualElement root,
            string name) where T : VisualElement
        {
            return root.Q<T>(name) ??
                throw new InvalidOperationException(
                    $"Email UI is missing '{name}'.");
        }
    }
}