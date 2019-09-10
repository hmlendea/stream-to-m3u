using System.Collections.Generic;
using System.IO;

using NuciDAL.Repositories;

using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Service.Mapping;
using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public sealed class PlaylistFileGenerator : IPlaylistFileGenerator
    {
        readonly IFileDownloader fileDownloader;
        readonly IPlaylistUrlRetriever urlRetriever;
        readonly IRepository<ChannelStreamEntity> channelStreamRepository;

        public PlaylistFileGenerator(
            IFileDownloader fileDownloader,
            IPlaylistUrlRetriever urlRetriever,
            IRepository<ChannelStreamEntity> channelStreamRepository)
        {
            this.fileDownloader = fileDownloader;
            this.urlRetriever = urlRetriever;
            this.channelStreamRepository = channelStreamRepository;
        }

        public void GeneratePlaylist(string inputFile, string outputFile)
        {
            IEnumerable<ChannelStream> channelStreams = channelStreamRepository.GetAll().ToServiceModels();
            IList<string> playlistLines = new List<string>();
            
            playlistLines.Add("#EXTM3U");

            foreach (ChannelStream channelStream in channelStreams)
            {
                string url = urlRetriever.GetStreamUrl(channelStream);

                if (string.IsNullOrWhiteSpace(url))
                {
                    continue;
                }

                playlistLines.Add($"#EXTINF:-1,{channelStream.ChannelName}");
                playlistLines.Add(url);
            }

            File.WriteAllLines(outputFile, playlistLines);
        }
    }
}
