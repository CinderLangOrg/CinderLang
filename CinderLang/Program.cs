using BackendInterface;
using CCBBackend;
using LLVMBackend;

namespace CinderLang
{
    internal class Program
    {
        public static IBuilder Builder { get; set; }

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: CinderLang <input.cin> [--backend llvm|ccb] [--backend=llvm|ccb] [-o <out>] [--output <out>] [--output=<out>]");
                Environment.Exit(1);
            }

            string backendName = "llvm";
            string? outputPath = null;
            for (int i = 1; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.StartsWith("--backend=", StringComparison.OrdinalIgnoreCase))
                {
                    backendName = arg.Split('=', 2)[1].Trim().ToLowerInvariant();
                    continue;
                }

                if (string.Equals(arg, "--backend", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("--backend expects a value: llvm or ccb.");
                    }

                    backendName = args[++i].Trim().ToLowerInvariant();
                    continue;
                }

                if (string.Equals(arg, "-o", StringComparison.Ordinal) ||
                    string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException($"{arg} expects a file path.");
                    }

                    outputPath = args[++i].Trim();
                    continue;
                }

                if (arg.StartsWith("--output=", StringComparison.OrdinalIgnoreCase))
                {
                    outputPath = arg.Split('=', 2)[1].Trim();
                }
            }

            Builder = backendName switch
            {
                "ccb" => new CCBBackend.Builder(),
                "llvm" => new LLVMBackend.Builder(),
                _ => throw new ArgumentException($"Unknown backend '{backendName}'. Supported backends: llvm, ccb.")
            };

            var namespaces = Parser.Parse(File.ReadAllText(args[0]));

            var extension = backendName == "ccb" ? ".ccb" : ".asm";

            foreach (var item in namespaces)
            {
                item.Generate(null!);

                if (!item.Module.TryVerify(out var error))
                    ErrorManager.Throw(ErrorType.Generation, $"The namespace \"{item.Name}\" failed to generate in backend '{backendName}': {error}");

                var emitPath = outputPath;
                if (string.IsNullOrWhiteSpace(emitPath))
                {
                    emitPath = item.Name + extension;
                }
                else if (namespaces.Length > 1)
                {
                    throw new ArgumentException("-o/--output supports a single namespace per compilation. Omit it to emit one file per namespace.");
                }

                Builder.EmitToFile(emitPath, item.Module);

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    var d = item.Module.PrintToString();
                    Console.WriteLine(d);
                }
            }
        }
    }
}
