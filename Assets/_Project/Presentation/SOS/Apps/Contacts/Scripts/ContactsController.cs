using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;

namespace Atlas.Presentation.SOS.Contacts
{
    public sealed class ContactsController : AppContentController
    {
        [Header("Data")]
        [SerializeField] private ContactLibrary contactLibrary;

        private ContactsBinder binder;
        private ContactsView view;
        private ContactsEngine engine;

        private ContactsFilter currentFilter = ContactsFilter.All;
        private ContactsSortMode currentSort = ContactsSortMode.FirstNameAscending;
        private ContactGroup currentGroup = ContactGroup.None;
        private ContactData selectedContact;
        private string currentSearch = string.Empty;

        protected override void OnInitialize()
        {
            if (contactLibrary == null)
            {
                throw new InvalidOperationException(
                    "[Contacts] ContactLibrary is not assigned.");
            }

            binder = new ContactsBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "[Contacts] UI binding failed.");
            }

            engine = new ContactsEngine(contactLibrary);
            view = new ContactsView(binder, new ContactsViewBuilder());

            RegisterEvents();

            view.ConfigureUnavailableActions();
            view.SetSearchText(currentSearch);

            // Groups are authored data and do not change through this UI.
            // Build their buttons once so filtering does not destroy focus.
            view.RenderGroups(
                engine.GetUsedGroups(),
                ContactGroup.None,
                engine.GetGroupCount,
                SelectGroup);

            Refresh();
        }

        protected override void OnShutdown()
        {
            UnregisterEvents();

            view?.Clear();
            binder?.Dispose();

            binder = null;
            view = null;
            engine = null;

            selectedContact = null;
            currentFilter = ContactsFilter.All;
            currentSort = ContactsSortMode.FirstNameAscending;
            currentGroup = ContactGroup.None;
            currentSearch = string.Empty;
        }

        private void RegisterEvents()
        {
            binder.AllContactsPressed += ShowAllContacts;
            binder.FavoritesPressed += ShowFavorites;
            binder.ClearSearchPressed += ClearSearch;
            binder.SortPressed += ToggleSort;
            binder.SearchChanged += Search;
        }

        private void UnregisterEvents()
        {
            if (binder == null)
                return;

            binder.AllContactsPressed -= ShowAllContacts;
            binder.FavoritesPressed -= ShowFavorites;
            binder.ClearSearchPressed -= ClearSearch;
            binder.SortPressed -= ToggleSort;
            binder.SearchChanged -= Search;
        }

        private void Refresh()
        {
            IReadOnlyList<ContactData> contacts = engine.GetContacts(
                currentFilter,
                currentSearch,
                currentSort,
                currentGroup);

            // Validate selection before rendering its highlight.
            if (selectedContact != null &&
                !ContainsContact(contacts, selectedContact))
            {
                selectedContact = null;
            }

            view.SetCounts(
                engine.GetAllCount(),
                engine.GetFavoritesCount());

            view.SetNavigation(currentFilter, currentGroup);
            view.SetSortMode(currentSort);
            view.SetSearchState(currentSearch);
            view.SetListHeader(GetCurrentTitle(), contacts.Count);

            view.RenderContacts(
                contacts,
                selectedContact,
                SelectContact,
                currentSort);

            if (selectedContact == null)
                view.ShowNoSelection();
            else
                view.ShowContact(selectedContact);
        }

        private void ShowAllContacts()
        {
            currentFilter = ContactsFilter.All;
            currentGroup = ContactGroup.None;
            Refresh();
        }

        private void ShowFavorites()
        {
            currentFilter = ContactsFilter.Favorites;
            currentGroup = ContactGroup.None;
            Refresh();
        }

        private void SelectGroup(ContactGroup group)
        {
            currentFilter = ContactsFilter.Group;
            currentGroup = group;
            Refresh();
        }

        private void SelectContact(ContactData contact)
        {
            selectedContact = contact;

            // Update the existing rows instead of rebuilding the list.
            // This preserves keyboard focus and the current scroll position.
            view.SetSelectedContact(contact);
            view.ShowContact(contact);
        }

        private void Search(string value)
        {
            currentSearch = value ?? string.Empty;
            Refresh();
        }

        private void ClearSearch()
        {
            currentSearch = string.Empty;
            view.SetSearchText(string.Empty);
            Refresh();
            binder.SearchField.Focus();
        }

        private void ToggleSort()
        {
            currentSort = currentSort == ContactsSortMode.FirstNameAscending
                ? ContactsSortMode.LastNameAscending
                : ContactsSortMode.FirstNameAscending;

            Refresh();
        }

        private string GetCurrentTitle()
        {
            return currentFilter switch
            {
                ContactsFilter.Favorites => "Favorites",
                ContactsFilter.Group => currentGroup.ToString(),
                ContactsFilter.RecentlyAdded => "Recently Added",
                _ => "All Contacts"
            };
        }

        private static bool ContainsContact(
            IReadOnlyList<ContactData> contacts,
            ContactData selected)
        {
            foreach (ContactData contact in contacts)
            {
                if (ReferenceEquals(contact, selected))
                    return true;
            }

            return false;
        }
    }
}