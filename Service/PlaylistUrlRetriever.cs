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

        public string GetStreamUrl(StreamInfo streamInfo)
        {
            string url = FindStreamUrl(streamInfo);

            if (IsUrlValid(url))
            {
                return url;
            }

            return null;
        }

        public string GetStreamUrl(ChannelStream channelStream)
        {
            StreamInfo streamInfo = new StreamInfo();
            streamInfo.Provider = channelStream.Provider;
            streamInfo.ChannelId = channelStream.ChannelId;
            streamInfo.Title = channelStream.Title;
            streamInfo.Url = channelStream.Url;
            
            return GetStreamUrl(streamInfo);
        }

        string FindStreamUrl(StreamInfo streamInfo)
        {
            try
            {
                switch (streamInfo.Provider)
                {
                    case StreamProvider.YouTube:
                        return GetYouTubeStreamUrl(streamInfo);
                    
                    case StreamProvider.Twitch:
                        return GetTwitchStreamUrl(streamInfo);
                    
                    case StreamProvider.SeeNow:
                        return GetSeeNowStreamUrl(streamInfo);
                    
                    case StreamProvider.TvSportHd:
                        return GetTvSportHdStreamUrl(streamInfo);
                    
                    case StreamProvider.AntenaPlay:
                        return GetAntenaPlayStreamUrl(streamInfo);
                    
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

        string GetTvSportHdStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = new TvSportHdProcessor();
            return processor.GetUrlAsync(streamInfo).Result;
        }

        string GetAntenaPlayStreamUrl(StreamInfo streamInfo)
        {
            IProcessor processor = new AntenaPlayProcessor();
            return processor.GetUrlAsync(streamInfo).Result;
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
