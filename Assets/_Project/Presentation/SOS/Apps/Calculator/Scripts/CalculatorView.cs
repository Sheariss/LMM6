namespace Atlas.Presentation.SOS.Calculator
{
    public sealed class CalculatorView
    {
        private readonly CalculatorBinder binder;

        private const string ErrorClass = "calculator-display--error";


        public CalculatorView(CalculatorBinder binder)
        {
            this.binder = binder;
        }

        // -------------------- RENDER --------------------
        public void Render(
            CalculatorViewState state)
        {
            if (state == null)
                return;

            SetDisplay(state.DisplayText);

            SetExpression(state.ExpressionText);

            SetErrorState(state.IsError);
        }

        // -------------------- DISPLAY --------------------
        private void SetDisplay(string value)
        {
            if (binder.DisplayField == null)
                return;

            /*
             * This is important.
             *
             * The TextField has a value-changed callback in the
             * Controller.
             *
             * If we used:
             *
             * CalculatorDisplay.value = value;
             *
             * rendering the UI would trigger another input event.
             *
             * SetValueWithoutNotify prevents that loop.
             */

            binder.DisplayField.SetValueWithoutNotify(value ?? "0");
        }

        // -------------------- EXPRESSION --------------------
        private void SetExpression(string expression)
        {
            if (binder.ExpressionLabel == null)
                return;

            binder.ExpressionLabel.text = expression ?? string.Empty;
        }

        // -------------------- ERROR VISUAL --------------------
        private void SetErrorState(bool isError)
        {
            if (binder.DisplayField == null)
                return;

            binder.DisplayField.EnableInClassList(ErrorClass, isError);
        }

        // -------------------- FOCUS --------------------
        public void FocusDisplay()
        {
            binder.DisplayField?.Focus();
        }
    }
}