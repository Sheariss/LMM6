using Atlas.Utils;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Messages
{
    public sealed class MessagesBinder : UIBinder
    {
        public VisualElement Root { get; }

        public VisualElement Sidebar { get; }
        public Button NewConversationButton { get; }
        public TextField SearchField { get; }
        public ScrollView ConversationList { get; }

        public VisualElement ChatPanel { get; }
        public VisualElement ChatHeader { get; }
        public VisualElement ContactAvatar { get; }
        public Label ContactName { get; }
        public Label ContactStatus { get; }

        public Button SearchConversationButton { get; }
        public Button MoreButton { get; }

        public ScrollView History { get; }
        public VisualElement HistoryContent { get; }

        public VisualElement Composer { get; }
        public Button AttachmentButton { get; }
        public TextField InputField { get; }
        public Button SendButton { get; }

        public VisualElement EmptyState { get; }

        public MessagesBinder(VisualElement root)
        {
            Root = Bind<VisualElement>(root, "Messages-Root");

            Sidebar = Bind<VisualElement>(root, "Messages-Sidebar");
            NewConversationButton = Bind<Button>(root, "Messages-NewConversationButton");
            SearchField = Bind<TextField>(root, "Messages-SearchField");
            ConversationList = Bind<ScrollView>(root, "Messages-ConversationList");

            ChatPanel = Bind<VisualElement>(root, "Messages-ChatPanel");
            ChatHeader = Bind<VisualElement>(root, "Messages-ChatHeader");
            ContactAvatar = Bind<VisualElement>(root, "Messages-ContactAvatar");
            ContactName = Bind<Label>(root, "Messages-ContactName");
            ContactStatus = Bind<Label>(root, "Messages-ContactStatus");

            SearchConversationButton = Bind<Button>(root, "Messages-SearchConversationButton");
            MoreButton = Bind<Button>(root, "Messages-MoreButton");

            History = Bind<ScrollView>(root, "Messages-History");
            HistoryContent = Bind<VisualElement>(root, "Messages-HistoryContent");

            Composer = Bind<VisualElement>(root, "Messages-Composer");
            AttachmentButton = Bind<Button>(root, "Messages-AttachmentButton");
            InputField = Bind<TextField>(root, "Messages-InputField");
            SendButton = Bind<Button>(root, "Messages-SendButton");

            EmptyState = Bind<VisualElement>(root, "Messages-EmptyState");
        }
    }
}