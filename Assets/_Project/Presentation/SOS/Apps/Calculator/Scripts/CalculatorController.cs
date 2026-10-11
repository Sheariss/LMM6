using Atlas.Presentation.SOS.Apps;
using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Calculator
{
    public sealed class CalculatorController : AppContentController
    {
        private CalculatorBinder binder;
        private CalculatorEngine engine;
        private CalculatorView view;
        private CalculatorViewBuilder viewBuilder;

        // -------------------- INITIALIZATION --------------------

        protected override void OnInitialize()
        {
            binder = new CalculatorBinder(InstanceRoot);

            if (!binder.IsValid)
            {
                throw new System.InvalidOperationException(
                    "Calculator UI is missing required elements.");
            }

            engine = new CalculatorEngine();
            view = new CalculatorView(binder);
            viewBuilder = new CalculatorViewBuilder();

            RegisterCallbacks();

            Refresh();
            view.FocusDisplay();
        }

        protected override void OnShutdown()
        {
            // The base class unregisters callbacks before this runs.
            binder = null;
            engine = null;
            view = null;
            viewBuilder = null;
        }

        // -------------------- CALLBACK REGISTRATION --------------------

        private void RegisterCallbacks()
        {
            RegisterNumberCallbacks();
            RegisterBinaryOperationCallbacks();
            RegisterUnaryOperationCallbacks();
            RegisterUtilityCallbacks();
            RegisterKeyboardCallbacks();
            RegisterDisplayCallbacks();
        }

        // -------------------- NUMBER BUTTONS --------------------

        private void RegisterNumberCallbacks()
        {
            BindClick(binder.ZeroButton, () => OnDigitPressed("0"));
            BindClick(binder.OneButton, () => OnDigitPressed("1"));
            BindClick(binder.TwoButton, () => OnDigitPressed("2"));
            BindClick(binder.ThreeButton, () => OnDigitPressed("3"));
            BindClick(binder.FourButton, () => OnDigitPressed("4"));
            BindClick(binder.FiveButton, () => OnDigitPressed("5"));
            BindClick(binder.SixButton, () => OnDigitPressed("6"));
            BindClick(binder.SevenButton, () => OnDigitPressed("7"));
            BindClick(binder.EightButton, () => OnDigitPressed("8"));
            BindClick(binder.NineButton, () => OnDigitPressed("9"));

            BindClick(binder.DecimalButton, OnDecimalPressed);
        }

        // -------------------- BINARY OPERATIONS --------------------

        private void RegisterBinaryOperationCallbacks()
        {
            BindClick(
                binder.AddButton,
                () => OnOperatorPressed(CalculatorOperation.Add));

            BindClick(
                binder.SubtractButton,
                () => OnOperatorPressed(CalculatorOperation.Subtract));

            BindClick(
                binder.MultiplyButton,
                () => OnOperatorPressed(CalculatorOperation.Multiply));

            BindClick(
                binder.DivideButton,
                () => OnOperatorPressed(CalculatorOperation.Divide));
        }

        // -------------------- UNARY OPERATIONS --------------------

        private void RegisterUnaryOperationCallbacks()
        {
            BindClick(
                binder.SquareButton,
                () => OnUnaryOperationPressed(CalculatorOperation.Squared));

            BindClick(
                binder.RootButton,
                () => OnUnaryOperationPressed(CalculatorOperation.Root));

            BindClick(
                binder.OneOverButton,
                () => OnUnaryOperationPressed(CalculatorOperation.OneOver));
        }

        // -------------------- UTILITY --------------------

        private void RegisterUtilityCallbacks()
        {
            BindClick(binder.EqualButton, OnEqualsPressed);
            BindClick(binder.ClearButton, OnClearPressed);
            BindClick(binder.ClearEntryButton, OnClearEntryPressed);
            BindClick(binder.SignButton, OnSignPressed);
            BindClick(binder.PercentButton, OnPercentPressed);
            BindClick(binder.BackspaceButton, OnBackspacePressed);
        }

        // -------------------- DISPLAY INPUT --------------------

        private void RegisterDisplayCallbacks()
        {
            BindCallback<ChangeEvent<string>>(
                binder.DisplayField,
                OnDisplayValueChanged);
        }

        // -------------------- KEYBOARD --------------------

        private void RegisterKeyboardCallbacks()
        {
            // Intercept keys before the TextField inserts them.
            BindCallback<KeyDownEvent>(
                binder.Root,
                OnKeyDown,
                TrickleDown.TrickleDown);
        }

        // -------------------- DIGIT --------------------

        private void OnDigitPressed(string digit)
        {
            engine.InputDigit(digit);
            Refresh();
        }

        // -------------------- DECIMAL --------------------

        private void OnDecimalPressed()
        {
            engine.InputDecimal();
            Refresh();
        }

        // -------------------- BINARY OPERATOR --------------------

        private void OnOperatorPressed(CalculatorOperation operation)
        {
            engine.InputOperator(operation);
            Refresh();
        }

        // -------------------- UNARY OPERATOR --------------------

        private void OnUnaryOperationPressed(CalculatorOperation operation)
        {
            engine.ApplyUnaryOperation(operation);
            Refresh();
        }

        // -------------------- EQUALS --------------------

        private void OnEqualsPressed()
        {
            engine.Equals();
            Refresh();
        }

        // -------------------- CLEAR --------------------

        private void OnClearPressed()
        {
            engine.Clear();
            Refresh();
        }

        // -------------------- CLEAR ENTRY --------------------

        private void OnClearEntryPressed()
        {
            engine.ClearEntry();
            Refresh();
        }

        // -------------------- SIGN --------------------

        private void OnSignPressed()
        {
            engine.ToggleSign();
            Refresh();
        }

        // -------------------- PERCENT --------------------

        private void OnPercentPressed()
        {
            engine.Percent();
            Refresh();
        }

        // -------------------- BACKSPACE --------------------

        private void OnBackspacePressed()
        {
            engine.Backspace();
            Refresh();
        }

        // -------------------- DIRECT TEXTFIELD INPUT --------------------

        private void OnDisplayValueChanged(ChangeEvent<string> evt)
        {
            engine.TrySetCurrentInput(evt.newValue);

            // Normalize accepted input or restore the last valid value.
            Refresh();
        }

        // -------------------- KEYBOARD INPUT --------------------

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Preserve shortcuts such as Ctrl+C and Ctrl+V.
            if (evt.ctrlKey || evt.commandKey)
                return;

            char character = evt.character;

            if (character >= '0' && character <= '9')
            {
                OnDigitPressed(character.ToString());
                ConsumeKeyEvent(evt);
                return;
            }

            switch (character)
            {
                case '.':
                case ',':
                    OnDecimalPressed();
                    ConsumeKeyEvent(evt);
                    return;

                case '+':
                    OnOperatorPressed(CalculatorOperation.Add);
                    ConsumeKeyEvent(evt);
                    return;

                case '-':
                    OnOperatorPressed(CalculatorOperation.Subtract);
                    ConsumeKeyEvent(evt);
                    return;

                case '*':
                    OnOperatorPressed(CalculatorOperation.Multiply);
                    ConsumeKeyEvent(evt);
                    return;

                case '/':
                    OnOperatorPressed(CalculatorOperation.Divide);
                    ConsumeKeyEvent(evt);
                    return;

                case '=':
                    OnEqualsPressed();
                    ConsumeKeyEvent(evt);
                    return;
            }

            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                    OnEqualsPressed();
                    ConsumeKeyEvent(evt);
                    return;

                case KeyCode.Escape:
                    OnClearPressed();
                    ConsumeKeyEvent(evt);
                    return;

                case KeyCode.Delete:
                    OnClearEntryPressed();
                    ConsumeKeyEvent(evt);
                    return;
            }
        }

        // -------------------- KEY EVENT UTILITY --------------------

        private static void ConsumeKeyEvent(KeyDownEvent evt)
        {
            // Prevent the TextField from also processing calculator keys.
            evt.StopPropagation();
        }

        // -------------------- REFRESH --------------------

        private void Refresh()
        {
            CalculatorViewState state = viewBuilder.Build(engine);
            view.Render(state);
        }
    }
}