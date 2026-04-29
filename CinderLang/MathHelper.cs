using System;
using System.Collections.Generic;
using System.Linq;
using BackendInterface;
using CinderLang.AstNodes;

namespace CinderLang
{
    public static class MathHelper
    {
        private static readonly Dictionary<string, int> Precedence = new()
        {
            { "+", 1 },
            { "-", 1 },
            { "*", 2 },
            { "/", 2 },
            { "%", 2 },
        };

        public static bool IsMathExpression(string value)
        {
            int depth = 0;

            foreach (char c in value)
            {
                if (c == '(') depth++;
                else if (c == ')') depth--;

                if (depth == 0 && "+-*/".Contains(c))
                    return true;
            }

            return false;
        }

        public static IValue ParseMathExpression(string expr, IType type, IAstContainerNode node)
        {
            var tokens = Tokenize(expr);
            var rpn = ToRPN(tokens);
            return Build(rpn, type, node);
        }

        private static List<string> Tokenize(string expr)
        {
            List<string> tokens = new();
            string current = "";

            foreach (char c in expr)
            {
                if (char.IsWhiteSpace(c)) continue;

                if ("+-*/()".Contains(c))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current);
                        current = "";
                    }
                    tokens.Add(c.ToString());
                }
                else
                {
                    current += c;
                }
            }

            if (current.Length > 0)
                tokens.Add(current);

            return tokens;
        }

        private static List<string> ToRPN(List<string> tokens)
        {
            List<string> output = new();
            Stack<string> ops = new();

            foreach (var token in tokens)
            {
                if (IsOperator(token))
                {
                    while (ops.Count > 0 &&
                           IsOperator(ops.Peek()) &&
                           Precedence[ops.Peek()] >= Precedence[token])
                    {
                        output.Add(ops.Pop());
                    }
                    ops.Push(token);
                }
                else if (token == "(")
                {
                    ops.Push(token);
                }
                else if (token == ")")
                {
                    while (ops.Peek() != "(")
                        output.Add(ops.Pop());

                    ops.Pop();
                }
                else
                {
                    output.Add(token);
                }
            }

            while (ops.Count > 0)
                output.Add(ops.Pop());

            return output;
        }

        private static bool IsOperator(string t)
            => Precedence.ContainsKey(t);

        private static IValue Build(List<string> rpn, IType type, IAstContainerNode node)
        {
            Stack<IValue> stack = new();

            foreach (var token in rpn)
            {
                if (!IsOperator(token))
                {
                    stack.Push(GenerationHelpers.ParseValue(token, type, node,true,true));
                    continue;
                }

                if (stack.Count == 0) ErrorManager.Throw(ErrorType.Syntax,"Invalid math syntax");

                var right = stack.Pop();

                if (stack.Count == 0) stack.Push(BuildNeg(right,type));
                else
                {
                    var left = stack.Pop();

                    stack.Push(EmitOp(token, left, right, type));
                }       
            }

            return stack.Pop();
        }

        private static IValue BuildNeg(IValue a, IType type)
        {
            bool isFloat = type.Kind == TypeKind.FloatTypeKind ||
                           type.Kind == TypeKind.DoubleTypeKind;

            return isFloat ? Program.Builder.BuildFNeg(a) : Program.Builder.BuildSNeg(a);
        }

        private static IValue EmitOp(string op, IValue a, IValue b, IType type)
        {
            bool isFloat = type.Kind == TypeKind.FloatTypeKind ||
                           type.Kind == TypeKind.DoubleTypeKind;

            return op switch
            {
                "+" => isFloat
                    ? Program.Builder.BuildFAdd(a, b)
                    : Program.Builder.BuildAdd(a, b),

                "-" => isFloat
                    ? Program.Builder.BuildFSub(a, b)
                    : Program.Builder.BuildSub(a, b),

                "*" => isFloat
                    ? Program.Builder.BuildFMul(a, b)
                    : Program.Builder.BuildMul(a, b),

                "/" => isFloat
                    ? Program.Builder.BuildFDiv(a, b)
                    : Program.Builder.BuildSDiv(a, b),

                "%" => isFloat
                ? Program.Builder.BuildFRem(a, b)
                : Program.Builder.BuildSRem(a, b),

                _ => throw new Exception($"Unknown operator {op}")
            };
        }
    }
}