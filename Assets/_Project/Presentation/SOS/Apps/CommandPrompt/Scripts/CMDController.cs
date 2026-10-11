using System;
using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.CommandPrompt
{
    public sealed class CMDController : AppContentController
    {
        [Header("Commands")]
        [SerializeField] private CMDCommandLibrary commandLibrary;

        private CMDBinder binder;
        private CMDView view;
        private CMDViewBuilder viewBuilder;
        private CMDEngine engine;

        // -------------------- INITIALIZATION --------------------

        protected override void OnInitialize()
        {
            if (commandLibrary == null)
            {
                throw new InvalidOperationException(
                    "CMDController requires a CMDCommandLibrary.");
            }

            binder = new CMDBinder(InstanceRoot);

            if (binder.CommandInput == null)
            {
                throw new InvalidOperationException(
                    "Command Prompt UI is missing its command input.");
            }

            engine = new CMDEngine(commandLibrary);
            viewBuilder = new CMDViewBuilder();
            view = new CMDView(binder);

            RegisterCallbacks();
            InitializeTerminal();
        }

        protected override void OnShutdown()
        {
            // The base class unregisters callbacks before this runs.
            binder = null;
            view = null;
            viewBuilder = null;
            engine = null;
        }

        private void InitializeTerminal()
        {
            Render();
            view.FocusInput();
        }

        // -------------------- CALLBACKS --------------------

        private void RegisterCallbacks()
        {
            // Intercept Enter before the TextField handles it.
            BindCallback<KeyDownEvent>(
                binder.CommandInput,
                OnInputKeyDown,
                TrickleDown.TrickleDown);
        }

        // -------------------- INPUT --------------------

        private void OnInputKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return &&
                evt.keyCode != KeyCode.KeypadEnter)
            {
                return;
            }

            evt.StopPropagation();

            SubmitCommand();
        }

        private void SubmitCommand()
        {
            string input = binder.CommandInput.value;

            if (string.IsNullOrWhiteSpace(input))
            {
                view.ClearInput();
                view.FocusInput();
                return;
            }

            engine.Execute(input);

            Render();

            view.ClearInput();
            view.FocusInput();
        }

        // -------------------- RENDER --------------------

        private void Render()
        {
            CMDViewState state = viewBuilder.Build(engine);
            view.Render(state);
        }
    }
}