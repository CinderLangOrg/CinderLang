using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang.AstNodes
{
    public class WhileLoopNode : IAstLooperNode
    {
        public string Name { get; set; }
        public IAstNode[] Children { get; set; }
        public IBlock CondBLock { get; set; }
        public IBlock Block { get; set; }
        public IBlock ContinueBlock { get; set; }
        public IAstContainerNode Parent { get; set; }

        public List<(IType,string,IValue)> ContextVariables { get; set; } = new();
        public bool HasBreak { get; set; }

        public void Generate(IAstNode parent)
        {
            if (parent is NameSpaceNode) ErrorManager.Throw(ErrorType.Syntax,"While loop cannot be nested inside a namepsace");
            else if (parent is IAstContainerNode container)
            {
                Parent = container;

                var iid = GenerateRandomString(16);

                Block = MethodNode.CurrentMethod.Function.AppendBasicBlock(iid + ".Loop");
                CondBLock = MethodNode.CurrentMethod.Function.AppendBasicBlock(iid + ".Condition");
                ContinueBlock = MethodNode.CurrentMethod.Function.AppendBasicBlock(iid + ".Then");

                if (parent is not IAstBreakerNode bn) MethodNode.CurrentMethod.Alignment = ContinueBlock;
                else bn.Block = ContinueBlock;

                Program.Builder.BuildBr(CondBLock);

                Program.Builder.PositionAtEnd(CondBLock);

                var statement = GenerationHelpers.ParseValue(Name, Program.Builder.Int1Type, container, true, true);
                var br = Program.Builder.BuildCondBr(statement, Block, ContinueBlock);

                foreach (var item in Children)
                {
                    Program.Builder.PositionAtEnd(Block);
                    item.Generate(this);

                    if (HasBreak) break;
                }

                if (!Children.Any(x=> x is BreakerNode))
                {
                    Program.Builder.PositionAtEnd(Block);
                    Program.Builder.BuildBr(CondBLock);
                }
            }
            else ErrorManager.Throw(ErrorType.Syntax, "While loop must be nested.");
        }

        static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return RandomNumberGenerator.GetString(chars, length);
        }
    }
}
