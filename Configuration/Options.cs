using NuciCLI;

namespace StreamToM3U.Configuration
{
    public sealed class Options
    {
        static string[] InputFileOptions = { "-i", "--input" };
        static string[] OutputFileOptions = { "-o", "--output-file" };
        static string[] OutputDirectoryOptions = { "-O", "--output-dir", "--output-directory" };

        static string[] ChannelIdOptions = { "-c", "--channel" };
        static string[] TitleOptions = { "-t", "--title" };
        static string[] UrlOptions = { "-u", "--url" };
        static string[] StreamBaseUrlOptions = { "-U", "--baseurl" };

        static string[] TwitchProcessorOptions = { "--twitch" };
        static string[] TvSportHdProcessorOptions = { "--tvs", "--tvsport", "--tvshd", "--tvsporthd" };
        static string[] AntenaPlayProccessorOptions = { "--antena-play", "--antenaplay", "--antena", "--aplay", "--ap" };
        static string[] OkLiveProcessorOptions = { "--ok", "--oklive" };

        public StreamProvider Provider { get; set; }

        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public string OutputDirectory { get; set; }

        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string StreamBaseUrl { get; set; }

        public static Options FromArguments(string[] args)
        {
            Options options = new Options();
            options.Provider = DetermineProviderFromArgs(args);
            options.InputFile = GetArgumentIfExists(args, InputFileOptions);
            options.OutputFile = GetArgumentIfExists(args, OutputFileOptions, "playlist.m3u");
            options.OutputDirectory = GetArgumentIfExists(args, OutputDirectoryOptions);
            options.ChannelId = GetArgumentIfExists(args, ChannelIdOptions);
            options.Title = GetArgumentIfExists(args, TitleOptions);
            options.Url = GetArgumentIfExists(args, UrlOptions);
            options.StreamBaseUrl = GetArgumentIfExists(args, StreamBaseUrlOptions);

            return options;
        }

        static StreamProvider DetermineProviderFromArgs(string[] args)
        {
            if (CliArgumentsReader.HasOption(args, TwitchProcessorOptions))
            {
                return StreamProvider.Twitch;
            }

            if (CliArgumentsReader.HasOption(args, TvSportHdProcessorOptions))
            {
                return StreamProvider.TvSportHd;
            }

            if (CliArgumentsReader.HasOption(args, AntenaPlayProccessorOptions))
            {
                return StreamProvider.AntenaPlay;
            }

            if (CliArgumentsReader.HasOption(args, OkLiveProcessorOptions))
            {
                return StreamProvider.OkLive;
            }

            return StreamProvider.Website;
        }

        static string GetArgumentIfExists(string[] args, string[] argumentOptions)
            => GetArgumentIfExists(args, argumentOptions, null);

        static string GetArgumentIfExists(string[] args, string[] argumentOptions, string fallbackValue)
        {
            if (CliArgumentsReader.HasOption(args, argumentOptions))
            {
                return CliArgumentsReader.GetOptionValue(args, argumentOptions);
            }

            return fallbackValue;
        }
    }
}
