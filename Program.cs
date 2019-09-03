using System;

using Microsoft.Extensions.DependencyInjection;

using NuciCLI;
using NuciDAL.Repositories;

using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Net;
using StreamToM3U.Service;
using StreamToM3U.Service.Models;

namespace StreamToM3U
{
    public class Program
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

        public static void Main(string[] args)
        {
            string inputFilePath = null;

            if (CliArgumentsReader.HasOption(InputFileOptions))
            {
                inputFilePath = CliArgumentsReader.GetOptionValue(InputFileOptions);
            }

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistUrlRetriever, PlaylistUrlRetriever>()
                .AddSingleton<IRepository<ChannelStreamEntity>>(x => new CsvRepository<ChannelStreamEntity>(inputFilePath))
                .BuildServiceProvider();

            StreamProvider provider = DetermineStreamProviderFromArgs(args);
            
            IPlaylistUrlRetriever urlRetriever = serviceProvider.GetService<IPlaylistUrlRetriever>();
            string playlistUrl = urlRetriever.GetStreamUrl(provider, null, null);
            Console.WriteLine(playlistUrl);
        }

        static StreamProvider DetermineStreamProviderFromArgs(string[] args)
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
    }
}
