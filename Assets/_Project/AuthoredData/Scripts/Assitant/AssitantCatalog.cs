using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Assistant
{
    [CreateAssetMenu(
        menuName = "ATLAS/Assistant/Catalog",
        fileName = "AssistantCatalog")]
    public sealed class AssistantCatalog : ScriptableObject
    {
        public AssistantConversationDefinition[] conversations =
            Array.Empty<AssistantConversationDefinition>();

        public AssistantTutorialDefinition[] tutorials =
            Array.Empty<AssistantTutorialDefinition>();

        public AssistantOfferDefinition[] offers =
            Array.Empty<AssistantOfferDefinition>();

        public AssistantConversationDefinition FindConversation(string id)
        {
            return Array.Find(
                conversations,
                item => item != null && item.id == id);
        }

        public AssistantTutorialDefinition FindTutorial(string id)
        {
            return Array.Find(
                tutorials,
                item => item != null && item.id == id);
        }

        public AssistantTutorialDefinition FindTutorialForConversation(
            string conversationId)
        {
            return Array.Find(
                tutorials,
                item => item != null &&
                        item.conversationId == conversationId);
        }

        public AssistantOfferDefinition FindOffer(string id)
        {
            return Array.Find(
                offers,
                item => item != null && item.id == id);
        }

        public void ValidateOrThrow()
        {
            var conversationIds = new HashSet<string>();
            var tutorialIds = new HashSet<string>();
            var offerIds = new HashSet<string>();
            var tutorialConversations = new HashSet<string>();

            foreach (var conversation in conversations)
            {
                RequireUnique(
                    conversation?.id,
                    conversationIds,
                    "conversation");

                var nodeIds = new HashSet<string>();

                foreach (var node in conversation.nodes)
                {
                    RequireUnique(node?.id, nodeIds, "node");

                    var choiceIds = new HashSet<string>();

                    foreach (var choice in node.choices)
                    {
                        RequireUnique(
                            choice?.id,
                            choiceIds,
                            "choice");
                    }
                }

                RequireNode(conversation, conversation.entryNodeId);

                foreach (var node in conversation.nodes)
                {
                    if (node.advanceWhen.Length > 0)
                        RequireOptionalNode(
                            conversation,
                            node.automaticNextNodeId);

                    foreach (var choice in node.choices)
                        RequireOptionalNode(
                            conversation,
                            choice.nextNodeId);
                }
            }

            foreach (var tutorial in tutorials)
            {
                RequireUnique(
                    tutorial?.id,
                    tutorialIds,
                    "tutorial");

                if (FindConversation(tutorial.conversationId) == null)
                    throw new InvalidOperationException(
                        $"Tutorial '{tutorial.id}' has no conversation.");

                if (!tutorialConversations.Add(tutorial.conversationId))
                    throw new InvalidOperationException(
                        "A conversation can belong to only one tutorial.");
            }

            foreach (var offer in offers)
            {
                RequireUnique(offer?.id, offerIds, "offer");

                if (FindConversation(offer.conversationId) == null)
                    throw new InvalidOperationException(
                        $"Offer '{offer.id}' has no conversation.");

                if (string.IsNullOrWhiteSpace(offer.triggerSignal))
                    throw new InvalidOperationException(
                        $"Offer '{offer.id}' has no trigger signal.");
            }
        }

        private static void RequireUnique(
            string id,
            HashSet<string> ids,
            string kind)
        {
            if (string.IsNullOrWhiteSpace(id) || !ids.Add(id))
                throw new InvalidOperationException(
                    $"Missing or duplicate {kind} ID: '{id}'.");
        }

        private static void RequireOptionalNode(
            AssistantConversationDefinition conversation,
            string nodeId)
        {
            if (!string.IsNullOrWhiteSpace(nodeId))
                RequireNode(conversation, nodeId);
        }

        private static void RequireNode(
            AssistantConversationDefinition conversation,
            string nodeId)
        {
            if (conversation.FindNode(nodeId) == null)
                throw new InvalidOperationException(
                    $"Conversation '{conversation.id}' " +
                    $"references missing node '{nodeId}'.");
        }
    }
}