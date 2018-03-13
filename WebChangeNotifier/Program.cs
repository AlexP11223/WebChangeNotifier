using System;
using System.IO;
using System.Linq;

namespace WebChangeNotifier
{
    class Program
    {
        public static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;

        static void Main(string[] args)
        {
            Logger.Init($"{AppDir}logs{Path.DirectorySeparatorChar}{Logger.GenerateFileName()}.txt");

            new Worker(configFilePath: args.Any() ? args.First() : "config.json", 
                    stateFilePath:args.Length >= 2 ? args[1] : "state.json", 
                    profileDataDir:args.Length >= 3 ? args[2] : "browser_data\\")
                .Launch();
        } 
    }
}
