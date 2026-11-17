using NuciLog.Core;

namespace StreamToM3U.Logging
{
    public sealed class MyOperation : Operation
    {
        public static Operation FileDownload => new MyOperation(nameof(FileDownload));

        public static Operation ChannelStreamFetching => new MyOperation(nameof(ChannelStreamFetching));

        private MyOperation(string name)
            : base(name)
        {
        }
    }
}
