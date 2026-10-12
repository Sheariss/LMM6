using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Contacts
{
    public sealed class ContactsViewBuilder
    {
        public Button BuildContactItem(
            ContactData contact,
            bool selected,
            Action<ContactData> onSelected)
        {
            if (contact == null)
                throw new ArgumentNullException(nameof(contact));

            Button root = new()
            {
                text = string.Empty,
                tooltip = contact.DisplayName
            };

            root.AddToClassList("spacing-reset");
            root.AddToClassList("contact-item");
            root.EnableInClassList("contact-item--selected", selected);

            VisualElement avatar = new()
            {
                pickingMode = PickingMode.Ignore
            };

            avatar.AddToClassList("contact-item-avatar");

            if (contact.ProfileImage != null)
            {
                avatar.style.backgroundImage =
                    new StyleBackground(contact.ProfileImage);
            }
            else
            {
                avatar.Add(BuildLabel(
                    contact.Initials,
                    "contact-item-initials"));
            }

            VisualElement content = new()
            {
                pickingMode = PickingMode.Ignore
            };

            content.AddToClassList("contact-item-content");

            content.Add(BuildLabel(
                contact.DisplayName,
                "contact-item-name"));

            string secondaryText = GetSecondaryText(contact);

            if (!string.IsNullOrWhiteSpace(secondaryText))
            {
                content.Add(BuildLabel(
                    secondaryText,
                    "contact-item-secondary"));
            }

            root.Add(avatar);
            root.Add(content);

            if (contact.IsFavorite)
            {
                Image favoriteIcon = new()
                {
                    pickingMode = PickingMode.Ignore
                };

                favoriteIcon.AddToClassList("contact-item-favorite");
                root.Add(favoriteIcon);
            }

            // Button supports pointer clicks and keyboard activation.
            root.clicked += () => onSelected?.Invoke(contact);

            return root;
        }

        public Button BuildGroupItem(
    ContactGroup group,
    int count,
    bool selected,
    Action<ContactGroup> onSelected)
        {
            Button button = new()
            {
                text = string.Empty,
                tooltip = $"{group}: {count} contacts"
            };

            button.AddToClassList("spacing-reset");
            button.AddToClassList("contacts-group-button");

            button.EnableInClassList(
                "contacts-group-button--active",
                selected);

            string iconClass = group switch
            {
                ContactGroup.Friends => "contacts-group-icon--friends",
                ContactGroup.Work => "contacts-group-icon--work",
                _ => null
            };

            if (iconClass != null)
            {
                Image icon = new()
                {
                    pickingMode = PickingMode.Ignore
                };

                icon.AddToClassList("contacts-group-icon");
                icon.AddToClassList(iconClass);
                button.Add(icon);
            }

            button.Add(BuildLabel(
                group.ToString(),
                "contacts-group-name"));

            button.Add(BuildLabel(
                count.ToString(),
                "contacts-group-count"));

            button.clicked += () => onSelected?.Invoke(group);

            return button;
        }

        public VisualElement BuildDetailField(string label, string value)
        {
            VisualElement root = new();
            root.AddToClassList("contact-detail-field");

            root.Add(BuildLabel(
                label,
                "contact-detail-field-label"));

            root.Add(BuildLabel(
                value,
                "contact-detail-field-value"));

            return root;
        }

        public VisualElement BuildTag(string text)
        {
            return BuildLabel(text, "contact-tag");
        }

        private static Label BuildLabel(string text, string className)
        {
            Label label = new(text ?? string.Empty)
            {
                pickingMode = PickingMode.Ignore
            };

            label.AddToClassList("spacing-reset");
            label.AddToClassList(className);

            return label;
        }

        private static string GetSecondaryText(ContactData contact)
        {
            if (!string.IsNullOrWhiteSpace(contact.PrimaryPhone))
                return contact.PrimaryPhone;

            if (!string.IsNullOrWhiteSpace(contact.PrimaryEmail))
                return contact.PrimaryEmail;

            if (!string.IsNullOrWhiteSpace(contact.Organization))
                return contact.Organization;

            return string.Empty;
        }
    }
}