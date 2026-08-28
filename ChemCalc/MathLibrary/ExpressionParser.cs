using ChemCalc.MathLibrary;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ChemCalc.MathLibrary
{
    /// <summary>
    /// Custom expression parser using Shunting-yard algorithm
    /// Handles operator precedence, parentheses, and function calls
    /// </summary>
    public class ExpressionParser
    {
        private CalculatorState _state;

        public ExpressionParser(CalculatorState state)
        {
            _state = state;
        }

        /// <summary>
        /// Main evaluation method - converts string expression to result
        /// </summary>
        public double Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0;

            // Tokenize the expression
            var tokens = Tokenize(expression);

            // Convert to postfix notation (Reverse Polish Notation) using Shunting-yard
            var postfix = ConvertToPostfix(tokens);

            // Evaluate the postfix expression
            return EvaluatePostfix(postfix);
        }

        /// <summary>
        /// Tokenize expression into numbers, operators, functions, and parentheses
        /// </summary>
        private List<Token> Tokenize(string expression)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < expression.Length)
            {
                char c = expression[i];

                // Skip whitespace
                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                // Handle numbers (including decimals)
                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < expression.Length && (char.IsDigit(expression[i]) || expression[i] == '.'))
                    {
                        i++;
                    }
                    tokens.Add(new Token(TokenType.Number, expression.Substring(start, i - start)));
                    continue;
                }

                // Handle multi-character functions and operators
                if (char.IsLetter(c))
                {
                    int start = i;
                    while (i < expression.Length && char.IsLetter(expression[i]))
                    {
                        i++;
                    }
                    string word = expression.Substring(start, i - start);

                    // Check if it's a function
                    if (IsFunction(word))
                    {
                        tokens.Add(new Token(TokenType.Function, word));
                    }
                    else if (word.Equals("Ans", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add(new Token(TokenType.Number, _state.LastAnswer.ToString()));
                    }
                    else if (word.Equals("e", StringComparison.OrdinalIgnoreCase))
                    {
                        tokens.Add(new Token(TokenType.Number, Math.E.ToString()));
                    }
                    else
                    {
                        throw new Exception($"Unknown identifier: {word}");
                    }
                    continue;
                }

                // Handle operators and parentheses
                switch (c)
                {
                    case '+':
                    case '-':
                    case '*':
                    case '/':
                    case '^':
                    case '%':
                        tokens.Add(new Token(TokenType.Operator, c.ToString()));
                        i++;
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.LeftParen, "("));
                        i++;
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RightParen, ")"));
                        i++;
                        break;
                    case 'π':
                        tokens.Add(new Token(TokenType.Number, Math.PI.ToString()));
                        i++;
                        break;
                    default:
                        throw new Exception($"Unknown character: {c}");
                }
            }

            return tokens;
        }

        /// <summary>
        /// Convert infix notation to postfix using Shunting-yard algorithm
        /// </summary>
        private List<Token> ConvertToPostfix(List<Token> tokens)
        {
            var output = new List<Token>();
            var operators = new Stack<Token>();

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                        output.Add(token);
                        break;

                    case TokenType.Function:
                        operators.Push(token);
                        break;

                    case TokenType.Operator:
                        while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator)
                        {
                            var topOp = operators.Peek();
                            if ((IsLeftAssociative(token.Value) && GetPrecedence(token.Value) <= GetPrecedence(topOp.Value)) ||
                                (!IsLeftAssociative(token.Value) && GetPrecedence(token.Value) < GetPrecedence(topOp.Value)))
                            {
                                output.Add(operators.Pop());
                            }
                            else
                            {
                                break;
                            }
                        }
                        operators.Push(token);
                        break;

                    case TokenType.LeftParen:
                        operators.Push(token);
                        break;

                    case TokenType.RightParen:
                        while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParen)
                        {
                            output.Add(operators.Pop());
                        }
                        if (operators.Count == 0)
                        {
                            throw new Exception("Mismatched parentheses");
                        }
                        operators.Pop(); // Remove left parenthesis

                        // If there's a function on top, pop it to output
                        if (operators.Count > 0 && operators.Peek().Type == TokenType.Function)
                        {
                            output.Add(operators.Pop());
                        }
                        break;
                }
            }

            // Pop remaining operators
            while (operators.Count > 0)
            {
                var op = operators.Pop();
                if (op.Type == TokenType.LeftParen)
                {
                    throw new Exception("Mismatched parentheses");
                }
                output.Add(op);
            }

            return output;
        }

        /// <summary>
        /// Evaluate postfix expression
        /// </summary>
        private double EvaluatePostfix(List<Token> postfix)
        {
            var stack = new Stack<double>();

            foreach (var token in postfix)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                        stack.Push(double.Parse(token.Value));
                        break;

                    case TokenType.Operator:
                        if (stack.Count < 2)
                            throw new Exception("Invalid expression");

                        double b = stack.Pop();
                        double a = stack.Pop();
                        stack.Push(ApplyOperator(token.Value, a, b));
                        break;

                    case TokenType.Function:
                        if (stack.Count < 1)
                            throw new Exception("Invalid expression");

                        double arg = stack.Pop();
                        stack.Push(ApplyFunction(token.Value, arg));
                        break;
                }
            }

            if (stack.Count != 1)
                throw new Exception("Invalid expression");

            return stack.Pop();
        }

        private double ApplyOperator(string op, double a, double b)
        {
            return op switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => b == 0 ? throw new Exception("Division by zero") : a / b,
                "^" => Math.Pow(a, b),
                "%" => a % b,
                _ => throw new Exception($"Unknown operator: {op}")
            };
        }

        private double ApplyFunction(string func, double arg)
        {
            return func.ToLower() switch
            {
                "sin" => MathFunctions.Sin(arg, _state.AngleMode),
                "cos" => MathFunctions.Cos(arg, _state.AngleMode),
                "tan" => MathFunctions.Tan(arg, _state.AngleMode),
                "asin" => MathFunctions.ASin(arg, _state.AngleMode),
                "acos" => MathFunctions.ACos(arg, _state.AngleMode),
                "atan" => MathFunctions.ATan(arg, _state.AngleMode),
                "log" => MathFunctions.Log(arg),
                "ln" => MathFunctions.Ln(arg),
                "sqrt" => MathFunctions.Sqrt(arg),
                "cbrt" => MathFunctions.CubeRoot(arg),
                "abs" => Math.Abs(arg),
                "factorial" => MathFunctions.Factorial((int)arg),
                _ => throw new Exception($"Unknown function: {func}")
            };
        }

        private bool IsFunction(string word)
        {
            var functions = new[] { "sin", "cos", "tan", "asin", "acos", "atan", "log", "ln", "sqrt", "cbrt", "abs", "factorial" };
            return functions.Contains(word.ToLower());
        }

        private int GetPrecedence(string op)
        {
            return op switch
            {
                "+" or "-" => 1,
                "*" or "/" or "%" => 2,
                "^" => 3,
                _ => 0
            };
        }

        private bool IsLeftAssociative(string op)
        {
            return op != "^"; // Only exponentiation is right-associative
        }
    }

    /// <summary>
    /// Token types for expression parsing
    /// </summary>
    public enum TokenType
    {
        Number,
        Operator,
        Function,
        LeftParen,
        RightParen
    }

    /// <summary>
    /// Token structure for parser
    /// </summary>
    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }
    }
}