using System.Threading.Tasks;

namespace StreamToM3U.Service.Processors
{
    public interface IOtherProcessor
    {
        Task<string> GetUrlAsync(string url);
    }
}
