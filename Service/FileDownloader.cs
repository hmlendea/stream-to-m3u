using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using StreamToM3U.Logging;

namespace StreamToM3U.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly ILogger logger;
        readonly HttpClient httpClient;

        public FileDownloader(ILogger logger)
        {
            this.logger = logger;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.79 Safari/537.36");
        }

        public async Task<string> TryDownloadStringAsync(string url)
        {
            logger.Debug(
                MyOperation.FileDownload,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Url, url));
            
            try
            {
                string result = await GetAsync(url);

                logger.Verbose(
                    MyOperation.FileDownload,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Url, url));

                return result;
            }
            catch
            {
                logger.Verbose(
                    MyOperation.FileDownload,
                    OperationStatus.Failure,
                    new LogInfo(MyLogInfoKey.Url, url));

                return null;
            }
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
