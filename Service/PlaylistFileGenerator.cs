using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NuciDAL.Repositories;
using NuciExtensions;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Service.Mapping;
using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public sealed class PlaylistFileGenerator : IPlaylistFileGenerator
    {
        readonly IFileDownloader fileDownloader;
        readonly IPlaylistUrlRetriever urlRetriever;
        readonly Options options;
        readonly IRepository<ChannelStreamEntity> channelStreamRepository;

        public PlaylistFileGenerator(
            IFileDownloader fileDownloader,
            IPlaylistUrlRetriever urlRetriever,
            Options options,
            IRepository<ChannelStreamEntity> channelStreamRepository)
        {
            this.fileDownloader = fileDownloader;
            this.urlRetriever = urlRetriever;
            this.options = options;
            this.channelStreamRepository = channelStreamRepository;
        }

        public void GeneratePlaylist()
        {
            IEnumerable<ChannelStream> channelStreams = channelStreamRepository
                .GetAll()
                .ToServiceModels();

            ConcurrentDictionary<ChannelStream, string> foundChannelUrls =
                new ConcurrentDictionary<ChannelStream, string>();
            List<Task> tasks = new List<Task>();

            foreach (ChannelStream channelStream in channelStreams)
            {
                Task task = Task.Run(async () =>
                {
                    string url = await urlRetriever.GetStreamUrlAsync(channelStream);

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        foundChannelUrls.AddOrUpdate(channelStream, url);
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            Dictionary<ChannelStream, string> channelUrls = foundChannelUrls
                .OrderBy(x => x.Key.ChannelName)
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

        public void GeneratePlaylistFile(Dictionary<ChannelStream, string> channelUrls)
        {
            IList<string> playlistLines = new List<string>();
            playlistLines.Add("#EXTM3U");
            
            foreach (var channel in channelUrls)
            {
                string content = fileDownloader.TryDownloadStringAsync(channel.Value).Result;

                playlistLines.Add($"#EXTINF:-1,{channel.Key.ChannelName}");
                playlistLines.Add(channel.Value);
            }

            File.WriteAllLines(options.OutputFile, playlistLines);
        }

        public void GeneratePlaylistDirectory(Dictionary<ChannelStream, string> channelUrls)
        {
            if (!Directory.Exists(options.OutputDirectory))
            {
                Directory.CreateDirectory(options.OutputDirectory);
            }

            string playlistFilePath = Path.Combine(options.OutputDirectory, "playlist.m3u");
            IList<string> playlistLines = new List<string>();
            playlistLines.Add("#EXTM3U");
            
            foreach (var channel in channelUrls)
            {
                string fileName = GenerateChannelFileName(channel.Key.Id);

                // TODO: Broken async
                string content = fileDownloader.TryDownloadStringAsync(channel.Value).Result;
                string path = Path.Combine(Path.GetFullPath(options.OutputDirectory), fileName);

                File.WriteAllText(path, content);

                if (string.IsNullOrWhiteSpace(options.Url))
                {
                    playlistLines.Add(path);
                }
                else
                {
                    playlistLines.Add($"{options.Url}/{fileName}");
                }
            }

            File.WriteAllLines(playlistFilePath, playlistLines);
        }

        string GenerateChannelFileName(string channelName)
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
            
            return $"{normalisedChannelName}.m3u";
        }
    }
}
