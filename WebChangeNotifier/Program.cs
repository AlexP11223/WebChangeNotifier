using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WebChangeNotifier
{
    class Program
    {
        static Config ParseConfig(string filePath)
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
        }

        static void Main(string[] args)
        {
            var config = ParseConfig("config.json");
        } 
    }
}
