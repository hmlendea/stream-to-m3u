using StreamToM3U.Configuration;

namespace StreamToM3U.Service.Models
{
    public sealed class ChannelStream
    {
        public string Id { get; set; }

        public string ChannelName { get; set; }

        public StreamProvider Provider { get; set; }

        public string ChannelId { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string StreamBaseUrl { get; set; }
    }
}
