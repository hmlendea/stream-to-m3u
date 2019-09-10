using System.Text.RegularExpressions;
using System.Threading.Tasks;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Processors
{
    public sealed class OtherProcessor : IProcessor
    {
        static string[] PlaylistUrlPatterns =
        {
            "\"(http.*\\.m3u[^\"]*)\"",
            "'(http.*\\.m3u[^']*)'"
        };

        readonly IFileDownloader downloader;

        public OtherProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public async Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            string html = await downloader.TryDownloadStringAsync(streamInfo.Url);
            
            foreach (string pattern in PlaylistUrlPatterns)
            {
                string playlistUrl = Regex.Match(html, pattern).Groups[1].Value;

                if (!string.IsNullOrWhiteSpace(playlistUrl))
                {
                    return playlistUrl;
                }
            }

            return null;
        }
    }
}
