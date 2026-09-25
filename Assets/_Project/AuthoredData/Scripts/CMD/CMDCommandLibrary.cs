using System;
using System.Collections.Generic;
using UnityEngine;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    [CreateAssetMenu(
        fileName = "CMDCommandLibrary",
        menuName = "ATLAS/SOS/Command Prompt/Command Library")]
    public sealed class CMDCommandLibrary : ScriptableObject
    {
        [SerializeField] private List<CMDCommand> commands = new();

        public IReadOnlyList<CMDCommand> Commands => commands;

        public CMDCommand FindCommand(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            foreach (CMDCommand command in commands)
            {
                if (command != null && command.Matches(input))
                    return command;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class CMDCommand
    {
        [Header("Command")]
        [SerializeField] private string command;
        [SerializeField] private string[] aliases;

        [Header("Output")]
        [TextArea(1, 10)]
        [SerializeField] private string result;

        public string Command => command;
        public string[] Aliases => aliases;
        public string Result => result;

        public bool Matches(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (string.Equals(
                command,
                input,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (aliases == null)
                return false;

            foreach (string alias in aliases)
            {
                if (string.Equals(
                    alias,
                    input,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}