using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantManager : MonoBehaviour
    {
        public static AssistantManager Instance { get; private set; }

        [Header("Data")]
        [SerializeField] private AssistantCatalog catalog;

        private AssistantSaveData state = new();
        private readonly Dictionary<string, AssistantSaveData> userStates =
            new(StringComparer.Ordinal);
        private readonly AssistantEngine engine = new();

        private bool pumping;
        private float nextReminderCheck;

        public event Action Changed;
        public event Action<AssistantActionRequest> ActionRequested;

        public TutorialManager Tutorials { get; private set; }

        public AssistantCatalog Catalog => catalog;
        public string ActiveUserId { get; private set; }
        public string SelectedOfferId { get; private set; }
        public long OfferSelectionVersion { get; private set; }

        public IReadOnlyList<AssistantConversationState> Conversations =>
            state.conversations;

        public IReadOnlyList<AssistantOfferState> Offers =>
            state.offers;

        public AssistantConversationState SelectedConversation =>
            FindConversation(state.selectedConversationId);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (catalog == null)
            {
                throw new InvalidOperationException(
                    "[AssistantManager] Assign an AssistantCatalog.");
            }

            catalog.ValidateOrThrow();

            Tutorials = new TutorialManager(catalog, state, engine);

            // Publish only after initialization succeeds.
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (ActiveUserId == null || Time.unscaledTime < nextReminderCheck)
                return;
            nextReminderCheck = Time.unscaledTime + 1f;
            ProcessReminders(DateTime.UtcNow.Ticks);
        }

        // Real-world deadlines are evaluated only for the currently bound account.
        // This API also permits deterministic reminder validation without waiting.
        public void ProcessReminders(long utcTicks)
        {
            if (ActiveUserId == null) return;
            bool changed = false;
            foreach (var offer in state.offers.ToArray())
            {
                if (offer.status != AssistantOfferStatus.Deferred ||
                    offer.reminderSuppressed || offer.reminderDueUtcTicks <= 0 ||
                    offer.reminderDueUtcTicks > utcTicks) continue;
                offer.reminderDueUtcTicks = 0;
                if (!IsOfferValid(offer))
                {
                    offer.status = AssistantOfferStatus.Expired;
                    changed = true;
                    continue;
                }
                offer.reminderDelivered = true;
                var invitation = EnsureInvitation(offer);
                if (invitation != null)
                {
                    invitation.finished = false;
                    invitation.currentNodeId = "offer";
                    AddMessage(invitation, "You asked me to remind you about the workspace introduction. Would you like to start now?", false);
                }
                changed = true;
            }
            if (changed) Refresh();
        }

        private AssistantConversationState EnsureInvitation(AssistantOfferState offer)
        {
            string id = catalog.FindOffer(offer.id)?.invitationConversationId;
            if (string.IsNullOrWhiteSpace(id)) return null;
            var conversation = FindConversation(id);
            if (conversation == null)
            {
                if (!StartConversation(id, false, offer.returnContext)) return null;
                conversation = FindConversation(id);
            }
            conversation.offerId = offer.id;
            return conversation;
        }

        public void SetReminderChoice(string conversationId, int minutes)
        {
            var conversation = FindConversation(conversationId);
            if (conversation == null || conversation.currentNodeId != "reminder" ||
                string.IsNullOrWhiteSpace(conversation.offerId) ||
                (minutes != 0 && minutes != 10 && minutes != 30 && minutes != 60)) return;
            var offer = state.offers.Find(item => item.id == conversation.offerId);
            if (offer != null && CanPresentOffer(offer.id)) offer.reminderMinutes = minutes;
        }

        public AssistantOfferState GetInvitationOffer(AssistantConversationState conversation) =>
            string.IsNullOrWhiteSpace(conversation?.offerId) ? null :
            state.offers.Find(item => item.id == conversation.offerId);

        public string GetOfferStatusMessage(AssistantOfferState offer)
        {
            var definition = catalog.FindOffer(offer.id);
            if (string.IsNullOrWhiteSpace(definition?.invitationConversationId))
                return (offer.status == AssistantOfferStatus.Deferred ? "Saved for later. " : "") +
                    (definition?.description ?? string.Empty);
            if (offer.reminderSuppressed)
                return "Reminders are off. You can start the introduction from Assistant whenever you're ready.";
            if (offer.reminderDueUtcTicks > 0)
                return $"A {offer.reminderMinutes}-minute reminder is set. You can open Assistant and start the introduction now.";
            if (offer.reminderDelivered)
                return "Your introduction reminder is ready in Assistant. Open the conversation whenever you're ready.";
            return definition.description ?? string.Empty;
        }

        // Account state is retained only for this manager's lifetime. The
        // existing JSON API still exports/imports the currently bound account.
        public void SetActiveUser(string userId)
        {
            userId = string.IsNullOrWhiteSpace(userId) ? null : userId;

            if (ActiveUserId == userId)
                return;

            if (ActiveUserId != null)
                userStates[ActiveUserId] = state;

            ActiveUserId = userId;

            if (userId == null || !userStates.TryGetValue(userId, out state))
                state = new AssistantSaveData();

            Tutorials = new TutorialManager(catalog, state, engine);
            SelectedOfferId = null;
            OfferSelectionVersion++;

            // Rebinding must not replay effects or re-evaluate another
            // account's signals. The desktop integration supplies fresh facts.
            Changed?.Invoke();
        }

        public bool CanPresentOffer(string offerId)
        {
            var offer = state.offers.Find(item => item.id == offerId);

            return offer != null &&
                   (offer.status == AssistantOfferStatus.Presented ||
                    offer.status == AssistantOfferStatus.Deferred) &&
                   !GetFact("assistant.offers.blocked") &&
                   IsOfferValid(offer);
        }

        public bool TrySelectOffer(string offerId)
        {
            if (!CanPresentOffer(offerId))
                return false;

            // Open the invitation chat without accepting or starting a tutorial.
            // Offers without an invitation retain their existing card selection.
            SelectedOfferId = offerId;
            OfferSelectionVersion++;
            var offer = state.offers.Find(item => item.id == offerId);
            var invitation = EnsureInvitation(offer);
            if (invitation != null)
                state.selectedConversationId = invitation.id;
            Changed?.Invoke();
            return true;
        }

        public AssistantConversationState FindConversation(string id)
        {
            return state.conversations.Find(item => item.id == id);
        }

        public AssistantNodeDefinition GetCurrentNode(
            AssistantConversationState conversation)
        {
            if (conversation == null || conversation.finished)
                return null;

            return catalog.FindConversation(conversation.id)?
                .FindNode(conversation.currentNodeId);
        }

        public List<AssistantChoiceDefinition> GetChoices(
            AssistantConversationState conversation)
        {
            return engine.GetChoices(
                GetCurrentNode(conversation),
                state,
                conversation?.nodeEnteredSequence ?? 0);
        }

        public bool StartConversation(
            string conversationId,
            bool select = true,
            string returnContext = null)
        {
            var definition = catalog.FindConversation(conversationId);

            if (definition == null)
                return false;

            var existing = FindConversation(conversationId);

            if (existing != null)
            {
                if (select)
                    SelectConversation(conversationId);

                return true;
            }

            if (!engine.Meets(definition.prerequisites, state, 0))
                return false;

            var tutorial =
                catalog.FindTutorialForConversation(conversationId);

            if (tutorial != null && !Tutorials.Begin(tutorial.id))
                return false;

            var runtime = new AssistantConversationState
            {
                id = conversationId,
                returnContext = returnContext
            };

            state.conversations.Add(runtime);

            if (select)
                state.selectedConversationId = conversationId;

            EnterNode(runtime, definition.entryNodeId);
            Refresh();

            return true;
        }

        public void SelectConversation(string conversationId)
        {
            if (FindConversation(conversationId) == null)
                return;

            state.selectedConversationId = conversationId;
            Changed?.Invoke();
        }

        public void SelectChoice(
            string conversationId,
            string expectedNodeId,
            string choiceId)
        {
            var runtime = FindConversation(conversationId);

            // Prevent stale UI buttons from advancing another node.
            if (runtime == null ||
                runtime.currentNodeId != expectedNodeId)
            {
                return;
            }

            var definition = catalog.FindConversation(conversationId);

            if (!string.IsNullOrWhiteSpace(runtime.offerId))
            {
                SelectInvitationChoice(runtime, choiceId);
                return;
            }

            var transition = engine.Choose(
                definition,
                runtime,
                choiceId,
                state);

            if (transition != null)
            {
                ApplyTransition(runtime, transition);
                Refresh();
            }
        }

        public void SubmitText(string conversationId, string text)
        {
            var runtime = FindConversation(conversationId);

            if (runtime == null || string.IsNullOrWhiteSpace(text))
                return;

            AddMessage(runtime, text.Trim(), true);

            // Free text never silently completes a tutorial step.
            AddMessage(
                runtime,
                "I can guide this interaction through the available " +
                "responses below. Your current progress is unchanged.",
                false);

            runtime.draft = string.Empty;
            Changed?.Invoke();
        }

        public void SetFact(string key, bool value = true)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            SetFactInternal(key, value);
            Refresh();
        }

        public bool GetFact(string key)
        {
            return state.facts.Exists(
                item => item.key == key && item.value);
        }

        public void ReportSignal(
            string key,
            string returnContext = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            state.sequence++;

            var signal = state.signals.Find(
                item => item.key == key);

            if (signal == null)
            {
                signal = new AssistantSignal
                {
                    key = key
                };

                state.signals.Add(signal);
            }

            signal.sequence = state.sequence;
            signal.context = returnContext;

            Refresh();
        }

        public bool IsOfferValid(AssistantOfferState offer)
        {
            var definition = catalog.FindOffer(offer.id);

            return definition != null &&
                   engine.Meets(definition.conditions, state, 0);
        }

        public void AcceptOffer(string offerId)
        {
            var offer = state.offers.Find(
                item => item.id == offerId);

            if (offer == null ||
                (offer.status != AssistantOfferStatus.Presented &&
                 offer.status != AssistantOfferStatus.Deferred))
            {
                return;
            }

            if (!IsOfferValid(offer))
            {
                offer.status = AssistantOfferStatus.Expired;
                Refresh();
                return;
            }

            var definition = catalog.FindOffer(offerId);

            if (!StartConversation(
                    definition.conversationId,
                    select: false,
                    returnContext: offer.returnContext))
            {
                return;
            }

            var conversation = FindConversation(
                definition.conversationId);

            conversation.returnContext = offer.returnContext;
            offer.status = AssistantOfferStatus.Accepted;
            offer.reminderDueUtcTicks = 0;
            offer.reminderSuppressed = false;
            var invitation = FindConversation(definition.invitationConversationId);
            if (invitation != null)
            {
                AddMessage(invitation, "Let's begin the introduction. Any scheduled introduction reminder is now canceled.", false);
                invitation.finished = true;
                invitation.currentNodeId = null;
            }

            state.selectedConversationId = conversation.id;

            Refresh();
        }

        public void DeferOffer(string offerId)
        {
            if (!string.IsNullOrWhiteSpace(catalog.FindOffer(offerId)?.invitationConversationId))
            {
                if (TrySelectOffer(offerId))
                    SelectInvitationChoice(SelectedConversation, "no-thanks");
                return;
            }
            SetOfferStatus(
                offerId,
                AssistantOfferStatus.Deferred);
        }

        public void DismissOffer(string offerId)
        {
            if (!string.IsNullOrWhiteSpace(catalog.FindOffer(offerId)?.invitationConversationId))
            {
                DeferOffer(offerId);
                return;
            }
            SetOfferStatus(
                offerId,
                AssistantOfferStatus.Dismissed);
        }

        private void SelectInvitationChoice(AssistantConversationState conversation, string choiceId)
        {
            var offer = GetInvitationOffer(conversation);
            if (offer == null || !CanPresentOffer(offer.id) || conversation.finished) return;
            var choice = GetChoices(conversation).Find(item => item.id == choiceId);
            if (choice == null) return;
            if (choiceId == "start")
            {
                // Do not claim success or clear the reminder unless starting succeeds.
                AcceptOffer(offer.id);
                return;
            }
            AddMessage(conversation, choice.label, true);
            if (choiceId == "no-thanks")
                EnterNode(conversation, "reminder");
            else if (choiceId == "back")
                EnterNode(conversation, "offer");
            else if (choiceId == "continue")
            {
                offer.status = AssistantOfferStatus.Deferred;
                offer.reminderSuppressed = offer.reminderMinutes == 0;
                offer.reminderDelivered = false;
                offer.reminderDueUtcTicks = offer.reminderSuppressed ? 0 :
                    DateTime.UtcNow.AddMinutes(offer.reminderMinutes).Ticks;
                AddMessage(conversation, offer.reminderSuppressed ? "Never remind me." :
                    $"Remind me in {offer.reminderMinutes} minutes.", true);
                EnterNode(conversation, "confirmation");
                AddMessage(conversation, offer.reminderSuppressed ?
                    "Okay. You can start the introduction from Assistant whenever you're ready." :
                    $"I'll remind you in {offer.reminderMinutes} minutes. You can also start the introduction here anytime.", false);
            }
            Refresh();
        }

        public void SkipTutorial(string tutorialId)
        {
            var definition = catalog.FindTutorial(tutorialId);

            if (definition == null)
                return;

            Tutorials.Skip(tutorialId);

            var conversation = FindConversation(
                definition.conversationId);

            if (conversation != null &&
                Tutorials.GetState(tutorialId).status ==
                AssistantTutorialStatus.Skipped)
            {
                conversation.finished = true;
                conversation.currentNodeId = null;

                AddMessage(
                    conversation,
                    "The introduction has been skipped. " +
                    "You can replay it from Help.",
                    false);
            }

            Refresh();
        }

        public bool ReplayTutorial(string tutorialId)
        {
            var definition = catalog.FindTutorial(tutorialId);

            if (definition == null)
                return false;

            // Check before removing the previous conversation.
            var conversationDefinition = catalog.FindConversation(
                definition.conversationId);

            if (!engine.Meets(definition.prerequisites, state, 0) ||
                !engine.Meets(
                    conversationDefinition.prerequisites,
                    state,
                    0))
            {
                return false;
            }

            Tutorials.ResetForReplay(tutorialId);

            state.conversations.RemoveAll(
                item => item.id == definition.conversationId);

            return StartConversation(definition.conversationId);
        }

        public string ExportJson()
        {
            return JsonUtility.ToJson(state, true);
        }

        public void ImportJson(string json)
        {
            var restored =
                JsonUtility.FromJson<AssistantSaveData>(json);

            if (restored == null || restored.version != 1)
            {
                throw new InvalidOperationException(
                    "[AssistantManager] Unsupported save data.");
            }

            restored.facts ??= new();
            restored.signals ??= new();
            restored.conversations ??= new();
            restored.offers ??= new();
            restored.tutorials ??= new();

            // Ignore conversations removed from the authored catalog.
            restored.conversations.RemoveAll(
                item => item == null ||
                        catalog.FindConversation(item.id) == null);

            foreach (var conversation in restored.conversations)
            {
                conversation.messages ??= new();

                if (!conversation.finished &&
                    catalog.FindConversation(conversation.id)
                        .FindNode(conversation.currentNodeId) == null)
                {
                    throw new InvalidOperationException(
                        $"Saved node is missing for '{conversation.id}'.");
                }
            }

            state = restored;
            Tutorials = new TutorialManager(catalog, state, engine);
            SelectedOfferId = null;
            OfferSelectionVersion++;

            // Restoring never reruns node entry effects.
            Refresh();
        }

        private void SetOfferStatus(
            string offerId,
            AssistantOfferStatus status)
        {
            var offer = state.offers.Find(
                item => item.id == offerId);

            if (offer == null ||
                (offer.status != AssistantOfferStatus.Presented &&
                 offer.status != AssistantOfferStatus.Deferred))
            {
                return;
            }

            offer.status = status;
            Refresh();
        }

        private void ApplyTransition(
            AssistantConversationState runtime,
            AssistantTransition transition)
        {
            if (runtime.currentNodeId != transition.fromNodeId ||
                !Tutorials.CanAdvance(
                    runtime.id,
                    transition.fromNodeId))
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(transition.playerText))
                AddMessage(runtime, transition.playerText, true);

            Tutorials.RecordTransition(
                runtime.id,
                transition.fromNodeId,
                transition.nextNodeId);

            runtime.currentNodeId = transition.nextNodeId;

            runtime.finished =
                string.IsNullOrWhiteSpace(transition.nextNodeId);

            runtime.nodeEnteredSequence = state.sequence;

            ApplyEffects(runtime, transition.effects);

            if (!runtime.finished)
                EnterNode(runtime, transition.nextNodeId);
        }

        private void EnterNode(
            AssistantConversationState runtime,
            string nodeId)
        {
            var node = catalog.FindConversation(runtime.id)
                .FindNode(nodeId);

            runtime.currentNodeId = nodeId;
            runtime.nodeEnteredSequence = state.sequence;
            runtime.finished = false;

            if (!string.IsNullOrWhiteSpace(node.message))
                AddMessage(runtime, node.message, false);

            ApplyEffects(runtime, node.enterEffects);
        }

        private void ApplyEffects(
            AssistantConversationState runtime,
            AssistantEffect[] effects)
        {
            if (effects == null)
                return;

            foreach (var effect in effects)
            {
                if (effect == null)
                    continue;

                if (effect.kind == AssistantEffectKind.SetFact)
                {
                    SetFactInternal(effect.key, effect.value);
                }
                else
                {
                    ActionRequested?.Invoke(
                        new AssistantActionRequest
                        {
                            actionId = effect.key,
                            conversationId = runtime.id,
                            returnContext = runtime.returnContext
                        });
                }
            }
        }

        private void SetFactInternal(string key, bool value)
        {
            var fact = state.facts.Find(
                item => item.key == key);

            if (fact == null)
            {
                fact = new AssistantFact
                {
                    key = key
                };

                state.facts.Add(fact);
            }

            fact.value = value;
        }

        private static void AddMessage(
            AssistantConversationState runtime,
            string text,
            bool fromPlayer)
        {
            runtime.messages.Add(
                new AssistantMessageData
                {
                    id = Guid.NewGuid().ToString("N"),
                    nodeId = runtime.currentNodeId,
                    text = text,
                    fromPlayer = fromPlayer,
                    time = DateTime.Now.ToString("HH:mm")
                });
        }

        private void Refresh()
        {
            if (pumping)
                return;

            pumping = true;

            try
            {
                bool advanced = false;

                // Bound automatic chains to catch accidental graph loops.
                for (int pass = 0; pass < 64; pass++)
                {
                    advanced = false;

                    foreach (var runtime in state.conversations.ToArray())
                    {
                        if (runtime.finished ||
                            !Tutorials.CanAdvance(
                                runtime.id,
                                runtime.currentNodeId))
                        {
                            continue;
                        }

                        var definition =
                            catalog.FindConversation(runtime.id);

                        var transition = engine.EvaluateAutomatic(
                            definition,
                            runtime,
                            state);

                        if (transition == null)
                            continue;

                        ApplyTransition(runtime, transition);
                        advanced = true;
                    }

                    if (!advanced)
                        break;
                }

                if (advanced)
                {
                    Debug.LogError(
                        "[AssistantManager] Automatic transition " +
                        "limit reached.",
                        this);
                }

                RefreshOffers();

                if (SelectedOfferId != null && !CanPresentOffer(SelectedOfferId))
                    SelectedOfferId = null;
            }
            finally
            {
                pumping = false;
            }

            Changed?.Invoke();
        }

        private void RefreshOffers()
        {
            foreach (var definition in catalog.offers)
            {
                if (state.offers.Exists(
                        item => item.id == definition.id))
                {
                    continue;
                }

                var signal = state.signals.Find(
                    item => item.key == definition.triggerSignal);

                if (signal == null ||
                    !engine.Meets(definition.conditions, state, 0))
                {
                    continue;
                }

                state.offers.Add(
                    new AssistantOfferState
                    {
                        id = definition.id,
                        status = AssistantOfferStatus.Pending,
                        returnContext = signal.context
                    });
            }

            bool alreadyPresented = false;

            AssistantOfferState candidate = null;
            int candidatePriority = int.MinValue;

            foreach (var offer in state.offers)
            {
                bool unresolved =
                    offer.status == AssistantOfferStatus.Pending ||
                    offer.status == AssistantOfferStatus.Presented ||
                    offer.status == AssistantOfferStatus.Deferred;

                if (unresolved && !IsOfferValid(offer))
                {
                    offer.status = AssistantOfferStatus.Expired;
                    offer.reminderDueUtcTicks = 0;
                }

                if (offer.status == AssistantOfferStatus.Presented)
                    alreadyPresented = true;

                if (offer.status != AssistantOfferStatus.Pending)
                    continue;

                int priority = catalog.FindOffer(offer.id).priority;

                if (candidate == null || priority > candidatePriority)
                {
                    candidate = offer;
                    candidatePriority = priority;
                }
            }

            bool blocked = GetFact("assistant.offers.blocked");

            if (!blocked && !alreadyPresented && candidate != null)
                candidate.status = AssistantOfferStatus.Presented;
        }
    }
}
