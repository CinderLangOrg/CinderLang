using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang.AstNodes
{
    public struct AssignNode : IAstNode
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public void Generate(IAstNode parent)
        {
            if (parent is IAstContainerNode method && parent is not NameSpaceNode)
            {
                var (type, name, value) = GenerationHelpers.ResolveVariable(Name,method);

                var data = GenerationHelpers.ParseValue(Value, type,method);
                Program.Builder.BuildStore(data, value);
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Assign statement must be nested inside a method.");
        }
    }
}
