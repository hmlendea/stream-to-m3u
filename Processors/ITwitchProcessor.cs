namespace StreamToM3U.Processors
{
    public interface ITwitchProcessor
    {
        string GetPlaylistUrl(string channelId);
    }
}
