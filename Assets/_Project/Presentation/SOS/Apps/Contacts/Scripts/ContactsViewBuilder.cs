using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Contacts
{
    public sealed class ContactsViewBuilder
    {
        public VisualElement BuildContactItem(
            ContactData contact,
            bool selected,
            Action<ContactData> onSelected)
        {
            VisualElement root = new();
            root.AddToClassList("contact-item");

            if (selected)
                root.AddToClassList("contact-item--selected");

            VisualElement avatar = new();
            avatar.AddToClassList("contact-item-avatar");

            if (contact.ProfileImage != null)
            {
                avatar.style.backgroundImage = new StyleBackground(contact.ProfileImage);
            }
            else
            {
                Label initials = new(contact.Initials);
                initials.AddToClassList("contact-item-initials");
                avatar.Add(initials);
            }

            VisualElement text = new();
            text.AddToClassList("contact-item-content");

            Label name = new(contact.DisplayName);
            name.AddToClassList("contact-item-name");

            string secondaryText = GetSecondaryText(contact);
            Label secondary = new(secondaryText);
            secondary.AddToClassList("contact-item-secondary");

            text.Add(name);

            if (!string.IsNullOrWhiteSpace(secondaryText))
                text.Add(secondary);

            Label favorite = new(contact.IsFavorite ? "★" : "");
            favorite.AddToClassList("contact-item-favorite");

            root.Add(avatar);
            root.Add(text);
            root.Add(favorite);

            root.RegisterCallback<ClickEvent>(_ => onSelected?.Invoke(contact));

            return root;
        }

        public VisualElement BuildGroupItem(
            ContactGroup group,
            int count,
            bool selected,
            Action<ContactGroup> onSelected)
        {
            Button button = new();
            button.AddToClassList("contacts-group-button");

            if (selected)
                button.AddToClassList("contacts-group-button--active");

            Label name = new(group.ToString());
            name.AddToClassList("contacts-group-name");

            Label countLabel = new(count.ToString());
            countLabel.AddToClassList("contacts-group-count");

            button.Add(name);
            button.Add(countLabel);
            button.clicked += () => onSelected?.Invoke(group);

            return button;
        }

        public VisualElement BuildDetailField(string label, string value)
        {
            VisualElement root = new();
            root.AddToClassList("contact-detail-field");

            Label heading = new(label);
            heading.AddToClassList("contact-detail-field-label");

            Label content = new(value);
            content.AddToClassList("contact-detail-field-value");

            root.Add(heading);
            root.Add(content);

            return root;
        }

        public VisualElement BuildTag(string text)
        {
            Label tag = new(text);
            tag.AddToClassList("contact-tag");
            return tag;
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