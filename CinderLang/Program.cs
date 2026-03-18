using System.Buffers.Text;
using System.Text;
using LLVMBackend;
using LLVMSharp;
using LLVMSharp.Interop;

namespace CinderLang
{
    internal class Program
    {
        public static Builder Builder { get; set; }

        static void Main(string[] args)
        {
            Builder = new Builder();

            var namespaces = Parser.Parse(File.ReadAllText(args[0]));

            foreach (var item in namespaces)
            {
                item.Generate(null!);

                if (!item.Module.TryVerify(out var error))
                    ErrorManager.Throw(ErrorType.Generation,$"The namespace \"{item.Name}\" failed to generate, with the LLVM error: {error}");

                Builder.EmitToFile(item.Name + ".asm",item.Module);

                var d = item.Module.PrintToString();
                Console.WriteLine(d);
            }
        }
    }
}
