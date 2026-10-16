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

        [Header("Authored Topics")]
        [SerializeField]
        private AssistantTopic[] topics =
        {
            new AssistantTopic
            {
                buttonLabel = "Review evidence",
                question = "How should I review the evidence?",
                keywords = new[] { "evidence", "findings" },
                response =
                    "Start with the material you have collected. " +
                    "Compare names, dates, and claims, and note which " +
                    "details are supported by more than one source."
            },
            new AssistantTopic
            {
                buttonLabel = "Evaluate a clue",
                question = "How should I evaluate a clue?",
                keywords = new[] { "clue", "clues" },
                response =
                    "Check where the clue came from, what it directly " +
                    "shows, and what you are inferring. Look for another " +
                    "source that can confirm or challenge your interpretation."
            },
            new AssistantTopic
            {
                buttonLabel = "Get unstuck",
                question = "What can I do when I am stuck?",
                keywords = new[] { "stuck", "hint", "help" },
                response =
                    "Revisit your current objective and look for an " +
                    "unanswered question. Review any messages or files " +
                    "you have not examined, then compare them with your notes."
            }
        };

        [TextArea(2, 5)]
        [SerializeField]
        private string fallbackResponse =
            "I do not have an authored answer for that question yet. " +
            "Try asking about evidence, clues, or getting unstuck.";

        private AssistantBinder binder;
        private AssistantView view;
        private AssistantEngine engine;

        protected override void OnInitialize()
        {
            if (assistantMessageTemplate == null ||
                playerMessageTemplate == null)
            {
                throw new InvalidOperationException(
                    "[Assistant] Assign both message templates.");
            }

            binder = new AssistantBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new InvalidOperationException(
                    "[Assistant] Required UI elements are missing.");
            }

            engine = new AssistantEngine(topics, fallbackResponse);

            view = new AssistantView(
                binder,
                assistantMessageTemplate,
                playerMessageTemplate);

            foreach (AssistantTopic topic in engine.Topics)
            {
                AssistantTopic selectedTopic = topic;

                string label = string.IsNullOrWhiteSpace(topic.buttonLabel)
                    ? topic.question
                    : topic.buttonLabel;

                Button button = view.AddPrompt(label);

                BindClick(
                    button,
                    () => Submit(selectedTopic.question, selectedTopic));
            }

            BindClick(binder.SendButton, SendTypedMessage);

            BindCallback<ChangeEvent<string>>(
                binder.Input,
                OnInputChanged);

            BindCallback<KeyDownEvent>(
                binder.Input,
                OnInputKeyDown,
                TrickleDown.TrickleDown);

            view.RefreshComposer();
        }

        private void OnInputChanged(ChangeEvent<string> evt)
        {
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

            // Prevent the multiline field from also adding a newline.
            evt.PreventDefault();
            evt.StopPropagation();

            SendTypedMessage();
        }

        private void SendTypedMessage()
        {
            string text = binder.Input.value;

            if (string.IsNullOrWhiteSpace(text))
                return;

            Submit(text);
            view.ClearInput();
        }

        private void Submit(
            string text,
            AssistantTopic selectedTopic = null)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(text))
                return;

            view.AddMessage(text.Trim(), fromPlayer: true);

            string response = engine.Respond(text, selectedTopic);
            view.AddMessage(response, fromPlayer: false);

            // Clicking a suggestion preserves any unfinished draft.
            binder.Input.Focus();
        }

        protected override void OnShutdown()
        {
            view?.Dispose();

            view = null;
            binder = null;
            engine = null;
        }
    }
}