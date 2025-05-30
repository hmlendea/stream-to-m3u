using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NuciDAL.Repositories;
using NuciExtensions;
using NuciLog.Core;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Logging;
using StreamToM3U.Service.Mapping;
using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public sealed class PlaylistFileGenerator(
        IFileDownloader fileDownloader,
        IPlaylistUrlRetriever urlRetriever,
        Options options,
        IRepository<ChannelStreamEntity> channelStreamRepository,
        ILogger logger) : IPlaylistFileGenerator
    {
        readonly IFileDownloader fileDownloader = fileDownloader;
        readonly IPlaylistUrlRetriever urlRetriever = urlRetriever;
        readonly Options options = options;
        readonly IRepository<ChannelStreamEntity> channelStreamRepository = channelStreamRepository;
        readonly ILogger logger = logger;

        public void GeneratePlaylist()
        {
            IEnumerable<ChannelStream> channelStreams = channelStreamRepository
                .GetAll()
                .ToServiceModels();

            List<Task> tasks = [];
            ConcurrentDictionary<string, List<string>> foundChannelUrls =
                new();

            Parallel.ForEach(channelStreams, channelStream =>
            {
                logger.Info(
                    MyOperation.ChannelStreamFetching,
                    OperationStatus.Started,
                    new LogInfo(MyLogInfoKey.ChannelStreamId, channelStream.Id));

                string url = urlRetriever.GetStreamUrlAsync(channelStream).Result;

                if (string.IsNullOrWhiteSpace(url))
                {
                    logger.Debug(
                        MyOperation.ChannelStreamFetching,
                        OperationStatus.Failure,
                        new LogInfo(MyLogInfoKey.ChannelStreamId, channelStream.Id));
                }
                else
                {
                    logger.Debug(
                        MyOperation.ChannelStreamFetching,
                        OperationStatus.Success,
                        new LogInfo(MyLogInfoKey.ChannelStreamId, channelStream.Id));

                    if (!foundChannelUrls.ContainsKey(channelStream.ChannelName))
                    {
                        foundChannelUrls.TryAdd(channelStream.ChannelName, []);
                    }

                    foundChannelUrls[channelStream.ChannelName].Add(url);
                }
            });

            Dictionary<string, List<string>> channelUrls = foundChannelUrls
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Value);

            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                GeneratePlaylistFile(channelUrls);
            }
            else
            {
                GeneratePlaylistDirectory(channelUrls);
            }
        }

        public void GeneratePlaylistFile(Dictionary<string, List<string>> channelUrls)
        {
            IList<string> playlistLines = ["#EXTM3U"];

            foreach (string channelName in channelUrls.Keys)
            {
                foreach (string url in channelUrls[channelName])
                {
                    string content = fileDownloader.TryDownloadStringAsync(url).Result;

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        continue;
                    }

                    playlistLines.Add($"#EXTINF:-1,{channelName}");
                    playlistLines.Add(url);
                }
            }

            File.WriteAllLines(options.OutputFile, playlistLines);
        }

        public void GeneratePlaylistDirectory(Dictionary<string, List<string>> channelUrls)
        {
            if (!Directory.Exists(options.OutputDirectory))
            {
                Directory.CreateDirectory(options.OutputDirectory);
            }

            string playlistFilePath = Path.Combine(options.OutputDirectory, "playlist.m3u");
            IList<string> playlistLines = ["#EXTM3U"];

            foreach (string channelName in channelUrls.Keys)
            {
                string fileName = GenerateChannelFileName(channelName);
                string channelFilePath = Path.Combine(Path.GetFullPath(options.OutputDirectory), fileName);
                IList<string> channelFileLines = [];

                playlistLines.Add($"#EXTINF:-1,{channelName}");

                if (string.IsNullOrWhiteSpace(options.Url))
                {
                    playlistLines.Add(channelFilePath);
                }
                else
                {
                    playlistLines.Add($"{options.Url}/{fileName}");
                }

                channelFileLines.Add("#EXTM3U");

                foreach (string url in channelUrls[channelName])
                {
                    channelFileLines.Add($"#EXT-X-STREAM-INF:BANDWIDTH=873");
                    channelFileLines.Add(url);
                }

                File.WriteAllLines(channelFilePath, channelFileLines);
            }

            File.WriteAllLines(playlistFilePath, playlistLines);
        }

        static string GenerateChannelFileName(string channelName)
        {
            string normalisedChannelName = channelName;

            if (channelName.Contains(':'))
            {
                normalisedChannelName = channelName.Substring(channelName.IndexOf(':') + 1);
            }

            normalisedChannelName = normalisedChannelName
                .RemoveDiacritics()
                .RemovePunctuation()
                .Replace(" ", "");

            return $"{normalisedChannelName}.m3u8";
        }
    }
}
