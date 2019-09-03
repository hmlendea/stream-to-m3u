using StreamToM3U.Configuration;
using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public interface IPlaylistUrlRetriever
    {
        string GetStreamUrl(Options options);

        string GetStreamUrl(ChannelStream channelStream);
    }
}
