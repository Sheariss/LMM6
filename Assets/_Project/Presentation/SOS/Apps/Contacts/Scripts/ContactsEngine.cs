using System;
using System.Collections.Generic;
using System.Linq;

namespace Atlas.Presentation.SOS.Contacts
{
    public enum ContactsFilter
    {
        All,
        Favorites,
        RecentlyAdded,
        Group
    }

    public enum ContactsSortMode
    {
        FirstNameAscending,
        LastNameAscending
    }

    public sealed class ContactsEngine
    {
        private readonly ContactLibrary library;

        public ContactsEngine(ContactLibrary library)
        {
            this.library = library;
        }

        public IReadOnlyList<ContactData> GetContacts(
            ContactsFilter filter,
            string search,
            ContactsSortMode sortMode,
            ContactGroup group = ContactGroup.None)
        {
            if (library == null || library.Contacts == null)
                return Array.Empty<ContactData>();

            // ContactData has no added-date field.
            // The corresponding navigation button is disabled in the view.
            if (filter == ContactsFilter.RecentlyAdded)
                return Array.Empty<ContactData>();

            IEnumerable<ContactData> query = GetValidContacts();

            query = filter switch
            {
                ContactsFilter.Favorites =>
                    query.Where(contact => contact.IsFavorite),

                ContactsFilter.Group =>
                    query.Where(contact => contact.IsInGroup(group)),

                _ => query
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                string normalizedSearch = search.Trim();

                query = query.Where(contact =>
                    MatchesSearch(contact, normalizedSearch));
            }

            return query
                .OrderBy(
                    contact => GetSortName(contact, sortMode),
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    contact => sortMode == ContactsSortMode.LastNameAscending
                        ? contact.FirstName ?? string.Empty
                        : contact.LastName ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    contact => contact.DisplayName,
                    StringComparer.OrdinalIgnoreCase)
                .ThenBy(
                    contact => contact.Id ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public int GetAllCount()
        {
            return GetValidContacts().Count();
        }

        public int GetFavoritesCount()
        {
            return GetValidContacts().Count(contact => contact.IsFavorite);
        }

        public int GetGroupCount(ContactGroup group)
        {
            if (group == ContactGroup.None)
                return 0;

            return GetValidContacts().Count(contact => contact.IsInGroup(group));
        }

        public IReadOnlyList<ContactGroup> GetUsedGroups()
        {
            return GetValidContacts()
                .SelectMany(contact =>
                    contact.Groups ?? Array.Empty<ContactGroup>())
                .Where(group => group != ContactGroup.None)
                .Distinct()
                .OrderBy(group => group.ToString(), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public ContactData GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return GetValidContacts().FirstOrDefault(contact => contact.Id == id);
        }

        public static string GetSortName(
            ContactData contact,
            ContactsSortMode sortMode)
        {
            if (contact == null)
                return string.Empty;

            string preferredName = sortMode == ContactsSortMode.LastNameAscending
                ? contact.LastName
                : contact.FirstName;

            return string.IsNullOrWhiteSpace(preferredName)
                ? contact.DisplayName.Trim()
                : preferredName.Trim();
        }

        private IEnumerable<ContactData> GetValidContacts()
        {
            if (library == null || library.Contacts == null)
                return Enumerable.Empty<ContactData>();

            return library.Contacts.Where(contact => contact != null);
        }

        private static bool MatchesSearch(ContactData contact, string search)
        {
            return
                Contains(contact.DisplayName, search) ||
                Contains(contact.FirstName, search) ||
                Contains(contact.MiddleName, search) ||
                Contains(contact.LastName, search) ||
                Contains(contact.Nickname, search) ||
                Contains(contact.Organization, search) ||
                Contains(contact.JobTitle, search) ||
                Contains(contact.MobilePhone, search) ||
                Contains(contact.HomePhone, search) ||
                Contains(contact.WorkPhone, search) ||
                Contains(contact.PersonalEmail, search) ||
                Contains(contact.WorkEmail, search);
        }

        private static bool Contains(string source, string value)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}