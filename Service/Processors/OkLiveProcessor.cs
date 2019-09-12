using System.Text.RegularExpressions;
using System.Threading.Tasks;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Processors
{
    public sealed class OkLiveProcessor : IProcessor
    {
        static string HomePageUrl => "http://ok.ru";
        static string ChannelPageUrl => $"{HomePageUrl}/live/{{0}}";

        const string PlaylistUrlPattern = @"&quot;hlsMasterPlaylistUrl\\&quot;:\\&quot;(http[^&]*m3u8\?p)\\&quot;,";

        readonly IFileDownloader downloader;

        public OkLiveProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public async Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            string channelUrl = string.Format(ChannelPageUrl, streamInfo.ChannelId);
            string html = await downloader.TryDownloadStringAsync(channelUrl);
            
            return Regex.Match(html, PlaylistUrlPattern).Groups[1].Value;
        }
    }
}
