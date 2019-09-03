namespace StreamToM3U.Service
{
    public interface IPlaylistUrlRetriever
    {
        string GetStreamUrl(string[] args);
    }
}
