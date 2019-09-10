using System;

using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Service;
using StreamToM3U.Service.Models;

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
                StreamInfo streamInfo = new StreamInfo();
                streamInfo.Provider = options.Provider;
                streamInfo.ChannelId = options.ChannelId;
                streamInfo.Title = options.Title;
                streamInfo.Url = options.Url;

                IPlaylistUrlRetriever urlRetriever = serviceProvider.GetService<IPlaylistUrlRetriever>();
                string playlistUrl = urlRetriever.GetStreamUrl(streamInfo);
                
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
