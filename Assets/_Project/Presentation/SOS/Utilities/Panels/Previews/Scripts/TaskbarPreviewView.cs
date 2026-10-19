using System;
using System.Collections.Generic;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    public sealed class TaskbarPreviewView : IDisposable
    {
        private readonly TemplateContainer host;
        private readonly TaskbarPreviewBinder binder;
        private readonly VisualElement layer;
        private readonly TaskbarPreviewViewBuilder builder;

        private readonly List<TaskbarPreviewCard> cards = new();

        public VisualElement Surface => binder.Root;

        public bool HasFocus
        {
            get
            {
                var focused = Surface.panel?.focusController
                    ?.focusedElement as VisualElement;

                return Contains(focused);
            }
        }

        public TaskbarPreviewView(
            TemplateContainer host,
            TaskbarPreviewBinder binder,
            VisualElement layer,
            TaskbarPreviewViewBuilder builder)
        {
            this.host = host ??
                throw new ArgumentNullException(nameof(host));

            this.binder = binder ??
                throw new ArgumentNullException(nameof(binder));

            this.layer = layer ??
                throw new ArgumentNullException(nameof(layer));

            this.builder = builder ??
                throw new ArgumentNullException(nameof(builder));
        }

        public bool Contains(VisualElement element)
        {
            return element != null &&
                (element == Surface || Surface.Contains(element));
        }

        public void Render(
            IReadOnlyList<AppWindowPreview> windows,
            Func<IAppWindow, Texture> getThumbnail,
            Action<IAppWindow> select,
            Action<IAppWindow> close)
        {
            if (windows == null)
                throw new ArgumentNullException(nameof(windows));

            if (getThumbnail == null)
                throw new ArgumentNullException(nameof(getThumbnail));

            int focusedIndex = GetFocusedIndex();
            bool sameWindows = cards.Count == windows.Count;

            if (sameWindows)
            {
                for (int i = 0; i < cards.Count; i++)
                {
                    if (ReferenceEquals(
                        cards[i].Window,
                        windows[i].Window))
                    {
                        continue;
                    }

                    sameWindows = false;
                    break;
                }
            }

            if (!sameWindows)
            {
                ClearCards();

                foreach (AppWindowPreview window in windows)
                {
                    TaskbarPreviewCard card =
                        builder.BuildCard(window, select, close);

                    cards.Add(card);
                    binder.Cards.Add(card.Root);
                }
            }

            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].Update(
                    windows[i],
                    getThumbnail(windows[i].Window));
            }

            if (!sameWindows &&
                focusedIndex >= 0 &&
                cards.Count > 0)
            {
                int nextIndex = Mathf.Min(
                    focusedIndex,
                    cards.Count - 1);

                cards[nextIndex].SelectButton.Focus();
            }
        }

        public bool Position(Button anchor)
        {
            float availableWidth = layer.contentRect.width;
            float availableHeight = layer.contentRect.height;

            if (anchor == null ||
                anchor.panel == null ||
                cards.Count == 0 ||
                availableWidth <= 24f ||
                availableHeight <= 24f ||
                float.IsNaN(availableWidth) ||
                float.IsInfinity(availableWidth) ||
                float.IsNaN(availableHeight) ||
                float.IsInfinity(availableHeight))
            {
                return false;
            }

            float maximumWidth = availableWidth - 16f;

            float cardWidth = Mathf.Clamp(
                (maximumWidth - 12f) / cards.Count - 4f,
                124f,
                224f);

            foreach (TaskbarPreviewCard card in cards)
                card.Root.style.width = cardWidth;

            float stripWidth = Mathf.Min(
                maximumWidth,
                12f + cards.Count * (cardWidth + 4f));

            Vector2 anchorCenter =
                layer.WorldToLocal(anchor.worldBound.center);

            float left = Mathf.Clamp(
                anchorCenter.x - stripWidth * 0.5f,
                8f,
                availableWidth - stripWidth - 8f);

            Surface.style.width = stripWidth;
            Surface.style.height =
                Mathf.Min(180f, availableHeight - 16f);

            Surface.style.left = left;
            Surface.style.bottom = 8f;

            return true;
        }

        public void Show()
        {
            host.style.display = DisplayStyle.Flex;

            layer.BringToFront();
            host.BringToFront();
        }

        public void Hide()
        {
            host.style.display = DisplayStyle.None;
        }

        public void FocusFirst()
        {
            if (cards.Count > 0)
                cards[0].SelectButton.Focus();
        }

        public void MoveFocus(int direction)
        {
            if (cards.Count == 0)
                return;

            int index = GetFocusedIndex();

            if (index < 0)
            {
                index = direction > 0
                    ? 0
                    : cards.Count - 1;
            }
            else
            {
                index =
                    (index + direction + cards.Count) % cards.Count;
            }

            cards[index].SelectButton.Focus();
        }

        private int GetFocusedIndex()
        {
            var focused = Surface.panel?.focusController
                ?.focusedElement as VisualElement;

            for (int i = 0; i < cards.Count; i++)
            {
                if (focused == cards[i].Root ||
                    (focused != null &&
                     cards[i].Root.Contains(focused)))
                {
                    return i;
                }
            }

            return -1;
        }

        private void ClearCards()
        {
            foreach (TaskbarPreviewCard card in cards)
                card.Dispose();

            cards.Clear();
        }

        public void Dispose()
        {
            ClearCards();
            host.RemoveFromHierarchy();
        }
    }
}