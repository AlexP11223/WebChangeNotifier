using System;

namespace WebChangeNotifier
{
    public class Logger
    { 
        public static void Log(string text)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {text}");
        }
    }
}
