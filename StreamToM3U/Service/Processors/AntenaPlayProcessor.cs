using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NuciExtensions;
using NuciWeb;
using NuciWeb.Automation;
using NuciWeb.Automation.Selenium;
using OpenQA.Selenium;

using StreamToM3U.Service.Models;
using StreamToM3U.Utils;

namespace StreamToM3U.Service.Processors
{
    public sealed class AntenaPlayProcessor : IProcessor
    {
        static bool acceptedGdpr = false;

        static string HomeUrl => "https://antenaplay.ro";
        static string RegistrationUrl => $"{HomeUrl}/cont-nou";
        static string LogOutUrl => $"{HomeUrl}/logout";
        static string ChannelUrlFormat => $"{HomeUrl}/live/{{0}}";

        const string StreamUrlPattern = "streamURL: \"([^\"]*)\"";

        readonly IWebProcessor webProcessor = new SeleniumWebProcessor(WebDriverHandler.WebDriver);

        public Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            WebDriverHandler.GainLock();

            string url = string.Format(ChannelUrlFormat, streamInfo.ChannelId);

            RegisterAccount();
            webProcessor.GoToUrl(url);

            string playlistUrl = GetStreamUrlFromPageSource();

            ClearResources();
            return Task.FromResult(playlistUrl);
        }

        string GetStreamUrlFromPageSource()
        {
            string startStreamButtonSelector = Select.ById("start-video");

            webProcessor.Click(startStreamButtonSelector);

            string html = webProcessor.GetPageSource();
            return Regex.Match(html, StreamUrlPattern).Groups[1].Value;
        }

        void RegisterAccount()
        {
            string emailInputSelector = Select.ByName("email");
            string passwordInputSelector = Select.ByName("password");
            string firstNameInputSelector = Select.ByName("firstname");
            string lastNameInputSelector = Select.ByName("lastname");
            string tosCheckboxSelector = Select.ById("agree");
            string submitButtonSelector = Select.ByXPath(@"//form/button");
            string smsValidationButtonSelector = Select.ById("js-btn-sms");

            webProcessor.GoToUrl(RegistrationUrl);

            AcceptGdpr();

            webProcessor.SetText(emailInputSelector, GenerateRandomEmail());
            webProcessor.SetText(passwordInputSelector, GenerateRandomString());
            webProcessor.SetText(firstNameInputSelector, GenerateRandomName());
            webProcessor.SetText(lastNameInputSelector, GenerateRandomName());

            webProcessor.UpdateCheckbox(tosCheckboxSelector, true);

            webProcessor.Click(submitButtonSelector);

            webProcessor.WaitForElementToBeVisible(smsValidationButtonSelector);
        }

        void AcceptGdpr()
        {
            if (acceptedGdpr)
            {
                return;
            }

            string acceptGdprButtonSelector = Select.ByXPath("/html/body/div[1]/div[2]/div[4]/div[2]/div/button");

            webProcessor.WaitForElementToBeVisible(acceptGdprButtonSelector);
            webProcessor.Click(acceptGdprButtonSelector);

            acceptedGdpr = true;
        }

        void ClearResources()
        {
            webProcessor.GoToUrl(LogOutUrl);
            webProcessor.Dispose();
            WebDriverHandler.ReleaseLock();
        }

        static string GenerateRandomEmail()
            => GenerateRandomString() + "@gmail.com";

        static string GenerateRandomName()
            => GenerateRandomString().ToSentenceCase();

        static string GenerateRandomString()
        {
            const int length = 10;
            const string chars = "abcdefghijklmnopqrstuvwxyz";

            string result = string.Empty;

            for (int i = 0; i < length; i++)
            {
                result += chars.GetRandomElement();
            }

            return result;
        }
    }
}
