using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.SOS.StickyNotes
{
    public sealed class StickyNotesViewBuilder
    {
        private const string MenuIconResource =
            "Icons/UI/More-Horiz-Menu/more_horiz_24dp";

        private readonly Texture2D menuIcon;

        public sealed class Card
        {
            public VisualElement Root;
            public Label Title;
            public Label Preview;
            public Label Stamp;

            public Button Menu;
            public Image MenuImage;
        }

        public StickyNotesViewBuilder()
        {
            menuIcon = Resources.Load<Texture2D>(
                MenuIconResource);

            if (menuIcon == null)
            {
                Debug.LogWarning(
                    "[StickyNotes] Could not load menu icon at " +
                    $"Resources/{MenuIconResource}");
            }
        }

        public Card BuildCard(
            string id,
            Action<string> select,
            Action<string> open,
            Action<string, Vector2> showMenu)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A note ID is required.", nameof(id));

            if (select == null)
                throw new ArgumentNullException(nameof(select));

            if (open == null)
                throw new ArgumentNullException(nameof(open));

            if (showMenu == null)
                throw new ArgumentNullException(nameof(showMenu));

            Card card = new()
            {
                Root = new VisualElement
                {
                    name = "Note-" + id,
                    focusable = true,
                    tabIndex = 0
                },

                Title = new Label
                {
                    enableRichText = false,
                    pickingMode = PickingMode.Ignore
                },

                Preview = new Label
                {
                    enableRichText = false,
                    pickingMode = PickingMode.Ignore
                },

                Stamp = new Label
                {
                    enableRichText = false,
                    pickingMode = PickingMode.Ignore
                },

                Menu = new Button
                {
                    text = string.Empty,
                    tooltip = "Note actions"
                },

                MenuImage = new Image
                {
                    name = "note-menu-image",
                    pickingMode = PickingMode.Ignore
                }
            };

            card.Root.AddToClassList("sn-card");
            card.Title.AddToClassList("sn-card-title");
            card.Preview.AddToClassList("sn-card-preview");
            card.Stamp.AddToClassList("sn-card-stamp");

            card.Menu.AddToClassList("sn-card-menu");
            card.MenuImage.AddToClassList("sn-card-menu-image");

            if (menuIcon != null)
            {
                card.MenuImage.style.backgroundImage =
                    new StyleBackground(menuIcon);
            }

            card.Menu.Add(card.MenuImage);

            VisualElement header = new();
            header.AddToClassList("sn-card-header");

            header.Add(card.Stamp);
            header.Add(card.Menu);

            card.Root.Add(header);
            card.Root.Add(card.Title);
            card.Root.Add(card.Preview);

            RegisterInteractions(
                card,
                id,
                select,
                open,
                showMenu);

            return card;
        }

        private static void RegisterInteractions(
            Card card,
            string id,
            Action<string> select,
            Action<string> open,
            Action<string, Vector2> showMenu)
        {
            card.Menu.clicked += () =>
            {
                Vector2 position = card.Menu.LocalToWorld(
                    new Vector2(0f, card.Menu.layout.height));

                showMenu(id, position);
            };

            card.Root.RegisterCallback<ClickEvent>(evt =>
            {
                if (IsMenuTarget(card, evt.target as VisualElement))
                    return;

                if (evt.button != 0)
                    return;

                card.Root.Focus();
                select(id);

                if (evt.clickCount == 2)
                    open(id);
            });

            card.Root.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 1)
                    return;

                showMenu(
                    id,
                    new Vector2(evt.position.x, evt.position.y));

                evt.StopPropagation();
            });

            card.Root.RegisterCallback<KeyDownEvent>(evt =>
            {
                // Let the menu button handle its own keyboard activation.
                if (IsMenuTarget(card, evt.target as VisualElement))
                    return;

                if (evt.keyCode != KeyCode.Return &&
                    evt.keyCode != KeyCode.KeypadEnter)
                {
                    return;
                }

                open(id);
                evt.StopPropagation();
            });
        }

        private static bool IsMenuTarget(
            Card card,
            VisualElement target)
        {
            return target != null &&
                (target == card.Menu || card.Menu.Contains(target));
        }

        public void UpdateCard(
            Card card,
            StickyNoteData note,
            bool selected)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));

            if (note == null)
                throw new ArgumentNullException(nameof(note));

            string text = (note.text ?? string.Empty).Trim();
            int newline = text.IndexOf('\n');

            string title = newline >= 0
                ? text.Substring(0, newline).TrimEnd('\r')
                : text;

            string preview = newline >= 0
                ? text.Substring(newline + 1).Trim()
                : string.Empty;

            card.Title.text = title.Length == 0
                ? "Untitled note"
                : Limit(title, 70);

            card.Preview.text = Limit(preview, 200);

            card.Preview.style.display = preview.Length > 0
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            card.Stamp.text = note.stamp ?? string.Empty;

            StickyNotePalette palette =
                StickyNotesPalette.Get(note.color);

            card.Root.style.backgroundColor = palette.Body;
            card.Root.style.borderTopColor = palette.Header;

            card.Title.style.color = palette.Text;
            card.Preview.style.color = palette.Text;

            card.MenuImage.style.unityBackgroundImageTintColor =
                palette.Text;

            Color stampColor = palette.Text;
            stampColor.a = 0.7f;

            card.Stamp.style.color = stampColor;

            card.Root.EnableInClassList(
                "sn-card--selected",
                selected);
        }

        private static string Limit(
            string text,
            int maximum)
        {
            return text.Length <= maximum
                ? text
                : text.Substring(0, maximum - 1) + "…";
        }
    }
}