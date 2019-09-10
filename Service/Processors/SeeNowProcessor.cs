using System.Text.RegularExpressions;
using System.Threading.Tasks;

using StreamToM3U.Service.Models;

namespace StreamToM3U.Service.Processors
{
    public sealed class SeeNowProcessor : IProcessor
    {
        static string SeeNowUrl => "http://www.seenow.ro";
        static string SeeNowChannelUrlFormat => $"{SeeNowUrl}/live-{{0}}-9";

        const string PlaylistUrlPattern = "file: *\"(http[^\"]*)\"";

        readonly IFileDownloader downloader;

        public SeeNowProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public async Task<string> GetUrlAsync(StreamInfo streamInfo)
        {
            string channelUrl = string.Format(SeeNowChannelUrlFormat, streamInfo.ChannelId);
            string html = await downloader.TryDownloadStringAsync(channelUrl);
            
            return Regex.Match(html, PlaylistUrlPattern).Groups[1].Value;
        }
    }
}
