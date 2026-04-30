using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang.AstNodes
{
    public class MethodNode : IAstAttributeContainerNode
    {
        public string Name { get; set; }
        public IAstNode[] Children { get; set; }
        public IValue Function { get; set; }
        public IBlock Alignment { get; set; }
        public FunctionDefinition Definition { get; set; }
        public IAstContainerNode Parent { get; set; }

        public List<(IType,string,IValue)> ContextVariables { get; set; } = new();

        public string[] Attributes { get; set; }

        public static MethodNode CurrentMethod { get; private set; }
        public bool HasBreak { get; set; }

        public void Generate(IAstNode parent)
        {
            ValidateAttributes();
            bool isextern = Attributes.Contains("extern");
            bool isvariadic = Attributes.Contains("variadic");

            if (parent is NameSpaceNode ns)
            {
                Parent = ns;

                string AdditionalContent = "";

                if (Children == null && Name.Contains("=>"))
                {
                    var idx = Name.IndexOf("=>");

                    var nname = Name.Substring(0, idx).Trim();
                    AdditionalContent = Name.Substring(idx + 2).Trim() + ";";

                    Name = nname;
                }

                Definition = GenerationHelpers.ParseFunctionDefinition(Name, !isextern, isvariadic);

                var isVoid = Definition.ReturnType.Equals(Program.Builder.VoidType);

                if (AdditionalContent != "")
                    Children = Parser.Iterate((isVoid ? "" : "return ") + AdditionalContent);

                if (Children != null && isextern) ErrorManager.Throw(ErrorType.Syntax, "Extern methods must not have a body");
                if (Children == null && !isextern) ErrorManager.Throw(ErrorType.Syntax, "Non extern methods must have a body");

                if (ns.MethodDefinitions.Any(x=>x.Name == Definition.Name)) ErrorManager.Throw(ErrorType.Syntax, $"Method with overload \"{Definition.Name}\" is already defined in namespace \"{ns.Name}\".");

                Function = ns.Module.AddFunction(Definition.Name,Definition.Signature,isextern);

                ns.MethodDefinitions.Add(Definition);

                CurrentMethod = this;

                for (uint i = 0; i < Definition.Arguments.Length; i++)
                {
                    var item = Definition.Arguments[i];

                    ContextVariables.Add((item.llvmt,item.name,Function.GetParam(i)));
                }

                if (!isextern)
                {
                    Alignment = Function.AppendBasicBlock("start");

                    foreach (var child in Children!)
                    {
                        Program.Builder.PositionAtEnd(Alignment);
                        child.Generate(this);

                        if (HasBreak) break;
                    }

                    if (!HasBreak)
                    {
                        if (isVoid)
                        {
                            Program.Builder.PositionAtEnd(Alignment);
                            Program.Builder.BuildVoidRet();
                        }
                        else ErrorManager.Throw(ErrorType.Syntax, "Non-void method must have a return statement.");
                    }
                }
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Method must be nested inside a namespace.");
        }

        static readonly string[] validattrs = {"extern", "variadic"};
        void ValidateAttributes()
        {
            foreach (var item in Attributes)
                if (!validattrs.Contains(item)) ErrorManager.Throw(ErrorType.Syntax, $"Ivalid method attributed \"{item}\"");
        }
    }
}
