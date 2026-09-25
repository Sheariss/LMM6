using System;
using System.Collections.Generic;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    public sealed class CMDEngine
    {
        private readonly CMDCommandLibrary commandLibrary;
        private readonly List<string> outputLines = new();

        private const char DefaultBackgroundColor = '0';
        private const char DefaultForegroundColor = '7';

        public IReadOnlyList<string> OutputLines => outputLines;

        public char BackgroundColor { get; private set; } =
            DefaultBackgroundColor;

        public char ForegroundColor { get; private set; } =
            DefaultForegroundColor;

        public CMDEngine(CMDCommandLibrary commandLibrary)
        {
            this.commandLibrary = commandLibrary;

            Initialize();
        }

        // -------------------- INITIALIZATION --------------------
        private void Initialize()
        {
            BackgroundColor = DefaultBackgroundColor;
            ForegroundColor = DefaultForegroundColor;

            outputLines.Clear();

            AddStartupText();
        }

        private void AddStartupText()
        {
            outputLines.Add(
                "Microsoft Windows [Version 10.0.26100.6584]");

            outputLines.Add(
                "(c) Microsoft Corporation. All rights reserved.");

            outputLines.Add(string.Empty);
        }

        // -------------------- EXECUTE --------------------
        public void Execute(string rawInput)
        {
            if (string.IsNullOrWhiteSpace(rawInput))
                return;

            string input = rawInput.Trim();

            AddCommandToOutput(input);

            string[] parts = input.Split(
                ' ',
                2,
                StringSplitOptions.RemoveEmptyEntries);

            string commandName =
                parts[0].ToLowerInvariant();

            string argument =
                parts.Length > 1
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
            outputLines.Add(
                $@"C:\Users\Guest>{input}");
        }

        // -------------------- CLS --------------------
        private void ExecuteClear()
        {
            outputLines.Clear();
        }

        // -------------------- COLOR --------------------
        private void ExecuteColor(string argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                ResetColors();
                return;
            }

            if (argument.Equals(
                "/?",
                StringComparison.OrdinalIgnoreCase))
            {
                AddColorHelp();
                return;
            }

            string attribute =
                argument.Trim().ToUpperInvariant();

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
            BackgroundColor =
                DefaultBackgroundColor;

            ForegroundColor =
                DefaultForegroundColor;
        }

        private bool IsValidColor(char value)
        {
            return
                (value >= '0' && value <= '9') ||
                (value >= 'A' && value <= 'F');
        }

        private void AddColorHelp()
        {
            outputLines.Add(
                "Sets the default console foreground and background colors.");

            outputLines.Add(string.Empty);

            outputLines.Add(
                "COLOR [attr]");

            outputLines.Add(string.Empty);

            outputLines.Add(
                "  attr        Specifies color attribute of console output");

            outputLines.Add(string.Empty);

            outputLines.Add(
                "Color attributes are specified by TWO hex digits -- the first");

            outputLines.Add(
                "corresponds to the background; the second the foreground. Each digit");

            outputLines.Add(
                "can be any of the following values:");

            outputLines.Add(string.Empty);

            outputLines.Add(
                "    0 = Black        8 = Gray");

            outputLines.Add(
                "    1 = Blue         9 = Light Blue");

            outputLines.Add(
                "    2 = Green        A = Light Green");

            outputLines.Add(
                "    3 = Aqua         B = Light Aqua");

            outputLines.Add(
                "    4 = Red          C = Light Red");

            outputLines.Add(
                "    5 = Purple       D = Light Purple");

            outputLines.Add(
                "    6 = Yellow       E = Light Yellow");

            outputLines.Add(
                "    7 = White        F = Bright White");

            outputLines.Add(string.Empty);

            outputLines.Add(
                "If no argument is given, this command restores the color to its default.");
        }

        // -------------------- HELP --------------------
        private void ExecuteHelp()
        {
            outputLines.Add(
                "For more information on a specific command, type HELP command-name.");

            outputLines.Add(string.Empty);

            outputLines.Add(
                "CLS        Clears the screen.");

            outputLines.Add(
                "COLOR      Sets the default console foreground and background colors.");

            outputLines.Add(
                "HELP       Provides Help information for Windows commands.");

            outputLines.Add(
                "VER        Displays the Windows version.");

            outputLines.Add(
                "WHOAMI     Displays the current user.");

            outputLines.Add(string.Empty);
        }

        // -------------------- VERSION --------------------
        private void ExecuteVersion()
        {
            outputLines.Add(
                "Microsoft Windows [Version 10.0.26100.6584]");
        }

        // -------------------- WHOAMI --------------------
        private void ExecuteWhoAmI()
        {
            outputLines.Add(
                @"atlas\guest");
        }

        // -------------------- AUTHORED COMMANDS --------------------
        private void ExecuteAuthoredCommand(string input)
        {
            CMDCommand command =
                commandLibrary?.FindCommand(input);

            if (command != null)
            {
                AddAuthoredResult(command);
                return;
            }

            AddUnknownCommand(input);
        }

        private void AddAuthoredResult(
            CMDCommand command)
        {
            if (string.IsNullOrWhiteSpace(
                command.Result))
            {
                return;
            }

            string[] lines =
                command.Result.Replace(
                    "\r\n",
                    "\n")
                .Split('\n');

            foreach (string line in lines)
            {
                outputLines.Add(line);
            }
        }

        // -------------------- ERRORS --------------------
        private void AddUnknownCommand(string input)
        {
            outputLines.Add(
                $"'{input}' is not recognized as an internal or external command,");

            outputLines.Add(
                "operable program or batch file.");
        }

        private void AddIncorrectSyntax()
        {
            outputLines.Add(
                "The syntax of the command is incorrect.");
        }
    }
}