using StreamToM3U.Service.Models;

namespace StreamToM3U.Service
{
    public interface IPlaylistUrlRetriever
    {
        string GetStreamUrl(StreamProvider provider, string argument1);

        string GetStreamUrl(StreamProvider provider, string argument1, string argument2);
    }
}
