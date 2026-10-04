using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Messages
{
    public sealed class MessagesManager : MonoBehaviour
    {
        public static MessagesManager Instance { get; private set; }

        [SerializeField]
        private List<MessageConversationData> conversations = new();

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

            Instance = this;
        }

        public MessageConversationData GetConversation(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return conversations.Find(x => x.Id == id);
        }

        public void AddConversation(MessageConversationData conversation)
        {
            if (conversation == null)
                return;

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