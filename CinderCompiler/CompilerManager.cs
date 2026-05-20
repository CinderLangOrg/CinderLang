using BackendInterface;
using CinderLang;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderCompiler
{
    public static class CompilerManager
    {
        public static void SetBuilder(IBuilder builder) => Program.Builder = builder;
        public static void EmitToFile(string path, IModule module) => Program.Builder.EmitToFile(path,module);
    }
}
