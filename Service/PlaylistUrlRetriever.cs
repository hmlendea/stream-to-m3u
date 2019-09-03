using System;

using NuciCLI;

using StreamToM3U.Net;
using StreamToM3U.Service.Processors;

namespace StreamToM3U.Service
{
    public sealed class PlaylistUrlRetriever : IPlaylistUrlRetriever
    {
        static string[] ChannelIdOptions = { "-c", "--channel" };
        static string[] TitleOptions = { "-t", "--title" };
        static string[] UrlOptions = { "-u", "--url" };

        static string[] YouTubeProcessorOptions = { "--yt", "--youtube" };
        static string[] TwitchProcessorOptions = { "--twitch" };
        static string[] SeeNowProcessorOptions = { "--seenow" };
        static string[] TvSportHdProcessorOptions = { "--tvs", "--tvsport", "--tvshd", "--tvsporthd" };
        static string[] AntenaPlayProccessorOptions = { "--antena-play", "--antenaplay", "--antena", "--aplay", "--ap" };

        readonly IFileDownloader downloader;

        public PlaylistUrlRetriever(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public string GetStreamUrl(string[] args)
        {
            string url = null;

            try
            {
                if (CliArgumentsReader.HasOption(args, YouTubeProcessorOptions))
                {
                    url = GetYouTubeStreamUrl(args);
                }
                else if (CliArgumentsReader.HasOption(args, TwitchProcessorOptions))
                {
                    url = GetTwitchStreamUrl(args);
                }
                else if (CliArgumentsReader.HasOption(args, SeeNowProcessorOptions))
                {
                    url = GetSeeNowStreamUrl(args);
                }
                else if (CliArgumentsReader.HasOption(args, TvSportHdProcessorOptions))
                {
                    url = GetTvSportHdStreamUrl(args);
                }
                else if (CliArgumentsReader.HasOption(args, AntenaPlayProccessorOptions))
                {
                    url = GetAntenaPlayStreamUrl(args);
                }
                else
                {
                    url = GetOtherStreamUrl(args);
                }
            }
            catch { }

            if (IsUrlValid(url))
            {
                return url;
            }

            return null;
        }

        string GetSeeNowStreamUrl(string[] args)
        {
            ISeeNowProcessor processor = new SeeNowProcessor(downloader);
            string channelId = CliArgumentsReader.GetOptionValue(args, ChannelIdOptions);

            return processor.GetPlaylistUrl(channelId);
        }

        string GetYouTubeStreamUrl(string[] args)
        {
            IYouTubeStreamProcessor processor = new YouTubeStreamProcessor(downloader);
            string channelId = CliArgumentsReader.GetOptionValue(args, ChannelIdOptions);

            if (CliArgumentsReader.HasOption(args, TitleOptions))
            {
                string streamTitle = CliArgumentsReader.GetOptionValue(args, TitleOptions);
                return processor.GetPlaylistUrl(channelId, streamTitle);
            }
            else
            {
                return processor.GetPlaylistUrl(channelId);
            }
        }

        string GetTwitchStreamUrl(string[] args)
        {
            ITwitchProcessor processor = new TwitchProcessor();
            string channelId = CliArgumentsReader.GetOptionValue(args, ChannelIdOptions);

            return processor.GetPlaylistUrl(channelId);
        }

        string GetTvSportHdStreamUrl(string[] args)
        {
            ITvSportHdProcessor processor = new TvSportHdProcessor(downloader);
            string channelId = CliArgumentsReader.GetOptionValue(args, ChannelIdOptions);

            return processor.GetPlaylistUrl(channelId);
        }

        string GetAntenaPlayStreamUrl(string[] args)
        {
            IAntenaPlayProcessor processor = new AntenaPlayProcessor(downloader);
            string channelId = CliArgumentsReader.GetOptionValue(args, ChannelIdOptions);

            return processor.GetPlaylistUrl(channelId);
        }

        string GetOtherStreamUrl(string[] args)
        {
            IOtherProcessor processor = new OtherProcessor(downloader);
            string url = CliArgumentsReader.GetOptionValue(args, UrlOptions);

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
