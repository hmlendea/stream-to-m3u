using NUnit.Framework;

using StreamToM3U.Logging;

namespace StreamToM3U.UnitTests.Logging
{
    [TestFixture]
    public sealed class MyLogInfoKeyTests
    {
        [Test]
        public static void GivenTheUrlLogInfoKey_WhenReadingItsName_ThenItIsUrl()
            => Assert.That(MyLogInfoKey.Url.Name, Is.EqualTo(nameof(MyLogInfoKey.Url)));

        [Test]
        public static void GivenTheChannelStreamIdLogInfoKey_WhenReadingItsName_ThenItIsChannelStreamId()
            => Assert.That(
                MyLogInfoKey.ChannelStreamId.Name,
                Is.EqualTo(nameof(MyLogInfoKey.ChannelStreamId)));

        [Test]
        public static void GivenTwoLogInfoKeysWithDifferentNames_WhenComparingThem_ThenTheyAreNotEqual()
            => Assert.That(MyLogInfoKey.Url, Is.Not.EqualTo(MyLogInfoKey.ChannelStreamId));
    }
}
