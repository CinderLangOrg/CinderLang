using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;
using LLVMSharp;
using LLVMSharp.Interop;

namespace LLVMBackend
{
    public class Builder : IBuilder
    {
        public LLVMContextRef context;
        public LLVMBuilderRef builder;
        LLVMTargetRef target;
        LLVMTargetMachineRef targetMachine;

        public Builder()
        {
            context = LLVMContextRef.Create();
            builder = context.CreateBuilder();

            LLVM.InitializeAllTargetInfos();
            LLVM.InitializeAllTargets();
            LLVM.InitializeAllTargetMCs();
            LLVM.InitializeAllAsmParsers();
            LLVM.InitializeAllAsmPrinters();

            target = LLVMTargetRef.GetTargetFromTriple(LLVMTargetRef.DefaultTriple);
            targetMachine = target.CreateTargetMachine(
                LLVMTargetRef.DefaultTriple,
                "generic",
                "",
                LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
                LLVMRelocMode.LLVMRelocDefault,
                LLVMCodeModel.LLVMCodeModelDefault);
        }

        public IType Int32Type => new LLVMType(context.Int32Type);

        public IType FloatType => new LLVMType(context.FloatType);

        public IType DoubleType => new LLVMType(context.DoubleType);

        public IType VoidType => new LLVMType(context.VoidType);

        public IType Int8Type => new LLVMType(context.Int8Type);

        public IValue BuildAlloca(IType t, string name = "") => 
            new LLVMValue(builder.BuildAlloca((t as LLVMType)!.type,name));

        public IValue BuildCall(IType Ty, IValue Fn, IValue[] Args, string name = "")
        {
            if (Ty is not LLVMType ty) throw new NotImplementedException();
            if (Fn is not LLVMValue fn) throw new NotImplementedException();

            LLVMValueRef[] l = Args.Select(x => (x as LLVMValue)!.Value).ToArray();

            return new LLVMValue(builder.BuildCall2(ty.type,fn.Value,l,name));
        }

        public IValue BuildGlobalString(string str) => 
            new LLVMValue(builder.BuildGlobalString(str));

        public IValue BuildRet(IValue value)
        {
            if (value is not LLVMValue vl) throw new NotImplementedException();

            if (vl.Value.TypeOf.Kind == LLVMTypeKind.LLVMVoidTypeKind) return new LLVMValue(builder.BuildRetVoid());

            return new LLVMValue(builder.BuildRet(vl.Value));
        }

        public IValue BuildStore(IValue val, IValue ptr)
        {
            if (val is not LLVMValue v) throw new NotImplementedException();
            if (ptr is not LLVMValue p) throw new NotImplementedException();

            return new LLVMValue(builder.BuildStore(v.Value,p.Value));
        }

        public IValue BuildVoidRet() => 
            new LLVMValue(builder.BuildRetVoid());

        public IValue CreateConstInt(IType type, ulong value, bool signedext = false) => 
            LLVMValue.CreateConstInt(type, value, signedext);

        public IValue CreateConstReal(IType type, double value) => LLVMValue.CreateConstReal(type, value);

        public IType CreateFunction(IType returnt, IType[] Parameters, bool isVarArg = false) => 
            LLVMType.CreateFunction(returnt, Parameters, isVarArg);
        public IModule CreateModuleWithName(string name) => 
            new Module(context.CreateModuleWithName(name));

        public IType CreatePointer(IType t, int space = 0) => 
            LLVMType.CreatePointer(t,space);

        public void EmitToFile(string path, IModule module) => 
            targetMachine.EmitToFile((module as Module)!.module, path, LLVMCodeGenFileType.LLVMObjectFile);

        public void PositionAtEnd(IBlock block) => 
            builder.PositionAtEnd((block as Block)!.block);

        public IValue BuildLoad(IType t, IValue v, string name = "") => 
            new LLVMValue(builder.BuildLoad2((t as LLVMType)!.type, (v as LLVMValue)!.Value, name));

        public IValue BuildFAdd(IValue a, IValue b, string name = "") => 
            new LLVMValue(builder.BuildFAdd(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));
        public IValue BuildAdd(IValue a, IValue b, string name = "") =>
            new LLVMValue(builder.BuildAdd(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));

        public IValue BuildFSub(IValue a, IValue b, string name = "") =>
            new LLVMValue(builder.BuildFSub(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));
        public IValue BuildSub(IValue a, IValue b, string name = "") =>
            new LLVMValue(builder.BuildSub(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));

        public IValue BuildFMul(IValue a, IValue b, string name = "") =>
            new LLVMValue(builder.BuildFMul(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));
        public IValue BuildMul(IValue a, IValue b, string name = "") =>
            new LLVMValue(builder.BuildMul(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));

        public IValue BuildFDiv(IValue a, IValue b, string name = "") =>
            new LLVMValue(builder.BuildFDiv(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));
        public IValue BuildSDiv(IValue a, IValue b, string name = "") =>
            new LLVMValue(builder.BuildSDiv(LLVMValue.ToLLVM(a), LLVMValue.ToLLVM(b)));
    }
}
