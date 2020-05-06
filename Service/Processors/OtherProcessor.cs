using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Processors
{
    public sealed class OtherProcessor : IProcessor
    {
        static string[] PlaylistUrlPatterns =
        {
            "\"(http[^\"]*\\.m3u[^\"]*)\"",
            "'(http[^\"]*\\.m3u[^']*)'"
        };

        static string[] VideoUrlPatterns =
        {
            "\"(https://vk[^\"]*)\"",
        };

        readonly IFileDownloader downloader;

        public OtherProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public async Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            return await CrawlPage(streamInfo.Url, 2);
        }

        async Task<string> CrawlPage(string url, int remainingRecurrency)
        {
            System.Console.WriteLine(url + " @@@@@@@@@@@@@ " + remainingRecurrency);
            if (remainingRecurrency == 0)
            {
                return null;
            }

            string html = await downloader.TryDownloadStringAsync(url);

            if (string.IsNullOrWhiteSpace(html))
            {
                return null;
            }

            foreach (string pattern in PlaylistUrlPatterns)
            {
                string playlistUrl = Regex.Match(html, pattern).Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(playlistUrl))
                {
                    return playlistUrl;
                }
            }

            string videoUrl = null;
            foreach (string pattern in VideoUrlPatterns)
            {
                videoUrl = Regex.Match(html, pattern).Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(videoUrl))
                {
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(videoUrl))
            {
                return await CrawlPage(videoUrl, remainingRecurrency - 1);
            }

            return null;
        }
    }
}
