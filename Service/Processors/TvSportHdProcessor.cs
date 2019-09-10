using System.Threading.Tasks;

using NuciWeb;
using OpenQA.Selenium;

using StreamToM3U.Service.Models;
using StreamToM3U.Utils;

namespace StreamToM3U.Service.Processors
{
    public sealed class TvSportHdProcessor : IProcessor
    {
        static string TvSportHdUrl => "http://www.tv-sport-hd.com";
        static string ChannelUrlFormat => $"{TvSportHdUrl}/channel/tvs.php?ch={{0}}";

        const string PlaylistUrlPattern = "file: *\"(http[^\"]*)\"";

        readonly IWebProcessor webProcessor;

        public TvSportHdProcessor()
        {
            this.webProcessor = new WebProcessor(WebDriverHandler.WebDriver);
        }

        public Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            string channelUrl = string.Format(ChannelUrlFormat, streamInfo.ChannelId);
            string playlistUrl = GetPlaylistUrlFromPage(channelUrl);

            WebDriverHandler.WebDriver.Dispose();
            return Task.FromResult(playlistUrl);
        }

        string GetPlaylistUrlFromPage(string url)
        {
            webProcessor.GoToUrl(url);
            SwitchToVideoIframe();

            By playlistUrlSelector = By.XPath(@"//*[@id='playerDIV_html5_api']/source");
            webProcessor.WaitForElementToExist(playlistUrlSelector);

            return WebDriverHandler.WebDriver
                .FindElement(playlistUrlSelector)
                .GetAttribute("src");
        }

        void SwitchToVideoIframe()
        {
            By iframeSelector = By.Id("thatframe");
            webProcessor.WaitForElementToExist(iframeSelector);

            IWebElement iframe = WebDriverHandler.WebDriver.FindElement(iframeSelector);
            WebDriverHandler.WebDriver.SwitchTo().Frame(iframe);
        }
    }
}
