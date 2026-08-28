using System;
using System.Text;

namespace ChemCalc.Helpers
{
    /// <summary>
    /// Helper class for formatting chemical formulas with proper notation
    /// Converts numbers to Unicode subscripts and superscripts
    /// </summary>
    public static class ChemicalFormatter
    {
        // ==================== SUBSCRIPT CONVERSION ====================

        /// <summary>
        /// Convert a number string to Unicode subscript (e.g., "2" → "₂")
        /// </summary>
        public static string ToSubscript(string number)
        {
            if (string.IsNullOrEmpty(number))
                return "";

            StringBuilder result = new StringBuilder();

            foreach (char c in number)
            {
                result.Append(CharToSubscript(c));
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert a single digit character to subscript
        /// </summary>
        private static char CharToSubscript(char c)
        {
            switch (c)
            {
                case '0': return '₀';
                case '1': return '₁';
                case '2': return '₂';
                case '3': return '₃';
                case '4': return '₄';
                case '5': return '₅';
                case '6': return '₆';
                case '7': return '₇';
                case '8': return '₈';
                case '9': return '₉';
                case '+': return '₊';
                case '-': return '₋';
                case '=': return '₌';
                case '(': return '₍';
                case ')': return '₎';
                default: return c;
            }
        }

        // ==================== SUPERSCRIPT CONVERSION ====================

        /// <summary>
        /// Convert a charge to Unicode superscript (e.g., +1 → "⁺", -2 → "²⁻")
        /// </summary>
        public static string ToSuperscript(int charge)
        {
            if (charge == 0)
                return "";

            StringBuilder result = new StringBuilder();

            if (charge > 0)
            {
                if (charge > 1)
                    result.Append(NumberToSuperscript(charge));
                result.Append('⁺');
            }
            else
            {
                if (charge < -1)
                    result.Append(NumberToSuperscript(Math.Abs(charge)));
                result.Append('⁻');
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert a number string to Unicode superscript
        /// </summary>
        public static string ToSuperscript(string number)
        {
            if (string.IsNullOrEmpty(number))
                return "";

            StringBuilder result = new StringBuilder();

            foreach (char c in number)
            {
                result.Append(CharToSuperscript(c));
            }

            return result.ToString();
        }

        /// <summary>
        /// Convert number to superscript string
        /// </summary>
        private static string NumberToSuperscript(int number)
        {
            return ToSuperscript(number.ToString());
        }

        /// <summary>
        /// Convert a single character to superscript
        /// </summary>
        private static char CharToSuperscript(char c)
        {
            switch (c)
            {
                case '0': return '⁰';
                case '1': return '¹';
                case '2': return '²';
                case '3': return '³';
                case '4': return '⁴';
                case '5': return '⁵';
                case '6': return '⁶';
                case '7': return '⁷';
                case '8': return '⁸';
                case '9': return '⁹';
                case '+': return '⁺';
                case '-': return '⁻';
                case '=': return '⁼';
                case '(': return '⁽';
                case ')': return '⁾';
                default: return c;
            }
        }

        // ==================== STATE NOTATION ====================

        /// <summary>
        /// Get state notation string
        /// </summary>
        public static string GetStateNotation(ChemCalc.ChemEngine.MoleculeState state)
        {
            switch (state)
            {
                case ChemCalc.ChemEngine.MoleculeState.Solid:
                    return "(s)";
                case ChemCalc.ChemEngine.MoleculeState.Liquid:
                    return "(l)";
                case ChemCalc.ChemEngine.MoleculeState.Gas:
                    return "(g)";
                case ChemCalc.ChemEngine.MoleculeState.Aqueous:
                    return "(aq)";
                default:
                    return "";
            }
        }

        // ==================== ARROW SYMBOLS ====================

        /// <summary>
        /// Get reaction arrow symbol
        /// </summary>
        public static string GetReactionArrow()
        {
            return "→"; // Unicode right arrow
        }

        /// <summary>
        /// Get equilibrium arrow symbol
        /// </summary>
        public static string GetEquilibriumArrow()
        {
            return "⇌"; // Unicode equilibrium arrow
        }

        /// <summary>
        /// Get reversible arrow symbol
        /// </summary>
        public static string GetReversibleArrow()
        {
            return "⇄"; // Unicode reversible arrow
        }

        // ==================== COMMON CHEMICAL SYMBOLS ====================

        /// <summary>
        /// Get delta (Δ) symbol for heat/change
        /// </summary>
        public static string GetDelta()
        {
            return "Δ";
        }

        /// <summary>
        /// Get degrees symbol (°)
        /// </summary>
        public static string GetDegrees()
        {
            return "°";
        }

        /// <summary>
        /// Get plus-minus symbol (±)
        /// </summary>
        public static string GetPlusMinus()
        {
            return "±";
        }

        // ==================== FORMATTING HELPERS ====================

        /// <summary>
        /// Format chemical formula with proper subscripts
        /// Example: "H2O" → "H₂O"
        /// </summary>
        public static string FormatFormula(string rawFormula)
        {
            if (string.IsNullOrEmpty(rawFormula))
                return "";

            StringBuilder result = new StringBuilder();

            for (int i = 0; i < rawFormula.Length; i++)
            {
                char c = rawFormula[i];

                // If it's a digit, convert to subscript
                if (char.IsDigit(c))
                {
                    result.Append(CharToSubscript(c));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Format molar mass with units
        /// </summary>
        public static string FormatMolarMass(double mass)
        {
            return string.Format("{0:F3} g/mol", mass);
        }

        /// <summary>
        /// Format temperature with degrees
        /// </summary>
        public static string FormatTemperature(double temp, string unit = "C")
        {
            return string.Format("{0:F1}°{1}", temp, unit);
        }
    }
}