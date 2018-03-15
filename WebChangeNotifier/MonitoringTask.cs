using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenQA.Selenium;

namespace WebChangeNotifier
{
    public class MonitoringTask
    {
        public string Url { get; set; }

        public string Selector { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public SelectorType SelectorType { get; set; }

        public bool AllowEmptyContent { get; set; } = false;
        public bool SkipUntilNextCheckIfEmpty { get; set; } = false;

        [JsonIgnore]
        public string UrlDomain => new Uri(Url).Host;

        [JsonIgnore]
        public string Id => Url + Selector;

        public override string ToString()
        {
            return $"{nameof(Url)}: {Url}, {nameof(Selector)}: {Selector}, {nameof(SelectorType)}: {SelectorType}, {nameof(AllowEmptyContent)}: {AllowEmptyContent}, {nameof(SkipUntilNextCheckIfEmpty)}: {SkipUntilNextCheckIfEmpty}";
        }

        public By SeleniumSelector()
        {
            switch (SelectorType)
            {
                case SelectorType.CSS:
                    return By.CssSelector(Selector);
                case SelectorType.XPath:
                    return By.XPath(Selector);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}