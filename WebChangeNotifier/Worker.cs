﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using WebChangeNotifier.Helpers;
using static WebChangeNotifier.Logger;

namespace WebChangeNotifier
{
    public class Worker : IDisposable
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

        private readonly Differ _differ = new Differ();

        private RemoteWebDriver _webDriver;

        private readonly string _profileDataDir;

        // ReSharper disable once RedundantDefaultMemberInitializer
        private int _errorsCount = 0;

        // ReSharper disable once RedundantDefaultMemberInitializer
        private int _runCount = 0;

        private readonly Dictionary<string, int> _tasksConsecutiveEmptyCounts;

        public Worker(string configFilePath, string stateFilePath, string profileDataDir)
        {
            _profileDataDir = profileDataDir;
            _config = ParseConfig(configFilePath);
            _stateContainer = new WebsitesStateContainer(stateFilePath);
            _mailer = new MailgunSender(_config.MailgunSettings);

            _tasksConsecutiveEmptyCounts = _config.Tasks.ToDictionary(t => t.Id, t => 0);
        }

        public void Launch()
        {
            Log("Started");

            while (true)
            {
                Run(_config);

                Log("Pause");

                Thread.Sleep(_config.Delay * 1000);
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public void Dispose()
        {
            DestroyWebDriver();
        }

        ~Worker()
        {
            Dispose();
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

                    var options = new ChromeOptions();
                    options.AddArguments($"--user-data-dir={_profileDataDir}");

                    _webDriver = new ChromeDriver(service, options);

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

        private string LoadData(MonitoringTask task)
        {
            var webDriver = WebDriver;

            webDriver.Url = task.Url;

            WaitHelper.WaitUntil(webDriver, TimeSpan.FromSeconds(60), 
                ExpectedConditions.ElementExists(task.SeleniumSelector()), 
                $"Element '{task.Selector}' not found");

            if (!task.AllowEmptyContent)
            {
                try
                {
                    WaitHelper.WaitUntil(webDriver, TimeSpan.FromSeconds(20),
                        d => !String.IsNullOrWhiteSpace(d.FindElement(task.SeleniumSelector()).Text),
                        $"Element '{task.Selector}' is empty");
                }
                catch (WebDriverTimeoutException ex)
                {
                    Log(ex.Message);
                }
            }

            Thread.Sleep(3000);

            var element = webDriver.FindElement(task.SeleniumSelector());
            return element.Text.Trim();
        }

        private void Process(MonitoringTask task)
        {
            Log($"Running {task.UrlDomain}");

            string text = LoadData(task);

            if (!task.AllowEmptyContent)
            {
                _tasksConsecutiveEmptyCounts[task.Id] = String.IsNullOrWhiteSpace(text) ? _tasksConsecutiveEmptyCounts[task.Id] + 1 : 0;

                if (String.IsNullOrWhiteSpace(text) &&
                    !_stateContainer.Matches(task, text)) // not already recorded this as change (optimization to avoid unnecessary delays)
                {
                    text = LoadData(task);

                    // still empty and need to wait until the next check before reporting
                    if (String.IsNullOrWhiteSpace(text) && task.SkipUntilNextCheckIfEmpty && _tasksConsecutiveEmptyCounts[task.Id] < 2)
                    {
                        return;
                    }
                }
            }

            if (!_stateContainer.Matches(task, text))
            {
                Log("Changed.");

                string before = _stateContainer.Get(task);

                var diff = _differ.Diff(before, text);

                _stateContainer.Set(task, text);

                _mailer.Send($"Detected changes on {task.Url}\r\n{diff.InsertedCount} +, {diff.DeletedCount} -", 
                    new []{new MailAttachment($"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}_{task.UrlDomain.Replace("www", "").Replace(".", "")}.diff", diff.DiffTextWithStats)});
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
