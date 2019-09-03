using System;

using StreamToM3U.Configuration;
using StreamToM3U.Net;
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

        public string GetStreamUrl(Options options)
        {
            string url = FindStreamUrl(options);

            if (IsUrlValid(url))
            {
                return url;
            }

            return null;
        }

        string FindStreamUrl(Options options)
        {
            try
            {
                switch (options.Provider)
                {
                    case StreamProvider.YouTube:
                        return GetYouTubeStreamUrl(options);
                    
                    case StreamProvider.Twitch:
                        return GetTwitchStreamUrl(options);
                    
                    case StreamProvider.SeeNow:
                        return GetSeeNowStreamUrl(options);
                    
                    case StreamProvider.TvSportHd:
                        return GetTvSportHdStreamUrl(options);
                    
                    case StreamProvider.AntenaPlay:
                        return GetAntenaPlayStreamUrl(options);
                    
                    default:
                        return GetOtherStreamUrl(options);
                }
            }
            catch
            {
                return null;
            }
        }

        string GetYouTubeStreamUrl(Options options)
        {
            IYouTubeStreamProcessor processor = new YouTubeStreamProcessor(downloader);

            if (!string.IsNullOrWhiteSpace(options.Title))
            {
                return processor.GetPlaylistUrl(options.ChannelId, options.Title);
            }
            else
            {
                return processor.GetPlaylistUrl(options.ChannelId);
            }
        }

        string GetTwitchStreamUrl(Options options)
        {
            ITwitchProcessor processor = new TwitchProcessor();
            return processor.GetPlaylistUrl(options.ChannelId);
        }

        string GetSeeNowStreamUrl(Options options)
        {
            ISeeNowProcessor processor = new SeeNowProcessor(downloader);
            return processor.GetPlaylistUrl(options.ChannelId);
        }

        string GetTvSportHdStreamUrl(Options options)
        {
            ITvSportHdProcessor processor = new TvSportHdProcessor(downloader);
            return processor.GetPlaylistUrl(options.ChannelId);
        }

        string GetAntenaPlayStreamUrl(Options options)
        {
            IAntenaPlayProcessor processor = new AntenaPlayProcessor(downloader);
            return processor.GetPlaylistUrl(options.ChannelId);
        }

        string GetOtherStreamUrl(Options options)
        {
            IOtherProcessor processor = new OtherProcessor(downloader);
            return processor.GetPlaylistUrl(options.Url);
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
