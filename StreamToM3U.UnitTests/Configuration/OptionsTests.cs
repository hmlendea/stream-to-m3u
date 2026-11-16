using NUnit.Framework;

using StreamToM3U.Configuration;

namespace StreamToM3U.UnitTests.Configuration
{
    [TestFixture]
    public sealed class OptionsTests
    {
        [Test]
        public static void GivenNoArguments_WhenParsing_ThenAllValuesAreSetToTheirDefaults()
        {
            Options options = Options.FromArguments([]);

            Assert.Multiple(() =>
            {
                Assert.That(options.Provider, Is.EqualTo(StreamProvider.Website));
                Assert.That(options.InputFile, Is.Null);
                Assert.That(options.OutputFile, Is.EqualTo("playlist.m3u"));
                Assert.That(options.OutputDirectory, Is.Null);
                Assert.That(options.ChannelId, Is.Null);
                Assert.That(options.Title, Is.Null);
                Assert.That(options.Url, Is.Null);
                Assert.That(options.StreamBaseUrl, Is.Null);
            });
        }

        [TestCase("-i", "Data/input-ro.xml")]
        [TestCase("--input", "Data/input-ro.xml")]
        [TestCase("--input=Data/input-ro.xml", "Data/input-ro.xml")]
        [TestCase("-i=Data/input-ro.xml", "Data/input-ro.xml")]
        public static void GivenAnInputFileArgument_WhenParsing_ThenTheInputFileIsSet(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).InputFile, Is.EqualTo(value));

        [TestCase("-o", "custom.m3u")]
        [TestCase("--output-file", "custom.m3u")]
        [TestCase("--output-file=custom.m3u", "custom.m3u")]
        public static void GivenAnOutputFileArgument_WhenParsing_ThenTheOutputFileIsSet(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).OutputFile, Is.EqualTo(value));

        [TestCase("-O", "playlists")]
        [TestCase("--output-dir", "playlists")]
        [TestCase("--output-directory", "playlists")]
        [TestCase("--output-dir=playlists", "playlists")]
        public static void GivenAnOutputDirectoryArgument_WhenParsing_ThenTheOutputDirectoryIsSet(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).OutputDirectory, Is.EqualTo(value));

        [TestCase("-c", "protv")]
        [TestCase("--channel", "protv")]
        [TestCase("--channel=protv", "protv")]
        public static void GivenAChannelArgument_WhenParsing_ThenTheChannelIdIsSet(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).ChannelId, Is.EqualTo(value));

        [TestCase("-t", "RO: Pro TV")]
        [TestCase("--title", "RO: Pro TV")]
        [TestCase("--title=RO: Pro TV", "RO: Pro TV")]
        public static void GivenATitleArgument_WhenParsing_ThenTheTitleIsSet(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).Title, Is.EqualTo(value));

        [TestCase("-u", "http://dummy-url.ro/live")]
        [TestCase("--url", "http://dummy-url.ro/live")]
        [TestCase("--url=http://dummy-url.ro/live", "http://dummy-url.ro/live")]
        public static void GivenAUrlArgument_WhenParsing_ThenTheUrlIsSet(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).Url, Is.EqualTo(value));

        [TestCase("-U", "http://cdn.nucilandia.ro/live")]
        [TestCase("--baseurl", "http://cdn.nucilandia.ro/live")]
        [TestCase("--baseurl=http://cdn.nucilandia.ro/live", "http://cdn.nucilandia.ro/live")]
        public static void GivenAStreamBaseUrlArgument_WhenParsing_ThenTheStreamBaseUrlIsSet(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).StreamBaseUrl, Is.EqualTo(value));

        [TestCase("-i")]
        [TestCase("--input")]
        public static void GivenAnInputOptionWithoutAValue_WhenParsing_ThenTheInputFileIsEmpty(string argument)
            => Assert.That(Options.FromArguments([argument]).InputFile, Is.Empty);

        [TestCase("-o")]
        [TestCase("--output-file")]
        public static void GivenAnOutputOptionWithoutAValue_WhenParsing_ThenTheOutputFileIsEmpty(string argument)
            => Assert.That(Options.FromArguments([argument]).OutputFile, Is.Empty);

        [TestCase("-u")]
        [TestCase("--url")]
        public static void GivenAUrlOptionWithoutAValue_WhenParsing_ThenTheUrlIsEmpty(string argument)
            => Assert.That(Options.FromArguments([argument]).Url, Is.Empty);

        [Test]
        public static void GivenAnOptionFollowedByAnotherOption_WhenParsing_ThenTheValueIsEmpty()
            => Assert.That(Options.FromArguments(["--url", "--title", "RO: Pro TV"]).Url, Is.Empty);

        [Test]
        public static void GivenAnOptionFollowedByAnotherOption_WhenParsing_ThenTheFollowingOptionIsParsed()
            => Assert.That(Options.FromArguments(["--url", "--title", "RO: Pro TV"]).Title, Is.EqualTo("RO: Pro TV"));

        [Test]
        public static void GivenRepeatedOptions_WhenParsing_ThenTheFirstValueIsUsed()
            => Assert.That(
                Options.FromArguments(["--url", "http://dummy-url.ro/live", "--url", "http://test.url.ro/live"]).Url,
                Is.EqualTo("http://dummy-url.ro/live"));

        [Test]
        public static void GivenAnInlineValueContainingSeparators_WhenParsing_ThenTheWholeValueIsUsed()
            => Assert.That(
                Options.FromArguments(["--url=http://dummy-url.ro/live?token=613&channel=4"]).Url,
                Is.EqualTo("http://dummy-url.ro/live?token=613&channel=4"));

        [Test]
        public static void GivenAnInlineEmptyValue_WhenParsing_ThenTheValueIsEmpty()
            => Assert.That(Options.FromArguments(["--url="]).Url, Is.Empty);

        [TestCase("--Url", "http://dummy-url.ro/live")]
        [TestCase("--URL", "http://dummy-url.ro/live")]
        [TestCase("-u2", "http://dummy-url.ro/live")]
        [TestCase("url", "http://dummy-url.ro/live")]
        public static void GivenAnUnrecognisedArgument_WhenParsing_ThenTheValueRemainsNull(string argument, string value)
            => Assert.That(Options.FromArguments([argument, value]).Url, Is.Null);

        [Test]
        public static void GivenSurroundingPositionalArguments_WhenParsing_ThenTheOptionsAreStillParsed()
            => Assert.That(
                Options.FromArguments(["stray", "--url", "http://dummy-url.ro/live", "more"]).Url,
                Is.EqualTo("http://dummy-url.ro/live"));

        [TestCase("--tvs")]
        [TestCase("--tvsport")]
        [TestCase("--tvshd")]
        [TestCase("--tvsporthd")]
        public static void GivenATvSportHdFlag_WhenParsing_ThenTheTvSportHdProviderIsSelected(string argument)
            => Assert.That(Options.FromArguments([argument]).Provider, Is.EqualTo(StreamProvider.TvSportHd));

        [TestCase("--antena-play")]
        [TestCase("--antenaplay")]
        [TestCase("--antena")]
        [TestCase("--aplay")]
        [TestCase("--ap")]
        public static void GivenAnAntenaPlayFlag_WhenParsing_ThenTheAntenaPlayProviderIsSelected(string argument)
            => Assert.That(Options.FromArguments([argument]).Provider, Is.EqualTo(StreamProvider.AntenaPlay));

        [TestCase("--sl")]
        [TestCase("--streamlink")]
        public static void GivenAStreamlinkFlag_WhenParsing_ThenTheStreamlinkProviderIsSelected(string argument)
            => Assert.That(Options.FromArguments([argument]).Provider, Is.EqualTo(StreamProvider.Streamlink));

        [TestCase("--unknown")]
        [TestCase("--TVS")]
        [TestCase("--url")]
        public static void GivenNoProviderFlag_WhenParsing_ThenTheWebsiteProviderIsSelected(string argument)
            => Assert.That(Options.FromArguments([argument]).Provider, Is.EqualTo(StreamProvider.Website));

        [TestCase("--sl", "--tvs", StreamProvider.TvSportHd)]
        [TestCase("--tvsporthd", "--antena", StreamProvider.TvSportHd)]
        [TestCase("--antena", "--sl", StreamProvider.AntenaPlay)]
        public static void GivenMultipleProviderFlags_WhenParsing_ThenTheHighestPriorityProviderIsSelected(
            string firstArgument,
            string secondArgument,
            StreamProvider expectedProvider)
            => Assert.That(
                Options.FromArguments([firstArgument, secondArgument]).Provider,
                Is.EqualTo(expectedProvider));
    }
}
