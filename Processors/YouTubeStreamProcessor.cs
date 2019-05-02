using System;
using System.Net;
using System.Text.RegularExpressions;

namespace SocialMediaStreamToM3U.Processors
{
    public sealed class YouTubeStreamProcessor : IYouTubeStreamProcessor
    {
        static string YouTubeUrl => "https://www.youtube.com";
        static string YouTubeChannelUrlFormat => $"{YouTubeUrl}/channel/{{0}}";
        static string YouTubeStreamUrlFormat => $"{YouTubeUrl}/watch?v={{0}}";

        static string StreamIdPatternFormat = $"title=\"{{0}}\".*href=\"\\/watch\\?v=([a-zA-Z0-9]*)\"";
        static string ManifestUrlPattern = "\"hlsManifestUrl\\\\\": *\\\\\"(.*\\.m3u8)\\\\\"";

        public string GetPlaylistUrl(
            string channelId,
            string streamTitle)
        {
            string streamUrl = GetYouTubeStreamUrl(channelId, streamTitle);
            string playlistUrl = GetYouTubeStreamPlaylistUrl(streamUrl);

            return playlistUrl;
        }

        string GetYouTubeStreamUrl(string channelId, string streamTitle)
        {
            string channelUrl = string.Format(
                YouTubeChannelUrlFormat,
                channelId);

            string html = null;

            using (WebClient downloader = new WebClient())
            {
                html = downloader.DownloadString(channelUrl);
            }

            string escapedStreamTitle = streamTitle
                .Replace("(", "\\(")
                .Replace(")", "\\)");

            string streamIdPattern = string.Format(StreamIdPatternFormat, escapedStreamTitle);
            string streamId = Regex.Match(html, streamIdPattern).Groups[1].Value;

            return string.Format(YouTubeStreamUrlFormat, streamId);
        }

        string GetYouTubeStreamPlaylistUrl(string streamUrl)
        {
            string html;

            using (WebClient downloader = new WebClient())
            {
                html = downloader.DownloadString(streamUrl);
            }

            string playlistRelativeUrl = Regex.Match(html, ManifestUrlPattern).Groups[1].Value;
            string playlistAbsoluteUrl = playlistRelativeUrl.Replace("\\/", "/");

            return playlistAbsoluteUrl;
        }
    }
}
