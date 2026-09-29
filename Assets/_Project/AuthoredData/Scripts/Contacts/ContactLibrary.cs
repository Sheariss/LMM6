using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Contacts
{
    [CreateAssetMenu(
        fileName = "ContactLibrary",
        menuName = "ATLAS/SOS/Contacts/Contact Library")]
    public sealed class ContactLibrary : ScriptableObject
    {
        [SerializeField] private List<ContactData> contacts = new();

        public IReadOnlyList<ContactData> Contacts => contacts;
        public int Count => contacts.Count;

        public ContactData GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            foreach (ContactData contact in contacts)
            {
                if (contact != null && contact.Id == id)
                    return contact;
            }

            return null;
        }

        public bool Contains(string id)
        {
            return GetById(id) != null;
        }
    }

    [Serializable]
    public sealed class ContactData
    {
        [Header("Identity")]
        [SerializeField] private string id;
        [SerializeField] private string firstName;
        [SerializeField] private string middleName;
        [SerializeField] private string lastName;
        [SerializeField] private string nickname;
        [SerializeField] private Sprite profileImage;

        [Header("Professional")]
        [SerializeField] private string jobTitle;
        [SerializeField] private string organization;

        [Header("Contact Information")]
        [SerializeField] private string mobilePhone;
        [SerializeField] private string homePhone;
        [SerializeField] private string workPhone;
        [SerializeField] private string personalEmail;
        [SerializeField] private string workEmail;

        [Header("Personal Information")]
        [SerializeField] private string birthday;
        [SerializeField] private string address;

        [Header("Additional Information")]
        [TextArea(3, 6)]
        [SerializeField] private string notes;

        [Header("Organization")]
        [SerializeField] private bool isFavorite;
        [SerializeField] private List<ContactGroup> groups = new();

        public string Id => id;
        public string FirstName => firstName;
        public string MiddleName => middleName;
        public string LastName => lastName;
        public string Nickname => nickname;
        public Sprite ProfileImage => profileImage;
        public string JobTitle => jobTitle;
        public string Organization => organization;
        public string MobilePhone => mobilePhone;
        public string HomePhone => homePhone;
        public string WorkPhone => workPhone;
        public string PersonalEmail => personalEmail;
        public string WorkEmail => workEmail;
        public string Birthday => birthday;
        public string Address => address;
        public string Notes => notes;
        public bool IsFavorite => isFavorite;
        public IReadOnlyList<ContactGroup> Groups => groups;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(firstName) &&
                    !string.IsNullOrWhiteSpace(lastName))
                    return $"{firstName} {lastName}";

                if (!string.IsNullOrWhiteSpace(firstName))
                    return firstName;

                if (!string.IsNullOrWhiteSpace(lastName))
                    return lastName;

                return "Unknown Contact";
            }
        }

        public string Initials
        {
            get
            {
                string initials = "";

                if (!string.IsNullOrWhiteSpace(firstName))
                    initials += char.ToUpper(firstName[0]);

                if (!string.IsNullOrWhiteSpace(lastName))
                    initials += char.ToUpper(lastName[0]);

                return string.IsNullOrEmpty(initials) ? "?" : initials;
            }
        }

        public string PrimaryPhone
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(mobilePhone))
                    return mobilePhone;

                if (!string.IsNullOrWhiteSpace(workPhone))
                    return workPhone;

                return homePhone;
            }
        }

        public string PrimaryEmail
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(personalEmail))
                    return personalEmail;

                return workEmail;
            }
        }

        public bool IsInGroup(ContactGroup group)
        {
            return groups != null && groups.Contains(group);
        }
    }
}