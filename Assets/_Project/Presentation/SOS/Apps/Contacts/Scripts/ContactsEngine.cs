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
            if (library == null)
                return Array.Empty<ContactData>();

            IEnumerable<ContactData> query = library.Contacts.Where(contact => contact != null);

            query = filter switch
            {
                ContactsFilter.Favorites => query.Where(contact => contact.IsFavorite),
                ContactsFilter.Group => query.Where(contact => contact.IsInGroup(group)),
                _ => query
            };

            if (!string.IsNullOrWhiteSpace(search))
            {
                string normalizedSearch = search.Trim();

                query = query.Where(contact =>
                    Contains(contact.DisplayName, normalizedSearch) ||
                    Contains(contact.FirstName, normalizedSearch) ||
                    Contains(contact.MiddleName, normalizedSearch) ||
                    Contains(contact.LastName, normalizedSearch) ||
                    Contains(contact.Nickname, normalizedSearch) ||
                    Contains(contact.Organization, normalizedSearch) ||
                    Contains(contact.JobTitle, normalizedSearch) ||
                    Contains(contact.PrimaryPhone, normalizedSearch) ||
                    Contains(contact.PrimaryEmail, normalizedSearch));
            }

            query = sortMode switch
            {
                ContactsSortMode.LastNameAscending => query
                    .OrderBy(contact => contact.LastName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(contact => contact.FirstName ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                _ => query
                    .OrderBy(contact => contact.FirstName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(contact => contact.LastName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            };

            return query.ToList();
        }

        public int GetAllCount()
        {
            return library?.Contacts.Count(contact => contact != null) ?? 0;
        }

        public int GetFavoritesCount()
        {
            return library?.Contacts.Count(contact => contact != null && contact.IsFavorite) ?? 0;
        }

        public int GetGroupCount(ContactGroup group)
        {
            return library?.Contacts.Count(contact => contact != null && contact.IsInGroup(group)) ?? 0;
        }

        public IReadOnlyList<ContactGroup> GetUsedGroups()
        {
            if (library == null)
                return Array.Empty<ContactGroup>();

            return library.Contacts
                .Where(contact => contact != null)
                .SelectMany(contact => contact.Groups)
                .Where(group => group != ContactGroup.None)
                .Distinct()
                .OrderBy(group => group.ToString())
                .ToList();
        }

        public ContactData GetById(string id)
        {
            return library?.GetById(id);
        }

        private static bool Contains(string source, string value)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}