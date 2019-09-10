using System.Net.Http;
using System.Threading.Tasks;

namespace StreamToM3U.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly HttpClient httpClient;

        public FileDownloader()
        {
            httpClient = new HttpClient();
        }

        public async Task<string> TryDownloadStringAsync(string url)
        {
            try
            {
                return await GetAsync(url);
            }
            catch { }
            
            return null;
        }

        async Task<string> GetAsync(string url)
        {
            using (HttpResponseMessage response = await httpClient.GetAsync(url))
            {
                using (HttpContent content = response.Content)
                {
                    return await content.ReadAsStringAsync();
                }
            }
        }
    }
}
