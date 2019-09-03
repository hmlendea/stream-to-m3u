using System;

using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Net;
using StreamToM3U.Service;

namespace StreamToM3U
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Options options = Options.FromArguments(args);
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton<IRepository<ChannelStreamEntity>>(x => new XmlRepository<ChannelStreamEntity>(options.InputFile))
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistUrlRetriever, PlaylistUrlRetriever>()
                .AddSingleton<IPlaylistFileGenerator, PlaylistFileGenerator>()
                .BuildServiceProvider();

            if (string.IsNullOrWhiteSpace(options.InputFile))
            {
                IPlaylistUrlRetriever urlRetriever = serviceProvider.GetService<IPlaylistUrlRetriever>();
                string playlistUrl = urlRetriever.GetStreamUrl(options);
                Console.WriteLine(playlistUrl);
            }
            else
            {
                IPlaylistFileGenerator fileGenerator = serviceProvider.GetService<IPlaylistFileGenerator>();
                fileGenerator.GeneratePlaylist(options.InputFile, options.OutputFile);
            }
        }
    }
}
