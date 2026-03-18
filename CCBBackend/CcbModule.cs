using System.Globalization;
using System.Text;
using BackendInterface;

namespace CCBBackend
{
    internal sealed class CCBGlobal
    {
        public string Name { get; }
        public CCBType Type { get; }
        public CCBValue? Initializer { get; set; }

        public CCBGlobal(string name, CCBType type)
        {
            Name = name;
            Type = type;
        }
    }

    internal sealed class CCBFunction
    {
        public string Name { get; }
        public CCBType Signature { get; }
        public CCBType ReturnType { get; }
        public List<CCBType> ParameterTypes { get; }
        public List<CCBType> LocalTypes { get; } = new();
        public List<string> Instructions { get; } = new();
        public List<CCBValue> Parameters { get; }

        public CCBFunction(string name, CCBType signature)
        {
            Name = name;
            Signature = signature;
            ReturnType = signature.ReturnType ?? CCBType.Void;
            ParameterTypes = signature.ParameterTypes.ToList();

            Parameters = new List<CCBValue>(ParameterTypes.Count);
            for (int i = 0; i < ParameterTypes.Count; i++)
            {
                Parameters.Add(CCBValue.Parameter(this, i, ParameterTypes[i]));
            }
        }

        public void Emit(string instruction)
        {
            Instructions.Add(instruction);
        }
    }

    public sealed class Module : IModule
    {
        private readonly string _name;
        private readonly Dictionary<string, CCBGlobal> _globals = new(StringComparer.Ordinal);
        private readonly Dictionary<string, CCBFunction> _functions = new(StringComparer.Ordinal);
        private readonly List<CCBGlobal> _globalOrder = new();
        private readonly List<CCBFunction> _functionOrder = new();

        public Module(string name)
        {
            _name = name;
        }

        internal IEnumerable<CCBGlobal> Globals => _globalOrder;
        internal IEnumerable<CCBFunction> Functions => _functionOrder;

        public IValue AddFunction(string name, IType definition)
        {
            if (_functions.ContainsKey(name))
            {
                throw new InvalidOperationException($"Function '{name}' already exists in module '{_name}'.");
            }

            var signature = CCBType.AsCCB(definition);
            if (signature.Kind != TypeKind.FunctionTypeKind)
            {
                throw new InvalidOperationException("Function definition must be a function type.");
            }

            var function = new CCBFunction(name, signature);
            _functions[name] = function;
            _functionOrder.Add(function);

            return CCBValue.Function(function);
        }

        public IValue AddGlobal(IType global, string name)
        {
            if (_globals.ContainsKey(name))
            {
                throw new InvalidOperationException($"Global '{name}' already exists in module '{_name}'.");
            }

            var type = CCBType.AsCCB(global);
            var g = new CCBGlobal(name, type);
            _globals[name] = g;
            _globalOrder.Add(g);

            return CCBValue.GlobalAddress(g);
        }

        public IValue GetNamedFunction(string name)
        {
            if (!_functions.TryGetValue(name, out var function))
            {
                return CCBValue.Invalid();
            }

            return CCBValue.Function(function);
        }

        public bool TryVerify(out string message)
        {
            message = string.Empty;
            return true;
        }

        public string PrintToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ccbytecode 3");

            if (_globalOrder.Count > 0)
            {
                sb.AppendLine();
                foreach (var global in _globalOrder)
                {
                    sb.Append(".global ");
                    sb.Append(global.Name);
                    sb.Append(" type=");
                    sb.Append(global.Type.ToCcbName());

                    if (global.Initializer is not null)
                    {
                        sb.Append(" init=");
                        sb.Append(FormatInitializer(global.Initializer));
                    }

                    sb.AppendLine();
                }
            }

            foreach (var function in _functionOrder)
            {
                sb.AppendLine();
                sb.Append(".func ");
                sb.Append(function.Name);
                sb.Append(" ret=");
                sb.Append(function.ReturnType.ToCcbName());
                sb.Append(" params=");
                sb.Append(function.ParameterTypes.Count.ToString(CultureInfo.InvariantCulture));
                sb.Append(" locals=");
                sb.Append(function.LocalTypes.Count.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine();

                if (function.ParameterTypes.Count > 0)
                {
                    sb.Append(".params ");
                    sb.AppendLine(string.Join(' ', function.ParameterTypes.Select(p => p.ToCcbName())));
                }

                if (function.LocalTypes.Count > 0)
                {
                    sb.Append(".locals ");
                    sb.AppendLine(string.Join(' ', function.LocalTypes.Select(p => p.ToCcbName())));
                }

                foreach (var instruction in function.Instructions)
                {
                    sb.Append("  ");
                    sb.AppendLine(instruction);
                }

                sb.AppendLine(".endfunc");
            }

            return sb.ToString();
        }

        private static string FormatInitializer(CCBValue initializer)
        {
            if (initializer.Kind == CCBValueKind.ConstantInt)
            {
                var ci = (CCBConstInt)initializer.Payload!;
                if (CCBType.AsCCB(initializer.TypeOf).Kind == TypeKind.PointerTypeKind && ci.Value == 0)
                {
                    return "null";
                }

                return ci.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (initializer.Kind == CCBValueKind.ConstantReal)
            {
                var real = (double)initializer.Payload!;
                return real.ToString("R", CultureInfo.InvariantCulture);
            }

            throw new InvalidOperationException("Unsupported global initializer kind.");
        }
    }
}
