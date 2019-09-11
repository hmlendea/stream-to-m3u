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
            

            Dictionary<string, string> foundChannels = new Dictionary<string, string>();


            List<Task> tasks = new List<Task>();

            foreach (ChannelStream channelStream in channelStreams)
            {
                Task task = Task.Run(async () =>
                {
                    string url = await urlRetriever.GetStreamUrlAsync(channelStream);

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        foundChannels.Add(channelStream.ChannelName, url);
                    }
                });

                tasks.Add(task);
            }

            Task.WaitAll(tasks.ToArray());

            if (string.IsNullOrWhiteSpace(options.OutputDirectory))
            {
                GeneratePlaylistFile(foundChannels);
            }
            else
            {
                GeneratePlaylistDirectory(foundChannels);
            }
        }

        public void GeneratePlaylistFile(Dictionary<string, string> channels)
        {
            IList<string> playlistLines = new List<string>();
            playlistLines.Add("#EXTM3U");
            
            foreach (var channel in channels.OrderBy(x => x.Key))
            {
                playlistLines.Add($"#EXTINF:-1,{channel.Key}");
                playlistLines.Add(channel.Value);
            }

            File.WriteAllLines(options.OutputFile, playlistLines);
        }

        public void GeneratePlaylistDirectory(Dictionary<string, string> channels)
        {
            if (!Directory.Exists(options.OutputDirectory))
            {
                Directory.CreateDirectory(options.OutputDirectory);
            }

            string playlistFilePath = Path.Combine(options.OutputDirectory, "playlist.m3u");
            IList<string> playlistLines = new List<string>();
            playlistLines.Add("#EXTM3U");
            
            foreach (var channel in channels.OrderBy(x => x.Key))
            {
                playlistLines.Add($"#EXTINF:-1,{channel.Key}");
                string fileName = GenerateChannelFileName(channel.Key);

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
