using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang.AstNodes
{
    public struct VariableNode : IAstNode
    {
        public string Name { get; set; }
        public string rtype { get; set; }

        public void Generate(IAstNode parent)
        {
            var p = Name.Split('=', 2);

            if (p.Length == 0) ErrorManager.Throw(ErrorType.Syntax, "Variable declaration must have a name.");

            var t = GenerationHelpers.GetLLVMType(rtype);

            if (t.Equals(Program.Builder.VoidType)) ErrorManager.Throw(ErrorType.Syntax, "Variable cannot be of type void.");

            var name = p[0].Trim();
            
            if (parent is NameSpaceNode ns)
            {
                if (ns.ContextVariables.Any(x => x.Item2 == p[0].Trim())) ErrorManager.Throw(ErrorType.Syntax, $"Variable \"{p[0].Trim()}\" is already defined in this scope.");

                var s = ns.Module.AddGlobal(t,name);
                if (p.Length == 2)
                {
                    var data = GenerationHelpers.ParseValue(p[1].Trim(), t,ns);
                    s.Initializer = data;
                }

                ns.ContextVariables.Add((t, name, s));
            }
            else if (parent is IAstContainerNode method)
            {
                if (method.ContextVariables.Any(x => x.Item2 == p[0].Trim())) ErrorManager.Throw(ErrorType.Syntax, $"Variable \"{p[0].Trim()}\" is already defined in this scope.");

                var s = Program.Builder.BuildAlloca(t, name);

                if (p.Length == 2)
                {
                    var data = GenerationHelpers.ParseValue(p[1].Trim(), t,method, true, true);
                    Program.Builder.BuildStore(data, s);
                }

                method.ContextVariables.Add((t, name, s));
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Declaration statement must be nested inside a namespace or a method.");
        }
    }
}
