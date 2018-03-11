using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace WebChangeNotifier
{
    public class WebsitesStateContainer
    {
        private readonly string _filePath;
        private Dictionary<string, string> _state = new Dictionary<string, string>();

        public WebsitesStateContainer(string filePath)
        {
            _filePath = filePath;

            LoadState();
        }

        public bool Matches(MonitoringTask task, string data)
        {
            string d;
            bool found = _state.TryGetValue(task.Id, out d);

            if (!found)
            {
                Set(task, data);
            }

            return !found || d == data;
        }

        public void Set(MonitoringTask task, string data)
        {
            _state[task.Id] = data;

            SaveState();
        }

        private void SaveState()
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(_state, Formatting.Indented));
        }

        private void LoadState()
        {
            if (File.Exists(_filePath))
            {
                _state = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(_filePath));
            }
        }
    }
}
