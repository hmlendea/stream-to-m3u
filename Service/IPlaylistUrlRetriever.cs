using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public interface IPlaylistUrlRetriever
    {
        string GetStreamUrl(StreamInfo streamInfo);

        string GetStreamUrl(ChannelStream channelStream);
    }
}
