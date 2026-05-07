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
            if (parent is NameSpaceNode) ErrorManager.Throw(ErrorType.Syntax,"Asm statement cannot be nested inside a namepsace");
            else if (parent is IAstContainerNode container)
            {
                string[] asm = Children.Select(x => 
                {
                    if (x is RawExprNode n)
                    {
                        return n.Name;
                    }

                    ErrorManager.Throw(ErrorType.Syntax, $"Asm statement cannot contain '{x.GetType().Name}'");
                    return "";
                }).ToArray();

                var asmstr = string.Join(Environment.NewLine,asm);
                var asmt = Program.Builder.CreateFunction(Program.Builder.VoidType, []);

                Program.Builder.BuildCall(asmt, Program.Builder.BuildInlineAsm(asmt, asmstr,""), []);
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Asm statement must be nested.");
        }
    }
}
