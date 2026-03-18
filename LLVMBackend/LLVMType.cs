using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;
using LLVMSharp.Interop;

namespace LLVMBackend
{
    public class LLVMType : IType
    {
        public LLVMTypeRef type;

        public LLVMType(LLVMTypeRef t) { type = t; }

        public TypeKind Kind => (TypeKind)type.Kind;

        public static IType CreateFunction(IType returnt, IType[] Parameters, bool isVarArg = false)
        {
            if (returnt is not LLVMType rt) throw new NotImplementedException();

            LLVMTypeRef[] l = Parameters.Select(x=> (x as LLVMType)!.type).ToArray();

            return new LLVMType(LLVMTypeRef.CreateFunction(rt.type,l,isVarArg));
        }

        public static IType CreatePointer(IType t, int space = 0)
        {
            if (t is not LLVMType rt) throw new NotImplementedException();

            return new LLVMType(LLVMTypeRef.CreatePointer(rt.type, (uint)space));
        }

        public bool Equals(IType other)
        {
            if (other is not LLVMType o) return false;
            return type == o.type;
        }

        public override bool Equals(object obj)
        => obj is IType other && Equals(other);

        public override int GetHashCode()
            => type.Handle.GetHashCode();

        public override string ToString() => type.ToString();

        public static LLVMTypeRef ToLLVM(IType t) => (t as LLVMType)!.type;
    }
}
