using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackendInterface
{
    public interface IModule
    {
        public IValue GetNamedFunction(string name);
        public IValue AddFunction(string name,IType definition);
        public IValue AddGlobal(IType global,string name);
        public bool TryVerify(out string message);
        public string PrintToString();
    }
}
