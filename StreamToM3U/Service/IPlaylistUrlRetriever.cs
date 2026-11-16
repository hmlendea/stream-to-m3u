using System.Threading.Tasks;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public interface IPlaylistUrlRetriever
    {
        Task<string> GetStreamUrlAsync(StreamInfo streamInfo);

        Task<string> GetStreamUrlAsync(ChannelStream channelStream);
    }
}
