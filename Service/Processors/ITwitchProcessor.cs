namespace StreamToM3U.Service.Processors
{
    public interface ITwitchProcessor
    {
        string GetPlaylistUrl(string channelId);
    }
}
