using NuciDAL.DataObjects;

namespace StreamToM3U.DataAccess.DataObjects
{
    public sealed class ChannelStreamEntity : EntityBase
    {
        public string ChannelName { get; set; }

        public string Provider { get; set; }

        public string ChannelId { get; set; }

        public string Title { get; set; }

        public string Url { get; set; }

        public string StreamBaseUrl { get; set; }
    }
}
