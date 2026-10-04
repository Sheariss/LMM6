using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Messages
{
    [Serializable]
    public sealed class MessageConversationData
    {
        [SerializeField] private string id;
        [SerializeField] private string contactName;
        [SerializeField] private Sprite contactAvatar;
        [SerializeField] private string status;
        [SerializeField] private List<MessageData> messages = new();

        public string Id => id;
        public string ContactName => contactName;
        public Sprite ContactAvatar => contactAvatar;
        public string Status => status;
        public IReadOnlyList<MessageData> Messages => messages;

        public MessageConversationData(
            string id,
            string contactName,
            Sprite contactAvatar = null,
            string status = "")
        {
            this.id = id;
            this.contactName = contactName;
            this.contactAvatar = contactAvatar;
            this.status = status;
        }

        public void AddMessage(MessageData message)
        {
            if (message == null)
                return;

            messages.Add(message);
        }

        public MessageData GetLastMessage()
        {
            if (messages == null || messages.Count == 0)
                return null;

            return messages[^1];
        }

        public int GetUnreadCount()
        {
            int count = 0;

            foreach (MessageData message in messages)
            {
                if (message.Direction == MessageDirection.Incoming &&
                    message.ReadState != MessageReadState.Read)
                {
                    count++;
                }
            }

            return count;
        }

        public void MarkAllRead()
        {
            foreach (MessageData message in messages)
            {
                if (message.Direction == MessageDirection.Incoming)
                    message.SetReadState(MessageReadState.Read);
            }
        }
    }

    [Serializable]
    public sealed class MessageData
    {
        [SerializeField] private string id;
        [SerializeField] private string senderName;

        [TextArea]
        [SerializeField] private string text;

        [SerializeField] private string timestamp;
        [SerializeField] private MessageDirection direction;
        [SerializeField] private MessageReadState readState;

        public string Id => id;
        public string SenderName => senderName;
        public string Text => text;
        public string Timestamp => timestamp;
        public MessageDirection Direction => direction;
        public MessageReadState ReadState => readState;

        public bool IsIncoming => direction == MessageDirection.Incoming;
        public bool IsOutgoing => direction == MessageDirection.Outgoing;

        public MessageData(
            string id,
            string senderName,
            string text,
            string timestamp,
            MessageDirection direction,
            MessageReadState readState = MessageReadState.Sent)
        {
            this.id = id;
            this.senderName = senderName;
            this.text = text;
            this.timestamp = timestamp;
            this.direction = direction;
            this.readState = readState;
        }

        public void SetReadState(MessageReadState state)
        {
            readState = state;
        }
    }

    public enum MessageDirection
    {
        Incoming,
        Outgoing
    }

    public enum MessageReadState
    {
        Sent,
        Delivered,
        Read
    }
}