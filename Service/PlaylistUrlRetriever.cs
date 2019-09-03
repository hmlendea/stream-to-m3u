using System;

using NuciCLI;

using StreamToM3U.Net;
using StreamToM3U.Service.Models;
using StreamToM3U.Service.Processors;

namespace StreamToM3U.Service
{
    public sealed class PlaylistUrlRetriever : IPlaylistUrlRetriever
    {
        readonly IFileDownloader downloader;

        public PlaylistUrlRetriever(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public string GetStreamUrl(StreamProvider provider, string argument1)
            => GetStreamUrl(provider, argument1, null);

        public string GetStreamUrl(StreamProvider provider, string argument1, string argument2)
        {
            string url = FindStreamUrl(provider, argument1, argument2);

            if (IsUrlValid(url))
            {
                return url;
            }

            return null;
        }

        string FindStreamUrl(StreamProvider provider, string argument1, string argument2)
        {
            try
            {
                switch (provider)
                {
                    case StreamProvider.YouTube:
                        return GetYouTubeStreamUrl(argument1, argument2);
                    
                    case StreamProvider.Twitch:
                        return GetTwitchStreamUrl(argument1);
                    
                    case StreamProvider.SeeNow:
                        return GetSeeNowStreamUrl(argument1);
                    
                    case StreamProvider.TvSportHd:
                        return GetTvSportHdStreamUrl(argument1);
                    
                    case StreamProvider.AntenaPlay:
                        return GetAntenaPlayStreamUrl(argument1);
                    
                    default:
                        return GetOtherStreamUrl(argument1);
                }
            }
            catch
            {
                return null;
            }
        }

        string GetYouTubeStreamUrl(string channelId, string streamTitle)
        {
            IYouTubeStreamProcessor processor = new YouTubeStreamProcessor(downloader);

            if (!string.IsNullOrWhiteSpace(streamTitle))
            {
                return processor.GetPlaylistUrl(channelId, streamTitle);
            }
            else
            {
                return processor.GetPlaylistUrl(channelId);
            }
        }

        string GetTwitchStreamUrl(string channelId)
        {
            ITwitchProcessor processor = new TwitchProcessor();

            return processor.GetPlaylistUrl(channelId);
        }

        string GetSeeNowStreamUrl(string channelId)
        {
            ISeeNowProcessor processor = new SeeNowProcessor(downloader);
            return processor.GetPlaylistUrl(channelId);
        }

        string GetTvSportHdStreamUrl(string channelId)
        {
            ITvSportHdProcessor processor = new TvSportHdProcessor(downloader);
            return processor.GetPlaylistUrl(channelId);
        }

        string GetAntenaPlayStreamUrl(string channelId)
        {
            IAntenaPlayProcessor processor = new AntenaPlayProcessor(downloader);
            return processor.GetPlaylistUrl(channelId);
        }

        string GetOtherStreamUrl(string url)
        {
            IOtherProcessor processor = new OtherProcessor(downloader);
            return processor.GetPlaylistUrl(url);
        }

        bool IsUrlValid(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            Uri uriResult;
            bool isUrl = Uri.TryCreate(url, UriKind.Absolute, out uriResult);

            if (uriResult is null)
            {
                return false;
            }

            bool isHttp = uriResult.Scheme == Uri.UriSchemeHttp;
            bool isHttps =  uriResult.Scheme == Uri.UriSchemeHttps;

            return isUrl && (isHttp || isHttps);
        }
    }
}
