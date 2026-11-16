using System.Threading;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace StreamToM3U.Utils
{
    public static class WebDriverHandler
    {
        static IWebDriver webDriver;
        static readonly Lock sync = new();

        public static bool IsWebDriverLocked { get; private set; }

        public static IWebDriver WebDriver
        {
            get
            {
                lock (sync)
                {
                    webDriver ??= CreateDriver();

                    return webDriver;
                }
            }
        }

        public static void CloseDriver() => webDriver?.Quit();

        public static void GainLock()
        {
            lock (sync)
            {
                while (IsWebDriverLocked)
                {
                    Thread.Sleep(1000);
                }

                IsWebDriverLocked = true;
            }
        }

        public static void ReleaseLock() => IsWebDriverLocked = false;

        static IWebDriver CreateDriver()
        {
            ChromeOptions options = new()
            {
                PageLoadStrategy = PageLoadStrategy.None
            };

            options.AddArgument("--silent");
			options.AddArgument("--disable-translate");
			options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-extensions");
            options.AddArgument("--disable-notifications");
            options.AddArgument("--disable-application-cache");
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1920,1080");
            options.AddArgument("--start-maximized");
            options.AddArgument("--blink-settings=imagesEnabled=false");

            ChromeDriverService service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;
            service.HideCommandPromptWindow = true;

            IWebDriver driver = new ChromeDriver(service, options);
            driver.Manage().Window.Maximize();

            return driver;
        }
    }
}
