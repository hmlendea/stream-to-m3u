using StreamToM3U.Configuration;

namespace StreamToM3U.Service
{
    public interface IPlaylistUrlRetriever
    {
        string GetStreamUrl(Options options);
    }
}
