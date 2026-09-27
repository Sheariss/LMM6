using System;
using System.Collections.Generic;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    public sealed class CMDEngine
    {
        private readonly CMDCommandLibrary commandLibrary;
        private readonly List<string> outputEntries = new();

        public IReadOnlyList<string> OutputEntries => outputEntries;

        public string CurrentPath { get; private set; } = @"C:\Users\Guest";
        public string Prompt => $"{CurrentPath}>";

        private const char DefaultBackgroundColor = '0';
        private const char DefaultForegroundColor = '7';

        public char BackgroundColor { get; private set; } = DefaultBackgroundColor;
        public char ForegroundColor { get; private set; } = DefaultForegroundColor;


        public CMDEngine(CMDCommandLibrary commandLibrary) {
            this.commandLibrary = commandLibrary;
            Initialize();
        }

        // -------------------- INITIALIZATION --------------------
        private void Initialize()
        {
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;

            outputEntries.Clear();

            AddStartupText();
        }

        private void AddStartupText()
        {
            AddOutput(
                "ATLAS Operating System [Version 1.0.0]\n" +
                "(c) ATLAS Systems. All rights reserved.\n\n");
        }

        // -------------------- EXECUTE --------------------
        public void Execute(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
                return;

            string input = rawInput.Trim();

            AddCommandToOutput(input);

            string[] parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);

            string commandName =  parts[0].ToLowerInvariant();

            string argument = parts.Length > 1
                    ? parts[1].Trim()
                    : string.Empty;

            switch (commandName)
            {
                case "cls":
                    ExecuteClear();
                    return;

                case "color":
                    ExecuteColor(argument);
                    return;

                case "help":
                    ExecuteHelp();
                    return;

                case "ver":
                    ExecuteVersion();
                    return;

                case "whoami":
                    ExecuteWhoAmI();
                    return;
            }

            ExecuteAuthoredCommand(input);
        }

        // -------------------- COMMAND OUTPUT --------------------
        private void AddCommandToOutput(string input)
        {
            outputEntries.Add($@"C:\Users\Guest> {input}");
        }

        // -------------------- CLS --------------------
        private void ExecuteClear()
        {
            outputEntries.Clear();
        }

        // -------------------- COLOR --------------------
        private void ExecuteColor(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                ResetColors();
                return;
            }

            if (argument.Equals("/?", StringComparison.OrdinalIgnoreCase))
            {
                AddColorHelp();
                return;
            }

            string attribute = argument.Trim().ToUpperInvariant();

            if (attribute.Length != 2)
            {
                AddIncorrectSyntax();
                return;
            }

            char background = attribute[0];
            char foreground = attribute[1];

            if (!IsValidColor(background) ||
                !IsValidColor(foreground))
            {
                AddIncorrectSyntax();
                return;
            }

            if (background == foreground)
            {
                return;
            }

            BackgroundColor = background;
            ForegroundColor = foreground;
        }

        private void ResetColors()
        {
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;
        }

        private bool IsValidColor(char value)
        {
            return
                (value >= '0' && value <= '9') ||
                (value >= 'A' && value <= 'F');
        }

        private void AddColorHelp()
        {
            AddOutput(
                "Sets the default console foreground and background colors.\n\n" +
                "COLOR [attr]\n\n" +
                "  attr        Specifies color attribute of console output\n\n" +
                "Color attributes are specified by TWO hex digits -- the first\n" +
                "corresponds to the background; the second the foreground. Each digit\n" +
                "can be any of the following values:\n\n" +
                "    0 = Black        8 = Gray\n" +
                "    1 = Blue         9 = Light Blue\n" +
                "    2 = Green        A = Light Green\n" +
                "    3 = Aqua         B = Light Aqua\n" +
                "    4 = Red          C = Light Red\n" +
                "    5 = Purple       D = Light Purple\n" +
                "    6 = Yellow       E = Light Yellow\n" +
                "    7 = White        F = Bright White\n\n" +
                "If no argument is given, this command restores the color to its default.");
        }

        // -------------------- HELP --------------------
        private void ExecuteHelp()
        {
            AddOutput(
               "For more information on a specific command, type HELP command-name.\n\n" +
               "CLS        Clears the screen.\n" +
               "COLOR      Sets the default console foreground and background colors.\n" +
               "HELP       Provides Help information for commands.\n" +
               "VER        Displays the operating system version.\n" +
               "WHOAMI     Displays the current user.");
        }

        // -------------------- VERSION --------------------
        private void ExecuteVersion()
        {
            AddOutput("AtlasOS [Version 2.0.15600.3625]");
        }

        // -------------------- WHOAMI --------------------
        private void ExecuteWhoAmI()
        {
            AddOutput(@"atlas\guest");
        }

        // -------------------- AUTHORED COMMANDS --------------------
        private void ExecuteAuthoredCommand(string input)
        {
            CMDCommand command = commandLibrary?.FindCommand(input);

            if (command != null) {
                AddAuthoredResult(command);
                return;
            }

            AddUnknownCommand(input);
        }

        private void AddAuthoredResult(CMDCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.Result))
                return;

            AddOutput(command.Result);
        }

        // -------------------- ERRORS --------------------
        private void AddUnknownCommand(string input)
        {
            AddOutput($"'{input}' is not recognized as an internal or external command, operable program or batch file.");
        }

        private void AddIncorrectSyntax()
        {
            AddOutput("The syntax of the command is incorrect.");
        }

        // -------------------- HELPERS --------------------
        private void AddOutput(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            outputEntries.Add(text);
        }
    }
}