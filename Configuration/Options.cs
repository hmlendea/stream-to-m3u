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

        static readonly string[] TvSportHdProcessorOptions = ["--tvs", "--tvsport", "--tvshd", "--tvsporthd"];
        static readonly string[] AntenaPlayProccessorOptions = ["--antena-play", "--antenaplay", "--antena", "--aplay", "--ap"];
        static readonly string[] StreamlinkProcessorOptions = ["--sl", "--streamlink"];

        public StreamProvider Provider { get; set; }

        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public string OutputDirectory { get; set; }

        public string ChannelId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string StreamBaseUrl { get; set; }

        public static Options FromArguments(string[] args) => new()
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

        static StreamProvider DetermineProviderFromArgs(string[] args)
        {
            if (HasOption(args, TvSportHdProcessorOptions))
            {
                return StreamProvider.TvSportHd;
            }

            if (HasOption(args, AntenaPlayProccessorOptions))
            {
                return StreamProvider.AntenaPlay;
            }

            if (HasOption(args, StreamlinkProcessorOptions))
            {
                return StreamProvider.Streamlink;
            }

            return StreamProvider.Website;
        }

        static string GetArgumentIfExists(string[] args, string[] argumentOptions)
            => GetArgumentIfExists(args, argumentOptions, null);

        static string GetArgumentIfExists(string[] args, string[] argumentOptions, string fallbackValue)
            => HasOption(args, argumentOptions)
                ? GetOptionValue(args, argumentOptions)
                : fallbackValue;

        static bool HasOption(string[] args, string[] options)
            => GetOptionValue(args, options) is not null;

        static string GetOptionValue(string[] args, string[] options)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                foreach (string option in options)
                {
                    if (arg == option)
                    {
                        if (i + 1 < args.Length && !args[i + 1].StartsWith('-'))
                        {
                            return args[i + 1];
                        }

                        return string.Empty;
                    }

                    string optionPrefix = $"{option}=";
                    if (arg.StartsWith(optionPrefix))
                    {
                        return arg[optionPrefix.Length..];
                    }
                }
            }

            return null;
        }
    }
}
