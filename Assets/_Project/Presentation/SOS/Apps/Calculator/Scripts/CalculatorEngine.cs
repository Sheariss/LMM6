using System;
using System.Globalization;
namespace Atlas.Presentation.SOS.Calculator
{
    public enum CalculatorOperation
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide,
        Squared,
        Root,
        OneOver
    }
    public sealed class CalculatorEngine
    {
        private const int MaxInputLength = 16;
        public string CurrentInput { get; private set; } = "0";
        public string ExpressionText { get; private set; } =
            string.Empty;
        public double Accumulator { get; private set; }
        public CalculatorOperation PendingOperation
        {
            get;
            private set;
        } = CalculatorOperation.None;
        public bool WaitingForOperand { get; private set; }
        public bool JustCalculated { get; private set; }
        public bool HasError { get; private set; }
        // Used for repeated "=" behavior.
        //
        // Example:
        //
        // 2 + 2 =
        // → 4
        //
        // =
        // → 6
        //
        // =
        // → 8
        private CalculatorOperation lastOperation =
            CalculatorOperation.None;
        private double lastOperand;
        private bool hasRepeatOperation;

        // -------------------- DIGIT INPUT --------------------
        public void InputDigit(string digit)
        {
            if (string.IsNullOrEmpty(digit))
                return;
            if (HasError)
                Clear();
            if (JustCalculated &&
                PendingOperation == CalculatorOperation.None)
            {
                StartNewCalculation();
            }
            if (WaitingForOperand)
            {
                CurrentInput = digit;
                WaitingForOperand = false;
                JustCalculated = false;
                return;
            }
            if (CurrentInput.Length >= MaxInputLength)
                return;
            if (CurrentInput == "0")
            {
                CurrentInput = digit;
            }
            else
            {
                CurrentInput += digit;
            }
            JustCalculated = false;
        }

        // -------------------- DIRECT TEXT INPUT --------------------
        public bool TrySetCurrentInput(string input)
        {
            if (input == null)
                return false;
            if (HasError)
                Clear();
            input = input.Trim();
            if (input.Length == 0)
            {
                CurrentInput = "0";
                return true;
            }
            input = input.Replace(',', '.');
            if (input.Length > MaxInputLength)
                return false;
            if (input == ".")
                input = "0.";
            if (input == "-.")
                input = "-0.";
            if (!IsValidNumberInput(input))
                return false;
            if (JustCalculated &&
                PendingOperation == CalculatorOperation.None)
            {
                StartNewCalculation();
            }
            CurrentInput = input;
            WaitingForOperand = false;
            JustCalculated = false;
            return true;
        }

        // -------------------- DECIMAL --------------------
        public void InputDecimal()
        {
            if (HasError)
                Clear();
            if (JustCalculated &&
                PendingOperation == CalculatorOperation.None)
            {
                StartNewCalculation();
            }
            if (WaitingForOperand)
            {
                CurrentInput = "0.";
                WaitingForOperand = false;
                JustCalculated = false;
                return;
            }
            if (CurrentInput.Contains("."))
                return;
            if (CurrentInput.Length >= MaxInputLength)
                return;
            CurrentInput += ".";
        }

        // -------------------- BINARY OPERATORS --------------------
        public void InputOperator(
            CalculatorOperation operation)
        {
            if (!IsBinaryOperation(operation))
                return;
            if (HasError)
                return;
            double value = GetCurrentValue();
            /*
             * If another operation is already pending
             * and the user has entered another operand,
             * calculate it immediately.
             *
             * Example:
             *
             * 2 + 2 +
             *
             * becomes:
             *
             * 4
             *
             * with another + waiting.
             */
            if (PendingOperation != CalculatorOperation.None &&
                !WaitingForOperand)
            {
                CalculatorOperation previousOperation =
                    PendingOperation;
                if (!TryCalculate(
                    Accumulator,
                    value,
                    previousOperation,
                    out double result))
                {
                    SetError();
                    return;
                }
                ExpressionText =
                    $"{ExpressionText} " +
                    $"{FormatNumber(value)} " +
                    $"{GetOperatorSymbol(operation)}";
                Accumulator = result;
                CurrentInput =
                    FormatNumber(result);
            }
            /*
             * No operation was pending.
             */
            else if (PendingOperation ==
                     CalculatorOperation.None)
            {
                Accumulator = value;
                ExpressionText =
                    $"{FormatNumber(value)} " +
                    $"{GetOperatorSymbol(operation)}";
            }
            /*
             * User pressed another operator without entering
             * another number.
             *
             * Example:
             *
             * 5 +
             *
             * followed by:
             *
             * ×
             *
             * becomes:
             *
             * 5 ×
             */
            else if (WaitingForOperand)
            {
                ExpressionText =
                    ReplaceTrailingOperator(
                        ExpressionText,
                        GetOperatorSymbol(operation));
            }
            PendingOperation = operation;
            WaitingForOperand = true;
            JustCalculated = false;
            hasRepeatOperation = false;
        }
        // -------------------- EQUALS --------------------
        public void Equals()
        {
            if (HasError)
                return;
            /*
             * Normal pending operation.
             */
            if (PendingOperation != CalculatorOperation.None)
            {
                double rightOperand =
                    WaitingForOperand
                        ? Accumulator
                        : GetCurrentValue();
                if (!TryCalculate(
                    Accumulator,
                    rightOperand,
                    PendingOperation,
                    out double result))
                {
                    SetError();
                    return;
                }
                ExpressionText =
                    $"{ExpressionText} " +
                    $"{FormatNumber(rightOperand)} =";
                lastOperation = PendingOperation;
                lastOperand = rightOperand;
                hasRepeatOperation = true;
                Accumulator = result;
                CurrentInput = FormatNumber(result);
                PendingOperation =
                    CalculatorOperation.None;
                WaitingForOperand = false;
                JustCalculated = true;
                return;
            }
            /*
             * Repeated equals.
             *
             * Example:
             *
             * 2 + 2 = 4
             *
             * press =
             *
             * 4 + 2 = 6
             */
            if (!hasRepeatOperation ||
                lastOperation == CalculatorOperation.None)
            {
                return;
            }
            double leftOperand = GetCurrentValue();
            if (!TryCalculate(
                leftOperand,
                lastOperand,
                lastOperation,
                out double repeatedResult))
            {
                SetError();
                return;
            }
            ExpressionText =
                $"{FormatNumber(leftOperand)} " +
                $"{GetOperatorSymbol(lastOperation)} " +
                $"{FormatNumber(lastOperand)} =";
            CurrentInput =
                FormatNumber(repeatedResult);
            Accumulator = repeatedResult;
            JustCalculated = true;
        }

        // -------------------- UNARY OPERATIONS --------------------
        public void ApplyUnaryOperation(
            CalculatorOperation operation)
        {
            if (HasError)
                return;
            double value = GetCurrentValue();
            double result;
            string unaryExpression;
            switch (operation)
            {
                case CalculatorOperation.Squared:
                    result = value * value;
                    unaryExpression =
                        $"sqr({FormatNumber(value)})";
                    break;
                case CalculatorOperation.Root:
                    if (value < 0)
                    {
                        SetError();
                        return;
                    }
                    result = Math.Sqrt(value);
                    unaryExpression =
                        $"√({FormatNumber(value)})";
                    break;
                case CalculatorOperation.OneOver:
                    if (value == 0d)
                    {
                        SetError();
                        return;
                    }
                    result = 1d / value;
                    unaryExpression =
                        $"1/({FormatNumber(value)})";
                    break;
                default:
                    return;
            }
            /*
             * If there is a pending operation, append the
             * unary expression to the history.
             *
             * Example:
             *
             * 5 + √(9)
             */
            if (PendingOperation != CalculatorOperation.None)
            {
                if (WaitingForOperand)
                {
                    ExpressionText +=
                        $" {unaryExpression}";
                }
                else
                {
                    ExpressionText =
                        $"{ExpressionText} " +
                        $"{unaryExpression}";
                }
            }
            else
            {
                ExpressionText = unaryExpression;
            }
            CurrentInput =
                FormatNumber(result);
            WaitingForOperand = false;
            JustCalculated = false;
        }

        // -------------------- PERCENT --------------------
        public void Percent()
        {
            if (HasError)
                return;
            double value = GetCurrentValue();
            double result;
            switch (PendingOperation)
            {
                /*
                 * Windows-style behavior:
                 *
                 * 100 + 10%
                 *
                 * 10% becomes 10.
                 */
                case CalculatorOperation.Add:
                case CalculatorOperation.Subtract:
                    result =
                        Accumulator *
                        value /
                        100d;
                    break;
                /*
                 * 100 × 10%
                 *
                 * 10% becomes 0.1.
                 */
                case CalculatorOperation.Multiply:
                case CalculatorOperation.Divide:
                    result =
                        value /
                        100d;
                    break;
                default:
                    result =
                        value /
                        100d;
                    break;
            }
            CurrentInput =
                FormatNumber(result);
            WaitingForOperand = false;
            JustCalculated = false;
        }
        // ============================================================
        // SIGN
        // ============================================================
        public void ToggleSign()
        {
            if (HasError)
                return;
            if (CurrentInput == "0")
                return;
            if (CurrentInput.StartsWith("-"))
            {
                CurrentInput =
                    CurrentInput.Substring(1);
            }
            else
            {
                CurrentInput =
                    "-" + CurrentInput;
            }
            WaitingForOperand = false;
        }
        // ============================================================
        // BACKSPACE
        // ============================================================
        public void Backspace()
        {
            if (HasError ||
                WaitingForOperand ||
                JustCalculated)
            {
                return;
            }
            if (CurrentInput.Length <= 1)
            {
                CurrentInput = "0";
                return;
            }
            CurrentInput =
                CurrentInput.Substring(
                    0,
                    CurrentInput.Length - 1);
            if (CurrentInput == "-" ||
                CurrentInput.Length == 0)
            {
                CurrentInput = "0";
            }
        }
        // ============================================================
        // CLEAR ENTRY
        // ============================================================
        public void ClearEntry()
        {
            if (HasError)
            {
                Clear();
                return;
            }
            CurrentInput = "0";
            WaitingForOperand = false;
            JustCalculated = false;
        }
        // ============================================================
        // CLEAR ALL
        // ============================================================
        public void Clear()
        {
            CurrentInput = "0";
            ExpressionText =
                string.Empty;
            Accumulator = 0d;
            PendingOperation =
                CalculatorOperation.None;
            WaitingForOperand = false;
            JustCalculated = false;
            HasError = false;
            lastOperation =
                CalculatorOperation.None;
            lastOperand = 0d;
            hasRepeatOperation = false;
        }
        // ============================================================
        // CALCULATION
        // ============================================================
        private static bool TryCalculate(
            double left,
            double right,
            CalculatorOperation operation,
            out double result)
        {
            result = 0d;
            switch (operation)
            {
                case CalculatorOperation.Add:
                    result =
                        left + right;
                    return true;
                case CalculatorOperation.Subtract:
                    result =
                        left - right;
                    return true;
                case CalculatorOperation.Multiply:
                    result =
                        left * right;
                    return true;
                case CalculatorOperation.Divide:
                    if (right == 0d)
                        return false;
                    result =
                        left / right;
                    return true;
                default:
                    return false;
            }
        }
        // ============================================================
        // CURRENT VALUE
        // ============================================================
        private double GetCurrentValue()
        {
            string value =
                CurrentInput.EndsWith(".")
                    ? CurrentInput.TrimEnd('.')
                    : CurrentInput;
            if (double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double result))
            {
                return result;
            }
            return 0d;
        }
        // ============================================================
        // INPUT VALIDATION
        // ============================================================
        private static bool IsValidNumberInput(
            string value)
        {
            if (value == "-")
                return true;
            if (value.EndsWith("."))
            {
                string withoutPeriod =
                    value.Substring(
                        0,
                        value.Length - 1);
                if (withoutPeriod == "" ||
                    withoutPeriod == "-")
                {
                    return true;
                }
                return double.TryParse(
                    withoutPeriod,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out _);
            }
            return double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _);
        }
        // ============================================================
        // HELPERS
        // ============================================================
        private void StartNewCalculation()
        {
            CurrentInput = "0";
            ExpressionText =
                string.Empty;
            Accumulator = 0d;
            PendingOperation =
                CalculatorOperation.None;
            WaitingForOperand = false;
            JustCalculated = false;
            hasRepeatOperation = false;
        }
        private void SetError()
        {
            CurrentInput = "Error";
            ExpressionText =
                string.Empty;
            Accumulator = 0d;
            PendingOperation =
                CalculatorOperation.None;
            WaitingForOperand = true;
            JustCalculated = true;
            HasError = true;
            hasRepeatOperation = false;
        }
        private static bool IsBinaryOperation(
            CalculatorOperation operation)
        {
            return operation ==
                       CalculatorOperation.Add ||
                   operation ==
                       CalculatorOperation.Subtract ||
                   operation ==
                       CalculatorOperation.Multiply ||
                   operation ==
                       CalculatorOperation.Divide;
        }
        private static string GetOperatorSymbol(
            CalculatorOperation operation)
        {
            return operation switch
            {
                CalculatorOperation.Add => "+",
                CalculatorOperation.Subtract => "−",
                CalculatorOperation.Multiply => "×",
                CalculatorOperation.Divide => "÷",
                _ => string.Empty
            };
        }
        private static string ReplaceTrailingOperator(
            string expression,
            string newOperator)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return newOperator;
            int lastSpace =
                expression.LastIndexOf(' ');
            if (lastSpace < 0)
                return newOperator;
            return
                expression.Substring(
                    0,
                    lastSpace + 1)
                + newOperator;
        }
        private static string FormatNumber(
            double value)
        {
            if (double.IsNaN(value) ||
                double.IsInfinity(value))
            {
                return "Error";
            }
            return value.ToString(
                "0.###############",
                CultureInfo.InvariantCulture);
        }
    }
}
