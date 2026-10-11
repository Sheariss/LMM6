using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Messages
{
    public sealed class MessagesManager : MonoBehaviour
    {
        public static MessagesManager Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private MessagesLibrary messagesLibrary;

        private readonly List<MessageConversationData> conversations = new();

        public IReadOnlyList<MessageConversationData> Conversations =>
            conversations;

        public event Action<MessageConversationData> ConversationUpdated;

        public event Action<
            MessageConversationData,
            MessageData> MessageReceived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            LoadLibrary();
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void LoadLibrary()
        {
            conversations.Clear();

            if (messagesLibrary == null)
            {
                Debug.LogError(
                    "[MessagesManager] MessagesLibrary is not assigned.",
                    this);

                return;
            }

            foreach (MessageConversationDefinition definition
                     in messagesLibrary.Conversations)
            {
                if (definition == null)
                    continue;

                if (string.IsNullOrWhiteSpace(definition.Id))
                {
                    Debug.LogWarning(
                        "[MessagesManager] Skipping a conversation with no ID.",
                        this);

                    continue;
                }

                if (GetConversation(definition.Id) != null)
                {
                    Debug.LogWarning(
                        $"[MessagesManager] Duplicate conversation ID: {definition.Id}",
                        this);

                    continue;
                }

                MessageConversationData conversation = new(
                    definition.Id,
                    definition.ContactName,
                    definition.ContactAvatar,
                    definition.Status);

                foreach (MessageDefinition messageDefinition
                         in definition.InitialMessages)
                {
                    if (messageDefinition == null)
                        continue;

                    MessageData message = new(
                        messageDefinition.Id,
                        messageDefinition.SenderName,
                        messageDefinition.Text,
                        messageDefinition.Timestamp,
                        messageDefinition.Direction,
                        messageDefinition.InitialReadState);

                    conversation.AddMessage(message);
                }

                conversations.Add(conversation);
            }
        }

        public MessageConversationData GetConversation(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return conversations.Find(conversation =>
                conversation.Id == id);
        }

        public void AddConversation(MessageConversationData conversation)
        {
            if (conversation == null ||
                string.IsNullOrWhiteSpace(conversation.Id))
            {
                return;
            }

            if (GetConversation(conversation.Id) != null)
                return;

            conversations.Add(conversation);
            ConversationUpdated?.Invoke(conversation);
        }

        public void ReceiveMessage(
            string conversationId,
            MessageData message)
        {
            MessageConversationData conversation =
                GetConversation(conversationId);

            if (conversation == null || message == null)
                return;

            conversation.AddMessage(message);

            MessageReceived?.Invoke(conversation, message);
            ConversationUpdated?.Invoke(conversation);
        }

        public void AddOutgoingMessage(
            string conversationId,
            MessageData message)
        {
            MessageConversationData conversation =
                GetConversation(conversationId);

            if (conversation == null || message == null)
                return;

            conversation.AddMessage(message);
            ConversationUpdated?.Invoke(conversation);
        }

        public void MarkConversationRead(string conversationId)
        {
            MessageConversationData conversation =
                GetConversation(conversationId);

            if (conversation == null)
                return;

            conversation.MarkAllRead();
            ConversationUpdated?.Invoke(conversation);
        }
    }
}