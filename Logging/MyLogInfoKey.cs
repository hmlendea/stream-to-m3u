using NuciLog.Core;

namespace StreamToM3U.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        MyLogInfoKey(string name)
            : base(name)
        {
            
        }

        public static LogInfoKey ChannelStreamId => new MyLogInfoKey(nameof(ChannelStreamId));

        public static LogInfoKey Url => new MyLogInfoKey(nameof(Url));
    }
}
