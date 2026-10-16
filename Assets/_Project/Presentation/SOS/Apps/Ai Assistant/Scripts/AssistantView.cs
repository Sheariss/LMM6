using System;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantView : IDisposable
    {
        private readonly AssistantBinder binder;
        private readonly AssistantViewBuilder builder;

        private readonly VisualElement conversationBar;
        private readonly VisualElement offerArea;

        private AssistantConversationState renderedConversation;
        private int renderedMessageCount;
        private int renderVersion;
        private bool disposed;

        public event Action<string> ConversationSelected;

        public event Action<string, string, string> ChoiceSelected;

        public event Action<string> OfferAccepted;
        public event Action<string> OfferDeferred;
        public event Action<string> OfferDismissed;

        public AssistantView(
            AssistantBinder binder,
            AssistantViewBuilder builder)
        {
            this.binder = binder;
            this.builder = builder;

            conversationBar = new VisualElement
            {
                name = "AssistantConversationBar"
            };

            offerArea = new VisualElement
            {
                name = "AssistantOfferArea"
            };

            conversationBar.style.flexShrink = 0;

            offerArea.style.flexShrink = 0;
            offerArea.style.maxHeight = 220;

            var offerScroll = new ScrollView();
            offerScroll.style.maxHeight = 220;
            offerScroll.Add(offerArea);

            var parent = binder.MessageScroll.parent;
            int index = parent.IndexOf(binder.MessageScroll);

            parent.Insert(index, conversationBar);
            parent.Insert(index + 1, offerScroll);

            // Retain this wrapper for disposal.
            OfferScroll = offerScroll;
        }

        private ScrollView OfferScroll { get; }

        public void Capture()
        {
            if (renderedConversation == null)
                return;

            renderedConversation.draft = binder.Input.value;
            renderedConversation.scrollY =
                binder.MessageScroll.scrollOffset.y;
        }

        public void Render(AssistantManager manager)
        {
            if (disposed)
                return;

            bool wasNearBottom =
                binder.MessageScroll.verticalScroller.highValue -
                binder.MessageScroll.scrollOffset.y < 48f;

            Capture();

            var selected = manager.SelectedConversation;

            bool sameConversation =
                renderedConversation != null &&
                selected != null &&
                renderedConversation.id == selected.id;

            bool receivedMessage =
                sameConversation &&
                selected.messages.Count > renderedMessageCount;

            conversationBar.Clear();

            foreach (var conversation in manager.Conversations)
            {
                string id = conversation.id;

                string title = manager.Catalog
                    .FindConversation(id).title;

                conversationBar.Add(builder.BuildConversation(
                    title,
                    selected?.id == id,
                    () => ConversationSelected?.Invoke(id)));
            }

            RenderOffers(manager);

            binder.MessageList.Clear();
            binder.PromptList.Clear();

            binder.Welcome.style.display =
                selected == null || selected.messages.Count == 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            if (selected != null)
            {
                foreach (var message in selected.messages)
                    binder.MessageList.Add(
                        builder.BuildMessage(message));

                string conversationId = selected.id;
                string nodeId = selected.currentNodeId;

                foreach (var choice in manager.GetChoices(selected))
                {
                    string choiceId = choice.id;

                    binder.PromptList.Add(builder.BuildChoice(
                        choice.label,
                        () => ChoiceSelected?.Invoke(
                            conversationId,
                            nodeId,
                            choiceId)));
                }
            }

            binder.Choices.style.display =
                binder.PromptList.childCount > 0
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

            binder.Input.SetValueWithoutNotify(
                selected?.draft ?? string.Empty);

            binder.Input.SetEnabled(selected != null);

            binder.Status.text = selected == null
                ? "Choose a conversation to begin."
                : selected.finished
                    ? "You can return to another conversation."
                    : "Explore at your own pace.";

            renderedConversation = selected;
            renderedMessageCount = selected?.messages.Count ?? 0;

            RefreshComposer();

            int version = ++renderVersion;
            float restoreY = selected?.scrollY ?? 0;

            bool followNewMessage =
                sameConversation &&
                receivedMessage &&
                wasNearBottom;

            binder.MessageScroll.schedule.Execute(() =>
            {
                if (disposed || version != renderVersion)
                    return;

                float y = followNewMessage
                    ? binder.MessageScroll.verticalScroller.highValue
                    : restoreY;

                binder.MessageScroll.scrollOffset =
                    new UnityEngine.Vector2(0, y);
            });
        }

        public void RefreshComposer()
        {
            bool empty = string.IsNullOrEmpty(binder.Input.value);

            binder.Placeholder.style.display =
                empty ? DisplayStyle.Flex : DisplayStyle.None;

            binder.SendButton.SetEnabled(
                renderedConversation != null &&
                !string.IsNullOrWhiteSpace(binder.Input.value));
        }

        public void ClearInput()
        {
            binder.Input.SetValueWithoutNotify(string.Empty);

            if (renderedConversation != null)
                renderedConversation.draft = string.Empty;

            RefreshComposer();
        }

        private void RenderOffers(AssistantManager manager)
        {
            offerArea.Clear();

            // A blocking moment hides presentation without losing offers.
            if (manager.GetFact("assistant.offers.blocked"))
                return;

            foreach (var offer in manager.Offers)
            {
                bool deferred =
                    offer.status == AssistantOfferStatus.Deferred;

                if (!deferred &&
                    offer.status != AssistantOfferStatus.Presented)
                {
                    continue;
                }

                var definition = manager.Catalog.FindOffer(offer.id);

                if (definition == null)
                    continue;

                string id = offer.id;

                offerArea.Add(builder.BuildOffer(
                    definition,
                    deferred,
                    () => OfferAccepted?.Invoke(id),
                    () => OfferDeferred?.Invoke(id),
                    () => OfferDismissed?.Invoke(id)));
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            Capture();
            disposed = true;
            renderVersion++;

            conversationBar.RemoveFromHierarchy();
            OfferScroll.RemoveFromHierarchy();

            binder.MessageList.Clear();
            binder.PromptList.Clear();
        }
    }
}