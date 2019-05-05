using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SocialMediaStreamToM3U.Utils
{
    public static class WebDriverHandler
    {
        static IWebDriver webDriver;

        public static IWebDriver WebDriver
        {
            get
            {
                if (webDriver is null)
                {
                    webDriver = CreateDriver();
                }

                return webDriver;
            }
        }

        static IWebDriver CreateDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.PageLoadStrategy = PageLoadStrategy.None;
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
