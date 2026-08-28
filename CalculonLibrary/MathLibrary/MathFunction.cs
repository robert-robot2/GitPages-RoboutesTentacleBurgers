using System;

namespace CalculonLibrary.MathLibrary
{
    /// <summary>
    /// All scientific calculator math functions
    /// </summary>
    public static class MathFunctions
    {
        // ==================== TRIGONOMETRIC FUNCTIONS ====================

        public static double Sin(double value, AngleMode mode)
        {
            return Math.Sin(ToRadians(value, mode));
        }

        public static double Cos(double value, AngleMode mode)
        {
            return Math.Cos(ToRadians(value, mode));
        }

        public static double Tan(double value, AngleMode mode)
        {
            return Math.Tan(ToRadians(value, mode));
        }

        public static double ASin(double value, AngleMode mode)
        {
            double radians = Math.Asin(value);
            return FromRadians(radians, mode);
        }

        public static double ACos(double value, AngleMode mode)
        {
            double radians = Math.Acos(value);
            return FromRadians(radians, mode);
        }

        public static double ATan(double value, AngleMode mode)
        {
            double radians = Math.Atan(value);
            return FromRadians(radians, mode);
        }

        // ==================== POWER AND ROOT FUNCTIONS ====================

        public static double Power(double baseValue, double exponent)
        {
            return Math.Pow(baseValue, exponent);
        }

        public static double Square(double value)
        {
            return value * value;
        }

        public static double Cube(double value)
        {
            return value * value * value;
        }

        public static double Sqrt(double value)
        {
            if (value < 0)
                throw new Exception("Cannot take square root of negative number");
            return Math.Sqrt(value);
        }

        public static double CubeRoot(double value)
        {
            return Math.Pow(value, 1.0 / 3.0);
        }

        public static double NthRoot(double value, double n)
        {
            return Math.Pow(value, 1.0 / n);
        }

        public static double Reciprocal(double value)
        {
            if (value == 0)
                throw new Exception("Cannot divide by zero");
            return 1.0 / value;
        }

        // ==================== LOGARITHMIC FUNCTIONS ====================

        public static double Log(double value)
        {
            if (value <= 0)
                throw new Exception("Logarithm undefined for non-positive numbers");
            return Math.Log10(value);
        }

        public static double Ln(double value)
        {
            if (value <= 0)
                throw new Exception("Natural log undefined for non-positive numbers");
            return Math.Log(value);
        }

        public static double PowerOf10(double exponent)
        {
            return Math.Pow(10, exponent);
        }

        public static double PowerOfE(double exponent)
        {
            return Math.Exp(exponent);
        }

        // ==================== COMBINATORICS ====================

        public static double Factorial(int n)
        {
            if (n < 0)
                throw new Exception("Factorial undefined for negative numbers");
            if (n > 170)
                throw new Exception("Factorial too large (max 170)");

            if (n == 0 || n == 1)
                return 1;

            double result = 1;
            for (int i = 2; i <= n; i++)
            {
                result *= i;
            }
            return result;
        }

        public static double Permutation(int n, int r)
        {
            if (n < 0 || r < 0)
                throw new Exception("n and r must be non-negative");
            if (r > n)
                throw new Exception("r cannot be greater than n");

            return Factorial(n) / Factorial(n - r);
        }

        public static double Combination(int n, int r)
        {
            if (n < 0 || r < 0)
                throw new Exception("n and r must be non-negative");
            if (r > n)
                throw new Exception("r cannot be greater than n");

            return Factorial(n) / (Factorial(r) * Factorial(n - r));
        }

        // ==================== UTILITY FUNCTIONS ====================

        public static double Random()
        {
            return new Random().NextDouble();
        }

        public static double Round(double value, int decimals = 0)
        {
            return Math.Round(value, decimals);
        }

        public static double Negate(double value)
        {
            return -value;
        }

        public static double AbsoluteValue(double value)
        {
            return Math.Abs(value);
        }

        public static double Percentage(double value, double percent)
        {
            return (value * percent) / 100.0;
        }

        public static double Modulo(double a, double b)
        {
            return a % b;
        }

        // ==================== ANGLE CONVERSION ====================

        private static double ToRadians(double value, AngleMode mode)
        {
            return mode == AngleMode.Degrees ? value * (Math.PI / 180.0) : value;
        }

        private static double FromRadians(double radians, AngleMode mode)
        {
            return mode == AngleMode.Degrees ? radians * (180.0 / Math.PI) : radians;
        }

        // ==================== SCIENTIFIC NOTATION ====================

        public static string ToScientificNotation(double value, int significantDigits = 10)
        {
            if (value == 0)
                return "0";

            int exponent = (int)Math.Floor(Math.Log10(Math.Abs(value)));
            double mantissa = value / Math.Pow(10, exponent);

            return string.Format("{0:F" + significantDigits + "}E{1}", mantissa, exponent);
        }

        public static double FromScientificNotation(double mantissa, int exponent)
        {
            return mantissa * Math.Pow(10, exponent);
        }
    }

    /// <summary>
    /// Angle mode for trigonometric functions
    /// </summary>
    public enum AngleMode
    {
        Degrees,
        Radians
    }
}