using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinderLang
{
    public static class ProgressBarManager
    {
        static int psize = 0;
        static int strcurloc = 0;
        static int pcw = Console.WindowWidth - 1;

        static LinkedList<string> Messages = new();

        public static void DrawBar(int compleated, int total)
        {
            var w = Console.WindowWidth;

            var percent = (int)(((float)compleated / (float)total) * 100f);
            var cstr = $" {percent}% ({compleated}/{total})";

            w -= cstr.Length;

            if (w <= 0) ErrorManager.Throw(ErrorType.Cli, "Terminal too small");

            if (Console.WindowWidth - 1 != pcw) return;

            try
            {
                if (psize != w)
                {
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(new string('-', w));
                    Console.ResetColor();

                    strcurloc = Console.CursorLeft;
                    Console.Write(cstr);

                    Console.CursorLeft -= Math.Min(Math.Max(pcw, 1), Console.WindowWidth - 1);
                    psize = w;
                }

                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.ForegroundColor = ConsoleColor.DarkBlue;

                int pelem = (int)(((float)percent / 100f) * w);

                Console.Write(new string('#', pelem));
                Console.ResetColor();

                Console.CursorLeft = strcurloc;
                Console.Write(cstr);
                Console.CursorLeft -= Math.Min(Math.Max(pcw, 1), Console.WindowWidth - 1);
            }
            catch { }
        }

        public static void SendMessage(string message)
        {
            Messages.AddFirst(message);

            if (Messages.Count > 5) Messages.RemoveLast();

            var cl = Console.CursorLeft;
            Console.CursorTop += 2;

            Console.ForegroundColor = ConsoleColor.DarkCyan;

            foreach (var item in Messages)
            {
                ClearRow();
                Console.CursorLeft = 1;
                Console.WriteLine(item);
            }

            Console.CursorTop -= Messages.Count + 2;

            Console.ResetColor();
        }

        static void ClearRow()
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', Console.WindowWidth));
            Console.CursorLeft = 0;
        }
    }
}
