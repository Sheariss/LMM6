using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Contacts
{
    public sealed class ContactsView
    {
        private readonly ContactsBinder binder;
        private readonly ContactsViewBuilder builder;

        private readonly List<(ContactData Contact, Button Button)> contactRows = new();
        private readonly Dictionary<ContactGroup, Button> groupButtons = new();

        public ContactsView(
            ContactsBinder binder,
            ContactsViewBuilder builder)
        {
            this.binder = binder ?? throw new ArgumentNullException(nameof(binder));
            this.builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        public void ConfigureUnavailableActions()
        {
            Disable(
                binder.RecentlyAddedButton,
                "Recently Added is unavailable because contacts do not store an added date.");

            Disable(binder.NewContactButton, "Creating contacts is not implemented yet.");
            Disable(binder.NewGroupButton, "Creating groups is not implemented yet.");
            Disable(binder.SettingsButton, "Contact settings are not implemented yet.");

            Disable(
                binder.FavoriteContactButton,
                "Favorite status is currently configured in the ContactLibrary asset.");

            Disable(binder.EditContactButton, "Editing contacts is not implemented yet.");
            Disable(binder.MoreContactButton, "Additional contact actions are not implemented yet.");
            Disable(binder.MessageContactButton, "Messaging from Contacts is not implemented yet.");
            Disable(binder.EmailContactButton, "Email from Contacts is not implemented yet.");
            Disable(binder.CallContactButton, "Calling from Contacts is not implemented yet.");

            ConfigureScrollView(binder.GroupsScrollView);
            ConfigureScrollView(binder.ContactsScrollView);
            ConfigureScrollView(binder.ContactDetailsScrollView);

            binder.ProfileImage.scaleMode = ScaleMode.ScaleAndCrop;
        }

        public void SetCounts(int allContacts, int favorites)
        {
            binder.AllContactsCountLabel.text = allContacts.ToString();
            binder.FavoritesCountLabel.text = favorites.ToString();
        }

        public void SetListHeader(string title, int count)
        {
            binder.ListTitleLabel.text = title;
            binder.ListCountLabel.text = count == 1
                ? "1 contact"
                : $"{count} contacts";
        }

        public void SetSearchText(string value)
        {
            string search = value ?? string.Empty;
            binder.SearchField.SetValueWithoutNotify(search);
            SetSearchState(search);
        }

        public void SetSearchState(string value)
        {
            binder.ClearSearchButton.SetEnabled(!string.IsNullOrEmpty(value));
        }

        public void SetSortMode(ContactsSortMode sortMode)
        {
            bool byFirstName = sortMode == ContactsSortMode.FirstNameAscending;

            // To Do: set better sort system
            binder.SortButton.text = byFirstName
                ? "First Name ▾"
                : "Last Name ▾";

            binder.SortButton.tooltip = byFirstName
                ? "Sorted by first name. Click to sort by last name."
                : "Sorted by last name. Click to sort by first name.";
        }

        public void SetNavigation(
            ContactsFilter filter,
            ContactGroup selectedGroup = ContactGroup.None)
        {
            binder.AllContactsButton.EnableInClassList(
                "contacts-nav-button--active",
                filter == ContactsFilter.All);

            binder.FavoritesButton.EnableInClassList(
                "contacts-nav-button--active",
                filter == ContactsFilter.Favorites);

            binder.RecentlyAddedButton.EnableInClassList(
                "contacts-nav-button--active",
                filter == ContactsFilter.RecentlyAdded);

            foreach (KeyValuePair<ContactGroup, Button> entry in groupButtons)
            {
                entry.Value.EnableInClassList(
                    "contacts-group-button--active",
                    filter == ContactsFilter.Group &&
                    entry.Key == selectedGroup);
            }
        }

        public void RenderGroups(
            IReadOnlyList<ContactGroup> groups,
            ContactGroup selectedGroup,
            Func<ContactGroup, int> countProvider,
            Action<ContactGroup> onSelected)
        {
            binder.GroupsContainer.Clear();
            groupButtons.Clear();

            if (groups == null)
                return;

            foreach (ContactGroup group in groups)
            {
                if (group == ContactGroup.None || groupButtons.ContainsKey(group))
                    continue;

                Button button = builder.BuildGroupItem(
                    group,
                    countProvider?.Invoke(group) ?? 0,
                    group == selectedGroup,
                    onSelected);

                groupButtons.Add(group, button);
                binder.GroupsContainer.Add(button);
            }
        }

        public void RenderContacts(
            IReadOnlyList<ContactData> contacts,
            ContactData selectedContact,
            Action<ContactData> onSelected,
            ContactsSortMode sortMode = ContactsSortMode.FirstNameAscending)
        {
            binder.ContactsContainer.Clear();
            contactRows.Clear();

            if (contacts != null)
            {
                char? currentLetter = null;

                foreach (ContactData contact in contacts)
                {
                    if (contact == null)
                        continue;

                    char letter = GetSectionLetter(contact, sortMode);

                    if (currentLetter != letter)
                    {
                        currentLetter = letter;

                        Label section = new(letter.ToString());
                        section.AddToClassList("contact-list-section");
                        section.pickingMode = PickingMode.Ignore;

                        binder.ContactsContainer.Add(section);
                    }

                    Button row = builder.BuildContactItem(
                        contact,
                        ReferenceEquals(selectedContact, contact),
                        onSelected);

                    contactRows.Add((contact, row));
                    binder.ContactsContainer.Add(row);
                }
            }

            bool hasContacts = contactRows.Count > 0;

            binder.ContactsScrollView.style.display = hasContacts
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.ContactsEmptyState.style.display = hasContacts
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        public void SetSelectedContact(ContactData selectedContact)
        {
            foreach (var row in contactRows)
            {
                row.Button.EnableInClassList(
                    "contact-item--selected",
                    ReferenceEquals(row.Contact, selectedContact));
            }
        }

        public void ShowContact(ContactData contact)
        {
            if (contact == null)
            {
                ShowNoSelection();
                return;
            }

            binder.NoContactSelectedState.style.display = DisplayStyle.None;
            binder.ContactDetailsScrollView.style.display = DisplayStyle.Flex;

            binder.ProfileNameLabel.text = contact.DisplayName;
            binder.ProfileSubtitleLabel.text = BuildSubtitle(contact);
            binder.ProfileInitialsLabel.text = contact.Initials;

            // Assign the sprite so packed sprites retain their own UV region.
            binder.ProfileImage.image = null;
            binder.ProfileImage.sprite = contact.ProfileImage;

            bool hasImage = contact.ProfileImage != null;

            binder.ProfileImage.style.display = hasImage
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.ProfileInitialsLabel.style.display = hasImage
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            binder.FavoriteContactButton.text = string.Empty;
            binder.FavoriteContactButton.EnableInClassList(
                "contacts-favorite-button--active",
                contact.IsFavorite);

            binder.FavoriteContactButton.tooltip = contact.IsFavorite
                ? "Remove from favorites"
                : "Add to favorites";

            RenderGroupsAsTags(binder.ProfileTagsContainer, contact.Groups);
            RenderContactInformation(contact);
            RenderPersonalInformation(contact);
            RenderNotes(contact);
            RenderContactGroups(contact);

            // Action buttons remain disabled until their handlers are implemented.
        }

        public void ShowNoSelection()
        {
            binder.ContactDetailsScrollView.style.display = DisplayStyle.None;
            binder.NoContactSelectedState.style.display = DisplayStyle.Flex;

            binder.ProfileImage.image = null;
            binder.ProfileImage.sprite = null;
        }

        public void Clear()
        {
            contactRows.Clear();
            groupButtons.Clear();

            binder.ContactsContainer.Clear();
            binder.GroupsContainer.Clear();
            binder.ProfileTagsContainer.Clear();
            binder.ContactInformationContainer.Clear();
            binder.PersonalInformationContainer.Clear();
            binder.ContactGroupsContainer.Clear();

            binder.ProfileImage.image = null;
            binder.ProfileImage.sprite = null;
        }

        private void RenderContactInformation(ContactData contact)
        {
            VisualElement container = binder.ContactInformationContainer;
            container.Clear();

            AddField(container, "Mobile", contact.MobilePhone);
            AddField(container, "Home", contact.HomePhone);
            AddField(container, "Work", contact.WorkPhone);
            AddField(container, "Personal Email", contact.PersonalEmail);
            AddField(container, "Work Email", contact.WorkEmail);

            SetSectionVisible(
                binder.ContactInformationSection,
                container.childCount > 0);
        }

        private void RenderPersonalInformation(ContactData contact)
        {
            VisualElement container = binder.PersonalInformationContainer;
            container.Clear();

            AddField(container, "Nickname", contact.Nickname);
            AddField(container, "Birthday", contact.Birthday);
            AddField(container, "Address", contact.Address);

            SetSectionVisible(
                binder.PersonalInformationSection,
                container.childCount > 0);
        }

        private void RenderNotes(ContactData contact)
        {
            bool hasNotes = !string.IsNullOrWhiteSpace(contact.Notes);

            binder.NotesLabel.text = hasNotes
                ? contact.Notes
                : string.Empty;

            SetSectionVisible(binder.NotesSection, hasNotes);
        }

        private void RenderContactGroups(ContactData contact)
        {
            RenderGroupsAsTags(
                binder.ContactGroupsContainer,
                contact.Groups);

            SetSectionVisible(
                binder.ContactGroupsSection,
                binder.ContactGroupsContainer.childCount > 0);
        }

        private void RenderGroupsAsTags(
            VisualElement container,
            IReadOnlyList<ContactGroup> groups)
        {
            container.Clear();

            if (groups == null)
                return;

            HashSet<ContactGroup> renderedGroups = new();

            foreach (ContactGroup group in groups)
            {
                if (group == ContactGroup.None || !renderedGroups.Add(group))
                    continue;

                container.Add(builder.BuildTag(group.ToString()));
            }
        }

        private void AddField(
            VisualElement container,
            string label,
            string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
                container.Add(builder.BuildDetailField(label, value));
        }

        private static void SetSectionVisible(
            VisualElement section,
            bool visible)
        {
            section.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        private static void Disable(Button button, string tooltip)
        {
            button.tooltip = tooltip;
            button.SetEnabled(false);
        }

        private static void ConfigureScrollView(ScrollView scrollView)
        {
            scrollView.mode = ScrollViewMode.Vertical;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        }

        private static string BuildSubtitle(ContactData contact)
        {
            if (!string.IsNullOrWhiteSpace(contact.JobTitle) &&
                !string.IsNullOrWhiteSpace(contact.Organization))
            {
                return $"{contact.JobTitle} · {contact.Organization}";
            }

            if (!string.IsNullOrWhiteSpace(contact.JobTitle))
                return contact.JobTitle;

            return contact.Organization ?? string.Empty;
        }

        private static char GetSectionLetter(
            ContactData contact,
            ContactsSortMode sortMode)
        {
            string value = ContactsEngine.GetSortName(contact, sortMode);

            if (string.IsNullOrWhiteSpace(value))
                return '#';

            char firstCharacter = char.ToUpperInvariant(value[0]);

            return char.IsLetter(firstCharacter)
                ? firstCharacter
                : '#';
        }
    }
}