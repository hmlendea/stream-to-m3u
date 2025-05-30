using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Processors
{
    public sealed class WebsiteProcessr(IFileDownloader downloader) : IProcessor
    {
        static readonly string[] PlaylistUrlPatterns =
        {
            "\"(http[^\"']*\\.m3u[^\"']*)\"",
            "'(http[^\"']*\\.m3u[^\"']*)'"
        };

        readonly IFileDownloader downloader = downloader;

        public async Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            return await CrawlPage(streamInfo);
        }

        async Task<string> CrawlPage(StreamInfo streamInfo)
        {
            string html = await downloader.TryDownloadStringAsync(streamInfo.Url);

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

            if (!string.IsNullOrWhiteSpace(streamInfo.StreamBaseUrl))
            {
                string playlistUrl = Regex.Match(html, "[\"']([^\"']*\\.m3u[^\"']*)[\"']").Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(playlistUrl))
                {
                    return Path.Join(streamInfo.StreamBaseUrl, playlistUrl);
                }
            }

            return null;
        }
    }
}
