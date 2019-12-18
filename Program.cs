using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NuciDAL.Repositories;
using NuciLog;
using NuciLog.Configuration;
using NuciLog.Core;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Service;
using StreamToM3U.Service.Models;
using StreamToM3U.Utils;

namespace StreamToM3U
{
    public class Program
    {
        static NuciLoggerSettings nuciLoggerSettings;
        static IServiceProvider serviceProvider;

        public static void Main(string[] args)
        {
            IConfiguration config = LoadConfiguration();

            nuciLoggerSettings = new NuciLoggerSettings();

            config.Bind(nameof(nuciLoggerSettings), nuciLoggerSettings);

            Options options = Options.FromArguments(args);

            Console.WriteLine(nuciLoggerSettings.LogFilePath);

            serviceProvider = new ServiceCollection()
                .AddSingleton(nuciLoggerSettings)
                .AddSingleton(options)
                .AddSingleton<IRepository<ChannelStreamEntity>>(x => new XmlRepository<ChannelStreamEntity>(options.InputFile))
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistUrlRetriever, PlaylistUrlRetriever>()
                .AddSingleton<IPlaylistFileGenerator, PlaylistFileGenerator>()
                .AddSingleton<ILogger, NuciLogger>()
                .BuildServiceProvider();
            
            ILogger logger = serviceProvider.GetService<ILogger>();
            logger.Info(Operation.StartUp, OperationStatus.Success);

            try
            {
                GetPlaylist(options);
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerException in ex.InnerExceptions)
                {
                    logger.Fatal(Operation.Unknown, OperationStatus.Failure, innerException);
                }
            }
            catch (Exception ex)
            {
                logger.Fatal(Operation.Unknown, OperationStatus.Failure, ex);
            }
            finally
            {
                WebDriverHandler.CloseDriver();
            }

            logger.Info(Operation.ShutDown, OperationStatus.Success);
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
            fileGenerator.GeneratePlaylist();
        }
        
        static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .Build();
        }
    }
}
