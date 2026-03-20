using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang
{
    public interface IAstNode
    {
        public void Generate(IAstNode parent);
    }

    public interface IAstContainerNode : IAstNode
    {
        public IAstNode[] Children { get; set; }
        public List<(IType, string, IValue)> ContextVariables { get; set; }

        public IAstContainerNode Parent { get; set; }
    }

    public interface IAstAttributeContainerNode : IAstContainerNode
    {
        public string[] Attributes { get; set; }
    }
}
