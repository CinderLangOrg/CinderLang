using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;

namespace CinderLang
{
    public static class CLIManager
    {
        // everything in here is tmp

        public static void ManageCommands(params string[] args)
        {
            if (args.Length == 0) { PrintUsage(); return; }

            var cmd = args[0];

            switch (cmd) 
            {
                case "help": PrintHelp(""); return;
                case "build": Compile(args.Skip(1).ToArray()); return;
                default: ErrorManager.Throw(ErrorType.Cli,$"Invalid command: \"{cmd}\""); return;
            }
        }

        public static void Compile(params string[] args)
        {
            if (args.Length == 0) { ErrorManager.Throw(ErrorType.Cli, "Invalid arguments number for build"); return; }

            new LLVMBackend.Block(null); // this tells c# to load the llvm backend

            var project = args[0];

            var BackendName = "llvm";
            var BackendT = BackendManager.GetBackend(BackendName);

            Program.Builder = (IBuilder)Activator.CreateInstance(BackendT)!;

            var namespaces = Parser.Parse(File.ReadAllText(project));

            foreach (var item in namespaces)
            {
                item.Generate(null!);

                if (!item.Module.TryVerify(out var error))
                    ErrorManager.Throw(ErrorType.Generation, $"The namespace \"{item.Name}\" failed to generate, with the LLVM error: {error}");

                Program.Builder.EmitToFile(item.Name + ".asm", item.Module);

                var d = item.Module.PrintToString();
                Console.WriteLine(d);
            }
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: CinderLang <command> [arguments]");
            Console.WriteLine("Run 'CinderLang help [command]' for more info");
        }

        public static void PrintHelp(string command)
        {
            Console.WriteLine("build---------| Compiles a .ciproj file");
            Console.WriteLine("help----------| Shows this message");
        }
    }
}
