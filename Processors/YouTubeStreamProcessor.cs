using System;
using System.Net;
using System.Text.RegularExpressions;

using StreamToM3U.Net;

namespace StreamToM3U.Processors
{
    public sealed class YouTubeStreamProcessor : IYouTubeStreamProcessor
    {
        static string YouTubeUrl => "https://www.youtube.com";
        static string YouTubeChannelUrlFormat => $"{YouTubeUrl}/channel/{{0}}";
        static string YouTubeStreamUrlFormat => $"{YouTubeUrl}/watch?v={{0}}";

        static string StreamIdFirstPatternFormat = $"title=\".*\".*href=\"\\/watch\\?v=([a-zA-Z0-9]*)\"";
        static string StreamIdByTitlePatternFormat = $"title=\"{{0}}\".*href=\"\\/watch\\?v=([a-zA-Z0-9]*)\"";
        static string ManifestUrlPattern = "\"hlsManifestUrl\\\\\": *\\\\\"(.*\\.m3u8)\\\\\"";

        readonly IFileDownloader downloader;

        public YouTubeStreamProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public string GetPlaylistUrl(
            string channelId)
        {
            string streamUrl = GetYouTubeStreamUrl(channelId);
            string playlistUrl = GetYouTubeStreamPlaylistUrl(streamUrl);

            return playlistUrl;
        }

        public string GetPlaylistUrl(
            string channelId,
            string streamTitle)
        {
            string streamUrl = GetYouTubeStreamUrl(channelId, streamTitle);
            string playlistUrl = GetYouTubeStreamPlaylistUrl(streamUrl);

            return playlistUrl;
        }

        string GetYouTubeStreamUrl(string channelId)
        {
            string channelUrl = string.Format(YouTubeChannelUrlFormat,channelId);
            string html = downloader.Download(channelUrl);
            string streamId = Regex.Match(html, StreamIdFirstPatternFormat).Groups[1].Value;

            return string.Format(YouTubeStreamUrlFormat, streamId);
        }

        string GetYouTubeStreamUrl(string channelId, string streamTitle)
        {
            string channelUrl = string.Format(YouTubeChannelUrlFormat,channelId);
            string html = downloader.Download(channelUrl);

            string escapedStreamTitle = streamTitle
                .Replace("(", "\\(")
                .Replace(")", "\\)");

            string streamIdPattern = string.Format(StreamIdByTitlePatternFormat, escapedStreamTitle);
            string streamId = Regex.Match(html, streamIdPattern).Groups[1].Value;

            return string.Format(YouTubeStreamUrlFormat, streamId);
        }

        string GetYouTubeStreamPlaylistUrl(string streamUrl)
        {
            string html = downloader.Download(streamUrl);

            string playlistRelativeUrl = Regex.Match(html, ManifestUrlPattern).Groups[1].Value;
            string playlistAbsoluteUrl = playlistRelativeUrl.Replace("\\/", "/");

            return playlistAbsoluteUrl;
        }
    }
}
