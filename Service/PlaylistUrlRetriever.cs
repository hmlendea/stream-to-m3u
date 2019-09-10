using System;

using StreamToM3U.Configuration;
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

        public string GetStreamUrl(Options options)
        {
            string url = FindStreamUrl(options);

            if (IsUrlValid(url))
            {
                return url;
            }

            return null;
        }

        public string GetStreamUrl(ChannelStream channelStream)
        {
            Options options = new Options();
            options.Provider = channelStream.Provider;
            options.ChannelId = channelStream.ChannelId;
            options.Title = channelStream.Title;
            options.Url = channelStream.Url;
            
            return GetStreamUrl(options);
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
                return processor.GetUrlAsync(options.ChannelId, options.Title).Result;
            }
            else
            {
                return processor.GetUrlAsync(options.ChannelId).Result;
            }
        }

        string GetTwitchStreamUrl(Options options)
        {
            ITwitchProcessor processor = new TwitchProcessor();
            return processor.GetUrlAsync(options.ChannelId).Result;
        }

        string GetSeeNowStreamUrl(Options options)
        {
            ISeeNowProcessor processor = new SeeNowProcessor(downloader);
            return processor.GetUrlAsync(options.ChannelId).Result;
        }

        string GetTvSportHdStreamUrl(Options options)
        {
            ITvSportHdProcessor processor = new TvSportHdProcessor();
            return processor.GetPlaylistUrl(options.ChannelId);
        }

        string GetAntenaPlayStreamUrl(Options options)
        {
            IAntenaPlayProcessor processor = new AntenaPlayProcessor();
            return processor.GetPlaylistUrl(options.ChannelId);
        }

        string GetOtherStreamUrl(Options options)
        {
            IOtherProcessor processor = new OtherProcessor(downloader);
            return processor.GetUrlAsync(options.Url).Result;
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
