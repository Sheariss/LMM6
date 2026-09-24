using UnityEngine;
using UnityEngine.UIElements;

namespace Atlas.Presentation.SOS.Calculator
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class CalculatorController : MonoBehaviour
    {
        private UIDocument uiDocument;

        private CalculatorBinder binder;

        private CalculatorEngine engine;

        private CalculatorView view;

        private CalculatorViewBuilder viewBuilder;


        // -------------------- INITIALIZATION --------------------
        private void Start()
        {
            uiDocument = GetComponent<UIDocument>();

            binder = new CalculatorBinder(uiDocument.rootVisualElement);
            engine = new CalculatorEngine();
            view = new CalculatorView(binder);
            viewBuilder = new CalculatorViewBuilder();

            RegisterCallbacks();

            Refresh();

            view.FocusDisplay();
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
            binder.ZeroButton.clicked += () => OnDigitPressed("0");

            binder.OneButton.clicked += () => OnDigitPressed("1");

            binder.TwoButton.clicked += () => OnDigitPressed("2");

            binder.ThreeButton.clicked += () => OnDigitPressed("3");

            binder.FourButton.clicked += () => OnDigitPressed("4");

            binder.FiveButton.clicked += () => OnDigitPressed("5");

            binder.SixButton.clicked += () => OnDigitPressed("6");

            binder.SevenButton.clicked += () => OnDigitPressed("7");

            binder.EightButton.clicked += () => OnDigitPressed("8");

            binder.NineButton.clicked += () => OnDigitPressed("9");

            binder.DecimalButton.clicked += OnDecimalPressed;
        }



        // -------------------- BINARY OPERATIONS --------------------
        private void RegisterBinaryOperationCallbacks()
        {
            binder.AddButton.clicked += () => OnOperatorPressed(CalculatorOperation.Add);
            binder.SubtractButton.clicked += () => OnOperatorPressed(CalculatorOperation.Subtract);
            binder.MultiplyButton.clicked += () => OnOperatorPressed(CalculatorOperation.Multiply);
            binder.DivideButton.clicked += () => OnOperatorPressed(CalculatorOperation.Divide);
        }



        // -------------------- UNARY OPERATIONS --------------------


        private void RegisterUnaryOperationCallbacks()
        {
            binder.SquareButton.clicked +=
                () => OnUnaryOperationPressed(
                    CalculatorOperation.Squared);

            binder.RootButton.clicked +=
                () => OnUnaryOperationPressed(
                    CalculatorOperation.Root);

            binder.OneOverButton.clicked +=
                () => OnUnaryOperationPressed(
                    CalculatorOperation.OneOver);
        }



        // -------------------- UTILITY --------------------


        private void RegisterUtilityCallbacks()
        {
            binder.EqualButton.clicked +=
                OnEqualsPressed;

            binder.ClearButton.clicked +=
                OnClearPressed;

            binder.ClearEntryButton.clicked +=
                OnClearEntryPressed;

            binder.SignButton.clicked +=
                OnSignPressed;

            binder.PercentButton.clicked +=
                OnPercentPressed;

            binder.BackspaceButton.clicked +=
                OnBackspacePressed;
        }


        // -------------------- DISPLAY INPUT --------------------


        private void RegisterDisplayCallbacks()
        {
            binder.DisplayField
                .RegisterValueChangedCallback(
                    OnDisplayValueChanged);
        }



        // -------------------- KEYBOARD --------------------


        private void RegisterKeyboardCallbacks()
        {
            /*
             * TrickleDown lets us intercept keys before
             * the TextField processes them normally.
             *
             * This is particularly useful for:
             *
             * +
             * -
             * *
             * /
             * Enter
             *
             * We don't want those characters inserted into
             * the numeric display.
             */

            binder.Root.RegisterCallback<KeyDownEvent>(
                OnKeyDown,
                TrickleDown.TrickleDown);
        }



        // -------------------- DIGIT --------------------


        private void OnDigitPressed(
            string digit)
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


        private void OnOperatorPressed(
            CalculatorOperation operation)
        {
            engine.InputOperator(
                operation);

            Refresh();
        }



        // -------------------- UNARY OPERATOR --------------------


        private void OnUnaryOperationPressed(
            CalculatorOperation operation)
        {
            engine.ApplyUnaryOperation(
                operation);

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


        private void OnDisplayValueChanged(
            ChangeEvent<string> evt)
        {
            bool accepted =
                engine.TrySetCurrentInput(
                    evt.newValue);

            /*
             * Refresh either way.
             *
             * If the input was valid, this normalizes it.
             *
             * If it was invalid, this restores the last valid
             * calculator value.
             */

            Refresh();
        }


        // -------------------- KEYBOARD INPUT --------------------


        private void OnKeyDown(
            KeyDownEvent evt)
        {
            /*
             * Don't interfere with Ctrl+C, Ctrl+V, etc.
             */

            if (evt.ctrlKey ||
                evt.commandKey)
            {
                return;
            }


            char character =
                evt.character;



            // -------------------- DIGITS --------------------


            if (character >= '0' &&
                character <= '9')
            {
                engine.InputDigit(
                    character.ToString());

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }



            //  -------------------- DECIMAL --------------------


            if (character == '.' ||
                character == ',')
            {
                engine.InputDecimal();

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }



            // -------------------- ADD --------------------


            if (character == '+')
            {
                engine.InputOperator(
                    CalculatorOperation.Add);

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }



            // -------------------- SUBTRACT --------------------


            if (character == '-')
            {
                engine.InputOperator(
                    CalculatorOperation.Subtract);

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }



            // -------------------- MULTIPLY --------------------


            if (character == '*')
            {
                engine.InputOperator(
                    CalculatorOperation.Multiply);

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }



            // -------------------- DIVIDE --------------------


            if (character == '/')
            {
                engine.InputOperator(
                    CalculatorOperation.Divide);

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }



            // -------------------- EQUALS --------------------


            if (character == '=')
            {
                engine.Equals();

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }

            // -------------------- ENTER --------------------


            if (evt.keyCode == KeyCode.Return ||
                evt.keyCode == KeyCode.KeypadEnter)
            {
                engine.Equals();

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }


            // -------------------- ESCAPE = CLEAR ALL --------------------

            if (evt.keyCode == KeyCode.Escape)
            {
                engine.Clear();

                Refresh();

                ConsumeKeyEvent(evt);

                return;
            }


            // -------------------- DELETE = CLEAR ENTRY --------------------
            if (evt.keyCode == KeyCode.Delete)
            {
                engine.ClearEntry();

                Refresh();

                ConsumeKeyEvent(evt);
            }
        }


        // -------------------- KEY EVENT UTILITY --------------------
        private static void ConsumeKeyEvent(
            KeyDownEvent evt)
        {
            evt.PreventDefault();
            evt.StopPropagation();
        }


        // -------------------- REFRESH --------------------
        private void Refresh()
        {
            CalculatorViewState state =
                viewBuilder.Build(engine);

            view.Render(state);
        }
    }
}