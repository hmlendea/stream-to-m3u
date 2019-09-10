using System.Text.RegularExpressions;
using System.Threading.Tasks;

using NuciExtensions;
using NuciWeb;
using OpenQA.Selenium;

using StreamToM3U.Service.Models;
using StreamToM3U.Utils;

namespace StreamToM3U.Service.Processors
{
    public sealed class AntenaPlayProcessor : IProcessor
    {
        static string HomeUrl => "https://antenaplay.ro";
        static string RegistrationUrl => $"{HomeUrl}/cont-nou";
        static string LogOutUrl => $"{HomeUrl}/logout";
        static string ChannelUrlFormat => $"{HomeUrl}/live/{{0}}";

        const string StreamUrlPattern = "streamURL: \"([^\"]*)\"";

        readonly IWebProcessor webProcessor;

        public AntenaPlayProcessor()
        {
            this.webProcessor = new WebProcessor(WebDriverHandler.WebDriver);
        }

        public Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            string url = string.Format(ChannelUrlFormat, streamInfo.ChannelId);

            RegisterAccount();
            webProcessor.GoToUrl(url);
            
            string playlistUrl = GetStreamUrlFromPageSource();

            LogOut();
            webProcessor.Dispose();

            return Task.FromResult(playlistUrl);
        }

        string GetStreamUrlFromPageSource()
        {
            By startStreamButtonSelector = By.Id("start-video");

            webProcessor.Click(startStreamButtonSelector);

            string html = webProcessor.GetPageSource();
            string streamUrl = Regex.Match(html, StreamUrlPattern).Groups[1].Value;

            return streamUrl;
        }

        void RegisterAccount()
        {
            By acceptGdprButtonSelector = By.XPath("/html/body/div[1]/div[2]/div[4]/div[2]/div/button");
            By emailInputSelector = By.Name("email");
            By passwordInputSelector = By.Name("password");
            By firstNameInputSelector = By.Name("firstname");
            By lastNameInputSelector = By.Name("lastname");
            By tosCheckboxSelector = By.Id("agree");
            By submitButtonSelector = By.XPath(@"//form/button");
            By smsValidationButtonSelector = By.Id("js-btn-sms");

            webProcessor.GoToUrl(RegistrationUrl);

            webProcessor.Click(acceptGdprButtonSelector);

            webProcessor.SetText(emailInputSelector, GenerateRandomEmail());
            webProcessor.SetText(passwordInputSelector, GenerateRandomString());
            webProcessor.SetText(firstNameInputSelector, GenerateRandomName());
            webProcessor.SetText(lastNameInputSelector, GenerateRandomName());

            webProcessor.UpdateCheckbox(tosCheckboxSelector, true);
            
            webProcessor.Click(submitButtonSelector);

            webProcessor.WaitForElementToBeVisible(smsValidationButtonSelector);
        }

        void LogOut()
        {
            webProcessor.GoToUrl(LogOutUrl);
        }

        string GenerateRandomEmail()
            => GenerateRandomString() + "@gmail.com";

        string GenerateRandomName()
            => GenerateRandomString().ToSentanceCase();

        string GenerateRandomString()
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
