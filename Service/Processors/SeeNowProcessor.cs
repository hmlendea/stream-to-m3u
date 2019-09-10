using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamToM3U.Service.Processors
{
    public sealed class SeeNowProcessor : ISeeNowProcessor
    {
        static string SeeNowUrl => "http://www.seenow.ro";
        static string SeeNowChannelUrlFormat => $"{SeeNowUrl}/live-{{0}}-9";

        const string PlaylistUrlPattern = "file: *\"(http[^\"]*)\"";

        readonly IFileDownloader downloader;

        public SeeNowProcessor(IFileDownloader downloader)
        {
            this.downloader = downloader;
        }

        public async Task<string> GetUrlAsync(string channelId)
        {
            string channelUrl = string.Format(SeeNowChannelUrlFormat, channelId);
            string html = await downloader.TryDownloadStringAsync(channelUrl);
            
            return Regex.Match(html, PlaylistUrlPattern).Groups[1].Value;
        }
    }
}
