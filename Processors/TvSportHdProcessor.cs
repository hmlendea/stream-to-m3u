using System;
using System.Text.RegularExpressions;

using NuciWeb;
using OpenQA.Selenium;

using StreamToM3U.Net;
using StreamToM3U.Utils;

namespace StreamToM3U.Processors
{
    public sealed class TvSportHdProcessor : WebProcessor, ITvSportHdProcessor
    {
        static string TvSportHdUrl => "http://www.tv-sport-hd.com";
        static string ChannelUrlFormat => $"{TvSportHdUrl}/channel/tvs.php?ch={{0}}";

        const string PlaylistUrlPattern = "file: *\"(http[^\"]*)\"";

        readonly IFileDownloader downloader;

        public TvSportHdProcessor(IFileDownloader downloader)
            : base(WebDriverHandler.WebDriver)
        {
            this.downloader = downloader;
        }

        public string GetPlaylistUrl(string channelId)
        {
            string channelUrl = string.Format(ChannelUrlFormat, channelId);
            string playlistUrl = GetPlaylistUrlFromPage(channelUrl);

            WebDriverHandler.WebDriver.Dispose();
            return playlistUrl;
        }

        string GetPlaylistUrlFromPage(string url)
        {
            GoToUrl(url);
            SwitchToVideoIframe();

            By playlistUrlSelector = By.XPath(@"//*[@id='playerDIV_html5_api']/source");
            WaitForElementToExist(playlistUrlSelector);

            return WebDriverHandler.WebDriver
                .FindElement(playlistUrlSelector)
                .GetAttribute("src");
        }

        void SwitchToVideoIframe()
        {
            By iframeSelector = By.Id("thatframe");
            WaitForElementToExist(iframeSelector);

            IWebElement iframe = WebDriverHandler.WebDriver.FindElement(iframeSelector);
            WebDriverHandler.WebDriver.SwitchTo().Frame(iframe);
        }
    }
}
