using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackendInterface;
using CinderLang.AstNodes;

namespace CinderLang
{
    public static class CLIManager
    {
        // everything in here is tmp

        public static void ManageCommands(params string[] args)
        {
            DrawCompilerHead.DrawHead();

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

            var project = args[0];

            var proj = ProjectManager.LoadProject(project);
            var namespaces = new List<NameSpaceNode>();

            foreach (var item in Directory.GetFiles(proj.SrcDir, "*.cin", proj.SearchOpt))
                namespaces.AddRange(Parser.Parse(File.ReadAllText(item)));

            if (!Directory.Exists(proj.OutDir)) Directory.CreateDirectory(proj.OutDir);

            var BackendName = proj.Backend;

            var ad = GetArgsDict(args.Skip(1).ToArray());

            if (ad.ContainsKey("--backend")) BackendName = ad["--backend"];

            var BackendT = BackendManager.GetBackend(BackendName);

            Program.Builder = (IBuilder)Activator.CreateInstance(BackendT)!;

            var count = namespaces.Count;
            var compleated = 0;

            Console.WriteLine("\n\n\n\n\n\n\n");
            Console.CursorTop -= 7;

            Console.CursorVisible = false;

            foreach (var item in namespaces)
            {
                Console.CursorTop += 8;

                item.Generate(null!);

                if (!item.Module.TryVerify(out var error))
                    ErrorManager.Throw(ErrorType.Generation, $"The namespace \"{item.Name}\" failed to generate, with the LLVM error: {error}");

                Program.Builder.EmitToFile(Path.Combine(proj.OutDir, item.Name + ".o"), item.Module);

                Console.CursorTop -= 8;

                compleated++;

                ProgressBarManager.DrawBar(compleated, count);
                ProgressBarManager.SendMessage($"Compiled \"{item.Name}\" --> \"{item.Name}.o\"");
            }

            Console.CursorVisible = true;

            Console.CursorTop += 8;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Compiled {namespaces.Count} namespaces");
            Console.ResetColor();
        }

        static Dictionary<string,string> GetArgsDict(string[] args)
        {
            Dictionary<string, string> d = new();

            for (int i = 0; i < args.Length; i += 2) d.Add(args[i], args[i+1]);

            return d;
        }

        public static void PrintUsage()
        {
            Console.WriteLine("Usage: CinderLang <command> [arguments]");
            Console.WriteLine("Run 'CinderLang help [command]' for more info");
        }

        public static void PrintHelp(string command)
        {
            Console.WriteLine("build - - - - | Compiles a .ciproj file");
            Console.WriteLine("help  - - - - | Shows this message");
        }
    }
}
