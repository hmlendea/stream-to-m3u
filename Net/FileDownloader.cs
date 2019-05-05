using System;
using System.Net;

namespace StreamToM3U.Net
{
    public sealed class FileDownloader : IFileDownloader
    {
        readonly WebClient client;

        public FileDownloader()
        {
            client = new WebClient();
        }

        ~FileDownloader()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public string Download(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException(nameof(url));
            }
            
            return client.DownloadString(url);
        }

        public string TryDownload(string url)
        {
            try
            {
                return Download(url);
            }
            catch
            {
                return null;
            }
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                client.Dispose();
            }
        }
    }
}
