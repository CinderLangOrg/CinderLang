using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using CinderLang.AstNodes;
using SixLabors.ImageSharp.Memory;

namespace CinderLang
{
    public static class Parser
    {
        public static readonly string[] ValidAttributes = {
            "extern", "variadic"
        };

        public static NameSpaceNode[] Parse(string code)
        {
            var processed = Preprocessor.Process(code);

            var it = Iterate(processed);

            NameSpaceNode[] nodes = new NameSpaceNode[it.Length];

            for (int i = 0; i < it.Length; i++) nodes[i] = (NameSpaceNode)it[i];

            return nodes;
        }

        static IAstNode[] Iterate(string code)
        {
            List<IAstNode> nodes = new List<IAstNode>();
            
            string keyword = "",name = "",buffer = "";
            List<string> Attributes = new List<string>();

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
                        nodes.Add(GetContainerNode(name.Trim(), keyword.Trim(), buffer.Trim(),Attributes.ToArray()));
                        name = "";
                        buffer = "";
                        keyword = "";
                        Attributes.Clear();
                    }
                }
                else if (bracketDepth == 0 && code[i] == ';' && !isInString)
                {
                    nodes.Add(GetNode(keyword.Trim(), name.Trim(),Attributes.ToArray()));
                    name = "";
                    buffer = "";
                    keyword = "";
                    Attributes.Clear();
                }
                else if (bracketDepth > 0) buffer += code[i];
                else if (keyword.EndsWith(' ') && keyword.Trim().Length > 0)
                {
                    if (ValidAttributes.Contains(keyword.Trim()))
                    {
                        Attributes.Add(keyword.Trim());
                        keyword = code[i].ToString();
                    }
                    else name += code[i];
                }
                else keyword += code[i];
            }

            if (keyword != "")
                ErrorManager.Throw(ErrorType.Syntax, $"End of statement expected after {keyword.Trim()}{name.Trim()}");

            return nodes.ToArray();
        }

        static IAstNode GetNode(string keyword, string name, string[] attribs)
        {
            switch (keyword)
            {
                case "return":
                    ReturnAttrError(keyword, attribs.Length);
                    return new ReturnNode
                    {
                        Name = name
                    };
                case "def":
                    return new MethodNode
                    {
                        Name = name,
                        Children = null!,
                        Attributes = attribs
                    };
                case "break":
                    ReturnAttrError(keyword, attribs.Length);

                    if (name.Trim().Length > 0) ErrorManager.Throw(ErrorType.Syntax, "Break statements cannot contain a value");

                    return new BreakerNode();
                case "if":
                    ReturnAttrError(keyword, attribs.Length);

                    if (!name.StartsWith('(')) ErrorManager.Throw(ErrorType.Syntax, "If condition must be inside parenthesis");

                    var ipe = ParseParenthesisEnd(name);

                    return new IfConditionNode
                    {
                        Name = name.Substring(1,ipe-1).Trim(),
                        Children = Iterate(name[(ipe+1)..] +';'),
                    };
                case "while":
                    ReturnAttrError(keyword, attribs.Length);

                    if (!name.StartsWith('(')) ErrorManager.Throw(ErrorType.Syntax, "While condition must be inside parenthesis");

                    var wpe = ParseParenthesisEnd(name);

                    return new WhileLoopNode
                    {
                        Name = name.Substring(1, wpe - 1).Trim(),
                        Children = Iterate(name[(wpe + 1)..] + ';'),
                    };
                case "else":
                    ReturnAttrError(keyword, attribs.Length);

                    return new ElseConditionNode
                    {
                        Name = "",
                        Children = Iterate(name.Trim()+';'),
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

        static IAstContainerNode GetContainerNode(string name, string keyword, string buffer, string[] attribs)
        {
            switch (keyword)
            {
                case "namespace":
                    ReturnAttrError(keyword,attribs.Length);
                    return new NameSpaceNode
                    {
                        Name = name,
                        Children = Iterate(buffer)
                    };
                case "def":
                    return new MethodNode
                    {
                        Name = name,
                        Children = Iterate(buffer),
                        Attributes = attribs
                    };
                case "if":
                    ReturnAttrError(keyword, attribs.Length);

                    if (!name.StartsWith('(') || !name.EndsWith(')')) ErrorManager.Throw(ErrorType.Syntax,"If condition must be inside parenthesis");

                    return new IfConditionNode
                    {
                        Name = name[1..^1],
                        Children = Iterate(buffer),
                    };
                case "while":
                    ReturnAttrError(keyword, attribs.Length);

                    if (!name.StartsWith('(') || !name.EndsWith(')')) ErrorManager.Throw(ErrorType.Syntax, "While condition must be inside parenthesis");

                    return new WhileLoopNode
                    {
                        Name = name[1..^1],
                        Children = Iterate(buffer),
                    };
                case "else":
                    ReturnAttrError(keyword, attribs.Length);

                    return new ElseConditionNode
                    {
                        Name = "",
                        Children = Iterate(name.Trim().Length > 0 ? name+" {"+buffer+'}' : buffer),
                    };
                default:
                    ErrorManager.Throw(ErrorType.Syntax, $"Invalid keyword \"{keyword}\"");
                    break;
            }

            return null!;
        }

        static int ParseParenthesisEnd(string name)
        {
            int pdepth = 0;
            int send = 0;

            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '(') pdepth++;
                else if (name[i] == ')')
                {
                    pdepth--;

                    if (pdepth == 0)
                    {
                        send = i;
                        break;
                    }
                }
            }

            if (pdepth > 0) ErrorManager.Throw(ErrorType.Syntax, "All parenthesis must be closed");

            return send;
        }

        static void ReturnAttrError(string kw,int lenght)
        {
            if (lenght > 0)
                ErrorManager.Throw(ErrorType.Syntax, $"The keyword \"{kw}\" does not allow attributes");
        }
    }
}
