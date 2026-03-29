using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang
{
    public static class Preprocessor
    {
        public static string Process(string code)
        {
            return code.Replace(Environment.NewLine," ").Replace("\t","").Replace("(", " (").Replace(")", ") ");
        }
    }
}
