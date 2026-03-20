using System.Buffers.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using BackendInterface;

namespace CinderLang
{
    internal class Program
    {
        public static IBuilder Builder { get; set; }

        static void Main(string[] args)
        {
            string[] patterns;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                patterns = new[] { "*.dll" };
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                patterns = new[] { "*.dll", "*.so" };
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                patterns = new[] { "*.dll", "*.dylib", "*.so" };
            else
                patterns = Array.Empty<string>();

            if (!Directory.Exists("backends")) Directory.CreateDirectory("backends");

            foreach (var item in Directory.GetDirectories("backends"))
            {
                foreach (var pattern in patterns)
                {
                    foreach (var dll in Directory.GetFiles(item, pattern, SearchOption.AllDirectories))
                        try 
                        {
                            Assembly.LoadFrom(dll);
                        } 
                        catch 
                        {
                            NativeLibrary.Load(dll);
                        }
                }
            }

            CLIManager.ManageCommands(args);
        }
    }
}
