namespace SocialMediaStreamToIptv.Processors
{
    public interface IYouTubeStreamProcessor
    {
        string GetPlaylistUrl(
            string channelId,
            string streamTitle);
    }
}
