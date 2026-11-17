using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using StreamToM3U.Configuration;
using StreamToM3U.Service.Models;
using StreamToM3U.Service.Processors;

namespace StreamToM3U.Service
{
    public sealed class PlaylistUrlRetriever(IFileDownloader downloader) : IPlaylistUrlRetriever
    {
        private readonly IFileDownloader downloader = downloader;

        private static string PlaylistHeader => "#EXTM3U";

        private static string GoogleVideoHost => "googlevideo";

        public async Task<string> GetStreamUrlAsync(StreamInfo streamInfo)
        {
            string url = await FindStreamUrl(streamInfo);

            bool requiresValidation = !Equals(streamInfo.Provider, StreamProvider.Streamlink) &&
                !url.Contains(GoogleVideoHost);

            if (requiresValidation)
            {
                url = await ResolveStreamUrl(url);

                if (url is null)
                {
                    return null;
                }
            }

            if (IsUrlValid(url))
            {
                return url;
            }

            return NormaliseUrl(url);
        }

        public async Task<string> GetStreamUrlAsync(ChannelStream channelStream)
        {
            StreamInfo streamInfo = new()
            {
                Provider = channelStream.Provider,
                ChannelId = channelStream.ChannelId,
                Title = channelStream.Title,
                Url = channelStream.Url,
                StreamBaseUrl = channelStream.StreamBaseUrl
            };

            return await GetStreamUrlAsync(streamInfo);
        }

        private static string ProcessStreamUrl(string playlistUrl, string streamUrl)
        {
            if (streamUrl.StartsWith("http"))
            {
                return streamUrl;
            }

            return Regex.Replace(playlistUrl, "[A-Za-z0-9_-]*\\.m3u[8]*\\?.*$", streamUrl);
        }

        private static bool IsUrlValid(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            bool isUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult);

            if (uriResult is null)
            {
                return false;
            }

            bool isHttp = Equals(uriResult.Scheme, Uri.UriSchemeHttp);
            bool isHttps = Equals(uriResult.Scheme, Uri.UriSchemeHttps);

            return isUrl && (isHttp || isHttps);
        }

        private static string NormaliseUrl(string url)
            => HttpUtility
                .UrlDecode(url)
                .Replace("\\/", "/")
                .Replace("#038;", "");

        private static string SelectStreamUrl(string playlistUrl, List<string> contentLines)
        {
            List<string> httpSources = contentLines
                .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith('#'))
                .Select(x => ProcessStreamUrl(playlistUrl, x))
                .ToList();

            if (!httpSources.Any())
            {
                return playlistUrl;
            }

            if (httpSources.Count == 1)
            {
                return httpSources.First();
            }

            string lastSource = httpSources.Last();

            if (lastSource.EndsWith("m3u") || lastSource.EndsWith("m3u8"))
            {
                return lastSource;
            }

            return playlistUrl;
        }

        private async Task<string> ResolveStreamUrl(string url)
        {
            string content = await downloader.TryDownloadStringAsync(url);

            List<string> contentLines = [];

            if (!string.IsNullOrWhiteSpace(content))
            {
                contentLines = content
                    .Replace("\r", "")
                    .Split("\n")
                    .ToList();
            }

            if (!contentLines.Any() ||
                !contentLines.First().StartsWith(PlaylistHeader))
            {
                return null;
            }

            return SelectStreamUrl(url, contentLines);
        }

        private async Task<string> FindStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = CreateProcessor(streamInfo.Provider);

            try
            {
                return await processor.GetUrlAsync(streamInfo);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private IProcessor CreateProcessor(StreamProvider provider) => provider switch
        {
            StreamProvider.TvSportHd => new TvSportHdProcessor(),
            StreamProvider.AntenaPlay => new AntenaPlayProcessor(),
            StreamProvider.Streamlink => new StreamlinkProcessor(),
            _ => new WebsiteProcessr(downloader),
        };
    }
}
