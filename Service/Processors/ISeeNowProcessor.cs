namespace StreamToM3U.Service.Processors
{
    public interface ISeeNowProcessor
    {
        string GetPlaylistUrl(string channelId);
    }
}
