using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;
using LLVMSharp.Interop;

namespace CinderLang.AstNodes
{
    public class NameSpaceNode : IAstContainerNode
    {
        public static NameSpaceNode CurrentNamespace { get; set; }

        public string Name { get; set; }
        public IAstNode[] Children { get; set; }
        public IModule Module { get; set; }
        public List<(IType, string, IValue)> ContextVariables { get; set; } = new();
        public List<FunctionDefinition> MethodDefinitions { get; set; } = new();
        public IAstContainerNode Parent { get; set; } = null;

        public void Generate(IAstNode parent)
        {
            if (parent != null) ErrorManager.Throw(ErrorType.Syntax, "Namespace cannot be nested inside another node.");

            Module = Program.Builder.CreateModuleWithName(Name);
            CurrentNamespace = this;

            foreach (var child in Children) child.Generate(this);
        }
    }
}
