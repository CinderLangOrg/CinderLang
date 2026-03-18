using System.Buffers.Text;
using System.Text;
using BackendInterface;
using LLVMBackend;
using LLVMSharp;
using LLVMSharp.Interop;

namespace CinderLang
{
    internal class Program
    {
        public static IBuilder Builder { get; set; }

        static void Main(string[] args) => CLIManager.ManageCommands(args);
    }
}
