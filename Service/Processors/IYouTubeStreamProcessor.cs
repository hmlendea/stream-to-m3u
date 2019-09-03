namespace StreamToM3U.Service.Processors
{
    public interface IYouTubeStreamProcessor
    {
        string GetPlaylistUrl(string channelId);
        
        string GetPlaylistUrl(string channelId, string streamTitle);
    }
}
