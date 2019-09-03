using NuciDAL.DataObjects;

namespace StreamToM3U.DataAccess.DataObjects
{
    public sealed class ChannelStreamEntity : EntityBase
    {
        public string ChannelName { get; set; }

        public string ProcessorId { get; set; }

        public string Argument1 { get; set; }
    }
}
