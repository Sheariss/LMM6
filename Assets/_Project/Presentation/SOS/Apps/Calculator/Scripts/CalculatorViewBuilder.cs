namespace Atlas.Presentation.SOS.Calculator
{
    public sealed class CalculatorViewState
    {
        public string DisplayText { get; }

        public string ExpressionText { get; }

        public bool IsError { get; }

        public CalculatorViewState(
            string displayText,
            string expressionText,
            bool isError)
        {
            DisplayText = displayText;
            ExpressionText = expressionText;
            IsError = isError;
        }
    }


    public sealed class CalculatorViewBuilder
    {
        public CalculatorViewState Build(
            CalculatorEngine engine)
        {
            if (engine == null)
            {
                return new CalculatorViewState(
                    "0",
                    string.Empty,
                    false);
            }

            return new CalculatorViewState(
                displayText:
                    engine.CurrentInput,

                expressionText:
                    engine.ExpressionText,

                isError:
                    engine.HasError);
        }
    }
}