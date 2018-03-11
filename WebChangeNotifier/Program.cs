using System;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
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

        static void Log(string text)
        {
            Console.WriteLine($"[{DateTime.Now.ToString("HH:mm:ss tt")}] {text}");
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

        static void Process(MonitoringTask task, Config config)
        {
            Log($"Running {task.UrlDomain}");


        }

        static void Run(Config config)
        {
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
            }
            else
            {
                _errorsCount = 0;
            }
        }

        static void Main(string[] args)
        {
            var config = ParseConfig(args.Any() ? args.First() : "config.json");

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
