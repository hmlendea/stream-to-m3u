using System;

using Microsoft.Extensions.DependencyInjection;

using StreamToM3U.Net;
using StreamToM3U.Service;

namespace StreamToM3U
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton<IFileDownloader, FileDownloader>()
                .AddSingleton<IPlaylistUrlRetriever, PlaylistUrlRetriever>()
                .BuildServiceProvider();
            
            IPlaylistUrlRetriever urlRetriever = serviceProvider.GetService<IPlaylistUrlRetriever>();
            string playlistUrl = urlRetriever.GetStreamUrl(args);
            Console.WriteLine(playlistUrl);
        }
    }
}
