using StreamToM3U.Configuration;

namespace StreamToM3U.Service.Models
{
    public sealed class StreamInfo
    {
        public StreamProvider Provider { get; set; }

        public string ChannelId { get; set; }

        public string Title { get; set; }
        
        public string Url { get; set; }
    }
}