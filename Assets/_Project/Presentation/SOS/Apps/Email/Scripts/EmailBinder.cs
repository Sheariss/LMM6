using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailBinder
    {
        public VisualElement Root { get; }
        public VisualElement Sidebar { get; }
        public VisualElement InboxPanel { get; }
        public VisualElement MessageList { get; }
        public VisualElement Tabs { get; }
        public VisualElement Toolbar { get; }

        public ScrollView MessageScroll { get; }
        public TextField SearchField { get; }
        public Toggle SelectAllToggle { get; }

        public Label InboxCount { get; }
        public Label DraftsCount { get; }
        public Label MessageRange { get; }
        public Label StorageLabel { get; }
        public Label SyncStatus { get; }
        public Label PrimaryBadge { get; }

        public Dictionary<EmailFilter, Button> Navigation { get; } =
            new();

        public Dictionary<EmailCategory, Button> Categories { get; } =
            new();

        public EmailBinder(VisualElement host)
        {
            Root = Require<VisualElement>(host, "MailRoot");
            Sidebar = Require<VisualElement>(Root, "MailSidebar");
            InboxPanel = Require<VisualElement>(Root, "InboxPanel");
            MessageList = Require<VisualElement>(Root, "MessageList");
            MessageScroll = Require<ScrollView>(Root, "MessageScroll");
            SearchField = Require<TextField>(Root, "SearchField");
            SelectAllToggle = Require<Toggle>(Root, "SelectAllToggle");

            InboxCount = Require<Label>(Root, "InboxCount");
            DraftsCount = Require<Label>(Root, "DraftsCount");
            MessageRange = Require<Label>(Root, "MessageRangeLabel");
            StorageLabel = Require<Label>(Root, "StorageLabel");
            SyncStatus = Require<Label>(Root, "SyncStatusLabel");

            Tabs = Root.Q<VisualElement>(className: "mail-tabs");
            Toolbar = SelectAllToggle.parent;

            if (Tabs == null || Toolbar == null)
                throw new InvalidOperationException(
                    "Email tabs or toolbar could not be bound.");

            Navigation.Add(EmailFilter.Inbox, Button("InboxButton"));
            Navigation.Add(EmailFilter.Starred, Button("StarredButton"));
            Navigation.Add(EmailFilter.Sent, Button("SentButton"));
            Navigation.Add(EmailFilter.Drafts, Button("DraftsButton"));
            Navigation.Add(EmailFilter.Work, Button("WorkLabelButton"));
            Navigation.Add(
                EmailFilter.Personal, Button("PersonalLabelButton"));
            Navigation.Add(EmailFilter.All, Button("MoreFoldersButton"));

            Categories.Add(EmailCategory.Primary, Button("PrimaryTab"));
            Categories.Add(
                EmailCategory.Promotions, Button("PromotionsTab"));
            Categories.Add(EmailCategory.Social, Button("SocialTab"));

            PrimaryBadge = Categories[EmailCategory.Primary]
                .Q<Label>(className: "mail-badge");
        }

        public Button Button(string name)
        {
            return Require<Button>(Root, name);
        }

        private static T Require<T>(
            VisualElement root,
            string name) where T : VisualElement
        {
            T element = root.Q<T>(name);

            if (element == null)
                throw new InvalidOperationException(
                    $"Email UI is missing '{name}' ({typeof(T).Name}).");

            return element;
        }
    }
}