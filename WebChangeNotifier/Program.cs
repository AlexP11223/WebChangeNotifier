using System.Linq;

namespace WebChangeNotifier
{
    class Program
    {
        static void Main(string[] args)
        {
            new Worker(configFilePath: args.Any() ? args.First() : "config.json", 
                    stateFilePath:args.Length >= 2 ? args[1] : "state.json")
                .Launch();
        } 
    }
}
