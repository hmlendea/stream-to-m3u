using System;

using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Service;
using StreamToM3U.Service.Models;
using StreamToM3U.Utils;

namespace StreamToM3U
{
    public class Program
    {
        static IServiceProvider serviceProvider;

        public static void Main(string[] args)
        {
            Options options = Options.FromArguments(args);

            serviceProvider = new ServiceCollection()
                .AddSingleton<IRepository<ChannelStreamEntity>>(x => new XmlRepository<ChannelStreamEntity>(options.InputFile))
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistUrlRetriever, PlaylistUrlRetriever>()
                .AddSingleton<IPlaylistFileGenerator, PlaylistFileGenerator>()
                .BuildServiceProvider();

            try
            {
                GetPlaylist(options);
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    Console.WriteLine(innerException);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                WebDriverHandler.CloseDriver();
            }
        }

        static void GetPlaylist(Options options)
        {
            if (string.IsNullOrWhiteSpace(options.InputFile))
            {
                GetPlaylistForStream(options);
            }
            else
            {
                GetPlaylistForInputFile(options);
            }
        }

        static void GetPlaylistForStream(Options options)
        {
            StreamInfo streamInfo = new StreamInfo();
            streamInfo.Provider = options.Provider;
            streamInfo.ChannelId = options.ChannelId;
            streamInfo.Title = options.Title;
            streamInfo.Url = options.Url;

            IPlaylistUrlRetriever urlRetriever = serviceProvider.GetService<IPlaylistUrlRetriever>();
            string playlistUrl = urlRetriever.GetStreamUrlAsync(streamInfo).Result;
            
            Console.WriteLine(playlistUrl);
        }

        static void GetPlaylistForInputFile(Options options)
        {
            IPlaylistFileGenerator fileGenerator = serviceProvider.GetService<IPlaylistFileGenerator>();
            fileGenerator.GeneratePlaylist(options.InputFile, options.OutputFile);
        }
    }
}
