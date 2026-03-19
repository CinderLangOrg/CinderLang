using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;
using LLVMSharp.Interop;

namespace LLVMBackend
{
    public class Module : IModule
    {
        public LLVMModuleRef module;
        public Module(LLVMModuleRef m)
        {
            module = m;
        }

        public IValue AddFunction(string name, IType definition, bool extenal = false) => new LLVMValue(module.AddFunction(name, (definition as LLVMType)!.type));

        public IValue AddGlobal(IType global, string name) => new LLVMValue(module.AddGlobal((global as LLVMType)!.type,name));
        public IValue GetNamedFunction(string name) => new LLVMValue(module.GetNamedFunction(name));

        public string PrintToString() => module.PrintToString();

        public bool TryVerify(out string message) => module.TryVerify(LLVMVerifierFailureAction.LLVMPrintMessageAction,out message);
    }
}
