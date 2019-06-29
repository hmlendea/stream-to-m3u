using System;
using System.Text.RegularExpressions;

using NuciExtensions;
using NuciWeb;
using OpenQA.Selenium;

using StreamToM3U.Net;
using StreamToM3U.Utils;

namespace StreamToM3U.Processors
{
    public sealed class AntenaPlayProcessor : WebProcessor, IAntenaPlayProcessor
    {
        static string HomeUrl => "https://antenaplay.ro";
        static string RegistrationUrl => $"{HomeUrl}/cont-nou";
        static string ChannelUrlFormat => $"{HomeUrl}/live/{{0}}";

        const string StreamUrlPattern = "streamURL: \"([^\"]*)\"";

        readonly IFileDownloader downloader;

        public AntenaPlayProcessor(IFileDownloader downloader)
            : base(WebDriverHandler.WebDriver)
        {
            this.downloader = downloader;
        }

        public string GetPlaylistUrl(string channelId)
        {
            string url = string.Format(ChannelUrlFormat, channelId);

            RegisterAccount();
            GoToUrl(url);
            
            string playlistUrl = GetStreamUrlFromPageSource();

            WebDriverHandler.WebDriver.Quit();

            return playlistUrl;
        }

        string GetStreamUrlFromPageSource()
        {
            By startStreamButtonSelector = By.Id("start-video");

            Click(startStreamButtonSelector);

            string html = GetPageSource();
            string streamUrl = Regex.Match(html, StreamUrlPattern).Groups[1].Value;

            return streamUrl;
        }

        void RegisterAccount()
        {
            By acceptGdprButtonSelector = By.XPath("//*[@id='gdpr']/div/div/a[2]/button");
            By emailInputSelector = By.Name("email");
            By passwordInputSelector = By.Name("password");
            By firstNameInputSelector = By.Name("firstname");
            By lastNameInputSelector = By.Name("lastname");
            By tosCheckboxSelector = By.Id("agree");
            By submitButtonSelector = By.XPath(@"//form/button");
            By smsValidationButtonSelector = By.Id("js-btn-sms");

            GoToUrl(RegistrationUrl);

            Click(acceptGdprButtonSelector);

            SetText(emailInputSelector, GenerateRandomEmail());
            SetText(passwordInputSelector, GenerateRandomString());
            SetText(firstNameInputSelector, GenerateRandomName());
            SetText(lastNameInputSelector, GenerateRandomName());

            UpdateCheckbox(tosCheckboxSelector, true);
            
            Click(submitButtonSelector);

            WaitForElementToBeVisible(smsValidationButtonSelector);
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
