using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;
using LLVMSharp.Interop;

namespace LLVMBackend
{
    public class LLVMValue : IValue
    {
        public LLVMValueRef Value;
        private LLVMValue _initializer;

        public LLVMValue(LLVMValueRef val) { Value = val;}

        public int Handle => (int)Value.Handle;

        public IType TypeOf => new LLVMType(Value.TypeOf);

        public IValue Initializer
        {
            get
            {
                if (_initializer == null && Value.Initializer.Handle != IntPtr.Zero)
                {
                    _initializer = new LLVMValue(Value.Initializer);
                }
                return _initializer;
            }
            set
            {
                Value.Initializer = (value as LLVMValue)!.Value;
                _initializer = (LLVMValue)value;
            }
        }

        public IBlock AppendBasicBlock(string name) => new Block(Value.AppendBasicBlock(name));

        public static IValue CreateConstInt(IType type, ulong value, bool signedext = false)
        {
            if (type is not LLVMType t) throw new NotImplementedException();

            return new LLVMValue(LLVMValueRef.CreateConstInt(t.type, value, signedext));
        }

        public static IValue CreateConstReal(IType type, double value)
        {
            if (type is not LLVMType t) throw new NotImplementedException();

            return new LLVMValue(LLVMValueRef.CreateConstReal(t.type, value));
        }

        public IValue GetParam(uint p) => new LLVMValue(Value.GetParam(p));

        public bool Equals(IType other)
        {
            if (other is not LLVMValue o) return false;
            return Value == o.Value;
        }

        public override bool Equals(object obj)
        => obj is IType other && Equals(other);

        public override int GetHashCode()
            => Value.GetHashCode();

        public override string ToString() => Value.ToString();

        public static LLVMValueRef ToLLVM(IValue t) => (t as LLVMValue)!.Value;
    }
}
