using System.Threading.Tasks;

namespace StreamToM3U.Service.Processors
{
    public interface ITwitchProcessor
    {
        Task<string> GetUrlAsync(string channelId);
    }
}
