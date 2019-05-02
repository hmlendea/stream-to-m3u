using System;

using NuciCLI;

using SocialMediaStreamToIptv.Processors;

namespace SocialMediaStreamToIptv
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
                string streamTitle = CliArgumentsReader.GetOptionValue(args, YtTitleOptions);

                string playlistUrl = processor.GetPlaylistUrl(channelId, streamTitle);
                
                Console.WriteLine(playlistUrl);
                return;
            }
            
            Console.WriteLine("ERROR");
        }
    }
}
