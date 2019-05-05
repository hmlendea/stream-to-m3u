using System;
using System.Text.RegularExpressions;

using StreamToM3U.Net;

namespace StreamToM3U.Processors
{
    public sealed class SeeNowProcessor : ISeeNowProcessor
    {
        static string SeeNowUrl => "http://www.seenow.ro";
        static string SeeNowChannelUrlFormat => $"{SeeNowUrl}/live-{{0}}-9";

        const string PlaylistUrlPattern = "file: *\"(http[^\"]*)\"";

        readonly IFileDownloader downloader;

        public SeeNowProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public string GetPlaylistUrl(string channelId)
        {
            string channelUrl = string.Format(SeeNowChannelUrlFormat, channelId);
            string html = downloader.TryDownload(channelUrl);
            
            return Regex.Match(html, PlaylistUrlPattern).Groups[1].Value;
        }
    }
}
