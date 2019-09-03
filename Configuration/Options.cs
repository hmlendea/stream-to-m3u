using NuciCLI;

namespace StreamToM3U.Configuration
{
    public sealed class Options
    {
        static string[] InputFileOptions = { "-i" };
        static string[] ChannelIdOptions = { "-c", "--channel" };
        static string[] TitleOptions = { "-t", "--title" };
        static string[] UrlOptions = { "-u", "--url" };

        static string[] YouTubeProcessorOptions = { "--yt", "--youtube" };
        static string[] TwitchProcessorOptions = { "--twitch" };
        static string[] SeeNowProcessorOptions = { "--seenow" };
        static string[] TvSportHdProcessorOptions = { "--tvs", "--tvsport", "--tvshd", "--tvsporthd" };
        static string[] AntenaPlayProccessorOptions = { "--antena-play", "--antenaplay", "--antena", "--aplay", "--ap" };

        public StreamProvider Provider { get; set; }

        public string InputFile { get; set; }

        public string ChannelId { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public static Options FromArguments(string[] args)
        {
            Options options = new Options();
            options.Provider = DetermineProviderFromArgs(args);
            options.InputFile = GetArgumentIfExists(InputFileOptions);
            options.Title = GetArgumentIfExists(TitleOptions);
            options.Url = GetArgumentIfExists(UrlOptions);

            return options;
        }

        static StreamProvider DetermineProviderFromArgs(string[] args)
        {
            if (CliArgumentsReader.HasOption(args, YouTubeProcessorOptions))
            {
                return StreamProvider.YouTube;
            }

            if (CliArgumentsReader.HasOption(args, TwitchProcessorOptions))
            {
                return StreamProvider.Twitch;
            }

            if (CliArgumentsReader.HasOption(args, SeeNowProcessorOptions))
            {
                return StreamProvider.SeeNow;
            }

            if (CliArgumentsReader.HasOption(args, TvSportHdProcessorOptions))
            {
                return StreamProvider.TvSportHd;
            }

            if (CliArgumentsReader.HasOption(args, AntenaPlayProccessorOptions))
            {
                return StreamProvider.AntenaPlay;
            }
            
            return StreamProvider.Unknown;
        }

        static string GetArgumentIfExists(string[] argumentOptions)
        {
            if (CliArgumentsReader.HasOption(argumentOptions))
            {
                return CliArgumentsReader.GetOptionValue(argumentOptions);
            }

            return null;
        }
    }
}
