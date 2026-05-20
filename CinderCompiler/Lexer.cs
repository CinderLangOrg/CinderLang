using CinderLang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderCompiler
{
    public static class Lexer
    {
        public static Token[] Lex(string source)
        {
            List<Token> tokens = new();

            string tokenbuffer = "";
            uint cstart = 0;

            bool isInString = false;
            StringType stringType = StringType.word;
            int stringlenght = 0;

            void pushToken(int i)
            {
                if (TokenMapper.IsToken(tokenbuffer)) tokens.Add(new(TokenMapper.GetTokenType(tokenbuffer), "", cstart));
                else tokens.Add(new(TokenType.NOMINAL, tokenbuffer, cstart));

                cstart = (uint)i + 1;
                tokenbuffer = "";
            }

            for (int i = 0; i < source.Length; i++)
            {
                if (StringTypeExtensions.IsString(source[i]))
                {
                    if (!isInString)
                    {
                        isInString = true;
                        stringType = StringTypeExtensions.GetStringType(source[i]);
                        continue;
                    }
                    else if (stringType.GetString() == source[i])
                    {
                        if ((i > 0 && source[i - 1] != '\\') || i == 0)
                        {
                            if ((stringType == StringType.character && stringlenght == 2) || stringType == StringType.word)
                            {
                                isInString = false;
                                stringlenght = 0;

                                tokens.Add(new(
                                    stringType == StringType.word ? TokenType.STRING : TokenType.CHARACTER, 
                                    tokenbuffer.Substring(0,tokenbuffer.Length-1), 
                                    cstart
                                ));

                                cstart = (uint)i + 1;
                                tokenbuffer = "";

                                continue;
                            }
                            else ErrorManager.Throw(ErrorType.Syntax, $"Invalid character string lenght");
                        }
                    }
                }
                if (isInString) stringlenght++;

                if ((source[i] == ' ' || TokenMapper.IsSpecialToken(source[i].ToString())) && !isInString)
                    pushToken(i);
                else tokenbuffer += source[i];
            }

            pushToken(source.Length);

            return tokens.ToArray();
        }
    }
}
