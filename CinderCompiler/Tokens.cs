using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderCompiler
{
    public struct Token(TokenType t,string d,uint p)
    {
        public TokenType type = t;
        public string data = d;
        public uint Position = p;
    }

    public enum TokenType
    {
        EXTERN,
        VARIADIC,
        ASSEMBLY,
        RETURN,
        DEF,
        BREAK,
        IF,
        WHILE,
        ELSE,
        NAMESPACE,
        ASM,

        SEMICOLON,
        PLUS,
        MINUS,
        EQUALS,
        DIVIDE,
        MODULO,
        TIMES,
        OPEN_STATEMENT,
        CLOSE_STATEMENT,
        OPEN_PARENTH,
        CLOSE_PARENTH,
        OPEN_SQARE,
        CLOSE_SQARE,

        STRING,
        CHARACTER,
        NOMINAL,
    }

    public static class TokenMapper
    {
        public static bool IsToken(string buffer) => Enum.IsDefined(typeof(TokenType), buffer.ToUpper()) || IsSpecialToken(buffer);

        public static bool IsSpecialToken(string buffer)
        {
            switch (buffer)
            {
                case "+":
                case "-":
                case "=":
                case "/":
                case "%":
                case "*":
                case "{":
                case "}":
                case "(":
                case ")":
                case ";":
                    return true;
            }

            return false;
        }

        public static TokenType GetTokenType(string buffer)
        {
            switch (buffer)
            {
                case "+":
                    return TokenType.PLUS;
                case "-":
                    return TokenType.MINUS;
                case "=":
                    return TokenType.EQUALS;
                case "/":
                    return TokenType.DIVIDE;
                case "%":
                    return TokenType.MODULO;
                case "*":
                    return TokenType.TIMES;
                case "{":
                    return TokenType.OPEN_STATEMENT;
                case "}":
                    return TokenType.CLOSE_STATEMENT;
                case "(":
                    return TokenType.OPEN_PARENTH;
                case ")":
                    return TokenType.CLOSE_PARENTH;
                case ";":
                    return TokenType.SEMICOLON;
            }

            return (TokenType)Enum.Parse(typeof(TokenType), buffer.ToUpper());
        }
    }
}
