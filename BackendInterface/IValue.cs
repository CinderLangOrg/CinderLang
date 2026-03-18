using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackendInterface
{
    public interface IValue : IEquatable<IType>
    {
        public int Handle { get; }

        public IType TypeOf { get; }
        public IValue Initializer { get; set; }
        IBlock AppendBasicBlock(string name);
        IValue GetParam(uint p);
    }
}
