using System.Threading.Tasks;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Processors
{
    public interface IProcessor
    {
        Task<string> GetUrlAsync(StreamInfo streamInfo);
    }
}
