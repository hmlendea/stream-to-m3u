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
            StreamInfo streamInfo = new StreamInfo();
            streamInfo.ChannelId = options.ChannelId;
            streamInfo.Title = options.Title;
            streamInfo.Url = options.Url;

            try
            {
                switch (options.Provider)
                {
                    case StreamProvider.YouTube:
                        return GetYouTubeStreamUrl(streamInfo);
                    
                    case StreamProvider.Twitch:
                        return GetTwitchStreamUrl(streamInfo);
                    
                    case StreamProvider.SeeNow:
                        return GetSeeNowStreamUrl(streamInfo);
                    
                    case StreamProvider.TvSportHd:
                        return GetTvSportHdStreamUrl(options);
                    
                    case StreamProvider.AntenaPlay:
                        return GetAntenaPlayStreamUrl(options);
                    
                    default:
                        return GetOtherStreamUrl(streamInfo);
                }
            }
            catch
            {
                return null;
            }
        }

        string GetYouTubeStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = new YouTubeStreamProcessor(downloader);
            return processor.GetUrlAsync(streamInfo).Result;
        }

        string GetTwitchStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = new TwitchProcessor();
            return processor.GetUrlAsync(streamInfo).Result;
        }

        string GetSeeNowStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = new SeeNowProcessor(downloader);
            return processor.GetUrlAsync(streamInfo).Result;
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

        string GetOtherStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = new OtherProcessor(downloader);
            return processor.GetUrlAsync(streamInfo).Result;
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
