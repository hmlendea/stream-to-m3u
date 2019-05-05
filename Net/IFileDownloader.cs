using System;

namespace StreamToM3U.Net
{
    public interface IFileDownloader : IDisposable
    {
        string Download(string url);

        string TryDownload(string url);
    }
}
