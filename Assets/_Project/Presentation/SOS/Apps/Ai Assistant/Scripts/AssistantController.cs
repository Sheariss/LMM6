using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantController : AppContentController
    {
        [Header("Message Templates")]
        [SerializeField] private VisualTreeAsset assistantMessageTemplate;
        [SerializeField] private VisualTreeAsset playerMessageTemplate;

        private AssistantManager manager;
        private AssistantBinder binder;
        private AssistantView view;

        protected override void OnInitialize()
        {
            manager = AssistantManager.Instance;

            if (manager == null)
            {
                throw new InvalidOperationException(
                    "[AssistantController] AssistantManager was not found.");
            }

            if (assistantMessageTemplate == null ||
                playerMessageTemplate == null)
            {
                throw new InvalidOperationException(
                    "[AssistantController] Assign both message templates.");
            }

            binder = new AssistantBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "[AssistantController] Required UI elements are missing.");
            }

            var builder = new AssistantViewBuilder(
                assistantMessageTemplate,
                playerMessageTemplate);

            view = new AssistantView(binder, builder);

            view.ConversationSelected += OnConversationSelected;
            view.ChoiceSelected += OnChoiceSelected;

            view.OfferAccepted += OnOfferAccepted;
            view.OfferDeferred += OnOfferDeferred;
            view.OfferDismissed += OnOfferDismissed;
            view.ReminderChanged += OnReminderChanged;

            manager.Changed += Render;

            BindClick(binder.SendButton, SendTypedMessage);

            BindCallback<ChangeEvent<string>>(
                binder.Input,
                OnInputChanged);

            BindCallback<KeyDownEvent>(
                binder.Input,
                OnInputKeyDown,
                TrickleDown.TrickleDown);

            Render();
        }

        private void Render()
        {
            if (manager == null || view == null)
                return;

            view.Render(manager);
        }

        private void OnConversationSelected(string conversationId)
        {
            view.Capture();
            manager.SelectConversation(conversationId);
        }

        private void OnChoiceSelected(
            string conversationId,
            string nodeId,
            string choiceId)
        {
            view.Capture();

            manager.SelectChoice(
                conversationId,
                nodeId,
                choiceId);
        }

        private void OnOfferAccepted(string offerId)
        {
            view.Capture();
            if (!string.IsNullOrWhiteSpace(manager.Catalog.FindOffer(offerId)?.invitationConversationId))
                manager.TrySelectOffer(offerId);
            else
                manager.AcceptOffer(offerId);
        }

        private void OnReminderChanged(string conversationId, int minutes)
        {
            manager.SetReminderChoice(conversationId, minutes);
        }

        private void OnOfferDeferred(string offerId)
        {
            view.Capture();
            manager.DeferOffer(offerId);
        }

        private void OnOfferDismissed(string offerId)
        {
            view.Capture();
            manager.DismissOffer(offerId);
        }

        private void OnInputChanged(ChangeEvent<string> evt)
        {
            view.Capture();
            view.RefreshComposer();
        }

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            bool enter =
                evt.keyCode == KeyCode.Return ||
                evt.keyCode == KeyCode.KeypadEnter;

            if (!enter ||
                evt.shiftKey ||
                evt.ctrlKey ||
                evt.altKey ||
                evt.commandKey)
            {
                return;
            }

            // Prevent the multiline field from adding a newline.
            evt.StopPropagation();

            SendTypedMessage();
        }

        private void SendTypedMessage()
        {
            var conversation = manager.SelectedConversation;
            string text = binder.Input.value;

            if (conversation == null ||
                string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            view.ClearInput();

            manager.SubmitText(conversation.id, text);

            binder.Input.Focus();
        }

        protected override void OnShutdown()
        {
            // AppContentController unregisters bound UI callbacks first.
            if (manager != null)
                manager.Changed -= Render;

            if (view != null)
            {
                view.ConversationSelected -= OnConversationSelected;
                view.ChoiceSelected -= OnChoiceSelected;

                view.OfferAccepted -= OnOfferAccepted;
                view.OfferDeferred -= OnOfferDeferred;
                view.OfferDismissed -= OnOfferDismissed;
                view.ReminderChanged -= OnReminderChanged;

                view.Dispose();
            }

            view = null;
            binder = null;
            manager = null;
        }
    }
}
