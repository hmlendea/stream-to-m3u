using System;

using StreamToM3U.Net;
using StreamToM3U.Service;

namespace StreamToM3U
{
    public class Program
    {
        static readonly IPlaylistUrlRetriever urlRetriever;
        static readonly IFileDownloader downloader;

        static Program()
        {
            downloader = new FileDownloader(); 
        }

        public static void Main(string[] args)
        {
            string playlistUrl = urlRetriever.GetStreamUrl(args);
            Console.WriteLine(playlistUrl);
        }
    }
}
