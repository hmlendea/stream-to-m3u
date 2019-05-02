using System;

using NuciCLI;

using SocialMediaStreamToM3U.Processors;

namespace SocialMediaStreamToM3U
{
    public class Program
    {
        static string[] YtProcessorOptions = { "--yt", "--youtube" };
        static string[] YtChannelIdOptions = { "-c", "--channel" };
        static string[] YtTitleOptions = { "-t", "--title" };

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
                if (CliArgumentsReader.HasOption(args, YtProcessorOptions))
                {
                    url =  GetYouTubeStreamUrl(args);
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
            IYouTubeStreamProcessor processor = new YouTubeStreamProcessor();

            string channelId = CliArgumentsReader.GetOptionValue(args, YtChannelIdOptions);

            if (CliArgumentsReader.HasOption(args, YtTitleOptions))
            {
                string streamTitle = CliArgumentsReader.GetOptionValue(args, YtTitleOptions);
                return processor.GetPlaylistUrl(channelId, streamTitle);
            }
            else
            {
                return processor.GetPlaylistUrl(channelId);
            }
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
