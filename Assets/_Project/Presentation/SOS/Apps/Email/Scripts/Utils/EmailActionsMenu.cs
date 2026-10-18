using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailActionsMenu : IDisposable
    {
        private readonly VisualElement root;
        private readonly Button anchor;

        private readonly List<Button> buttons = new();
        private readonly List<Action> cleanup = new();

        private VisualElement menu;
        private VisualElement eventRoot;

        private bool disposed;

        public EmailActionsMenu(
            VisualElement root,
            Button anchor)
        {
            this.root = root ??
                throw new ArgumentNullException(nameof(root));

            this.anchor = anchor ??
                throw new ArgumentNullException(nameof(anchor));
        }

        public void Toggle(
            bool canRead,
            bool canUnread,
            Action<bool> markSelected)
        {
            if (disposed)
                return;

            if (markSelected == null)
            {
                throw new ArgumentNullException(
                    nameof(markSelected));
            }

            if (menu != null)
            {
                Close(true);
                return;
            }

            if (root.panel == null ||
                (!canRead && !canUnread))
            {
                return;
            }

            menu = new VisualElement
            {
                name = "EmailActionsMenu",
                focusable = true
            };

            // All colors and hover states come from USS.
            menu.AddToClassList("mail-actions-menu");

            AddItem(
                "Mark as read",
                canRead,
                () => markSelected(true));

            AddItem(
                "Mark as unread",
                canUnread,
                () => markSelected(false));

            Rect bounds = anchor.worldBound;

            Vector2 position = root.WorldToLocal(
                new Vector2(
                    bounds.xMin,
                    bounds.yMax));

            float width = Mathf.Min(
                216f,
                Mathf.Max(1f, root.layout.width - 16f));

            menu.style.width = width;

            menu.style.left = Mathf.Clamp(
                position.x,
                8f,
                Mathf.Max(
                    8f,
                    root.layout.width - width - 8f));

            menu.style.top = Mathf.Clamp(
                position.y + 4f,
                8f,
                Mathf.Max(
                    8f,
                    root.layout.height - 94f));

            root.Add(menu);
            menu.BringToFront();

            eventRoot = root.panel.visualTree;

            eventRoot.RegisterCallback<PointerDownEvent>(
                OnPointerDown,
                TrickleDown.TrickleDown);

            eventRoot.RegisterCallback<KeyDownEvent>(
                OnKeyDown,
                TrickleDown.TrickleDown);

            foreach (Button button in buttons)
            {
                if (!button.enabledInHierarchy)
                    continue;

                button.Focus();
                return;
            }

            menu.Focus();
        }

        private void AddItem(
            string title,
            bool enabled,
            Action action)
        {
            Button button = new();

            button.AddToClassList("mail-actions-item");

            button.Add(
                EmailViewBuilder.Icon("icon-mail"));

            button.Add(
                EmailViewBuilder.Text(
                    title,
                    "mail-actions-title"));

            button.SetEnabled(enabled);

            Action clicked = () =>
            {
                Close(true);
                action();
            };

            button.clicked += clicked;

            cleanup.Add(() =>
            {
                button.clicked -= clicked;
            });

            buttons.Add(button);
            menu.Add(button);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (menu == null)
                return;

            VisualElement target =
                evt.target as VisualElement;

            if (target != null &&
                (target == menu ||
                 menu.Contains(target) ||
                 target == anchor ||
                 anchor.Contains(target)))
            {
                return;
            }

            // Allow the outside control to receive the click.
            Close(false);
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (menu == null)
                return;

            switch (evt.keyCode)
            {
                case KeyCode.Escape:
                    Close(true);
                    break;

                case KeyCode.DownArrow:
                    MoveFocus(1);
                    break;

                case KeyCode.UpArrow:
                    MoveFocus(-1);
                    break;

                default:
                    return;
            }

            evt.StopPropagation();
            evt.PreventDefault();
        }

        private void MoveFocus(int direction)
        {
            if (eventRoot?.panel == null ||
                buttons.Count == 0)
            {
                return;
            }

            Focusable focused =
                eventRoot.panel.focusController.focusedElement;

            int current = buttons.FindIndex(
                button => ReferenceEquals(button, focused));

            if (current < 0)
                current = direction > 0 ? -1 : 0;

            for (int step = 1; step <= buttons.Count; step++)
            {
                int next =
                    (current +
                     direction * step +
                     buttons.Count) %
                    buttons.Count;

                if (!buttons[next].enabledInHierarchy)
                    continue;

                buttons[next].Focus();
                return;
            }
        }

        public void Close(bool restoreFocus)
        {
            bool wasOpen = menu != null;

            if (eventRoot != null)
            {
                eventRoot.UnregisterCallback<PointerDownEvent>(
                    OnPointerDown,
                    TrickleDown.TrickleDown);

                eventRoot.UnregisterCallback<KeyDownEvent>(
                    OnKeyDown,
                    TrickleDown.TrickleDown);
            }

            eventRoot = null;

            foreach (Action action in cleanup)
                action();

            cleanup.Clear();
            buttons.Clear();

            menu?.RemoveFromHierarchy();
            menu = null;

            if (restoreFocus &&
                wasOpen &&
                anchor.panel != null &&
                anchor.enabledInHierarchy)
            {
                anchor.Focus();
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            Close(false);
            disposed = true;
        }
    }
}