using NuciLog.Core;

namespace StreamToM3U.Logging
{
    public sealed class MyLogInfoKey : LogInfoKey
    {
        public static LogInfoKey ChannelStreamId => new MyLogInfoKey(nameof(ChannelStreamId));

        public static LogInfoKey Url => new MyLogInfoKey(nameof(Url));

        private MyLogInfoKey(string name)
            : base(name)
        {
        }
    }
}
