using System.Threading.Tasks;

namespace StreamToM3U.Service.Processors
{
    public interface ISeeNowProcessor
    {
        Task<string> GetUrlAsync(string channelId);
    }
}
