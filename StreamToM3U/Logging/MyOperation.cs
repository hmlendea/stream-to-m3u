using NuciLog.Core;

namespace StreamToM3U.Logging
{
    public sealed class MyOperation : Operation
    {
        MyOperation(string name)
            : base(name)
        {
            
        }

        public static Operation FileDownload => new MyOperation(nameof(FileDownload));

        public static Operation ChannelStreamFetching => new MyOperation(nameof(ChannelStreamFetching));
    }
}
