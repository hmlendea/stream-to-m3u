using System.Threading.Tasks;

namespace StreamToM3U.Service
{
    public interface IFileDownloader
    {
        Task<string> TryDownloadStringAsync(string url);
    }
}
