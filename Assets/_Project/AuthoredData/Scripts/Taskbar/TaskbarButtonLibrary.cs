using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.Taskbar
{
    [CreateAssetMenu(
        fileName = "TaskbarButtonLibrary",
        menuName = "ATLAS/SOS/Taskbar/Button Library")]
    public sealed class TaskbarButtonLibrary : ScriptableObject
    {
        [SerializeField]
        private List<TaskbarButtonDefinition> buttons = new();

        public IReadOnlyList<TaskbarButtonDefinition> Buttons => buttons;

        public TaskbarButtonDefinition GetButton(string buttonId)
        {
            if (string.IsNullOrWhiteSpace(buttonId))
                return null;

            foreach (TaskbarButtonDefinition button in buttons)
            {
                if (button != null &&
                    string.Equals(
                        button.Id,
                        buttonId,
                        StringComparison.Ordinal))
                {
                    return button;
                }
            }

            return null;
        }

        public TaskbarButtonStateDefinition GetState(
            string buttonId,
            string stateId)
        {
            return GetButton(buttonId)?.GetState(stateId);
        }

        public TaskbarButtonStateDefinition GetDefaultState(
            string buttonId)
        {
            return GetButton(buttonId)?.DefaultState;
        }

        private void OnValidate()
        {
            var buttonIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (TaskbarButtonDefinition button in buttons)
            {
                if (button == null)
                {
                    Warn("The library contains an empty button entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(button.Id))
                {
                    Warn("A button is missing its ID.");
                }
                else if (!buttonIds.Add(button.Id))
                {
                    Warn($"Duplicate button ID: '{button.Id}'.");
                }

                ValidateStates(button);
            }
        }

        private void ValidateStates(TaskbarButtonDefinition button)
        {
            var stateIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (TaskbarButtonStateDefinition state in button.States)
            {
                if (state == null)
                {
                    Warn($"Button '{button.Id}' contains an empty state.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(state.Id))
                {
                    Warn($"Button '{button.Id}' has a state without an ID.");
                }
                else if (!stateIds.Add(state.Id))
                {
                    Warn(
                        $"Button '{button.Id}' has duplicate state ID " +
                        $"'{state.Id}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(button.DefaultStateId))
            {
                Warn($"Button '{button.Id}' needs a default state ID.");
            }
            else if (button.DefaultState == null)
            {
                Warn(
                    $"Button '{button.Id}' references missing default " +
                    $"state '{button.DefaultStateId}'.");
            }
        }

        private void Warn(string message)
        {
            Debug.LogWarning(
                $"[{nameof(TaskbarButtonLibrary)}] {name}: {message}",
                this);
        }
    }

    [Serializable]
    public sealed class TaskbarButtonDefinition
    {
        [Header("Identity")]

        [Tooltip("Stable, case-sensitive ID. Example: volume")]
        [SerializeField]
        private string id;

        [Tooltip("Human-readable name for this button.")]
        [SerializeField]
        private string displayName;

        [Header("States")]

        [Tooltip("ID of the state displayed when initialized.")]
        [SerializeField]
        private string defaultStateId;

        [SerializeField]
        private List<TaskbarButtonStateDefinition> states = new();

        public string Id => id;
        public string DisplayName => displayName;
        public string DefaultStateId => defaultStateId;

        public IReadOnlyList<TaskbarButtonStateDefinition> States => states;

        public TaskbarButtonStateDefinition DefaultState =>
            GetState(defaultStateId);

        public TaskbarButtonStateDefinition GetState(string stateId)
        {
            if (string.IsNullOrWhiteSpace(stateId))
                return null;

            foreach (TaskbarButtonStateDefinition state in states)
            {
                if (state != null &&
                    string.Equals(
                        state.Id,
                        stateId,
                        StringComparison.Ordinal))
                {
                    return state;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class TaskbarButtonStateDefinition
    {
        [Header("Identity")]

        [Tooltip("Unique within this button. Example: low or rainy")]
        [SerializeField]
        private string id;

        [Header("Appearance")]

        [Tooltip("Texture displayed by the button's Image element.")]
        [SerializeField]
        private Sprite icon;

        [Header("Default Text")]

        [Tooltip("Text shown when hovering over the button.")]
        [TextArea(1, 3)]
        [SerializeField]
        private string tooltip;

        [Tooltip(
            "Optional first line. Runtime data can override this, " +
            "for example with the current temperature.")]
        [SerializeField]
        private string primaryText;

        [Tooltip(
            "Optional second line. Example: Partly cloudy.")]
        [SerializeField]
        private string secondaryText;

        [Header("Badge")]

        [Tooltip(
            "Allows a numeric badge in this state. " +
            "The controller supplies the count and hides it when zero.")]
        [SerializeField]
        private bool showBadge;

        public string Id => id;
        public Sprite Icon => icon;
        public string Tooltip => tooltip;
        public string PrimaryText => primaryText;
        public string SecondaryText => secondaryText;
        public bool ShowBadge => showBadge;
    }
}