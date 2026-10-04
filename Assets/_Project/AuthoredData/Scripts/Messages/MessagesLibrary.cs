using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Messages
{
    [CreateAssetMenu(
        fileName = "MessagesLibrary",
        menuName = "ATLAS/SOS/Messages/Messages Library")]
    public sealed class MessagesLibrary : ScriptableObject
    {
        [SerializeField]
        private List<MessageConversationDefinition> conversations = new();

        public IReadOnlyList<MessageConversationDefinition> Conversations =>
            conversations;

        public MessageConversationDefinition GetConversation(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return conversations.Find(conversation =>
                conversation.Id == id);
        }
    }

    [Serializable]
    public sealed class MessageConversationDefinition
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string contactName;

        [SerializeField]
        private Sprite contactAvatar;

        [SerializeField]
        private string status;

        [SerializeField]
        private List<MessageDefinition> initialMessages = new();

        public string Id => id;
        public string ContactName => contactName;
        public Sprite ContactAvatar => contactAvatar;
        public string Status => status;

        public IReadOnlyList<MessageDefinition> InitialMessages =>
            initialMessages;
    }

    [Serializable]
    public sealed class MessageDefinition
    {
        [SerializeField]
        private string id;

        [SerializeField]
        private string senderName;

        [TextArea]
        [SerializeField]
        private string text;

        [SerializeField]
        private string timestamp;

        [SerializeField]
        private MessageDirection direction;

        [SerializeField]
        private MessageReadState initialReadState =
            MessageReadState.Read;

        public string Id => id;
        public string SenderName => senderName;
        public string Text => text;
        public string Timestamp => timestamp;
        public MessageDirection Direction => direction;
        public MessageReadState InitialReadState => initialReadState;
    }
}