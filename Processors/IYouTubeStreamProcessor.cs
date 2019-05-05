namespace StreamToM3U.Processors
{
    public interface IYouTubeStreamProcessor
    {
        string GetPlaylistUrl(string channelId);
        
        string GetPlaylistUrl(string channelId, string streamTitle);
    }
}
