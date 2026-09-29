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

        public ContactsView(ContactsBinder binder, ContactsViewBuilder builder)
        {
            this.binder = binder;
            this.builder = builder;
        }

        public void SetCounts(int allContacts, int favorites)
        {
            binder.AllContactsCountLabel.text = allContacts.ToString();
            binder.FavoritesCountLabel.text = favorites.ToString();
        }

        public void SetListHeader(string title, int count)
        {
            binder.ListTitleLabel.text = title;
            binder.ListCountLabel.text = count == 1 ? "1 contact" : $"{count} contacts";
        }

        public void SetSearchText(string value)
        {
            binder.SearchField.SetValueWithoutNotify(value ?? string.Empty);
        }

        public void SetSortMode(ContactsSortMode sortMode)
        {
            binder.SortButton.text = sortMode == ContactsSortMode.FirstNameAscending
                ? "A–Z  ▾"
                : "Last Name  ▾";
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
        }

        public void RenderGroups(
            IReadOnlyList<ContactGroup> groups,
            ContactGroup selectedGroup,
            Func<ContactGroup, int> countProvider,
            Action<ContactGroup> onSelected)
        {
            binder.GroupsContainer.Clear();

            foreach (ContactGroup group in groups)
            {
                VisualElement element = builder.BuildGroupItem(
                    group,
                    countProvider(group),
                    group == selectedGroup,
                    onSelected);

                binder.GroupsContainer.Add(element);
            }
        }

        public void RenderContacts(
            IReadOnlyList<ContactData> contacts,
            ContactData selectedContact,
            Action<ContactData> onSelected)
        {
            binder.ContactsContainer.Clear();

            bool hasContacts = contacts != null && contacts.Count > 0;

            binder.ContactsScrollView.style.display = hasContacts
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.ContactsEmptyState.style.display = hasContacts
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            if (!hasContacts)
                return;

            char? currentLetter = null;

            foreach (ContactData contact in contacts)
            {
                char letter = GetSectionLetter(contact);

                if (currentLetter != letter)
                {
                    currentLetter = letter;

                    Label section = new(letter.ToString());
                    section.AddToClassList("contact-list-section");
                    binder.ContactsContainer.Add(section);
                }

                bool selected = selectedContact != null &&
                                selectedContact.Id == contact.Id;

                binder.ContactsContainer.Add(
                    builder.BuildContactItem(contact, selected, onSelected));
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

            if (contact.ProfileImage != null)
            {
                binder.ProfileImage.image = contact.ProfileImage.texture;
                binder.ProfileImage.style.display = DisplayStyle.Flex;
                binder.ProfileInitialsLabel.style.display = DisplayStyle.None;
            }
            else
            {
                binder.ProfileImage.image = null;
                binder.ProfileImage.style.display = DisplayStyle.None;
                binder.ProfileInitialsLabel.style.display = DisplayStyle.Flex;
            }

            binder.FavoriteContactButton.text = contact.IsFavorite ? "★" : "☆";

            RenderProfileTags(contact);
            RenderContactInformation(contact);
            RenderPersonalInformation(contact);
            RenderNotes(contact);
            RenderContactGroups(contact);

            binder.MessageContactButton.SetEnabled(
                !string.IsNullOrWhiteSpace(contact.MobilePhone));

            binder.EmailContactButton.SetEnabled(
                !string.IsNullOrWhiteSpace(contact.PrimaryEmail));

            binder.CallContactButton.SetEnabled(
                !string.IsNullOrWhiteSpace(contact.PrimaryPhone));
        }

        public void ShowNoSelection()
        {
            binder.ContactDetailsScrollView.style.display = DisplayStyle.None;
            binder.NoContactSelectedState.style.display = DisplayStyle.Flex;
        }

        private void RenderProfileTags(ContactData contact)
        {
            binder.ProfileTagsContainer.Clear();

            foreach (ContactGroup group in contact.Groups)
            {
                if (group == ContactGroup.None)
                    continue;

                binder.ProfileTagsContainer.Add(builder.BuildTag(group.ToString()));
            }
        }

        private void RenderContactInformation(ContactData contact)
        {
            binder.ContactInformationContainer.Clear();

            AddField(
                binder.ContactInformationContainer,
                "Mobile",
                contact.MobilePhone);

            AddField(
                binder.ContactInformationContainer,
                "Home",
                contact.HomePhone);

            AddField(
                binder.ContactInformationContainer,
                "Work",
                contact.WorkPhone);

            AddField(
                binder.ContactInformationContainer,
                "Personal Email",
                contact.PersonalEmail);

            AddField(
                binder.ContactInformationContainer,
                "Work Email",
                contact.WorkEmail);

            binder.ContactInformationSection.style.display =
                binder.ContactInformationContainer.childCount > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        private void RenderPersonalInformation(ContactData contact)
        {
            binder.PersonalInformationContainer.Clear();

            AddField(
                binder.PersonalInformationContainer,
                "Birthday",
                contact.Birthday);

            AddField(
                binder.PersonalInformationContainer,
                "Address",
                contact.Address);

            binder.PersonalInformationSection.style.display =
                binder.PersonalInformationContainer.childCount > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        private void RenderNotes(ContactData contact)
        {
            bool hasNotes = !string.IsNullOrWhiteSpace(contact.Notes);

            binder.NotesSection.style.display = hasNotes
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            binder.NotesLabel.text = hasNotes
                ? contact.Notes
                : string.Empty;
        }

        private void RenderContactGroups(ContactData contact)
        {
            binder.ContactGroupsContainer.Clear();

            foreach (ContactGroup group in contact.Groups)
            {
                if (group == ContactGroup.None)
                    continue;

                binder.ContactGroupsContainer.Add(builder.BuildTag(group.ToString()));
            }

            binder.ContactGroupsSection.style.display =
                binder.ContactGroupsContainer.childCount > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }

        private void AddField(
            VisualElement container,
            string label,
            string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            container.Add(builder.BuildDetailField(label, value));
        }

        private static string BuildSubtitle(ContactData contact)
        {
            if (!string.IsNullOrWhiteSpace(contact.JobTitle) &&
                !string.IsNullOrWhiteSpace(contact.Organization))
                return $"{contact.JobTitle} · {contact.Organization}";

            if (!string.IsNullOrWhiteSpace(contact.JobTitle))
                return contact.JobTitle;

            return contact.Organization ?? string.Empty;
        }

        private static char GetSectionLetter(ContactData contact)
        {
            string value = !string.IsNullOrWhiteSpace(contact.FirstName)
                ? contact.FirstName
                : contact.DisplayName;

            return string.IsNullOrWhiteSpace(value)
                ? '#'
                : char.ToUpperInvariant(value[0]);
        }
    }
}