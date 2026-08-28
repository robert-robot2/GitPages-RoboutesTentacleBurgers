using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using CalculonLibrary.MathLibrary;
using CalculonLibrary.Helpers;

namespace CalculonLibrary
{
    public partial class Calculon2 : ComponentBase
    {
     
        // Calculator state and engine
        protected CalculatorState State = new();
        protected ExpressionParser Parser;
        protected ThemeManager ThemeManager = new();
        protected LocalStorageHelper LocalStorage;

        // Button layout: 6 columns × 9 rows = 54 buttons
        protected string[] Buttons = new[]
        {
            // Row 1
            "1", "2", "3", "+", "(", ")",
            // Row 2
            "4", "5", "6", "-", ".", "^",
            // Row 3
            "7", "8", "9", "*", "/", "+/-",
            // Row 4
            "AC", "0", "DEL", "=", "√", "%",
            // Row 5
            "sin", "cos", "tan", "asin", "acos", "atan",
            // Row 6
            "x²", "x³", "∛", "1/x", "10ˣ", "eˣ",
            // Row 7
            "log", "ln", "|x|", "n!", "nCr", "nPr",
            // Row 8
            "π", "e", "Ans", "Ran", "Rnd", "EXP",
            // Row 9
            "MC", "MR", "MS", "M+", "M-", "DEG/RAD"
        };

        protected override async Task OnInitializedAsync()
        {
            Parser = new ExpressionParser(State);
            LocalStorage = new LocalStorageHelper(JSRuntime);

            // Load saved preferences
            await LoadPreferences();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ApplyCurrentTheme();
            }
        }

        /// <summary>
        /// Main button press handler
        /// </summary>
        protected void Press(string key)
        {
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
                    case "=":
                        CalculateResult();
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
                        State.AppendToExpression("C"); // Placeholder - will need special handling
                        break;

                    case "nPr":
                        State.AppendToExpression("P"); // Placeholder - will need special handling
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
                    case "DEG/RAD":
                        State.ToggleAngleMode();
                        _ = SaveAngleMode(); // Save preference
                        break;

                    default:
                        State.SetError($"Unknown: {key}");
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
            return btn switch
            {
                "x²" => "x²",
                "x³" => "x³",
                "√" => "√",
                "∛" => "∛",
                "10ˣ" => "10ˣ",
                "eˣ" => "eˣ",
                "1/x" => "1/x",
                "|x|" => "|x|",
                "n!" => "n!",
                "+/-" => "±",
                "π" => "π",
                _ => btn
            };
        }

        // ==================== THEME MANAGEMENT ====================

        protected async Task CycleTheme()
        {
            var themes = ThemeManager.GetThemeNames();
            int currentIndex = themes.IndexOf(ThemeManager.CurrentTheme);
            int nextIndex = (currentIndex + 1) % themes.Count;
            ThemeManager.SetTheme(themes[nextIndex]);

            await ApplyCurrentTheme();
            await LocalStorage.SaveThemeAsync(ThemeManager.CurrentTheme);
        }

        private async Task ApplyCurrentTheme()
        {
            var theme = ThemeManager.GetCurrentTheme();
            await JSRuntime.InvokeVoidAsync("calculonInterop.applyTheme",
                theme.PrimaryColor,
                theme.SecondaryColor,
                theme.GlowColor,
                theme.DisplayTextColor,
                theme.ButtonTextColor,
                theme.BorderColor);
        }

        // ==================== BACKGROUND UPLOAD ====================

        protected async Task TriggerBackgroundUpload()
        {
            await JSRuntime.InvokeVoidAsync("calculonInterop.triggerFileInput", "bgFileInput");
        }

        protected async Task HandleBackgroundUpload(InputFileChangeEventArgs e)
        {
            try
            {
                var file = e.File;
                if (file != null)
                {
                    // Read file as base64
                    using var stream = file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024); // 5MB max
                    var buffer = new byte[file.Size];
                    await stream.ReadAsync(buffer);
                    var base64 = Convert.ToBase64String(buffer);
                    var dataUrl = $"data:{file.ContentType};base64,{base64}";

                    // Apply background
                    await JSRuntime.InvokeVoidAsync("calculonInterop.applyBackgroundImage", dataUrl);

                    // Save to localStorage
                    await LocalStorage.SaveBackgroundAsync(dataUrl);

                    BackgroundStyle = $"background-image: url('{dataUrl}');";
                    StateHasChanged();
                }
            }
            catch (Exception ex)
            {
                State.SetError("Upload failed");
            }
        }

        // ==================== PREFERENCES ====================

        private async Task LoadPreferences()
        {
            // Load theme
            var savedTheme = await LocalStorage.LoadThemeAsync();
            if (!string.IsNullOrEmpty(savedTheme))
            {
                ThemeManager.SetTheme(savedTheme);
            }

            // Load background
            var savedBackground = await LocalStorage.LoadBackgroundAsync();
            if (!string.IsNullOrEmpty(savedBackground))
            {
                BackgroundStyle = $"background-image: url('{savedBackground}');";
            }

            // Load angle mode
            var savedAngleMode = await LocalStorage.LoadAngleModeAsync();
            if (savedAngleMode == "RAD")
            {
                State.AngleMode = AngleMode.Radians;
            }
        }

        private async Task SaveAngleMode()
        {
            await LocalStorage.SaveAngleModeAsync(State.GetAngleModeDisplay());
        }
    }
}