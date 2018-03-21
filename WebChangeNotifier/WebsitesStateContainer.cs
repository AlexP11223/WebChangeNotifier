using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Newtonsoft.Json;

namespace WebChangeNotifier
{
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local")]
    public class WebsiteState
    {
        public WebsiteState(string data, DateTime lastUpdateTime)
        {
            Data = data;
            LastUpdateTime = lastUpdateTime;
        }

        public string Data { get; private set; }
        public DateTime LastUpdateTime { get; private set; }

        public override string ToString() => $"{nameof(LastUpdateTime)}: {LastUpdateTime}, {nameof(Data)}: {Data}";

        public static WebsiteState CreateNew(string data) => new WebsiteState(data, DateTime.Now);
    }

    public class WebsitesStateContainer
    {
        private readonly string _filePath;
        private Dictionary<string, WebsiteState> _state = new Dictionary<string, WebsiteState>();

        public WebsitesStateContainer(string filePath)
        {
            _filePath = filePath;

            LoadState();
        }

        public bool Matches(MonitoringTask task, string data)
        {
            bool found = _state.TryGetValue(task.Id, out var it);

            if (!found)
            {
                Set(task, data);
                return true;
            }

            return it.Data == data;
        }

        public WebsiteState Get(MonitoringTask task)
        {
            return _state[task.Id];
        }

        public WebsiteState GetOrDefault(MonitoringTask task)
        {
            if (_state.ContainsKey(task.Id))
            {
                return _state[task.Id];
            }
            return new WebsiteState(null, DateTime.MinValue);
        }

        public void Set(MonitoringTask task, string data)
        {
            _state[task.Id] = WebsiteState.CreateNew(data);

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
                _state = JsonConvert.DeserializeObject<Dictionary<string, WebsiteState>>(File.ReadAllText(_filePath));
            }
        }

        public void UpdateTime(MonitoringTask task)
        {
            Set(task, Get(task).Data);
        }
    }
}
