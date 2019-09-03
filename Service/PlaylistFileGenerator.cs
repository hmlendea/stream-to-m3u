using System;
using System.Collections.Generic;
using System.IO;

using NuciDAL.Repositories;

using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Net;
using StreamToM3U.Service.Mapping;
using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public sealed class PlaylistFileGenerator : IPlaylistFileGenerator
    {
        readonly IFileDownloader fileDownloader;
        readonly IPlaylistUrlRetriever urlRetriever;
        readonly Repository<ChannelStreamEntity> channelStreamRepository;

        public PlaylistFileGenerator(
            IFileDownloader fileDownloader,
            IPlaylistUrlRetriever urlRetriever,
            Repository<ChannelStreamEntity> channelStreamRepository)
        {
            this.fileDownloader = fileDownloader;
            this.urlRetriever = urlRetriever;
            this.channelStreamRepository = channelStreamRepository;
        }

        public void GeneratePlaylist(string inputFile, string outputFile)
        {
            IEnumerable<ChannelStream> channelStreams = channelStreamRepository.GetAll().ToServiceModels();

            string playlist = "#EXTM3U" + Environment.NewLine;

            foreach (ChannelStream channelStream in channelStreams)
            {
                playlist += $"#EXTINF:-1,{channelStream.ChannelName}";
                playlist += urlRetriever.GetStreamUrl(channelStream);
            }

            File.WriteAllText(outputFile, playlist);
        }
    }
}
