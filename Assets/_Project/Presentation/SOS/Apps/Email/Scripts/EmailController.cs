using System;
using Atlas.Presentation.SOS.Apps;
using Atlas.SOS.Authentication;
using Atlas.SOS.FileSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Email
{
    public sealed class EmailController : AppContentController
    {
        [Header("Presentation")]
        [SerializeField] private VisualTreeAsset emailLayout;
        [SerializeField] private StyleSheet conversationStyles;

        [Header("Attachments")]
        [SerializeField] private FileSystemLibrary fileSystemLibrary;
        [SerializeField] private FileAssociationCatalog fileAssociations;

        private EmailManager manager;
        private EmailBinder binder;
        private EmailEngine engine;
        private EmailViewBuilder builder;
        private EmailView view;

        private EmailAttachmentResolver attachmentResolver;
        private FileOpenRouter fileOpenRouter;
        private TemplateContainer layoutInstance;

        private bool sidebarVisible = true;
        private bool executingAction;

        protected override void OnInitialize()
        {
            if (emailLayout == null || conversationStyles == null)
            {
                throw new InvalidOperationException(
                    "Email requires its layout and conversation stylesheet.");
            }

            if (fileSystemLibrary == null || fileAssociations == null)
            {
                throw new InvalidOperationException(
                    "Email requires the filesystem library and file associations.");
            }

            manager = EmailManager.Instance;

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "An initialized EmailManager is required.");
            }

            string userId =
                AuthenticationManager.Instance?.CurrentUser?.UserId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException(
                    "Email requires a signed-in user.");
            }

            AppController launcher = GetComponentInParent<AppController>();

            if (launcher == null)
            {
                throw new InvalidOperationException(
                    "Email runtime must be launched by AppController.");
            }

            fileOpenRouter = new FileOpenRouter(
                fileAssociations, launcher);

            attachmentResolver = new EmailAttachmentResolver(
                fileSystemLibrary, userId);

            engine = new EmailEngine(
                manager, userId, attachmentResolver);

            builder = new EmailViewBuilder();

            InstanceRoot.style.height = StyleKeyword.Auto;
            InstanceRoot.style.flexGrow = 1;
            InstanceRoot.style.flexShrink = 1;
            InstanceRoot.style.flexBasis = 0;
            InstanceRoot.style.minHeight = 0;
            InstanceRoot.style.minWidth = 0;

            layoutInstance = emailLayout.CloneTree();

            layoutInstance.style.flexGrow = 1;
            layoutInstance.style.flexShrink = 1;
            layoutInstance.style.flexBasis = 0;
            layoutInstance.style.minHeight = 0;
            layoutInstance.style.minWidth = 0;

            InstanceRoot.Add(layoutInstance);

            binder = new EmailBinder(layoutInstance);
            binder.Root.styleSheets.Add(conversationStyles);

            view = new EmailView(binder, builder, engine.Account);

            RegisterCallbacks();

            manager.MailboxChanged += OnMailboxChanged;

            view.OpenRequested += OnOpenRequested;
            view.StarRequested += OnStarRequested;
            view.SelectionChanged += OnSelectionChanged;
            view.MessageExpansionRequested += OnMessageExpansion;
            view.MessageStarRequested += OnMessageStar;
            view.AttachmentRequested += OnAttachmentRequested;

            binder.SearchField.SetValueWithoutNotify("");
            view.SetSidebarVisible(sidebarVisible);

            float width = binder.Root.resolvedStyle.width;

            if (!float.IsNaN(width) && width > 0)
                view.SetCompact(width < 1000f);

            Render();
        }

        private void RegisterCallbacks()
        {
            foreach (var pair in binder.Navigation)
            {
                EmailFilter filter = pair.Key;

                BindClick(
                    pair.Value,
                    () => Execute(() => engine.SetFilter(filter)));
            }

            foreach (var pair in binder.Categories)
            {
                EmailCategory category = pair.Key;

                BindClick(
                    pair.Value,
                    () => Execute(() => engine.SetCategory(category)));
            }

            BindCallback<ChangeEvent<string>>(
                binder.SearchField,
                evt => Execute(() => engine.Search(evt.newValue)));

            BindCallback<ChangeEvent<bool>>(
                binder.SelectAllToggle,
                evt => Execute(() => engine.SelectAll(evt.newValue)));

            BindCallback<GeometryChangedEvent>(
                binder.Root,
                evt =>
                {
                    view.CloseActionsMenu(false);
                    view.SetCompact(evt.newRect.width < 1000f);
                });

            BindClick(
                binder.Button("PreviousPageButton"),
                () => Execute(() => engine.MovePage(-1)));

            BindClick(
                binder.Button("NextPageButton"),
                () => Execute(() => engine.MovePage(1)));

            BindClick(binder.Button("RefreshButton"), Render);

            BindClick(
                binder.Button("MoreActionsButton"),
                () => view.ToggleActionsMenu(isRead =>
                    Execute(() => engine.MarkSelected(isRead))));

            BindClick(
                view.Reader.Back,
                () => Execute(engine.CloseConversation));

            BindClick(
                binder.Button("MenuButton"),
                () =>
                {
                    sidebarVisible = !sidebarVisible;
                    view.SetSidebarVisible(sidebarVisible);
                });
        }

        private void OnOpenRequested(string id)
        {
            Execute(() => engine.OpenConversation(id));
        }

        private void OnStarRequested(string id)
        {
            Execute(() => engine.ToggleConversationStar(id));
        }

        private void OnSelectionChanged(string id, bool selected)
        {
            Execute(() => engine.Select(id, selected));
        }

        private void OnMessageExpansion(string id)
        {
            Execute(() => engine.ToggleExpanded(id));
        }

        private void OnMessageStar(string id)
        {
            Execute(() => engine.ToggleMessageStar(id));
        }

        private void OnAttachmentRequested(string messageId, string fileId)
        {
            if (!engine.IsAttachmentInOpenedConversation(messageId, fileId))
            {
                view.ShowAttachmentError(
                    "That attachment is not part of the open conversation.");
                return;
            }

            EmailResolvedAttachment attachment =
                attachmentResolver.Resolve(fileId);

            if (!attachment.IsAvailable)
            {
                view.ShowAttachmentError(attachment.Error);
                return;
            }

            if (!fileOpenRouter.TryOpen(attachment.Request, out string error))
            {
                view.ShowAttachmentError(error);
                return;
            }

            // Leave focus with the destination app.
            view.ShowAttachmentError(null);
        }

        private void Execute(Action action)
        {
            executingAction = true;

            try
            {
                action();
            }
            finally
            {
                executingAction = false;
            }

            Render();
        }

        private void OnMailboxChanged(string userId)
        {
            if (executingAction || engine == null)
                return;

            if (userId == null || userId == engine.UserId)
                Render();
        }

        private void Render()
        {
            if (view != null && engine != null)
                view.Render(builder.Build(engine));
        }

        protected override void OnShutdown()
        {
            if (manager != null)
                manager.MailboxChanged -= OnMailboxChanged;

            if (view != null)
            {
                view.OpenRequested -= OnOpenRequested;
                view.StarRequested -= OnStarRequested;
                view.SelectionChanged -= OnSelectionChanged;
                view.MessageExpansionRequested -= OnMessageExpansion;
                view.MessageStarRequested -= OnMessageStar;
                view.AttachmentRequested -= OnAttachmentRequested;

                view.Dispose();
            }

            layoutInstance?.RemoveFromHierarchy();

            manager = null;
            binder = null;
            engine = null;
            builder = null;
            view = null;
            attachmentResolver = null;
            fileOpenRouter = null;
            layoutInstance = null;

            sidebarVisible = true;
            executingAction = false;
        }
    }
}