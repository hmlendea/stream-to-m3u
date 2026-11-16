using NUnit.Framework;

using StreamToM3U.Configuration;

namespace StreamToM3U.UnitTests.Configuration
{
    [TestFixture]
    public sealed class StreamProviderTests
    {
        [Test]
        public static void GivenTheStreamProviderValues_WhenReadingThem_ThenEveryProviderIsPresent()
            => Assert.That(
                System.Enum.GetValues<StreamProvider>(),
                Is.EqualTo(
                [
                    StreamProvider.Website,
                    StreamProvider.TvSportHd,
                    StreamProvider.AntenaPlay,
                    StreamProvider.Streamlink
                ]));

        [TestCase(StreamProvider.Website, 0)]
        [TestCase(StreamProvider.TvSportHd, 1)]
        [TestCase(StreamProvider.AntenaPlay, 2)]
        [TestCase(StreamProvider.Streamlink, 3)]
        public static void GivenAProvider_WhenReadingItsNumericValue_ThenItMatchesThePersistedContract(
            StreamProvider provider,
            int expectedValue)
            => Assert.That((int)provider, Is.EqualTo(expectedValue));

        [TestCase("Website", StreamProvider.Website)]
        [TestCase("TvSportHd", StreamProvider.TvSportHd)]
        [TestCase("AntenaPlay", StreamProvider.AntenaPlay)]
        [TestCase("Streamlink", StreamProvider.Streamlink)]
        public static void GivenAProviderName_WhenParsingIt_ThenTheMatchingProviderIsReturned(
            string name,
            StreamProvider expectedProvider)
            => Assert.That(System.Enum.Parse<StreamProvider>(name), Is.EqualTo(expectedProvider));
    }
}
