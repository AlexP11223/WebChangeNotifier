using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using RestSharp;
using RestSharp.Authenticators;

namespace WebChangeNotifier
{
    class Program
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

        static readonly Random Rand = new Random();

        // ReSharper disable once RedundantDefaultMemberInitializer
        static int _errorsCount = 0;

        // ReSharper disable once RedundantDefaultMemberInitializer
        static int _runCount = 0;

        private static WebsitesStateContainer _stateContainer;

        static void Log(string text)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss")}] {text}");
        }

        static Config ParseConfig(string filePath)
        {
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(filePath));
        }

        static void SendEmail(string text, MailgunSettings emailSettings)
        {
            Log("Sending email: " + text);

            var client = new RestClient
            {
                BaseUrl = new Uri("https://api.mailgun.net/v3"),
                Authenticator = new HttpBasicAuthenticator("api", emailSettings.ApiKey)
            };
            var request = new RestRequest();
            request.AddParameter("domain", emailSettings.Domain, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", emailSettings.From);
            request.AddParameter("to", emailSettings.To);
            request.AddParameter("subject", "WebChangeNotifier");
            request.AddParameter("text", text);
            request.Method = Method.POST;

            var response = client.Execute(request);

            Log(response.Content);
        }

        static RemoteWebDriver _webDriver;
        static RemoteWebDriver WebDriver
        {
            get
            {
                if (_webDriver == null)
                {
                    var service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    _webDriver = new ChromeDriver(service);

                    _webDriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
                    _webDriver.Manage().Window.Size = new Size(1600, 900);
                }
                return _webDriver;
            }
        }

        static void DestroyWebDriver()
        {
            if (_webDriver != null)
            {
                try
                {
                    _webDriver.Dispose();
                }
                catch (Exception ex)
                {
                    Log($"Destroy failed {ex}");
                }
                _webDriver = null;
            } 
        }

        static void Process(MonitoringTask task, Config config)
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

                _stateContainer.Set(task, text);

                SendEmail($"Detected changes on {task.Url}", config.MailgunSettings);
            }
        }

        static void Run(Config config)
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
                        Process(task, config);
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
                if (_errorsCount > 2)
                {
                    SendEmail("Error\r\n\r\n" + String.Join("\r\n\r\n", errors), config.MailgunSettings);
                    _errorsCount = 0;
                }

                DestroyWebDriver();
            }
            else
            {
                _errorsCount = 0;
            }
        }

        static void Main(string[] args)
        {
            var config = ParseConfig(args.Any() ? args.First() : "config.json");
            _stateContainer = new WebsitesStateContainer(args.Length >= 2 ? args[1] : "state.json");

            while (true)
            {
                Run(config);

                Log("Pause");

                Thread.Sleep(config.Delay * 1000);
            }
            // ReSharper disable once FunctionNeverReturns
        } 
    }
}
