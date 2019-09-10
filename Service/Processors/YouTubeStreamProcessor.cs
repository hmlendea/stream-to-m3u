using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamToM3U.Service.Processors
{
    public sealed class YouTubeStreamProcessor : IYouTubeStreamProcessor
    {
        static string YouTubeUrl => "https://www.youtube.com";
        static string YouTubeChannelUrlFormat => $"{YouTubeUrl}/channel/{{0}}";
        static string YouTubeStreamUrlFormat => $"{YouTubeUrl}/watch?v={{0}}";

        static string StreamIdFirstPatternFormat = $"href=\"\\/watch\\?v=([a-zA-Z0-9\\-]*)\"";
        static string StreamIdByTitlePatternFormat = $"title=\"{{0}}\".*href=\"\\/watch\\?v=([a-zA-Z0-9]*)\"";
        static string ManifestUrlPattern = "\"hlsManifestUrl\\\\\": *\\\\\"(.*\\.m3u8)\\\\\"";

        readonly IFileDownloader downloader;

        public YouTubeStreamProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public async Task<string> GetUrlAsync(string channelId)
        {
            string streamUrl = await GetYouTubeStreamUrl(channelId);
            string playlistUrl = await GetYouTubeStreamPlaylistUrl(streamUrl);

            return playlistUrl;
        }

        public async Task<string> GetUrlAsync(string channelId, string streamTitle)
        {
            string streamUrl = await GetYouTubeStreamUrl(channelId, streamTitle);
            string playlistUrl = await GetYouTubeStreamPlaylistUrl(streamUrl);

            return playlistUrl;
        }

        async Task<string> GetYouTubeStreamUrl(string channelId)
        {
            string channelUrl = string.Format(YouTubeChannelUrlFormat,channelId);
            string html = await downloader.TryDownloadStringAsync(channelUrl);
            string streamId = Regex.Match(html, StreamIdFirstPatternFormat).Groups[1].Value;

            return string.Format(YouTubeStreamUrlFormat, streamId);
        }

        async Task<string> GetYouTubeStreamUrl(string channelId, string streamTitle)
        {
            string channelUrl = string.Format(YouTubeChannelUrlFormat,channelId);
            string html = await downloader.TryDownloadStringAsync(channelUrl);

            string escapedStreamTitle = streamTitle
                .Replace("(", "\\(")
                .Replace(")", "\\)");

            string streamIdPattern = string.Format(StreamIdByTitlePatternFormat, escapedStreamTitle);
            string streamId = Regex.Match(html, streamIdPattern).Groups[1].Value;

            return string.Format(YouTubeStreamUrlFormat, streamId);
        }

        async Task<string> GetYouTubeStreamPlaylistUrl(string streamUrl)
        {
            string html = await downloader.TryDownloadStringAsync(streamUrl);

            string playlistRelativeUrl = Regex.Match(html, ManifestUrlPattern).Groups[1].Value;
            string playlistAbsoluteUrl = playlistRelativeUrl.Replace("\\/", "/");

            return playlistAbsoluteUrl;
        }
    }
}
