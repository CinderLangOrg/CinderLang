using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang.AstNodes
{
    public class ElseConditionNode : IAstConditionNode
    {
        public string Name { get; set; }
        public IAstNode[] Children { get; set; }
        public IAstContainerNode Parent { get; set; }
        public IBlock ContinueBlock { get; set; }

        public IfConditionNode ifcond;

        public List<(IType,string,IValue)> ContextVariables { get; set; } = new();

        public void Generate(IAstNode parent)
        {
            if (parent is NameSpaceNode) ErrorManager.Throw(ErrorType.Syntax,"Else statement cannot be nested inside a namepsace");
            else if (parent is IAstContainerNode container)
            {
                Parent = container;

                var tidx = Array.IndexOf(container.Children,this);

                Console.WriteLine(container.Children[tidx - 1]);

                void th() => ErrorManager.Throw(ErrorType.Syntax, "Else statement must be preceded by an if.");

                IfConditionNode cmp = null!;

                if (tidx == 0) th();
                else if (container.Children[tidx - 1] is IfConditionNode rcmp) cmp = rcmp;
                else if (container.Children[tidx - 1] is ElseConditionNode ecmp)
                {
                    if (ecmp.Children.Length == 1 && ecmp.Children[0] is IfConditionNode ic)
                        cmp = ic;
                    else ErrorManager.Throw(ErrorType.Syntax, "If statement cannot have two else statements.");
                }
                else th();

                cmp.Else.RemoveTerminator();

                ifcond = cmp;
                ContinueBlock = cmp.Else;

                foreach (var item in Children)
                {
                    Program.Builder.PositionAtHead(cmp.Else);
                    item.Generate(this);
                }

                Program.Builder.PositionAtEnd(ifcond.Else);
                Program.Builder.BuildBr(ifcond.ContinueBlock);
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Else statement must be nested.");
        }
    }
}
