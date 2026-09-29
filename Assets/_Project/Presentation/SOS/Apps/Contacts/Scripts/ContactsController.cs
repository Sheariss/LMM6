using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Contacts
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ContactsController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private ContactLibrary contactLibrary;

        private UIDocument document;
        private ContactsBinder binder;
        private ContactsViewBuilder viewBuilder;
        private ContactsView view;
        private ContactsEngine engine;

        private ContactsFilter currentFilter = ContactsFilter.All;
        private ContactsSortMode currentSort = ContactsSortMode.FirstNameAscending;
        private ContactGroup currentGroup = ContactGroup.None;
        private ContactData selectedContact;
        private string currentSearch = string.Empty;

        private void Awake()
        {
            document = GetComponent<UIDocument>();

            if (document == null)
            {
                Debug.LogError("[Contacts] UIDocument is missing.", this);
                enabled = false;
                return;
            }

            if (contactLibrary == null)
            {
                Debug.LogError("[Contacts] ContactLibrary is not assigned.", this);
                enabled = false;
                return;
            }

            binder = new ContactsBinder(document.rootVisualElement);

            if (!binder.IsValid)
            {
                Debug.LogError("[Contacts] UI binding failed.", this);
                enabled = false;
                return;
            }

            engine = new ContactsEngine(contactLibrary);
            viewBuilder = new ContactsViewBuilder();
            view = new ContactsView(binder, viewBuilder);

            RegisterEvents();
            Refresh();
            view.ShowNoSelection();
        }

        private void OnDestroy()
        {
            UnregisterEvents();
            binder?.Dispose();
        }

        private void RegisterEvents()
        {
            binder.AllContactsPressed += ShowAllContacts;
            binder.FavoritesPressed += ShowFavorites;
            binder.RecentlyAddedPressed += ShowRecentlyAdded;
            binder.ClearSearchPressed += ClearSearch;
            binder.SortPressed += ToggleSort;
            binder.SearchChanged += Search;
            binder.NewContactPressed += NewContact;
            binder.NewGroupPressed += NewGroup;
            binder.SettingsPressed += OpenSettings;
            binder.FavoriteContactPressed += ToggleFavorite;
            binder.EditContactPressed += EditSelectedContact;
            binder.MoreContactPressed += ShowMoreOptions;
            binder.MessageContactPressed += MessageSelectedContact;
            binder.EmailContactPressed += EmailSelectedContact;
            binder.CallContactPressed += CallSelectedContact;
        }

        private void UnregisterEvents()
        {
            if (binder == null)
                return;

            binder.AllContactsPressed -= ShowAllContacts;
            binder.FavoritesPressed -= ShowFavorites;
            binder.RecentlyAddedPressed -= ShowRecentlyAdded;
            binder.ClearSearchPressed -= ClearSearch;
            binder.SortPressed -= ToggleSort;
            binder.SearchChanged -= Search;
            binder.NewContactPressed -= NewContact;
            binder.NewGroupPressed -= NewGroup;
            binder.SettingsPressed -= OpenSettings;
            binder.FavoriteContactPressed -= ToggleFavorite;
            binder.EditContactPressed -= EditSelectedContact;
            binder.MoreContactPressed -= ShowMoreOptions;
            binder.MessageContactPressed -= MessageSelectedContact;
            binder.EmailContactPressed -= EmailSelectedContact;
            binder.CallContactPressed -= CallSelectedContact;
        }

        private void Refresh()
        {
            var contacts = engine.GetContacts(
                currentFilter,
                currentSearch,
                currentSort,
                currentGroup);

            view.SetCounts(
                engine.GetAllCount(),
                engine.GetFavoritesCount());

            view.SetNavigation(currentFilter, currentGroup);
            view.SetSortMode(currentSort);

            view.RenderGroups(
                engine.GetUsedGroups(),
                currentFilter == ContactsFilter.Group
                    ? currentGroup
                    : ContactGroup.None,
                engine.GetGroupCount,
                SelectGroup);

            view.SetListHeader(GetCurrentTitle(), contacts.Count);
            view.RenderContacts(contacts, selectedContact, SelectContact);

            if (selectedContact != null &&
                !ContainsContact(contacts, selectedContact.Id))
            {
                selectedContact = null;
                view.ShowNoSelection();
            }
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

        private void ShowRecentlyAdded()
        {
            currentFilter = ContactsFilter.RecentlyAdded;
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
            view.ShowContact(contact);
            Refresh();
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
                ContactsFilter.RecentlyAdded => "Recently Added",
                ContactsFilter.Group => currentGroup.ToString(),
                _ => "All Contacts"
            };
        }

        private static bool ContainsContact(
            System.Collections.Generic.IReadOnlyList<ContactData> contacts,
            string id)
        {
            foreach (ContactData contact in contacts)
            {
                if (contact.Id == id)
                    return true;
            }

            return false;
        }

        private void NewContact()
        {
            Debug.Log("[Contacts] New Contact is not implemented yet.");
        }

        private void NewGroup()
        {
            Debug.Log("[Contacts] New Group is not implemented yet.");
        }

        private void OpenSettings()
        {
            Debug.Log("[Contacts] Settings are not implemented yet.");
        }

        private void ToggleFavorite()
        {
            if (selectedContact == null)
                return;

            Debug.Log($"[Contacts] Toggle favorite: {selectedContact.DisplayName}");
        }

        private void EditSelectedContact()
        {
            if (selectedContact == null)
                return;

            Debug.Log($"[Contacts] Edit contact: {selectedContact.DisplayName}");
        }

        private void ShowMoreOptions()
        {
            if (selectedContact == null)
                return;

            Debug.Log($"[Contacts] More options: {selectedContact.DisplayName}");
        }

        private void MessageSelectedContact()
        {
            if (selectedContact == null)
                return;

            Debug.Log($"[Contacts] Message: {selectedContact.DisplayName}");
        }

        private void EmailSelectedContact()
        {
            if (selectedContact == null)
                return;

            Debug.Log($"[Contacts] Email: {selectedContact.DisplayName}");
        }

        private void CallSelectedContact()
        {
            if (selectedContact == null)
                return;

            Debug.Log($"[Contacts] Call: {selectedContact.DisplayName}");
        }
    }
}