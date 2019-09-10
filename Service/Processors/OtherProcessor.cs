using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamToM3U.Service.Processors
{
    public sealed class OtherProcessor : IOtherProcessor
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

        public async Task<string> GetUrlAsync(string url)
        {
            string html = await downloader.TryDownloadStringAsync(url);
            
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
