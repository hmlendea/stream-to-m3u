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
            if (CliArgumentsReader.HasOption(args, YtProcessorOptions))
            {
                IYouTubeStreamProcessor processor = new YouTubeStreamProcessor();

                string channelId = CliArgumentsReader.GetOptionValue(args, YtChannelIdOptions);
                string playlistUrl;

                if (CliArgumentsReader.HasOption(args, YtTitleOptions))
                {
                    string streamTitle = CliArgumentsReader.GetOptionValue(args, YtTitleOptions);
                    playlistUrl = processor.GetPlaylistUrl(channelId, streamTitle);
                }
                else
                {
                    playlistUrl = processor.GetPlaylistUrl(channelId);
                }

                Console.WriteLine(playlistUrl);
                return;
            }
            
            Console.WriteLine("ERROR");
        }
    }
}
