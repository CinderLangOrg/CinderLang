using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackendInterface
{
    public interface IBuilder
    {
        public IType Int32Type { get; }
        public IType FloatType { get; }
        public IType DoubleType { get; }
        public IType VoidType { get; }
        public IType Int8Type { get; }
        public IType Int1Type { get; }

        public void EmitToFile(string path,IModule module);
        public IValue BuildCall(IType Ty,IValue Fn, IValue[] Args, string name = "");
        public IValue BuildStore(IValue val, IValue ptr);
        public IValue BuildAlloca(IType t, string name = "");
        public void PositionAtEnd(IBlock block);
        public void PositionAtHead(IBlock block);
        public IValue BuildRet(IValue value);
        public IValue BuildVoidRet();
        public IModule CreateModuleWithName(string name);
        public IValue BuildGlobalString(string str);
        public IValue CreateConstInt(IType type, ulong value, bool signedext = false);
        public IValue CreateConstReal(IType type, double value);
        public IType CreatePointer(IType t, int space = 0);
        public IType CreateFunction(IType returnt, IType[] Parameters, bool isVarArg = false);
        public IValue BuildLoad(IType t, IValue v, string name = "");
        public IValue BuildFAdd(IValue a, IValue b, string name = "");
        public IValue BuildAdd(IValue a, IValue b, string name = "");
        public IValue BuildFSub(IValue a, IValue b, string name = "");
        public IValue BuildSub(IValue a, IValue b, string name = "");
        public IValue BuildFMul(IValue a, IValue b, string name = "");
        public IValue BuildMul(IValue a, IValue b, string name = "");
        public IValue BuildFDiv(IValue a, IValue b, string name = "");
        public IValue BuildSDiv(IValue a, IValue b, string name = "");
        public IValue BuildFRem(IValue a, IValue b, string name = "");
        public IValue BuildSRem(IValue a, IValue b, string name = "");
        public IValue BuildSNeg(IValue a, string name = "");
        public IValue BuildFNeg(IValue a, string name = "");
        public IValue BuildGEP(IType t, IValue ptr, IValue[] values, string name = "");
        public IValue BuildIntToPtr(IValue val, IType t, string name = "");
        public IValue BuildICmp(ComparationPredicate predicate, IValue a, IValue b, string name = "");
        public IValue BuildBr(IBlock block, string name = "");
        public IValue BuildCondBr(IValue If, IBlock Then, IBlock Else);
    }
}
