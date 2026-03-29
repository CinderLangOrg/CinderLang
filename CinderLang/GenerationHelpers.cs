using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using BackendInterface;
using CinderLang.AstNodes;
using static System.Net.Mime.MediaTypeNames;

namespace CinderLang
{
    public struct FunctionDefinition
    {
        public string Name { get; set; }
        public string RName { get; set; }
        public IType Signature { get; set; }
        public (IType llvmt, string name)[] Arguments { get; set; }
        public IType ReturnType { get; set; }
        public bool Variadic { get; set; }
    }

    public static class GenerationHelpers
    {
        public static List<string> TypeList = ["int","float","double","void","byte","bool"];

        public static FunctionDefinition ParseFunctionDefinition(string name,bool mangle = true,bool variadic = false)
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

            var nsPrefix = NameSpaceNode.CurrentNamespace?.Name?.Trim();
            var coreName = $"{def.ReturnType}.{rname}.{string.Join('.', arguments.Select(x => x.llvmt))}".TrimEnd('.');
            def.RName = string.IsNullOrWhiteSpace(nsPrefix) ? coreName : $"{nsPrefix}.{coreName}";

            if (rname == "main" && def.ReturnType.Equals(Program.Builder.Int32Type)) mangle = false;

            if (mangle) rname = def.RName;

            def.Arguments = arguments.ToArray();
            def.Name = rname;
            def.Signature = Program.Builder.CreateFunction(def.ReturnType, def.Arguments.Select(x => x.llvmt).ToArray(), variadic);
            def.Variadic = variadic;

            return def;
        }

        public static IValue ParseValue(string value, IType llvmt, IAstContainerNode searchp, bool typeKnown = true,bool loadptr = false)
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

                    bool isInString = false;
                    StringType stringType = StringType.word;

                    foreach (char c in argstr)
                    {
                        if (StringTypeExtensions.IsString(c))
                        {
                            if (!isInString)
                            {
                                isInString = true;
                                stringType = StringTypeExtensions.GetStringType(c);
                            }
                            else if (stringType.GetString() == c)
                            {
                                if (!current.ToString().EndsWith('\\')) isInString = false;
                            }
                        }

                        if (c == ',' && depth == 0 && !isInString)
                        {
                            var argType = InferTypeFromValue(current.ToString(), searchp);
                            args.Add(ParseValue(current.ToString(), argType, searchp, true));
                            current.Clear();
                            continue;
                        }

                        if (c == '(') depth++;
                        if (c == ')') depth--;

                        current.Append(c);
                    }

                    if (current.Length > 0)
                    {
                        var argType = InferTypeFromValue(current.ToString(), searchp);
                        args.Add(ParseValue(current.ToString(), argType, searchp, true));
                    }
                }

                var nname = (typeKnown ? $"{llvmt}." : "") + $"{fname}.{string.Join(".", args.Select(x => x.TypeOf))}".TrimEnd('.');
                
                var method = NameSpaceNode.CurrentNamespace.MethodDefinitions.FirstOrDefault(
                    m => 
                    {
                        if (m.Variadic && args.Count > 0)
                        {
                            var s = "."+args[^1].TypeOf.ToString();
                            var mname = TrimEndString(nname,s) + s;

                            return m.RName.EndsWith(mname);
                        }

                        return m.RName.EndsWith(nname);
                    },
                    new() {Name = nname,RName = ""});

                nname = method.Name;

                IValue func = NameSpaceNode.CurrentNamespace.Module.GetNamedFunction(nname);
                if (func.Handle == IntPtr.Zero || method.RName == "")
                    ErrorManager.Throw(ErrorType.Syntax, $"Unknown function \"{fname}\" with overload \"{nname}\"");

                return Program.Builder.BuildCall(method.Signature, func, args.ToArray());
            }

            if (MathHelper.IsMathExpression(value)) return MathHelper.ParseMathExpression(value,llvmt,searchp);

            bool isaddress = false;
            if (value.StartsWith('&'))
            {
                value = value.Substring(1).Trim();
                loadptr = false;
                isaddress = true;
            }

            if (IsVariable(value, searchp)) return ResolveVariable(value, searchp,loadptr).Item3;

            return llvmt.Kind switch
            {
                TypeKind.IntegerTypeKind => GetIntegerData(llvmt, searchp, value, isaddress),
                TypeKind.FloatTypeKind => GetFloatData(llvmt, value),
                TypeKind.DoubleTypeKind => GetDoubleData(llvmt, value),
                TypeKind.PointerTypeKind => GetPointerData(value, loadptr),
                _ => RetInvValue(value)
            };
        }

        static string TrimEndString(string input, string suffix)
        {
            var d = input;

            while (d.EndsWith(suffix)) d = d.Substring(0, d.Length - suffix.Length);

            return d;
        }

        public static IAstLooperNode ScanParents(IAstNode parent)
        {
            if (parent == null || parent is not IAstContainerNode)
                ErrorManager.Throw(ErrorType.Syntax, "break statement must be nested inside a loop.");

            (parent as IAstContainerNode)!.HasBreak = true;

            if (parent is IAstLooperNode l) return l;
            else return ScanParents((parent as IAstContainerNode)!.Parent);
        }

        static IValue BuildComparison(IValue lhs, IValue rhs, string op, bool isSigned = true)
        {
            var predicate = op switch
            {
                "==" => ComparationPredicate.Equal,
                "!=" => ComparationPredicate.NotEqual,
                ">" => isSigned ? ComparationPredicate.SGreater : ComparationPredicate.UGreater,
                ">=" => isSigned ? ComparationPredicate.SGreaterEqual : ComparationPredicate.UGreaterEqual,
                "<" => isSigned ? ComparationPredicate.SLess : ComparationPredicate.ULess,
                "<=" => isSigned ? ComparationPredicate.SLessEqual : ComparationPredicate.ULessEqual,
                _ => RetInvCompare(op)
            };

            return Program.Builder.BuildICmp(predicate, lhs, rhs);
        }
        static bool TryParseComparison(string value, IAstContainerNode searchp,bool isAddress, out IValue cmp)
        {
            string[] operators = { "==", "!=", ">=", "<=", ">", "<" };

            cmp = null!;

            foreach (var op in operators)
            {
                var parts = value.Split(op, 2, StringSplitOptions.TrimEntries);
                if (parts.Length != 2) continue;

                var lhsVal = ParseValue(parts[0],InferTypeFromValue(parts[0], searchp),searchp,true, !isAddress);
                var rhsVal = ParseValue(parts[1], InferTypeFromValue(parts[1], searchp),searchp,true,!isAddress);

                if (lhsVal == null || rhsVal == null) return false;

                cmp = BuildComparison(lhsVal, rhsVal, op);
                return true;
            }

            return false;
        }

        static IValue GetIntegerData(IType llvmt, IAstContainerNode searchp, string value,bool isAddress)
        {
            if (isAddress)
                return Program.Builder.BuildIntToPtr(GetIntegerData(llvmt,searchp,value,false),llvmt);

            if (ulong.TryParse(value, out var v))
                return Program.Builder.CreateConstInt(llvmt, v, false);
            else if (value.StartsWith('\'') && value.EndsWith('\''))
                return Program.Builder.CreateConstInt(llvmt, value[1], false);
            else if (llvmt.Equals(Program.Builder.Int1Type) && TryParseComparison(value,searchp,isAddress, out var cmp))
                return cmp;
            else if (value == "true") return Program.Builder.CreateConstInt(llvmt, 1, false);
            else if (value == "false") return Program.Builder.CreateConstInt(llvmt, 0, false);

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

        static IValue GetPointerData(string value,bool load)
        {
            if (ulong.TryParse(value, out _) && !load) return GetIntegerData(Program.Builder.Int32Type,null!,value,true);

            if (value.StartsWith('"') && value.EndsWith('"'))
            {
                if (load)
                    return Program.Builder.BuildLoad(
                    Program.Builder.CreatePointer(Program.Builder.Int8Type),
                    Program.Builder.BuildGlobalString(value[1..^1]));
                else
                    return Program.Builder.BuildGlobalString(value[1..^1]);
            }
            ErrorManager.Throw(ErrorType.Syntax, $"Invalid pointer value \"{value}\"");
            return null;
        }

        public static IType InferTypeFromValue(string value, IAstContainerNode scope)
        {
            value = value.Trim();

            int parenIndex = value.IndexOf('(');
            if (parenIndex > 0 && value.EndsWith(")"))
            {
                string fname = value[..parenIndex].Trim();
                string argstr = value[(parenIndex + 1)..^1];

                var argTypes = new List<IType>();

                if (!string.IsNullOrWhiteSpace(argstr))
                {
                    int depth = 0;
                    StringBuilder current = new();

                    bool isInString = false;
                    StringType stringType = StringType.word;

                    foreach (char c in argstr)
                    {
                        if (StringTypeExtensions.IsString(c))
                        {
                            if (!isInString)
                            {
                                isInString = true;
                                stringType = StringTypeExtensions.GetStringType(c);
                            }
                            else if (stringType.GetString() == c)
                            {
                                if (!current.ToString().EndsWith('\\')) isInString = false;
                            }
                        }

                        if (c == ',' && depth == 0 && !isInString)
                        {
                            argTypes.Add(InferTypeFromValue(current.ToString(), scope));
                            current.Clear();
                            continue;
                        }

                        if (c == '(') depth++;
                        if (c == ')') depth--;

                        current.Append(c);
                    }

                    if (current.Length > 0)
                        argTypes.Add(InferTypeFromValue(current.ToString(), scope));
                }

                string signature = $"{fname}.{string.Join(".", argTypes)}";

                var method = NameSpaceNode.CurrentNamespace.MethodDefinitions
                    .FirstOrDefault(m => m.Name.EndsWith(signature),new FunctionDefinition() { Name = ""});

                if (method.Name == "")
                    ErrorManager.Throw(ErrorType.Syntax, $"Unknown function \"{fname}\"");

                return method.Signature;
            }

            if (MathHelper.IsMathExpression(value))
                return Program.Builder.FloatType;

            if (IsVariable(value, scope))
                return ResolveVariable(value, scope,false).Item1;

            var address = false;

            if (value.StartsWith('&'))
            {
                value = value.Substring(1).Trim();
                address = true;
            }

            if (value.StartsWith("\"") && value.EndsWith("\""))
                return Program.Builder.CreatePointer(Program.Builder.Int8Type);

            if (int.TryParse(value, out _))
                return Program.Builder.Int32Type;

            if (address) ErrorManager.Throw(ErrorType.Syntax, $"Only strings and integers can be used as addresses");

            if (float.TryParse(value, out _))
                return Program.Builder.FloatType;

            if (double.TryParse(value, out _))
                return Program.Builder.DoubleType;

            if (value == "true" || value == "false")
                return Program.Builder.Int1Type;

            ErrorManager.Throw(ErrorType.Syntax,$"Cannot retrive the type of \"{value}\"");

            return Program.Builder.VoidType;
        }

        public static bool IsVariable(string name, IAstContainerNode node)
        {
            if (node.ContextVariables.Any(x => x.Item2 == name)) return true;

            if (node.Parent != null)
                return IsVariable(name, node.Parent);

            return false;
        }

        public static (IType, string, IValue) ResolveVariable(string name,IAstContainerNode node,bool loadptr)
        {
            if (node.ContextVariables.Any(x=>x.Item2 == name))
            {
                var c = node.ContextVariables.First(x => x.Item2 == name);

                if (!loadptr) return c;

                return (c.Item1, c.Item2, Program.Builder.BuildLoad(c.Item1, c.Item3));
            }

            if (node.Parent != null)
                return ResolveVariable(name, node.Parent,loadptr);

            ErrorManager.Throw(ErrorType.Syntax, $"Variable \"{name}\" does not exist");

            return (null,"",null);
        }

        static IValue RetInvValue(string value)
        {
            ErrorManager.Throw(ErrorType.Syntax, $"Invalid value \"{value}\"");
            return null;
        }
        static ComparationPredicate RetInvCompare(string value)
        {
            ErrorManager.Throw(ErrorType.Syntax, $"Invalid comparation operator \"{value}\"");
            return ComparationPredicate.Equal;
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
                "bool" => Program.Builder.Int1Type,
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
