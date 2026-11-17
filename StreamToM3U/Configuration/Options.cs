using System;

namespace StreamToM3U.Configuration
{
    public sealed class Options
    {
        private static readonly string[] inputFileOptions = ["-i", "--input"];
        private static readonly string[] outputFileOptions = ["-o", "--output-file"];
        private static readonly string[] outputDirectoryOptions = ["-O", "--output-dir", "--output-directory"];

        private static readonly string[] channelIdOptions = ["-c", "--channel"];
        private static readonly string[] titleOptions = ["-t", "--title"];
        private static readonly string[] urlOptions = ["-u", "--url"];
        private static readonly string[] streamBaseUrlOptions = ["-U", "--baseurl"];

        private static readonly string[] tvSportHdProcessorOptions = ["--tvs", "--tvsport", "--tvshd", "--tvsporthd"];
        private static readonly string[] antenaPlayProccessorOptions = ["--antena-play", "--antenaplay", "--antena", "--aplay", "--ap"];
        private static readonly string[] streamlinkProcessorOptions = ["--sl", "--streamlink"];

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
            InputFile = GetArgumentIfExists(args, inputFileOptions),
            OutputFile = GetArgumentIfExists(args, outputFileOptions, "playlist.m3u"),
            OutputDirectory = GetArgumentIfExists(args, outputDirectoryOptions),
            ChannelId = GetArgumentIfExists(args, channelIdOptions),
            Title = GetArgumentIfExists(args, titleOptions),
            Url = GetArgumentIfExists(args, urlOptions),
            StreamBaseUrl = GetArgumentIfExists(args, streamBaseUrlOptions)
        };

        private static StreamProvider DetermineProviderFromArgs(string[] args)
        {
            if (HasOption(args, tvSportHdProcessorOptions))
            {
                return StreamProvider.TvSportHd;
            }

            if (HasOption(args, antenaPlayProccessorOptions))
            {
                return StreamProvider.AntenaPlay;
            }

            if (HasOption(args, streamlinkProcessorOptions))
            {
                return StreamProvider.Streamlink;
            }

            return StreamProvider.Website;
        }

        private static string GetArgumentIfExists(string[] args, string[] argumentOptions)
            => GetArgumentIfExists(args, argumentOptions, null);

        private static string GetArgumentIfExists(
            string[] args,
            string[] argumentOptions,
            string fallbackValue)
        {
            if (HasOption(args, argumentOptions))
            {
                return GetOptionValue(args, argumentOptions);
            }

            return fallbackValue;
        }

        private static bool HasOption(string[] args, string[] options)
            => GetOptionValue(args, options) is not null;

        private static string GetOptionValue(string[] args, string[] options)
        {
            for (int i = 0; i < args.Length; i += 1)
            {
                string arg = args[i];

                foreach (string option in options)
                {
                    if (string.Equals(arg, option, StringComparison.Ordinal))
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