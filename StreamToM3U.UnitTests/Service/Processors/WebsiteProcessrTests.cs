using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using StreamToM3U.Service;
using StreamToM3U.Service.Models;
using StreamToM3U.Service.Processors;

namespace StreamToM3U.UnitTests.Service.Processors
{
    [TestFixture]
    public sealed class WebsiteProcessrTests
    {
        const string PageUrl = "http://dummy-url.ro/live/ro";
        const string PlaylistUrl = "http://cdn.nucilandia.ro/live/playmaster.m3u8";
        const string StreamBaseUrl = "http://base.nucilandia.ro";

        private Mock<IFileDownloader> downloader;

        [SetUp]
        public void SetUp() => downloader = new Mock<IFileDownloader>();

        [TestCase("<a href=\"http://cdn.nucilandia.ro/live/playmaster.m3u8\">Știrile Pro TV</a>")]
        [TestCase("<a href=\"http://cdn.nucilandia.ro/live/playmaster.m3u8?token=613\">Știrile Pro TV</a>")]
        [TestCase("<script>var url = \"http://cdn.nucilandia.ro/live/playmaster.m3u8\";</script>")]
        public async Task GivenADoubleQuotedPlaylistUrl_WhenResolving_ThenThePlaylistUrlIsReturned(string html)
        {
            SetPageContent(html);

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Does.StartWith("http://cdn.nucilandia.ro/live/playmaster.m3u8"));
        }

        [TestCase("<a href='http://cdn.nucilandia.ro/live/playmaster.m3u8'>Știrile Pro TV</a>")]
        [TestCase("<script>var url = 'http://cdn.nucilandia.ro/live/playmaster.m3u8';</script>")]
        public async Task GivenASingleQuotedPlaylistUrl_WhenResolving_ThenThePlaylistUrlIsReturned(string html)
        {
            SetPageContent(html);

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo(PlaylistUrl));
        }

        [Test]
        public async Task GivenAMixedQuotingPage_WhenResolving_ThenTheDoubleQuotedUrlIsPreferred()
        {
            SetPageContent(
                $"<a href=\"http://cdn.nucilandia.ro/first/playmaster.m3u8\">a</a>" +
                $"<a href='http://cdn.nucilandia.ro/second/playmaster.m3u8'>b</a>");

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo("http://cdn.nucilandia.ro/first/playmaster.m3u8"));
        }

        [Test]
        public async Task GivenARelativePlaylistUrlAndABaseUrl_WhenResolving_ThenThePlaylistUrlIsBuiltFromTheBaseUrl()
        {
            SetPageContent("<a href=\"assets/playmaster.m3u8\">Știrile Pro TV</a>");
            StreamInfo streamInfo = BuildStreamInfo();
            streamInfo.StreamBaseUrl = StreamBaseUrl;

            string result = await BuildProcessor().GetUrlAsync(streamInfo);

            Assert.That(result, Is.EqualTo($"{StreamBaseUrl}/assets/playmaster.m3u8"));
        }

        [Test]
        public async Task GivenARelativePlaylistUrlAndNoBaseUrl_WhenResolving_ThenAnEmptyStringIsReturned()
        {
            SetPageContent("<a href=\"assets/playmaster.m3u8\">Știrile Pro TV</a>");

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Empty);
        }

        [TestCase("")]
        [TestCase("   ")]
        public async Task GivenAnEmptyOrWhitespacePage_WhenResolving_ThenNullIsReturned(string html)
        {
            SetPageContent(html);

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GivenANullPage_WhenResolving_ThenNullIsReturned()
        {
            SetPageContent(null!);

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Null);
        }

        [TestCase("no playlist reference here")]
        [TestCase("http://cdn.nucilandia.ro/live/playmaster.m3u8")]
        [TestCase("<a href=\"assets/playmaster.m3u8\">Știrile Pro TV</a>")]
        public async Task GivenAPageWithoutAnAbsolutePlaylistUrl_WhenResolving_ThenAnEmptyStringIsReturned(string html)
        {
            SetPageContent(html);

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Empty);
        }

        [TestCase("<a href=\"http://cdn.nucilandia.ro/live/playmaster.m3u8\">\n</a>", PlaylistUrl)]
        [TestCase("<a href=\"http://cdn.nucilandia.ro/live/playmaster.m3u8\">\r\n</a>", PlaylistUrl)]
        [TestCase("<a href=\"http://cdn.nucilandia.ro/live/playmaster.m3u8&amp;token=613\">a</a>", "http://cdn.nucilandia.ro/live/playmaster.m3u8&token=613")]
        public async Task GivenAPlaylistUrlWithSurroundingMarkup_WhenResolving_ThenTheUrlIsSanitised(
            string html,
            string expectedPlaylistUrl)
        {
            SetPageContent(html);

            string result = await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo(expectedPlaylistUrl));
        }

        [Test]
        public async Task GivenAPageUrl_WhenResolving_ThenThePageIsDownloadedExactlyOnce()
        {
            SetPageContent($"\"{PlaylistUrl}\"");

            await BuildProcessor().GetUrlAsync(BuildStreamInfo());

            downloader.Verify(x => x.TryDownloadStringAsync(PageUrl), Times.Once);
        }

        private static StreamInfo BuildStreamInfo() => new()
        {
            Url = PageUrl
        };

        private WebsiteProcessr BuildProcessor() => new(downloader.Object);

        private void SetPageContent(string html)
            => downloader
                .Setup(x => x.TryDownloadStringAsync(PageUrl))
                .ReturnsAsync(html);
    }
}
