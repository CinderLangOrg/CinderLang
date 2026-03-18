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

        public void Generate(IAstNode parent)
        {
            if (parent is IAstContainerNode method)
            {
                var p = Name.Split('=', 2);

                if (p.Length == 2)
                {
                    new AssignNode()
                    { 
                        Name = p[0].Trim(),
                        Value = p[1].Trim()
                    }.Generate(method);
                }
                else GenerationHelpers.ParseValue(Name, Program.Builder.VoidType,method, false);
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Expression statement must be nested.");
        }
    }
}
