using NUnit.Framework;

using StreamToM3U.Logging;

namespace StreamToM3U.UnitTests.Logging
{
    [TestFixture]
    public sealed class MyOperationTests
    {
        [Test]
        public static void GivenTheFileDownloadOperation_WhenReadingItsName_ThenItIsFileDownload()
            => Assert.That(MyOperation.FileDownload.Name, Is.EqualTo(nameof(MyOperation.FileDownload)));

        [Test]
        public static void GivenTheChannelStreamFetchingOperation_WhenReadingItsName_ThenItIsChannelStreamFetching()
            => Assert.That(
                MyOperation.ChannelStreamFetching.Name,
                Is.EqualTo(nameof(MyOperation.ChannelStreamFetching)));
    }
}
