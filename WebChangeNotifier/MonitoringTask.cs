using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WebChangeNotifier
{
    public class MonitoringTask
    {
        public string Url { get; set; }

        public string Selector { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SelectorType SelectorType { get; set; }

        [JsonIgnore]
        public string UrlDomain => new Uri(Url).Host;

        public override string ToString()
        {
            return $"{nameof(Url)}: {Url}, {nameof(Selector)}: {Selector}, {nameof(SelectorType)}: {SelectorType}";
        }
    }
}