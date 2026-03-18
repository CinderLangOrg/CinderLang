using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CinderLang.AstNodes;

namespace CinderLang
{
    public static class Parser
    {
        public static NameSpaceNode[] Parse(string code)
        {
            var processed = Preprocessor.Process(code);

            var it = Iterate(code);

            NameSpaceNode[] nodes = new NameSpaceNode[it.Length];

            for (int i = 0; i < it.Length; i++) nodes[i] = (NameSpaceNode)it[i];

            return nodes;
        }

        static IAstNode[] Iterate(string code)
        {
            List<IAstNode> nodes = new List<IAstNode>();
            
            string keyword = "",name = "",buffer = "";
            bool isInString = false;
            StringType stringType = StringType.word;
            int stringlenght = 0;

            int bracketDepth = 0;

            for (int i = 0; i < code.Length; i++)
            {
                if (StringTypeExtensions.IsString(code[i]))
                {
                    if (!isInString)
                    {
                        isInString = true;
                        stringType = StringTypeExtensions.GetStringType(code[i]);
                    }
                    else if (stringType.GetString() == code[i])
                    {
                        if ((i > 0 && code[i - 1] != '\\') || i == 0)
                        {
                            if ((stringType == StringType.character && stringlenght == 2) || stringType == StringType.word)
                            {
                                isInString = false;
                                stringlenght = 0;
                            }
                            else ErrorManager.Throw(ErrorType.Syntax, $"Invalid character string lenght");
                        }
                    }
                }
                if (isInString) stringlenght++;

                if (code[i] == '{' && !isInString)
                {
                    if (bracketDepth > 0) buffer += code[i];

                    bracketDepth++;
                }
                else if (code[i] == '}' && !isInString)
                {
                    bracketDepth--;

                    if (bracketDepth > 0) buffer += code[i];

                    if (bracketDepth == 0)
                    {
                        nodes.Add(GetContainerNode(name.Trim(), keyword.Trim(), buffer.Trim()));
                        name = "";
                        buffer = "";
                        keyword = "";
                    }
                }
                else if (bracketDepth == 0 && code[i] == ';' && !isInString)
                {
                    nodes.Add(GetNode(keyword.Trim(), name.Trim()));
                    name = "";
                    buffer = "";
                    keyword = "";
                }
                else if (bracketDepth > 0) buffer += code[i];
                else if (keyword.EndsWith(' ') && keyword.Trim().Length > 0) name += code[i];
                else keyword += code[i];
            }

            return nodes.ToArray();
        }

        static IAstNode GetNode(string keyword, string name)
        {
            switch (keyword)
            {
                case "return":
                    return new ReturnNode
                    {
                        Name = name
                    };
                default:
                    if (GenerationHelpers.IsType(keyword))
                    {
                        return new VariableNode
                        {
                            Name = name,
                            rtype = keyword
                        };
                    }

                    return new RawExprNode
                    {
                        Name = keyword + " " + name
                    };
            }
        }

        static IAstContainerNode GetContainerNode(string name, string keyword, string buffer)
        {
            switch (keyword)
            {
                case "namespace":
                    return new NameSpaceNode
                    {
                        Name = name,
                        Children = Iterate(buffer)
                    };
                case "def":
                    return new MethodNode
                    {
                        Name = name,
                        Children = Iterate(buffer)
                    };
                default:
                    ErrorManager.Throw(ErrorType.Syntax, $"Invalid keyword \"{keyword}\"");
                    break;
            }

            return null!;
        }
    }
}
