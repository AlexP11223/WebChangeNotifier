using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace WebChangeNotifier.Helpers
{
    public static class WaitHelper
    {
        public static TResult WaitUntil<TResult>(IWebDriver webDriver, TimeSpan timeout, Func<IWebDriver, TResult> condition, string message = "")
        {
            var wait = new WebDriverWait(webDriver, timeout);
            if (!String.IsNullOrEmpty(message))
                wait.Message = message;

            return wait.Until(condition);
        }

        public static TResult WaitUntil<TResult>(IWebDriver webDriver, int timeoutSec, Func<IWebDriver, TResult> condition, string message = "")
        {
            return WaitUntil(webDriver, TimeSpan.FromSeconds(timeoutSec), condition, message);
        }
    }
}
