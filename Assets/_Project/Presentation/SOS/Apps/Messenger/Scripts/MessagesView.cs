using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Messages
{
    public sealed class MessagesView
    {
        private readonly MessagesBinder binder;

        public VisualElement ConversationContainer =>
            binder.ConversationList.contentContainer;

        public VisualElement MessageContainer =>
            binder.HistoryContent;

        public MessagesView(MessagesBinder binder)
        {
            this.binder = binder;
        }

        public void ShowEmptyState()
        {
            binder.EmptyState.RemoveFromClassList("messages-hidden");
            binder.ChatPanel.AddToClassList("messages-hidden");
        }

        public void ShowConversation()
        {
            binder.EmptyState.AddToClassList("messages-hidden");
            binder.ChatPanel.RemoveFromClassList("messages-hidden");
        }

        public void SetContact(
            string name,
            string status,
            Sprite avatar)
        {
            binder.ContactName.text = name;
            binder.ContactStatus.text = status;

            binder.ContactStatus.style.display =
                string.IsNullOrWhiteSpace(status)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;

            if (avatar != null)
            {
                binder.ContactAvatar.style.backgroundImage =
                    new StyleBackground(avatar);
            }
            else
            {
                // Clear the inline sprite and reveal the USS fallback.
                binder.ContactAvatar.style.backgroundImage =
                    StyleKeyword.Null;
            }
        }

        public void ClearConversationList()
        {
            binder.ConversationList.Clear();
        }

        public void ClearMessageHistory()
        {
            binder.HistoryContent.Clear();
        }

        public void ClearInput()
        {
            binder.InputField.value = string.Empty;
        }

        public string GetInputText()
        {
            return binder.InputField.value;
        }

        public void SetInputEnabled(bool enabled)
        {
            binder.InputField.SetEnabled(enabled);
            binder.SendButton.SetEnabled(enabled);
        }

        public void ScrollToBottom()
        {
            binder.History.schedule.Execute(() =>
            {
                binder.History.scrollOffset = new Vector2(
                    0,
                    binder.History.contentContainer.layout.height);
            });
        }
    }
}