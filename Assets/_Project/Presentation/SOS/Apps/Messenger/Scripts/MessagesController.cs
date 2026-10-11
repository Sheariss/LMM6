using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Messages
{
    public sealed class MessagesController : AppContentController
    {
        [Header("Templates")]
        [SerializeField]
        private VisualTreeAsset incomingMessageTemplate;

        [SerializeField]
        private VisualTreeAsset outgoingMessageTemplate;

        [SerializeField]
        private VisualTreeAsset typingIndicatorTemplate;

        private MessagesBinder binder;
        private MessagesView view;
        private MessagesViewBuilder builder;
        private MessagesEngine engine;

        private MessagesManager messagesManager;

        protected override void OnInitialize()
        {
            messagesManager = MessagesManager.Instance;

            if (messagesManager == null)
            {
                throw new InvalidOperationException(
                    "[MessagesController] MessagesManager was not found.");
            }

            binder = new MessagesBinder(InstanceRoot);
            view = new MessagesView(binder);

            builder = new MessagesViewBuilder(
                view,
                incomingMessageTemplate,
                outgoingMessageTemplate,
                typingIndicatorTemplate);

            engine = new MessagesEngine(messagesManager);

            RegisterCallbacks();

            Refresh();
        }

        protected override void OnShutdown()
        {
            // The base class unregisters UI callbacks before this runs.
            if (messagesManager != null)
            {
                messagesManager.ConversationUpdated -=
                    OnConversationUpdated;

                messagesManager.MessageReceived -=
                    OnMessageReceived;
            }

            binder = null;
            view = null;
            builder = null;
            engine = null;
            messagesManager = null;
        }

        private void RegisterCallbacks()
        {
            BindClick(
                binder.SendButton,
                OnSendPressed);

            BindCallback<ChangeEvent<string>>(
                binder.SearchField,
                OnSearchChanged);

            BindCallback<KeyDownEvent>(
                binder.InputField,
                OnInputKeyDown,
                TrickleDown.TrickleDown);

            messagesManager.ConversationUpdated +=
                OnConversationUpdated;

            messagesManager.MessageReceived +=
                OnMessageReceived;
        }

        private void Refresh()
        {
            builder.BuildConversationList(
                engine.GetConversations(),
                OnConversationSelected);

            if (engine.ActiveConversation == null)
            {
                view.ShowEmptyState();
                return;
            }

            builder.BuildConversation(
                engine.ActiveConversation);
        }

        private void OnConversationSelected(string id)
        {
            MessageConversationData conversation =
                engine.SelectConversation(id);

            builder.BuildConversation(conversation);

            builder.BuildConversationList(
                engine.GetConversations(),
                OnConversationSelected);
        }

        private void OnSendPressed()
        {
            SendCurrentMessage();
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            if (evt.shiftKey)
                return;

            evt.StopPropagation();

            SendCurrentMessage();
        }

        private void SendCurrentMessage()
        {
            string text = view.GetInputText();

            MessageData message =
                engine.SendMessage(text);

            if (message == null)
                return;

            view.ClearInput();

            builder.BuildConversation(
                engine.ActiveConversation);

            builder.BuildConversationList(
                engine.GetConversations(),
                OnConversationSelected);
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            builder.BuildConversationList(
                engine.SearchConversations(evt.newValue),
                OnConversationSelected);
        }

        private void OnConversationUpdated(
            MessageConversationData conversation)
        {
            builder.BuildConversationList(
                engine.GetConversations(),
                OnConversationSelected);

            if (engine.ActiveConversation == conversation)
            {
                builder.BuildConversation(conversation);
            }
        }

        private void OnMessageReceived(
            MessageConversationData conversation,
            MessageData message)
        {
            if (engine.ActiveConversation != conversation)
                return;

            messagesManager.MarkConversationRead(
                conversation.Id);

            builder.BuildConversation(conversation);
        }
    }
}