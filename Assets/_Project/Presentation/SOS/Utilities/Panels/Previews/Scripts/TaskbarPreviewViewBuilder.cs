using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Taskbar
{
    internal sealed class TaskbarPreviewCard
    {
        public VisualElement Root;
        public Button SelectButton;
        public Button CloseButton;

        public Image Icon;
        public Label Title;
        public Image Thumbnail;

        public IAppWindow Window;

        public Action SelectHandler;
        public Action CloseHandler;

        public void Update(
            AppWindowPreview window,
            Texture thumbnail)
        {
            Icon.sprite = window.Icon;
            Title.text = window.Title;

            string state = window.IsMinimized
                ? "Minimized"
                : window.IsFocused ? "Active" : "Open";

            SelectButton.tooltip =
                $"{window.Title}\nWindow {window.WindowNumber} - {state}";

            CloseButton.tooltip =
                $"Close {window.Title}";

            Thumbnail.image = null;
            Thumbnail.sprite = null;

            if (thumbnail != null)
                Thumbnail.image = thumbnail;
            else
                Thumbnail.sprite = window.Icon;

            Root.EnableInClassList(
                "taskbar-preview-card--active",
                window.IsFocused);
        }

        public void Dispose()
        {
            SelectButton.clicked -= SelectHandler;
            CloseButton.clicked -= CloseHandler;

            Root.RemoveFromHierarchy();
        }
    }

    public sealed class TaskbarPreviewViewBuilder
    {
        public TaskbarPreviewView Build(
            VisualTreeAsset template,
            VisualElement layer)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            if (layer == null)
                throw new ArgumentNullException(nameof(layer));

            TemplateContainer host = template.CloneTree();

            host.name = "TaskbarPreviewHost";
            host.pickingMode = PickingMode.Ignore;

            host.style.position = Position.Absolute;
            host.style.left = 0;
            host.style.right = 0;
            host.style.top = 0;
            host.style.bottom = 0;
            host.style.display = DisplayStyle.None;

            var binder = new TaskbarPreviewBinder(host);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "Taskbar preview template is missing required elements.");
            }

            layer.Add(host);

            return new TaskbarPreviewView(
                host,
                binder,
                layer,
                this);
        }

        internal TaskbarPreviewCard BuildCard(
            AppWindowPreview window,
            Action<IAppWindow> select,
            Action<IAppWindow> close)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));

            if (select == null)
                throw new ArgumentNullException(nameof(select));

            if (close == null)
                throw new ArgumentNullException(nameof(close));

            var root = new VisualElement();
            root.AddToClassList("taskbar-preview-card");

            var selectButton = new Button
            {
                text = string.Empty
            };

            selectButton.AddToClassList(
                "taskbar-preview-card__select");

            var header = new VisualElement
            {
                pickingMode = PickingMode.Ignore
            };

            header.AddToClassList(
                "taskbar-preview-card__header");

            var icon = new Image
            {
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };

            icon.AddToClassList(
                "taskbar-preview-card__icon");

            var title = new Label
            {
                enableRichText = false,
                pickingMode = PickingMode.Ignore
            };

            title.AddToClassList(
                "taskbar-preview-card__title");

            var thumbnail = new Image
            {
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };

            thumbnail.AddToClassList(
                "taskbar-preview-card__thumbnail");

            header.Add(icon);
            header.Add(title);

            selectButton.Add(header);
            selectButton.Add(thumbnail);

            var closeButton = new Button
            {
                text = string.Empty
            };

            closeButton.AddToClassList(
                "taskbar-preview-card__close");

            var closeImage = new Image
            {
                pickingMode = PickingMode.Ignore
            };

            closeImage.AddToClassList(
                "taskbar-preview-card__close-icon");

            closeButton.Add(closeImage);

            // Sibling buttons avoid nesting the close control
            // inside the window-selection button.
            root.Add(selectButton);
            root.Add(closeButton);

            var card = new TaskbarPreviewCard
            {
                Root = root,
                SelectButton = selectButton,
                CloseButton = closeButton,
                Icon = icon,
                Title = title,
                Thumbnail = thumbnail,
                Window = window.Window
            };

            card.SelectHandler =
                () => select(card.Window);

            card.CloseHandler =
                () => close(card.Window);

            selectButton.clicked += card.SelectHandler;
            closeButton.clicked += card.CloseHandler;

            return card;
        }
    }
}