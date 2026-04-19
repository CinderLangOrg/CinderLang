using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang.AstNodes
{
    public class IfConditionNode : IAstBreakerNode
    {
        public string Name { get; set; }
        public IAstNode[] Children { get; set; }
        public IBlock Else { get; set; }
        public IBlock Block { get; set; }
        public IBlock ContinueBlock { get; set; }
        public IAstContainerNode Parent { get; set; }

        public List<(IType,string,IValue)> ContextVariables { get; set; } = new();
        public bool HasBreak { get; set; }

        public void Generate(IAstNode parent)
        {
            if (parent is NameSpaceNode) ErrorManager.Throw(ErrorType.Syntax,"If statement cannot be nested inside a namepsace");
            else if (parent is IAstContainerNode container)
            {
                Parent = container;

                var statement = GenerationHelpers.ParseValue(Name,Program.Builder.Int1Type,container,true,true);

                var iid = GenerateRandomString(16);

                Block = MethodNode.CurrentMethod.Function.AppendBasicBlock(iid + ".True");
                Else = MethodNode.CurrentMethod.Function.AppendBasicBlock(iid + ".False");

                var br = Program.Builder.BuildCondBr(statement, Block, Else);

                foreach (var item in Children)
                {
                    Program.Builder.PositionAtEnd(Block);
                    item.Generate(this);

                    if (MethodNode.CurrentMethod.HasBreak)
                    {
                        MethodNode.CurrentMethod.HasBreak = false;
                        HasBreak = true;
                    }

                    if (HasBreak) break;
                }

                ContinueBlock = MethodNode.CurrentMethod.Function.AppendBasicBlock(iid + ".Then");

                if (parent is not IAstBreakerNode bn) MethodNode.CurrentMethod.Alignment = ContinueBlock;
                else bn.Block = ContinueBlock;

                if (!HasBreak)
                {
                    Program.Builder.PositionAtEnd(Block);
                    Program.Builder.BuildBr(ContinueBlock);
                }
                else if (parent is not IAstLooperNode)
                {
                    var p = GenerationHelpers.ScanParents(parent);

                    Program.Builder.PositionAtEnd(ContinueBlock);
                    Program.Builder.BuildBr(p.Block);
                }

                Program.Builder.PositionAtEnd(Else);
                Program.Builder.BuildBr(ContinueBlock);
            }
            else ErrorManager.Throw(ErrorType.Syntax, "If statement must be nested.");
        }

        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return RandomNumberGenerator.GetString(chars, length);
        }
    }
}
