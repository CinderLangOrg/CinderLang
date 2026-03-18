using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang.AstNodes
{
    public struct ReturnNode : IAstNode
    {
        public string Name { get; set; }

        public void Generate(IAstNode parent)
        {
            if (parent is MethodNode method)
            {
                if (method.Definition.ReturnType.Equals(Program.Builder.VoidType)) Program.Builder.BuildVoidRet();
                else
                {
                    var value = GenerationHelpers.ParseValue(Name, method.Definition.ReturnType,method);
                    if (value.TypeOf.Equals(method.Definition.ReturnType)) Program.Builder.BuildRet(value);
                    else ErrorManager.Throw(ErrorType.Syntax, $"Return type mismatch. Expected {method.Definition.ReturnType}, got {value.TypeOf}.");
                }
            }
            else ErrorManager.Throw(ErrorType.Syntax, "Return statement must be nested inside a method.");
        }
    }
}
