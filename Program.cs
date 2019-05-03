using System;

using NuciCLI;

using SocialMediaStreamToM3U.Net;
using SocialMediaStreamToM3U.Processors;

namespace SocialMediaStreamToM3U
{
    public class Program
    {
        static string[] YouTubeProcessorOptions = { "--yt", "--youtube" };
        static string[] TwitchProcessorOptions = { "--twitch" };

        static string[] ChannelIdOptions = { "-c", "--channel" };
        static string[] TitleOptions = { "-t", "--title" };

        static readonly IFileDownloader downloader;

        static Program()
        {
            downloader = new FileDownloader(); 
        }

        public static void Main(string[] args)
        {
            string playlistUrl = GetStreamUrl(args);
            Console.WriteLine(playlistUrl);
        }

        static string GetStreamUrl(string[] args)
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
            }
            catch { }

            if (IsUrlValid(url))
            {
                return url;
            }

            return null;
        }

        static string GetYouTubeStreamUrl(string[] args)
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

        static string GetTwitchStreamUrl(string[] args)
        {
            ITwitchProcessor processor = new TwitchProcessor();
            string channelId = CliArgumentsReader.GetOptionValue(args, ChannelIdOptions);

            return processor.GetPlaylistUrl(channelId);
        }

        static bool IsUrlValid(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            bool isUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult);
            bool isHttp = uriResult.Scheme == Uri.UriSchemeHttp;
            bool isHttps =  uriResult.Scheme == Uri.UriSchemeHttps;

            return isUrl && (isHttp || isHttps);
        }
    }
}
