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
            string playlistUrl = null;

            if (CliArgumentsReader.HasOption(args, YtProcessorOptions))
            {
                IYouTubeStreamProcessor processor = new YouTubeStreamProcessor();

                string channelId = CliArgumentsReader.GetOptionValue(args, YtChannelIdOptions);

                if (CliArgumentsReader.HasOption(args, YtTitleOptions))
                {
                    string streamTitle = CliArgumentsReader.GetOptionValue(args, YtTitleOptions);
                    playlistUrl = processor.GetPlaylistUrl(channelId, streamTitle);
                }
                else
                {
                    playlistUrl = processor.GetPlaylistUrl(channelId);
                }
            }
            
            if (string.IsNullOrWhiteSpace(playlistUrl) ||
                !IsUrlValid(playlistUrl))
            {
                throw new ApplicationException();
            }
    
            Console.WriteLine(playlistUrl);
        }

        static bool IsUrlValid(string url)
        {
            bool isUrl = Uri.TryCreate(url, UriKind.Absolute, out Uri uriResult);
            bool isHttp = uriResult.Scheme == Uri.UriSchemeHttp;
            bool isHttps =  uriResult.Scheme == Uri.UriSchemeHttps;

            return isUrl && (isHttp || isHttps);
        }
    }
}
