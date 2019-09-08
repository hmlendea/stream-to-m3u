using System;
using System.Text.RegularExpressions;

using StreamToM3U.Net;

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

        public string GetPlaylistUrl(string url)
        {
            string html = downloader.TryDownload(url);
            
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
