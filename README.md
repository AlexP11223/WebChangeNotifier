# WebChangeNotifier

C#/.NET console Windows app for website content changes detection. Sends notifications to email (via [Mailgun HTTP API](https://www.mailgun.com), it is free for up to 10 000 emails/month, credit card or domain are not needed).

The project was created because I wanted to monitor changes on several websites (product lists in local stores) and all services/apps I found did not have the features I needed or allowed only 1 check per day in free version.

It periodically opens all URLs in the list (in random order) and retrieves content (as text) using the specified selector. If the content changed since the last time, then it sends email notification with number of inserted/deleted lines and **DIFF** text file. Also it sends notifications about errors (if repeated more than once in a row) such as when the website is down or the content is not found.

Binary (\*.exe) can be downloaded in [Releases](https://github.com/AlexP11223/WebChangeNotifier/releases). Targets .NET Framework 4.5.2 (probably can be built with earlier versions, but you may need to reinstall some NuGet packages). 

## Requirements

Uses Chrome via Selenium, requires **chromedriver** from https://sites.google.com/a/chromium.org/chromedriver/downloads in the app dir or in PATH (it is included in the release and added via [this NuGet package](https://github.com/AlexP11223/nupkg-selenium-webdrivers) in the project, but it may become outdated).

Visual Studio 2017 is required for building from source code.

# Configuration

See `config.example.json` [in the source code](https://github.com/AlexP11223/WebChangeNotifier/blob/master/WebChangeNotifier/config.example.json) or `config.json` in the release. Basically it just needs URL and CSS or XPath selector for each task, and Mailgun settings. 

Some notable properties:

- Global
  - **Delay** - pause in seconds before repeating. That is it runs all tasks and then pauses for this amount of time.
  - **BrowserRestartPeriod** (default `100`) - how often the web browser will be restarted. That is if set to 100, it will restart on every 100th iteration. 0 disables restarts. It was implemented because using the same instance of web browser/WebDriver for a very long time in some cases may result in significant memory leaks or performance issues.
- Tasks
  - **MinDelay** (optional) - can be used to slowdown some tasks (to avoid bans, captchas etc.). For example, if `Delay` is 300 and `MinDelay` for one task is 900, then this task will skip some checks while 900 seconds are not elapsed since its' last check.
  - **AllowEmptyContent** and **SkipUntilNextCheckIfEmpty** (optional, default `false`) - `AllowEmptyContent` determines behavior when the specified element is empty (no text). If `false`, then it waits up to 20 sec and reloads the page if it is still empty before reporting this change (if needed), if `SkipUntilNextCheckIfEmpty` is also `true` then it reports such change only when it happens during two consecutive checks of the tasks. If `true`, then it just reports the change as normally.
The former behavior was needed for one website that loads the content via ajax, sometimes (every few hours) the content was empty for some reason (so I would get a report about empty content and then in the next check another report that everything returned back), however for some tasks empty content can be totally normal and expected. 

Paths for config, state files and directory to store browser data can be passed via cmd parameters. By default they are `config.json`, `state.json` and `browser_data\` (relative to the working dir).

The app does not support parallel handling of the tasks, but it should be possible to run multiple instances of the app with different config, state and browser data paths.

# Usage

Since it uses Chrome, it may not be convenient to use on the main PC, because sometimes it may show the window or restart this browser instance. I used it on a virtual machine (VirtualBox, VMWare Player, ...).

An alternative could be to use headless PhantomJS but I had some issues with it in the past and it is more difficult to debug and monitor, so I decided to take Chrome since I would use the VM anyway. 

Feel free to modify it :) Changing web driver should not be difficult, the only Chrome-specific thing here is `--user-data-dir` but I added it only because one website started to show captcha on first visit with fresh profile.

## State

State of the tasks (content retrieved in the last successful check and date/time for `MinDelay`) is saved to `state.json` by default, so you can restart the app/machine and continue with the last state. If you want to reset the state (for example if you have not used the app for a long time and don't care about changes since the last run), you can just delete the file. State for each task is identified by **URL + selector**, so if you changed any of these for some task, the state for this task will reset.

# Troubleshooting

It outputs log to the console window and to text files in `logs` directory in the app directory. Also it saves web page screenshot and HTML to `logs/errors` when encountered any error.
