using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BackendInterface;
using CinderLang.AstNodes;
using LLVMSharp;
using LLVMSharp.Interop;

namespace CinderLang
{
    public struct FunctionDefinition
    {
        public string Name { get; set; }
        public IType Signature { get; set; }
        public (IType llvmt, string name)[] Arguments { get; set; }
        public IType ReturnType { get; set; }
    }

    public static class GenerationHelpers
    {
        public static List<string> TypeList = ["int","float","double","void","byte"];

        public static FunctionDefinition ParseFunctionDefinition(string name,bool mangle = true)
        {
            var def = new FunctionDefinition();

            List<(IType llvmt, string name)> arguments = new();

            string rname = "",rtype = "void",typestring = "";
            int parencount = 0;
            bool hasreachedparents = false;

            void generatearg()
            {
                var s = typestring.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (s.Length != 2) ErrorManager.Throw(ErrorType.Syntax, $"Invalid type definition \"{typestring}\"");

                arguments.Add((GetLLVMType(s[0]), s[1]));
                typestring = "";
            }

            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '(')
                {
                    hasreachedparents = true;

                    parencount++;
                    if (parencount == 1) continue;
                }
                else if (name[i] == ')')
                {
                    parencount--;

                    if (parencount == 0 && typestring.Trim().Length > 0) generatearg();

                    if (parencount == 0) continue;
                }
                else if (rname.Trim().Length == 0 || !hasreachedparents) rname += name[i];
                else if (parencount > 0 && name[i] == ',') generatearg();
                else if (parencount > 0) typestring += name[i];
                else if (name[i] == ':' && hasreachedparents && parencount == 0)
                {
                    rtype = name[(i + 1)..].Trim();
                    break;
                }
            }

            def.ReturnType = GetLLVMType(rtype);

            if (mangle)
            {
                var nsPrefix = NameSpaceNode.CurrentNamespace?.Name?.Trim();
                var coreName = $"{def.ReturnType}.{rname}.{string.Join('.', arguments.Select(x => x.llvmt))}".TrimEnd('.');
                rname = string.IsNullOrWhiteSpace(nsPrefix) ? coreName : $"{nsPrefix}.{coreName}";
            }

            def.Arguments = arguments.ToArray();
            def.Name = rname;
            def.Signature = Program.Builder.CreateFunction(def.ReturnType, def.Arguments.Select(x => x.llvmt).ToArray(), false);

            return def;
        }

        public static IValue ParseValue(string value, IType llvmt, IAstContainerNode searchp, bool typeKnown = true)
        {
            value = value.Trim();

            if (llvmt.Equals(Program.Builder.VoidType) && typeKnown) ErrorManager.Throw(ErrorType.Syntax, "Cannot assign a value to void type.");

            int parenIndex = value.IndexOf('(');
            if (parenIndex > 0 && value.EndsWith(")"))
            {
                string fname = value[..parenIndex].Trim();
                string argstr = value[(parenIndex + 1)..^1];

                List<IValue> args = new();

                if (!string.IsNullOrWhiteSpace(argstr))
                {
                    int depth = 0;
                    StringBuilder current = new();

                    foreach (char c in argstr)
                    {
                        if (c == ',' && depth == 0)
                        {
                            args.Add(ParseValue(current.ToString(), llvmt, searchp));
                            current.Clear();
                            continue;
                        }

                        if (c == '(') depth++;
                        if (c == ')') depth--;

                        current.Append(c);
                    }

                    if (current.Length > 0)
                        args.Add(ParseValue(current.ToString(), llvmt, searchp));
                }

                var nname = (typeKnown ? $"{llvmt}." : "") + $"{fname}.{string.Join(".", args.Select(x => x.TypeOf))}".TrimEnd('.');
                
                var method = NameSpaceNode.CurrentNamespace.MethodDefinitions.FirstOrDefault(
                    m => m.Name.EndsWith(nname),
                    new() {Name = nname});

                nname = method.Name;

                IValue func = NameSpaceNode.CurrentNamespace.Module.GetNamedFunction(nname);
                if (func.Handle == IntPtr.Zero)
                    ErrorManager.Throw(ErrorType.Syntax, $"Unknown function \"{fname}\" with overload \"{nname}\"");

                return Program.Builder.BuildCall(method.Signature, func, args.ToArray());
            }

            if (MathHelper.IsMathExpression(value)) return MathHelper.ParseMathExpression(value,llvmt,searchp);
            if (IsVariable(value, searchp)) return ResolveVariable(value, searchp).Item3;

            return llvmt.Kind switch
            {
                TypeKind.IntegerTypeKind => GetIntegerData(llvmt,value),
                TypeKind.FloatTypeKind => GetFloatData(llvmt, value),
                TypeKind.DoubleTypeKind => GetDoubleData(llvmt, value),
                TypeKind.PointerTypeKind => GetPointerData(value),
                _ => RetInvValue(value)
            };
        }

        static IValue GetIntegerData(IType llvmt, string value)
        {
            if (ulong.TryParse(value, out var v))
                return Program.Builder.CreateConstInt(llvmt, v, false);
            else if (value.StartsWith('\'') && value.EndsWith('\''))
                return Program.Builder.CreateConstInt(llvmt, value[1], false);

            ErrorManager.Throw(ErrorType.Syntax, $"Invalid integer value \"{value}\"");
            return null;
        }
        static IValue GetFloatData(IType llvmt, string value)
        {
            if (float.TryParse(value, out var v))
                return Program.Builder.CreateConstReal(llvmt, v);
            ErrorManager.Throw(ErrorType.Syntax, $"Invalid float value \"{value}\"");
            return null;
        }
        static IValue GetDoubleData(IType llvmt, string value)
        {
            if (double.TryParse(value, out var v))
                return Program.Builder.CreateConstReal(llvmt, v);
            ErrorManager.Throw(ErrorType.Syntax, $"Invalid double value \"{value}\"");
            return null;
        }

        static IValue GetPointerData(string value)
        {
            if (value.StartsWith('"') && value.EndsWith('"'))
                return Program.Builder.BuildGlobalString(value[1..^1]);
            ErrorManager.Throw(ErrorType.Syntax, $"Invalid pointer value \"{value}\"");
            return null;
        }

        public static bool IsVariable(string name, IAstContainerNode node)
        {
            if (node.ContextVariables.Any(x => x.Item2 == name)) return true;

            if (node.Parent != null)
                return IsVariable(name, node.Parent);

            return false;
        }

        public static (IType, string, IValue) ResolveVariable(string name,IAstContainerNode node)
        {
            if (node.ContextVariables.Any(x=>x.Item2 == name))
            {
                var c = node.ContextVariables.First(x => x.Item2 == name);

                if (c.Item1.Equals(c.Item3.TypeOf))
                    return c;

                return (c.Item1, c.Item2, Program.Builder.BuildLoad(c.Item1, c.Item3));
            }

            if (node.Parent != null)
                return ResolveVariable(name, node.Parent);

            ErrorManager.Throw(ErrorType.Syntax, $"Variable \"{name}\" does not exist");

            return (null,"",null);
        }

        static IValue RetInvValue(string value)
        {
            ErrorManager.Throw(ErrorType.Syntax, $"Invalid value \"{value}\"");
            return null;
        }

        static IType RetInvType(string type)
        {
            ErrorManager.Throw(ErrorType.Syntax, $"Unknown type \"{type}\"");
            return null;
        }

        public static bool IsType(string type)
        {
            var trimmed = type.Trim();

            while (trimmed.EndsWith("*")) trimmed = trimmed[..^1].TrimEnd();

            if (TypeList.Contains(trimmed))
                return true;
            return false;
        }

        public static IType GetLLVMType(string type)
        {
            var trimmed = type.Trim();

            int pointerCount = 0;
            while (trimmed.EndsWith("*"))
            {
                pointerCount++;
                trimmed = trimmed[..^1].TrimEnd();
            }

            var t = trimmed switch
            {
                "int" => Program.Builder.Int32Type,
                "float" => Program.Builder.FloatType,
                "double" => Program.Builder.DoubleType,
                "void" => Program.Builder.VoidType,
                "byte" => Program.Builder.Int8Type,
                _ => RetInvType(type),
            };

            for (int i = 0; i < pointerCount; i++)
            {
                t = Program.Builder.CreatePointer(t, 0);
            }

            return t;
        }
    }
}
