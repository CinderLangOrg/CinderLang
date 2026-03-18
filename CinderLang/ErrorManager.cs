using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang
{
    public enum ErrorType
    {
        Syntax = 2,
        Generation = 3,
        Cli = 4,
        Backend = 5,
    }

    public static class ErrorManager
    {
        public static void Throw(ErrorType type,string message)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed; 
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"{type} error:");
            Console.ResetColor();

            Console.WriteLine(" "+message);
            Environment.Exit((int)type);
        }
    }
}
