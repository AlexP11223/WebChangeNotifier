using System;
using System.Collections.Generic;

namespace WebChangeNotifier
{
    public class Config
    {
        public List<MonitoringTask> Tasks { get; set; }
        public MailgunSettings MailgunSettings { get; set; }
        public int Delay { get; set; }

        public override string ToString()
        {
            return $"{nameof(Tasks)}: {String.Join(", ", Tasks)}, {nameof(MailgunSettings)}: {MailgunSettings}, {nameof(Delay)}: {Delay}";
        }
    }
}