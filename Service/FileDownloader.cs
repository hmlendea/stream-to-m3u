using System.Net.Http;
using System.Threading.Tasks;

using NuciLog.Core;

using StreamToM3U.Configuration;
using StreamToM3U.Logging;

namespace StreamToM3U.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly ApplicationSettings applicationSettings;
        readonly ILogger logger;
        readonly HttpClient httpClient;

        public FileDownloader(
            ApplicationSettings applicationSettings,
            ILogger logger)
        {
            this.applicationSettings = applicationSettings;
            this.logger = logger;

            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", applicationSettings.UserAgent);
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
