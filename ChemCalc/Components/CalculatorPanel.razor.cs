using System;
using Microsoft.AspNetCore.Components;
using ChemCalc.MathLibrary;

namespace ChemCalc.Components
{
    public partial class CalculatorPanel : ComponentBase
    {

        [Parameter]
        public bool IsChemMode { get; set; }

        [Parameter]
        public EventCallback<string> OnChemOperator { get; set; }
        /// <summary>
        /// Main button press handler
        /// </summary>
        protected async Task Press(string key)
        {
            if (string.IsNullOrEmpty(key))
                return;

            try
            {
                switch (key)
                {
                    // ==================== CLEAR & DELETE ====================
                    case "AC":
                        State.Clear();
                        break;

                    case "DEL":
                        State.DeleteLast();
                        break;

                    // ==================== NUMBERS & DECIMAL ====================
                    case "0":
                    case "1":
                    case "2":
                    case "3":
                    case "4":
                    case "5":
                    case "6":
                    case "7":
                    case "8":
                    case "9":
                    case ".":
                        State.AppendToExpression(key);
                        break;

                    // ==================== BASIC OPERATORS ====================
                    case "+":
                        // Check if in CHEM mode
                        if (IsChemMode)
                        {
                            // In CHEM mode, + adds to equation
                            await OnChemOperator.InvokeAsync("+");
                        }
                        else
                        {
                            // In CALC mode, normal math operation
                            State.AppendToExpression(key);
                        }
                        break;

                    case "-":
                    case "*":
                    case "/":
                    case "^":
                    case "%":
                        State.AppendToExpression(key);
                        break;

                    // ==================== PARENTHESES ====================
                    case "(":
                    case ")":
                        State.AppendToExpression(key);
                        break;

                    // ==================== EQUALS ====================
                    // ==================== EQUALS ====================
                    case "=":
                        // Check if in CHEM mode
                        if (IsChemMode)
                        {
                            // In CHEM mode, = processes the chemical reaction
                            await OnChemOperator.InvokeAsync("=");
                        }
                        else
                        {
                            // In CALC mode, calculate math result
                            CalculateResult();
                        }
                        break;

                    // ==================== CONSTANTS ====================
                    case "π":
                        State.AppendToExpression(Math.PI.ToString());
                        break;

                    case "e":
                        State.AppendToExpression(Math.E.ToString());
                        break;

                    case "Ans":
                        State.AppendToExpression(State.LastAnswer.ToString());
                        break;

                    // ==================== TRIGONOMETRIC FUNCTIONS ====================
                    case "sin":
                    case "cos":
                    case "tan":
                    case "asin":
                    case "acos":
                    case "atan":
                        State.AppendToExpression(key + "(");
                        break;

                    // ==================== POWER & ROOT FUNCTIONS ====================
                    case "x²":
                        ApplyUnaryFunction(x => MathFunctions.Square(x));
                        break;

                    case "x³":
                        ApplyUnaryFunction(x => MathFunctions.Cube(x));
                        break;

                    case "√":
                        ApplyUnaryFunction(x => MathFunctions.Sqrt(x));
                        break;

                    case "∛":
                        ApplyUnaryFunction(x => MathFunctions.CubeRoot(x));
                        break;

                    case "1/x":
                        ApplyUnaryFunction(x => MathFunctions.Reciprocal(x));
                        break;

                    // ==================== EXPONENTIAL FUNCTIONS ====================
                    case "10ˣ":
                        ApplyUnaryFunction(x => MathFunctions.PowerOf10(x));
                        break;

                    case "eˣ":
                        ApplyUnaryFunction(x => MathFunctions.PowerOfE(x));
                        break;

                    // ==================== LOGARITHMIC FUNCTIONS ====================
                    case "log":
                        ApplyUnaryFunction(x => MathFunctions.Log(x));
                        break;

                    case "ln":
                        ApplyUnaryFunction(x => MathFunctions.Ln(x));
                        break;

                    // ==================== OTHER FUNCTIONS ====================
                    case "|x|":
                        ApplyUnaryFunction(x => Math.Abs(x));
                        break;

                    case "n!":
                        ApplyUnaryFunction(x => MathFunctions.Factorial((int)x));
                        break;

                    case "+/-":
                        ApplyUnaryFunction(x => MathFunctions.Negate(x));
                        break;

                    // ==================== COMBINATORICS ====================
                    case "nCr":
                        State.AppendToExpression("C");
                        break;

                    case "nPr":
                        State.AppendToExpression("P");
                        break;

                    // ==================== RANDOM & ROUND ====================
                    case "Ran":
                        State.SetDisplay(MathFunctions.Random());
                        break;

                    case "Rnd":
                        ApplyUnaryFunction(x => MathFunctions.Round(x));
                        break;

                    // ==================== SCIENTIFIC NOTATION ====================
                    case "EXP":
                        State.AppendToExpression("E");
                        break;

                    // ==================== MEMORY OPERATIONS ====================
                    case "MC":
                        State.MemoryClear();
                        break;

                    case "MR":
                        State.AppendToExpression(State.MemoryRecall().ToString());
                        break;

                    case "MS":
                        State.MemoryStore(State.GetCurrentValue());
                        break;

                    case "M+":
                        State.MemoryAdd(State.GetCurrentValue());
                        break;

                    case "M-":
                        State.MemorySubtract(State.GetCurrentValue());
                        break;

                    // ==================== ANGLE MODE ====================
                    case "DEG":
                        if (State.AngleMode != AngleMode.Degrees)
                        {
                            State.AngleMode = AngleMode.Degrees;
                        }
                        break;

                    case "RAD":
                        if (State.AngleMode != AngleMode.Radians)
                        {
                            State.AngleMode = AngleMode.Radians;
                        }
                        break;

                    default:
                        // Empty buttons or unknown - do nothing
                        break;
                }
            }
            catch (Exception ex)
            {
                State.SetError(ex.Message);
            }

            StateHasChanged();
        }

        /// <summary>
        /// Calculate and display result
        /// </summary>
        private void CalculateResult()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(State.CurrentExpression))
                    return;

                double result = Parser.Evaluate(State.CurrentExpression);
                State.SetDisplay(result);
            }
            catch (Exception ex)
            {
                State.SetError(ex.Message.Contains("Division") ? "Div by 0" : "Error");
            }
        }

        /// <summary>
        /// Apply unary function to current display value
        /// </summary>
        private void ApplyUnaryFunction(Func<double, double> function)
        {
            try
            {
                double currentValue = State.GetCurrentValue();
                double result = function(currentValue);
                State.SetDisplay(result);
            }
            catch (Exception ex)
            {
                State.SetError(ex.Message);
            }
        }

        /// <summary>
        /// Get display label for button (handle special characters)
        /// </summary>
        protected string GetButtonLabel(string btn)
        {
            switch (btn)
            {
                case "x²":
                    return "x²";
                case "x³":
                    return "x³";
                case "√":
                    return "√";
                case "∛":
                    return "∛";
                case "10ˣ":
                    return "10ˣ";
                case "eˣ":
                    return "eˣ";
                case "1/x":
                    return "1/x";
                case "|x|":
                    return "|x|";
                case "n!":
                    return "n!";
                case "+/-":
                    return "±";
                case "π":
                    return "π";
                default:
                    return btn;
            }
        }
    }
}