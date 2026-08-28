using System;

namespace ChemCalc.MathLibrary
{
    /// <summary>
    /// Manages calculator state including memory, angle mode, and last answer
    /// </summary>
    public class CalculatorState
    {
        // ==================== DISPLAY & INPUT ====================

        public string CurrentExpression { get; set; } = "";
        public string DisplayValue { get; set; } = "0";
        public bool IsNewCalculation { get; set; } = true;
        public bool HasError { get; set; } = false;

        // ==================== MEMORY ====================

        public double MemoryValue { get; private set; } = 0;
        public bool HasMemory => MemoryValue != 0;

        // ==================== LAST ANSWER ====================

        public double LastAnswer { get; set; } = 0;

        // ==================== ANGLE MODE ====================

        public AngleMode AngleMode { get; set; } = AngleMode.Degrees;

        // ==================== MEMORY OPERATIONS ====================

        /// <summary>
        /// Store current display value to memory (MS)
        /// </summary>
        public void MemoryStore(double value)
        {
            MemoryValue = value;
        }

        /// <summary>
        /// Recall memory value (MR)
        /// </summary>
        public double MemoryRecall()
        {
            return MemoryValue;
        }

        /// <summary>
        /// Clear memory (MC)
        /// </summary>
        public void MemoryClear()
        {
            MemoryValue = 0;
        }

        /// <summary>
        /// Add to memory (M+)
        /// </summary>
        public void MemoryAdd(double value)
        {
            MemoryValue += value;
        }

        /// <summary>
        /// Subtract from memory (M-)
        /// </summary>
        public void MemorySubtract(double value)
        {
            MemoryValue -= value;
        }

        // ==================== ANGLE MODE OPERATIONS ====================

        /// <summary>
        /// Toggle between Degrees and Radians
        /// </summary>
        public void ToggleAngleMode()
        {
            AngleMode = AngleMode == AngleMode.Degrees ? AngleMode.Radians : AngleMode.Degrees;
        }

        public string GetAngleModeDisplay()
        {
            return AngleMode == AngleMode.Degrees ? "DEG" : "RAD";
        }

        // ==================== STATE MANAGEMENT ====================

        /// <summary>
        /// Reset calculator to initial state (AC)
        /// </summary>
        public void Clear()
        {
            CurrentExpression = "";
            DisplayValue = "0";
            IsNewCalculation = true;
            HasError = false;
        }

        /// <summary>
        /// Clear all including memory
        /// </summary>
        public void ClearAll()
        {
            Clear();
            MemoryClear();
            LastAnswer = 0;
        }

        /// <summary>
        /// Delete last character (DEL)
        /// </summary>
        public void DeleteLast()
        {
            if (CurrentExpression.Length > 0)
            {
                CurrentExpression = CurrentExpression[..^1];
                DisplayValue = CurrentExpression.Length == 0 ? "0" : CurrentExpression;
            }
        }

        /// <summary>
        /// Set error state
        /// </summary>
        public void SetError(string message = "Error")
        {
            HasError = true;
            DisplayValue = message;
            CurrentExpression = "";
        }

        /// <summary>
        /// Append value to current expression
        /// </summary>
        public void AppendToExpression(string value)
        {
            if (HasError || IsNewCalculation)
            {
                CurrentExpression = value;
                IsNewCalculation = false;
                HasError = false;
            }
            else
            {
                CurrentExpression += value;
            }
            DisplayValue = CurrentExpression;
        }

        /// <summary>
        /// Set display to a specific value (for results)
        /// </summary>
        public void SetDisplay(double value)
        {
            LastAnswer = value;
            DisplayValue = FormatDisplayValue(value);
            CurrentExpression = DisplayValue;
            IsNewCalculation = true;
        }

        /// <summary>
        /// Format value for display (handle scientific notation for very large/small numbers)
        /// </summary>
        private string FormatDisplayValue(double value)
        {
            // Handle special cases
            if (double.IsNaN(value))
                return "Error";
            if (double.IsInfinity(value))
                return value > 0 ? "Infinity" : "-Infinity";

            // Use scientific notation for very large or very small numbers
            if (Math.Abs(value) >= 1e10 || (Math.Abs(value) < 1e-6 && value != 0))
            {
                return value.ToString("E6");
            }

            // Round to avoid floating point precision issues
            double rounded = Math.Round(value, 10);

            // Remove trailing zeros after decimal point
            string result = rounded.ToString("G15");

            return result;
        }

        /// <summary>
        /// Get current display value as double
        /// </summary>
        public double GetCurrentValue()
        {
            if (double.TryParse(DisplayValue, out double value))
                return value;
            return 0;
        }
    }
}