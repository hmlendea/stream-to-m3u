using System;

using Microsoft.Extensions.DependencyInjection;

using NuciCLI;
using NuciDAL.Repositories;

using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Net;
using StreamToM3U.Service;

namespace StreamToM3U
{
    public class Program
    {
        static string[] InputFileOptions = { "-i" };

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
            
            IPlaylistUrlRetriever urlRetriever = serviceProvider.GetService<IPlaylistUrlRetriever>();
            string playlistUrl = urlRetriever.GetStreamUrl(args);
            Console.WriteLine(playlistUrl);
        }
    }
}
