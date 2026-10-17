using System;
using UnityEngine;

namespace Atlas.Presentation.SOS.Assistant
{
    public enum TutorialHighlightMode
    {
        None,
        Outline,
        Spotlight
    }

    public enum TutorialPointerMode
    {
        None,
        Arrow
    }

    public enum TutorialCardPlacement
    {
        Automatic,
        AboveTarget,
        BelowTarget,
        LeftOfTarget,
        RightOfTarget,
        ScreenCenter
    }

    public enum TutorialInputPolicy
    {
        // The player can interact with the whole desktop.
        Unrestricted,

        // Only highlighted targets, explicitly allowed targets,
        // and tutorial controls remain interactive.
        HighlightedTargetsOnly
    }

    public enum TutorialMissingTargetPolicy
    {
        // Show the instruction without restricting input.
        ShowInstructionOnly,

        // Suspend this step's overlay until a target becomes visible.
        // Tutorial controls must remain available.
        WaitForVisibleTarget
    }

    [Serializable]
    public sealed class TutorialHighlightTarget
    {
        [Tooltip(
            "Stable ID resolved by the runtime UI target registry. " +
            "This is not a UXML element reference.")]
        public string targetId;

        [Tooltip("Draw an arrow toward this target when it is visible.")]
        public TutorialPointerMode pointer = TutorialPointerMode.Arrow;

        [Min(0f)]
        [Tooltip("Space around the target's highlight, in panel units.")]
        public float padding = 8f;

        [Tooltip(
            "Allow this target to anchor the instruction card. " +
            "The presenter uses the first visible eligible target.")]
        public bool canAnchorCard = true;
    }

    [Serializable]
    public sealed class TutorialPresentationDefinition
    {
        [Tooltip(
            "Enable guided presentation for this node. " +
            "Disabled nodes use the normal Assistant conversation.")]
        public bool enabled;

        public TutorialHighlightMode highlightMode =
            TutorialHighlightMode.Spotlight;

        [Range(0f, 0.9f)]
        public float dimOpacity = 0.65f;

        [Tooltip(
            "Highlight every registered target that is currently visible. " +
            "Hidden targets are ignored until they become visible.")]
        public TutorialHighlightTarget[] targets =
            Array.Empty<TutorialHighlightTarget>();

        public TutorialCardPlacement cardPlacement =
            TutorialCardPlacement.Automatic;

        public TutorialInputPolicy inputPolicy =
            TutorialInputPolicy.Unrestricted;

        [Tooltip(
            "Additional UI regions that remain interactive when using " +
            "HighlightedTargetsOnly. Tutorial controls are always allowed.")]
        public string[] additionalAllowedTargetIds =
            Array.Empty<string>();

        public TutorialMissingTargetPolicy missingTargetPolicy =
            TutorialMissingTargetPolicy.ShowInstructionOnly;

        [Tooltip(
            "Instruction shown when no highlight target can be resolved. " +
            "Empty uses the node's normal message.")]
        [TextArea(2, 5)]
        public string missingTargetMessage;

        [Tooltip(
            "Status displayed while waiting for the node's automatic " +
            "advancement conditions. This does not control advancement.")]
        public string waitingStatus;

        public bool showBackButton = true;
        public bool showSkipButton = true;
    }
}