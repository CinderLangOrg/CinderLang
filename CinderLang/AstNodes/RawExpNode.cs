using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang.AstNodes
{
    public struct RawExprNode : IAstNode
    {
        public string Name { get; set; }

        public static readonly char[] OpList = ['-', '+', '*', '/', '%'];

        public void Generate(IAstNode parent)
        {
            if (Name.Trim().Length == 0) return;

            if (parent is IAstContainerNode method)
            {
                var p = Name.Split('=', 2);

                if (p.Length == 2)
                {
                    var name = p[0].Trim();
                    var val = p[1].Trim();

                    if (OpList.Contains(name.Last()))
                    {
                        var c = name.Last();

                        name = name.Substring(0, name.Length - 1).Trim();

                        val = $"{name} {c} ({val})";
                    }
                    
                    new AssignNode()
                    { 
                        Name = name,
                        Value = val
                    }.Generate(method);
                }
                else GenerationHelpers.ParseValue(Name, Program.Builder.VoidType,method, false);
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Expression statement must be nested.");
        }
    }
}
