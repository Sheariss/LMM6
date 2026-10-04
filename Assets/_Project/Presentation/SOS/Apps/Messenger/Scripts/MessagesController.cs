using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Messages
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MessagesController : MonoBehaviour
    {
        [Header("Templates")]
        [SerializeField]
        private VisualTreeAsset incomingMessageTemplate;

        [SerializeField]
        private VisualTreeAsset outgoingMessageTemplate;

        private UIDocument document;

        private MessagesBinder binder;
        private MessagesView view;
        private MessagesViewBuilder builder;
        private MessagesEngine engine;

        private MessagesManager messagesManager;

        private void Awake()
        {
            document = GetComponent<UIDocument>();

            messagesManager = MessagesManager.Instance;

            if (messagesManager == null)
            {
                Debug.LogError(
                    "[MessagesController] MessagesManager was not found.");

                enabled = false;
                return;
            }

            binder = new MessagesBinder(
                document.rootVisualElement);

            view = new MessagesView(binder);

            builder = new MessagesViewBuilder(
                view,
                incomingMessageTemplate,
                outgoingMessageTemplate);

            engine = new MessagesEngine(
                messagesManager);
        }

        private void OnEnable()
        {
            if (binder == null)
                return;

            binder.SendButton.clicked += OnSendPressed;

            binder.SearchField.RegisterValueChangedCallback(
                OnSearchChanged);

            binder.InputField.RegisterCallback<KeyDownEvent>(
                OnInputKeyDown);

            messagesManager.ConversationUpdated +=
                OnConversationUpdated;

            messagesManager.MessageReceived +=
                OnMessageReceived;

            Refresh();
        }

        private void OnDisable()
        {
            if (binder == null)
                return;

            binder.SendButton.clicked -= OnSendPressed;

            binder.SearchField.UnregisterValueChangedCallback(
                OnSearchChanged);

            binder.InputField.UnregisterCallback<KeyDownEvent>(
                OnInputKeyDown);

            if (messagesManager != null)
            {
                messagesManager.ConversationUpdated -=
                    OnConversationUpdated;

                messagesManager.MessageReceived -=
                    OnMessageReceived;
            }
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

        private void OnSearchChanged(
            ChangeEvent<string> evt)
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
                builder.BuildConversation(
                    conversation);
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

            builder.BuildConversation(
                conversation);
        }
    }
}