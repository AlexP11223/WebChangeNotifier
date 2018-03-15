using System;
using System.IO;
using System.Linq;
using WebChangeNotifier.Helpers;

namespace WebChangeNotifier
{
    class Program
    {
        public static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/') + Path.DirectorySeparatorChar;

        static void Main(string[] args)
        {
            Logger.Init($"{AppDir}logs{Path.DirectorySeparatorChar}{Logger.GenerateFileName()}.txt");

            var worker = new Worker(configFilePath: args.Any() ? args.First() : "config.json",
                stateFilePath: args.Length >= 2 ? args[1] : "state.json",
                profileDataDir: args.Length >= 3 ? args[2] : "browser_data\\");

            ConsoleExitDetector.ExitHandler += sig =>
            {
                worker?.Dispose();

                return true;
            };

            using (worker)
            {
                worker.Launch();
            }
        } 
    }
}
