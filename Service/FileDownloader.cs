using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using NuciLog.Core;

using StreamToM3U.Configuration;
using StreamToM3U.Logging;

namespace StreamToM3U.Service
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly ILogger logger;
        readonly HttpClient httpClient;

        public FileDownloader(
            ApplicationSettings applicationSettings,
            ILogger logger)
        {
            this.logger = logger;

            httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(applicationSettings.RequestTimeout)
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", applicationSettings.UserAgent);
        }

        public async Task<string> TryDownloadStringAsync(string url)
        {
            string normalisedUrl = NormaliseUrl(url);

            logger.Debug(
                MyOperation.FileDownload,
                OperationStatus.Started,
                new LogInfo(MyLogInfoKey.Url, normalisedUrl));

            try
            {
                string result = await GetAsync(normalisedUrl);

                logger.Verbose(
                    MyOperation.FileDownload,
                    OperationStatus.Success,
                    new LogInfo(MyLogInfoKey.Url, normalisedUrl));

                return result;
            }
            catch (Exception ex)
            {
                logger.Verbose(
                    MyOperation.FileDownload,
                    OperationStatus.Failure,
                    ex,
                    new LogInfo(MyLogInfoKey.Url, normalisedUrl));

                return null;
            }
        }

        async Task<string> GetAsync(string url)
        {
            using HttpResponseMessage response = await httpClient.GetAsync(url);
            using HttpContent content = response.Content;
            byte[] bytes = await content.ReadAsByteArrayAsync();
            string text = Encoding.UTF8.GetString(bytes);

            return text;
        }

        static string NormaliseUrl(string url)
        {
            if (url is null)
            {
                return null;
            }

            return HttpUtility
                .UrlDecode(url)
                .Replace("\\/", "/")
                .Replace("#038;", "");
        }
    }
}
