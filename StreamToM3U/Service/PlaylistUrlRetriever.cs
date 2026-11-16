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
        readonly IFileDownloader downloader = downloader;

        public async Task<string> GetStreamUrlAsync(StreamInfo streamInfo)
        {
            string url = await FindStreamUrl(streamInfo);

            if (!streamInfo.Provider.Equals(StreamProvider.Streamlink) && !url.Contains("googlevideo"))
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

                if (!contentLines.Any() || !contentLines.First().StartsWith("#EXTM3U"))
                {
                    return null;
                }

                List<string> httpSources = contentLines
                    .Where(x => !string.IsNullOrWhiteSpace(x) && !x.StartsWith("#"))
                    .Select(x=> ProcessStreamUrl(url, x))
                    .ToList();

                if (httpSources.Any())
                {
                    if (httpSources.Count == 1)
                    {
                        url = httpSources.First();
                    }
                    else if (
                        httpSources.Last().EndsWith("m3u") ||
                        httpSources.Last().EndsWith("m3u8"))
                    {
                        url = httpSources.Last();
                    }
                }
            }

            if (IsUrlValid(url))
            {
                return url;
            }

            return NormaliseUrl(url);
        }

        static string ProcessStreamUrl(string playlistUrl, string streamUrl)
        {
            if (streamUrl.StartsWith("http"))
            {
                return streamUrl;
            }

            return Regex.Replace(playlistUrl, "[A-Za-z0-9_-]*\\.m3u[8]*\\?.*$", streamUrl);
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

        async Task<string> FindStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = CreateProcessor(streamInfo.Provider);

            try
            {
                return await processor.GetUrlAsync(streamInfo);
            }
            catch
            {
                return null;
            }
        }

        IProcessor CreateProcessor(StreamProvider provider) => provider switch
        {
            StreamProvider.TvSportHd => new TvSportHdProcessor(),
            StreamProvider.AntenaPlay => new AntenaPlayProcessor(),
            StreamProvider.Streamlink => new StreamlinkProcessor(),
            _ => new WebsiteProcessr(downloader),
        };

        static bool IsUrlValid(string url)
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

            bool isHttp = uriResult.Scheme == Uri.UriSchemeHttp;
            bool isHttps =  uriResult.Scheme == Uri.UriSchemeHttps;

            return isUrl && (isHttp || isHttps);
        }

        static string NormaliseUrl(string url)
            => HttpUtility
                .UrlDecode(url)
                .Replace("\\/", "/")
                .Replace("#038;", "");
    }
}
