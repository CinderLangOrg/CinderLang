using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang.AstNodes
{
    public class AsmNode : IAstContainerNode
    {
        public IAstNode[] Children { get; set; }
        public List<(IType, string, IValue)> ContextVariables { get; set; }

        public IAstContainerNode Parent { get; set; }
        public bool HasBreak { get; set; }

        public void Generate(IAstNode parent)
        {
            if (parent is NameSpaceNode) ErrorManager.Throw(ErrorType.Syntax, "Asm statement cannot be nested inside a namepsace");
            else if (parent is IAstContainerNode container)
                GenerationHelpers.BuildASMFromChildren(Program.Builder.VoidType, Children, []);
            else ErrorManager.Throw(ErrorType.Syntax, "Asm statement must be nested.");
        }
    }
}
