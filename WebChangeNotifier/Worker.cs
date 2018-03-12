using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using static WebChangeNotifier.Logger;

namespace WebChangeNotifier
{
    class Worker
    {
        class ProcessErrorStatus
        {
            public Exception Error { get; }
            public MonitoringTask Task { get; }
            public bool IsSuccess => Error == null;

            private ProcessErrorStatus(Exception error, MonitoringTask task)
            {
                Error = error;
                Task = task;
            }

            public override string ToString() => $"{Task.Url}: " + (IsSuccess ? "ok" : Error.ToString());

            public static ProcessErrorStatus Fail(Exception error, MonitoringTask task) => new ProcessErrorStatus(error, task);
            public static ProcessErrorStatus Success(MonitoringTask task) => new ProcessErrorStatus(null, task);
        }

        private static readonly Random Rand = new Random();

        private readonly Config _config;

        private readonly WebsitesStateContainer _stateContainer;

        private readonly MailgunSender _mailer;

        private RemoteWebDriver _webDriver;

        // ReSharper disable once RedundantDefaultMemberInitializer
        private int _errorsCount = 0;

        // ReSharper disable once RedundantDefaultMemberInitializer
        private int _runCount = 0;

        public Worker(string configFilePath, string stateFilePath)
        {
            _config = ParseConfig(configFilePath);
            _stateContainer = new WebsitesStateContainer(stateFilePath);
            _mailer = new MailgunSender(_config.MailgunSettings);
        }

        public void Launch()
        {
            while (true)
            {
                Run(_config);

                Log("Pause");

                Thread.Sleep(_config.Delay * 1000);
            }
            // ReSharper disable once FunctionNeverReturns 
        }

        private RemoteWebDriver WebDriver
        {
            get
            {
                if (_webDriver == null)
                {
                    Log("Starting web driver");

                    var service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    _webDriver = new ChromeDriver(service);

                    _webDriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                    _webDriver.Manage().Window.Size = new Size(1600, 900);
                }
                return _webDriver;
            }
        }

        private void DestroyWebDriver()
        {
            if (_webDriver != null)
            {
                Log("Destroying web driver");

                try
                {
                    _webDriver.Close();
                    _webDriver.Dispose();
                }
                catch (Exception ex)
                {
                    Log($"Destroy failed {ex}");
                }
                _webDriver = null;
            }
        }

        private Config ParseConfig(string filePath)
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
        }

        private string Diff(string before, string after)
        {
            var diffBuilder = new InlineDiffBuilder(new Differ());
            var diff = diffBuilder.BuildDiffModel(before, after);

            return String.Join("\r\n", diff.Lines.Select(line =>
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        return "+ " + line.Text;
                    case ChangeType.Deleted:
                        return "- " + line.Text;
                    default:
                        return "  " + line.Text;
                }
            }));
        }

        private void Process(MonitoringTask task)
        {
            Log($"Running {task.UrlDomain}");

            var webDriver = WebDriver;

            webDriver.Url = task.Url;

            Thread.Sleep(3000);

            var element = webDriver.FindElement(task.SeleniumSelector());
            string text = element.Text.Trim();

            if (!_stateContainer.Matches(task, text))
            {
                Log("Changed.");

                string before = _stateContainer.Get(task);

                string diff = Diff(before, text);

                _stateContainer.Set(task, text);

                _mailer.Send($"Detected changes on {task.Url}", 
                    new []{new MailAttachment($"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}_{task.UrlDomain.Replace("www", "").Replace(".", "")}.diff", diff)});
            }
        }

        private void Run(Config config)
        {
            if (_runCount++ > 100)
            {
                DestroyWebDriver();
            }

            var results = config.Tasks
                .OrderBy(t => Rand.Next())
                .Select(task =>
                {
                    try
                    {
                        Process(task);
                    }
                    catch (Exception ex)
                    {
                        Log(ex.ToString());

                        return ProcessErrorStatus.Fail(ex, task);
                    }

                    return ProcessErrorStatus.Success(task);
                });

            var errors = results.Where(s => !s.IsSuccess).ToList();
            if (errors.Any())
            {
                _errorsCount++;
                if (_errorsCount > 1)
                {
                    DestroyWebDriver();
                }
                if (_errorsCount > 2)
                {
                    _mailer.Send("Error\r\n\r\n" + String.Join("\r\n\r\n", errors));
                    _errorsCount = 0;
                }
            }
            else
            {
                _errorsCount = 0;
            }
        }
    }
}
