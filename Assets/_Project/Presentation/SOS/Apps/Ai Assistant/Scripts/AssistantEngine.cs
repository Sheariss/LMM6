using System;
using System.Collections.Generic;

namespace Atlas.Presentation.SOS.Assistant
{
    public sealed class AssistantEngine
    {
        public bool Meets(
            AssistantCondition[] conditions,
            AssistantSaveData state,
            long nodeEnteredSequence)
        {
            if (conditions == null)
                return true;

            foreach (var condition in conditions)
            {
                if (condition == null)
                    continue;

                bool actual;

                switch (condition.kind)
                {
                    case AssistantConditionKind.Fact:
                        {
                            var fact = state.facts.Find(
                                item => item.key == condition.key);

                            actual = fact != null && fact.value;
                            break;
                        }

                    case AssistantConditionKind.SignalReceived:
                        {
                            actual = state.signals.Exists(
                                item => item.key == condition.key);

                            break;
                        }

                    case AssistantConditionKind.SignalAfterNodeEntered:
                        {
                            actual = state.signals.Exists(
                                item => item.key == condition.key &&
                                        item.sequence > nodeEnteredSequence);

                            break;
                        }

                    default:
                        return false;
                }

                if (actual != condition.expectedValue)
                    return false;
            }

            return true;
        }

        public List<AssistantChoiceDefinition> GetChoices(
            AssistantNodeDefinition node,
            AssistantSaveData state,
            long nodeEnteredSequence)
        {
            var result = new List<AssistantChoiceDefinition>();

            if (node == null)
                return result;

            foreach (var choice in node.choices)
            {
                if (choice != null &&
                    Meets(
                        choice.conditions,
                        state,
                        nodeEnteredSequence))
                {
                    result.Add(choice);
                }
            }

            return result;
        }

        public AssistantTransition Choose(
            AssistantConversationDefinition conversation,
            AssistantConversationState runtime,
            string choiceId,
            AssistantSaveData state)
        {
            var node = conversation.FindNode(runtime.currentNodeId);

            if (node == null || runtime.finished)
                return null;

            var choice = Array.Find(
                node.choices,
                item => item != null && item.id == choiceId);

            if (choice == null ||
                !Meets(
                    choice.conditions,
                    state,
                    runtime.nodeEnteredSequence))
            {
                return null;
            }

            return new AssistantTransition
            {
                conversationId = conversation.id,
                fromNodeId = node.id,
                nextNodeId = choice.nextNodeId,
                playerText = string.IsNullOrWhiteSpace(choice.playerText)
                    ? choice.label
                    : choice.playerText,
                effects = choice.effects
            };
        }

        public AssistantTransition EvaluateAutomatic(
            AssistantConversationDefinition conversation,
            AssistantConversationState runtime,
            AssistantSaveData state)
        {
            var node = conversation.FindNode(runtime.currentNodeId);

            if (node == null ||
                runtime.finished ||
                node.advanceWhen.Length == 0 ||
                !Meets(
                    node.advanceWhen,
                    state,
                    runtime.nodeEnteredSequence))
            {
                return null;
            }

            return new AssistantTransition
            {
                conversationId = conversation.id,
                fromNodeId = node.id,
                nextNodeId = node.automaticNextNodeId,
                effects = Array.Empty<AssistantEffect>()
            };
        }
    }
}