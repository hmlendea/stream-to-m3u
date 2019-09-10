using System.Threading.Tasks;

namespace StreamToM3U.Service.Processors
{
    public interface IYouTubeStreamProcessor
    {
        Task<string> GetUrlAsync(string channelId);
        
        Task<string> GetUrlAsync(string channelId, string streamTitle);
    }
}
