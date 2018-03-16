using System;
using System.Linq;
using WebChangeNotifier.Helpers;
using static System.IO.Path;

namespace WebChangeNotifier
{
    class Program
    {
        public static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/') + DirectorySeparatorChar;

        static void Main(string[] args)
        {
            Logger.Init($"{AppDir}logs{DirectorySeparatorChar}{Logger.GenerateFileName()}.txt");

            var worker = new Worker(configFilePath: args.Any() ? args.First() : "config.json",
                stateFilePath: args.Length >= 2 ? args[1] : "state.json",
                profileDataDir: args.Length >= 3 ? args[2] : $"browser_data{DirectorySeparatorChar}",
                errorDataDir: $"{AppDir}logs{DirectorySeparatorChar}errors{DirectorySeparatorChar}");

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
