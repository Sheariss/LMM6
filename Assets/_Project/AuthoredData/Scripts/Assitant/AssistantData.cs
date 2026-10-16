using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Assistant
{
    public enum AssistantConditionKind
    {
        Fact,
        SignalReceived,
        SignalAfterNodeEntered
    }

    public enum AssistantEffectKind
    {
        SetFact,
        RequestAction
    }

    public enum AssistantOfferStatus
    {
        Pending,
        Presented,
        Accepted,
        Deferred,
        Dismissed,
        Expired
    }

    public enum AssistantTutorialStatus
    {
        NotStarted,
        Active,
        Completed,
        Skipped
    }

    public enum AssistantTutorialNodeStatus
    {
        Locked,
        Available,
        Active,
        Completed,
        Skipped
    }

    [Serializable]
    public sealed class AssistantCondition
    {
        public AssistantConditionKind kind;
        public string key;
        public bool expectedValue = true;
    }

    [Serializable]
    public sealed class AssistantEffect
    {
        public AssistantEffectKind kind;
        public string key;
        public bool value = true;
    }

    [Serializable]
    public sealed class AssistantChoiceDefinition
    {
        public string id;
        public string label;
        public string playerText;

        // Empty ends the conversation.
        public string nextNodeId;

        public AssistantCondition[] conditions =
            Array.Empty<AssistantCondition>();

        public AssistantEffect[] effects =
            Array.Empty<AssistantEffect>();
    }

    [Serializable]
    public sealed class AssistantNodeDefinition
    {
        public string id;

        [TextArea(2, 8)]
        public string message;

        public AssistantChoiceDefinition[] choices =
            Array.Empty<AssistantChoiceDefinition>();

        // These effects run once when this node is entered.
        public AssistantEffect[] enterEffects =
            Array.Empty<AssistantEffect>();

        // A non-empty array enables automatic advancement.
        // All conditions must pass.
        public AssistantCondition[] advanceWhen =
            Array.Empty<AssistantCondition>();

        public string automaticNextNodeId;
    }

    [Serializable]
    public sealed class AssistantConversationDefinition
    {
        public string id;
        public string title;
        public string entryNodeId;

        public AssistantCondition[] prerequisites =
            Array.Empty<AssistantCondition>();

        public AssistantNodeDefinition[] nodes =
            Array.Empty<AssistantNodeDefinition>();

        public AssistantNodeDefinition FindNode(string nodeId)
        {
            return Array.Find(
                nodes,
                node => node != null && node.id == nodeId);
        }
    }

    [Serializable]
    public sealed class AssistantTutorialDefinition
    {
        public string id;
        public string conversationId;

        public AssistantCondition[] prerequisites =
            Array.Empty<AssistantCondition>();
    }

    [Serializable]
    public sealed class AssistantOfferDefinition
    {
        public string id;
        public string title;

        [TextArea(2, 5)]
        public string description;

        public string conversationId;
        public string triggerSignal;
        public int priority;

        public AssistantCondition[] conditions =
            Array.Empty<AssistantCondition>();
    }

    [Serializable]
    public sealed class AssistantFact
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public sealed class AssistantSignal
    {
        public string key;
        public long sequence;
        public string context;
    }

    [Serializable]
    public sealed class AssistantMessageData
    {
        public string id;
        public string nodeId;
        public string text;
        public bool fromPlayer;
        public string time;
    }

    [Serializable]
    public sealed class AssistantConversationState
    {
        public string id;
        public string currentNodeId;
        public long nodeEnteredSequence;
        public bool finished;

        public string draft;
        public float scrollY;

        // An opaque route understood by your navigation integration.
        public string returnContext;

        public List<AssistantMessageData> messages = new();
    }

    [Serializable]
    public sealed class AssistantOfferState
    {
        public string id;
        public AssistantOfferStatus status;
        public string returnContext;
    }

    [Serializable]
    public sealed class AssistantTutorialNodeState
    {
        public string nodeId;
        public AssistantTutorialNodeStatus status;
    }

    [Serializable]
    public sealed class AssistantTutorialState
    {
        public string id;
        public AssistantTutorialStatus status;
        public string activeNodeId;

        public List<AssistantTutorialNodeState> nodes = new();
    }

    [Serializable]
    public sealed class AssistantSaveData
    {
        public int version = 1;
        public long sequence;
        public string selectedConversationId;

        public List<AssistantFact> facts = new();
        public List<AssistantSignal> signals = new();

        public List<AssistantConversationState> conversations = new();
        public List<AssistantOfferState> offers = new();
        public List<AssistantTutorialState> tutorials = new();
    }

    public sealed class AssistantTransition
    {
        public string conversationId;
        public string fromNodeId;
        public string nextNodeId;
        public string playerText;
        public AssistantEffect[] effects;
    }

    public sealed class AssistantActionRequest
    {
        public string actionId;
        public string conversationId;
        public string returnContext;
    }
}