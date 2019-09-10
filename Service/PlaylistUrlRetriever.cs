using System;
using System.Threading.Tasks;

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

        public async Task<string> GetStreamUrlAsync(StreamInfo streamInfo)
        {
            string url = await FindStreamUrl(streamInfo);

            if (IsUrlValid(url))
            {
                return url;
            }

            return null;
        }

        public async Task<string> GetStreamUrlAsync(ChannelStream channelStream)
        {
            StreamInfo streamInfo = new StreamInfo();
            streamInfo.Provider = channelStream.Provider;
            streamInfo.ChannelId = channelStream.ChannelId;
            streamInfo.Title = channelStream.Title;
            streamInfo.Url = channelStream.Url;
            
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

        IProcessor CreateProcessor(StreamProvider provider)
        {
            switch (provider)
            {
                case StreamProvider.YouTube:
                    return new YouTubeStreamProcessor(downloader);
                
                case StreamProvider.Twitch:
                    return new TwitchProcessor();
                
                case StreamProvider.SeeNow:
                    return new SeeNowProcessor(downloader);
                
                case StreamProvider.TvSportHd:
                    return new TvSportHdProcessor();
                
                case StreamProvider.AntenaPlay:
                    return new AntenaPlayProcessor();
                
                default:
                    return new OtherProcessor(downloader);
            }
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
