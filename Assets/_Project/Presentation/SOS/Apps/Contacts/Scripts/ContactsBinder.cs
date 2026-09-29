using System;
using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Contacts
{
    public sealed class ContactsBinder : UIBinder
    {
        public VisualElement Root { get; }
        public Button NewContactButton { get; }
        public Button AllContactsButton { get; }
        public Label AllContactsCountLabel { get; }
        public Button FavoritesButton { get; }
        public Label FavoritesCountLabel { get; }
        public Button RecentlyAddedButton { get; }
        public Button NewGroupButton { get; }
        public ScrollView GroupsScrollView { get; }
        public VisualElement GroupsContainer { get; }
        public Button SettingsButton { get; }
        public VisualElement ContactsListPanel { get; }
        public TextField SearchField { get; }
        public Button ClearSearchButton { get; }
        public Label ListTitleLabel { get; }
        public Label ListCountLabel { get; }
        public Button SortButton { get; }
        public ScrollView ContactsScrollView { get; }
        public VisualElement ContactsContainer { get; }
        public VisualElement ContactsEmptyState { get; }
        public VisualElement ContactDetailsPanel { get; }
        public ScrollView ContactDetailsScrollView { get; }
        public VisualElement ContactDetailsContent { get; }
        public Button FavoriteContactButton { get; }
        public Button EditContactButton { get; }
        public Button MoreContactButton { get; }
        public VisualElement ProfileAvatar { get; }
        public Image ProfileImage { get; }
        public Label ProfileInitialsLabel { get; }
        public Label ProfileNameLabel { get; }
        public Label ProfileSubtitleLabel { get; }
        public VisualElement ProfileTagsContainer { get; }
        public VisualElement ContactQuickActions { get; }
        public Button MessageContactButton { get; }
        public Button EmailContactButton { get; }
        public Button CallContactButton { get; }
        public VisualElement ContactInformationSection { get; }
        public VisualElement ContactInformationContainer { get; }
        public VisualElement PersonalInformationSection { get; }
        public VisualElement PersonalInformationContainer { get; }
        public VisualElement NotesSection { get; }
        public Label NotesLabel { get; }
        public VisualElement ContactGroupsSection { get; }
        public VisualElement ContactGroupsContainer { get; }
        public VisualElement NoContactSelectedState { get; }

        public event Action NewContactPressed;
        public event Action AllContactsPressed;
        public event Action FavoritesPressed;
        public event Action RecentlyAddedPressed;
        public event Action NewGroupPressed;
        public event Action SettingsPressed;
        public event Action ClearSearchPressed;
        public event Action SortPressed;
        public event Action FavoriteContactPressed;
        public event Action EditContactPressed;
        public event Action MoreContactPressed;
        public event Action MessageContactPressed;
        public event Action EmailContactPressed;
        public event Action CallContactPressed;
        public event Action<string> SearchChanged;

        public ContactsBinder(VisualElement root)
        {
            Root = Bind<VisualElement>(root, "ContactsRoot");
            NewContactButton = Bind<Button>(root, "NewContactButton");
            AllContactsButton = Bind<Button>(root, "AllContactsButton");
            AllContactsCountLabel = Bind<Label>(root, "AllContactsCountLabel");
            FavoritesButton = Bind<Button>(root, "FavoritesButton");
            FavoritesCountLabel = Bind<Label>(root, "FavoritesCountLabel");
            RecentlyAddedButton = Bind<Button>(root, "RecentlyAddedButton");
            NewGroupButton = Bind<Button>(root, "NewGroupButton");
            GroupsScrollView = Bind<ScrollView>(root, "GroupsScrollView");
            GroupsContainer = Bind<VisualElement>(root, "GroupsContainer");
            SettingsButton = Bind<Button>(root, "SettingsButton");
            ContactsListPanel = Bind<VisualElement>(root, "ContactsListPanel");
            SearchField = Bind<TextField>(root, "SearchField");
            ClearSearchButton = Bind<Button>(root, "ClearSearchButton");
            ListTitleLabel = Bind<Label>(root, "ListTitleLabel");
            ListCountLabel = Bind<Label>(root, "ListCountLabel");
            SortButton = Bind<Button>(root, "SortButton");
            ContactsScrollView = Bind<ScrollView>(root, "ContactsScrollView");
            ContactsContainer = Bind<VisualElement>(root, "ContactsContainer");
            ContactsEmptyState = Bind<VisualElement>(root, "ContactsEmptyState");
            ContactDetailsPanel = Bind<VisualElement>(root, "ContactDetailsPanel");
            ContactDetailsScrollView = Bind<ScrollView>(root, "ContactDetailsScrollView");
            ContactDetailsContent = Bind<VisualElement>(root, "ContactDetailsContent");
            FavoriteContactButton = Bind<Button>(root, "FavoriteContactButton");
            EditContactButton = Bind<Button>(root, "EditContactButton");
            MoreContactButton = Bind<Button>(root, "MoreContactButton");
            ProfileAvatar = Bind<VisualElement>(root, "ProfileAvatar");
            ProfileImage = Bind<Image>(root, "ProfileImage");
            ProfileInitialsLabel = Bind<Label>(root, "ProfileInitialsLabel");
            ProfileNameLabel = Bind<Label>(root, "ProfileNameLabel");
            ProfileSubtitleLabel = Bind<Label>(root, "ProfileSubtitleLabel");
            ProfileTagsContainer = Bind<VisualElement>(root, "ProfileTagsContainer");
            ContactQuickActions = Bind<VisualElement>(root, "ContactQuickActions");
            MessageContactButton = Bind<Button>(root, "MessageContactButton");
            EmailContactButton = Bind<Button>(root, "EmailContactButton");
            CallContactButton = Bind<Button>(root, "CallContactButton");
            ContactInformationSection = Bind<VisualElement>(root, "ContactInformationSection");
            ContactInformationContainer = Bind<VisualElement>(root, "ContactInformationContainer");
            PersonalInformationSection = Bind<VisualElement>(root, "PersonalInformationSection");
            PersonalInformationContainer = Bind<VisualElement>(root, "PersonalInformationContainer");
            NotesSection = Bind<VisualElement>(root, "NotesSection");
            NotesLabel = Bind<Label>(root, "NotesLabel");
            ContactGroupsSection = Bind<VisualElement>(root, "ContactGroupsSection");
            ContactGroupsContainer = Bind<VisualElement>(root, "ContactGroupsContainer");
            NoContactSelectedState = Bind<VisualElement>(root, "NoContactSelectedState");

            if (!IsValid)
                return;

            NewContactButton.clicked += OnNewContactPressed;
            AllContactsButton.clicked += OnAllContactsPressed;
            FavoritesButton.clicked += OnFavoritesPressed;
            RecentlyAddedButton.clicked += OnRecentlyAddedPressed;
            NewGroupButton.clicked += OnNewGroupPressed;
            SettingsButton.clicked += OnSettingsPressed;
            ClearSearchButton.clicked += OnClearSearchPressed;
            SortButton.clicked += OnSortPressed;
            FavoriteContactButton.clicked += OnFavoriteContactPressed;
            EditContactButton.clicked += OnEditContactPressed;
            MoreContactButton.clicked += OnMoreContactPressed;
            MessageContactButton.clicked += OnMessageContactPressed;
            EmailContactButton.clicked += OnEmailContactPressed;
            CallContactButton.clicked += OnCallContactPressed;
            SearchField.RegisterValueChangedCallback(OnSearchChanged);
        }

        public void Dispose()
        {
            if (!IsValid)
                return;

            NewContactButton.clicked -= OnNewContactPressed;
            AllContactsButton.clicked -= OnAllContactsPressed;
            FavoritesButton.clicked -= OnFavoritesPressed;
            RecentlyAddedButton.clicked -= OnRecentlyAddedPressed;
            NewGroupButton.clicked -= OnNewGroupPressed;
            SettingsButton.clicked -= OnSettingsPressed;
            ClearSearchButton.clicked -= OnClearSearchPressed;
            SortButton.clicked -= OnSortPressed;
            FavoriteContactButton.clicked -= OnFavoriteContactPressed;
            EditContactButton.clicked -= OnEditContactPressed;
            MoreContactButton.clicked -= OnMoreContactPressed;
            MessageContactButton.clicked -= OnMessageContactPressed;
            EmailContactButton.clicked -= OnEmailContactPressed;
            CallContactButton.clicked -= OnCallContactPressed;
            SearchField.UnregisterValueChangedCallback(OnSearchChanged);
        }

        private void OnNewContactPressed() => NewContactPressed?.Invoke();
        private void OnAllContactsPressed() => AllContactsPressed?.Invoke();
        private void OnFavoritesPressed() => FavoritesPressed?.Invoke();
        private void OnRecentlyAddedPressed() => RecentlyAddedPressed?.Invoke();
        private void OnNewGroupPressed() => NewGroupPressed?.Invoke();
        private void OnSettingsPressed() => SettingsPressed?.Invoke();
        private void OnClearSearchPressed() => ClearSearchPressed?.Invoke();
        private void OnSortPressed() => SortPressed?.Invoke();
        private void OnFavoriteContactPressed() => FavoriteContactPressed?.Invoke();
        private void OnEditContactPressed() => EditContactPressed?.Invoke();
        private void OnMoreContactPressed() => MoreContactPressed?.Invoke();
        private void OnMessageContactPressed() => MessageContactPressed?.Invoke();
        private void OnEmailContactPressed() => EmailContactPressed?.Invoke();
        private void OnCallContactPressed() => CallContactPressed?.Invoke();
        private void OnSearchChanged(ChangeEvent<string> evt) => SearchChanged?.Invoke(evt.newValue);
    }
}