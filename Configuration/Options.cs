using NuciCLI;

namespace StreamToM3U.Configuration
{
    public sealed class Options
    {
        static readonly string[] InputFileOptions = ["-i", "--input"];
        static readonly string[] OutputFileOptions = ["-o", "--output-file"];
        static readonly string[] OutputDirectoryOptions = ["-O", "--output-dir", "--output-directory"];

        static readonly string[] ChannelIdOptions = ["-c", "--channel"];
        static readonly string[] TitleOptions = ["-t", "--title"];
        static readonly string[] UrlOptions = ["-u", "--url"];
        static readonly string[] StreamBaseUrlOptions = ["-U", "--baseurl"];

        static readonly string[] TwitchProcessorOptions = ["--twitch"];
        static readonly string[] TvSportHdProcessorOptions = ["--tvs", "--tvsport", "--tvshd", "--tvsporthd"];
        static readonly string[] AntenaPlayProccessorOptions = ["--antena-play", "--antenaplay", "--antena", "--aplay", "--ap"];
        static readonly string[] OkLiveProcessorOptions = ["--ok", "--oklive"];

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
            Options options = new()
            {
                Provider = DetermineProviderFromArgs(args),
                InputFile = GetArgumentIfExists(args, InputFileOptions),
                OutputFile = GetArgumentIfExists(args, OutputFileOptions, "playlist.m3u"),
                OutputDirectory = GetArgumentIfExists(args, OutputDirectoryOptions),
                ChannelId = GetArgumentIfExists(args, ChannelIdOptions),
                Title = GetArgumentIfExists(args, TitleOptions),
                Url = GetArgumentIfExists(args, UrlOptions),
                StreamBaseUrl = GetArgumentIfExists(args, StreamBaseUrlOptions)
            };

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
            => CliArgumentsReader.HasOption(args, argumentOptions)
                ? CliArgumentsReader.GetOptionValue(args, argumentOptions)
                : fallbackValue;
    }
}
