using BackendInterface;
using CinderLang.AstNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace CinderLang
{
    public static class CLIManager
    {
        static bool NoGraphics = false;

        public static string Aname = Path.GetFileNameWithoutExtension(Environment.ProcessPath)!;

        public static readonly Dictionary<string, (string desc, string usage, int argcount)> ValidArguments = new()
        {
            {"--no-graphics",("disables all compiler graphics","",0)},
            {"--saveir",("dumps the generated ir code to the disk","",0)},
            {"--backend",("selects a compilation backend","<backend name>",1)},
        };

        public static void ManageCommands(params string[] args)
        {
            NoGraphics = args.Contains("--no-graphics");

            if (!NoGraphics)
            {
                try
                {
                    DrawCompilerHead.DrawHead();
                }
                catch { NoGraphics = true; }
            }

            if (args.Length == 0) { PrintUsage(); return; }

            var cmd = args[0];

            switch (cmd) 
            {
                case "help": PrintHelp(args.Length > 1 ? args[1] : ""); return;
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

            bool saveir = ad.ContainsKey("--saveir");

            if (ad.ContainsKey("--backend")) BackendName = ad["--backend"][0];

            var BackendT = BackendManager.GetBackend(BackendName);

            Program.Builder = (IBuilder)Activator.CreateInstance(BackendT)!;

            var count = namespaces.Count;
            var compleated = 0;

            if (!NoGraphics) 
            {
                Console.WriteLine("\n\n\n\n\n\n\n\n\n\n");
                Console.CursorTop -= 11;
            }

            Console.CursorVisible = false;

            foreach (var item in namespaces)
            {
                if (!NoGraphics) Console.CursorTop += 8;

                item.Generate(null!);

                if (!item.Module.TryVerify(out var error))
                {
                    Console.WriteLine(item.Module.PrintToString());
                    ErrorManager.Throw(ErrorType.Generation, $"The namespace \"{item.Name}\" failed to generate, with the LLVM error: {error}");
                }

                if (saveir) File.WriteAllText(Path.Combine(proj.OutDir, item.Name + ".ir"), item.Module.PrintToString());

                Program.Builder.EmitToFile(Path.Combine(proj.OutDir, item.Name + ".o"), item.Module);

                if (!NoGraphics) Console.CursorTop -= 8;

                compleated++;

                if (!NoGraphics)
                {
                    ProgressBarManager.DrawBar(compleated, count);
                    ProgressBarManager.SendMessage($"Compiled \"{item.Name}\" --> \"{item.Name}.o\"");
                }
            }

            Console.CursorVisible = true;

            if (!NoGraphics) Console.CursorTop += 8;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Compiled {namespaces.Count} namespaces");
            Console.ResetColor();
        }

        static Dictionary<string, string[]> GetArgsDict(string[] args)
        {
            Dictionary<string, string[]> d = new();

            for (int i = 0; i < args.Length; i ++)
            {
                if (ValidArguments.ContainsKey(args[i]))
                {
                    var arg = ValidArguments[args[i]];

                    if (i + arg.argcount >= args.Length) 
                        ErrorManager.Throw(ErrorType.Cli,$"Invalid parameter count for argument {args[i]}");

                    d.Add(args[i],args[i..(i + arg.argcount)]);

                    i += arg.argcount;
                }
            }

            return d;
        }

        public static void PrintUsage()
        {
            Console.WriteLine($"Usage: {Aname} <command> [arguments]");
            Console.WriteLine($"Run '{Aname} help [command]' for more info");
        }

        public static void PrintHelp(string command)
        {
            int longest = 0;

            foreach (var item in ValidArguments)
            {
                var ns = $"{item.Key} {item.Value.usage}".Trim();
                if (ns.Length > longest) longest = ns.Length;
            }

            longest += 5;

            void printmsg(string msg, string desc)
            {
                var cspace = longest % 2 == msg.Length % 2 ? 0 : 1;

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write($"{msg}{new string(' ', cspace)}{Dash(new string(' ', longest - (msg.Length + cspace)))}");

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.Write(" | ");

                Console.ResetColor();
                Console.WriteLine(desc);
            }

            switch (command)
            {
                case "build":
                    Console.WriteLine($"{Aname} build <project> [args]");
                    Console.WriteLine();

                    foreach (var item in ValidArguments)
                    {
                        var ns = $"{item.Key} {item.Value.usage}".Trim();

                        printmsg(ns,item.Value.desc);
                    }

                    break;
                default:

                    printmsg("build", "Compiles a .ciproj file");
                    printmsg("help",  "Shows this message");
                    break;
            }
        }

        static string Dash(string input)
        {
            var sb = new StringBuilder(input);
            int spaceCount = 0;

            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == ' ')
                {
                    spaceCount++;
                    if (spaceCount % 2 == 0) sb[i] = '·';
                }
            }

            return sb.ToString();
        }
    }
}
