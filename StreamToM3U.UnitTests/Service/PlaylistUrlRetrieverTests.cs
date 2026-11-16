using System;
using System.Threading.Tasks;

using Moq;

using NUnit.Framework;

using StreamToM3U.Configuration;
using StreamToM3U.Service;
using StreamToM3U.Service.Models;

namespace StreamToM3U.UnitTests.Service
{
    [TestFixture]
    public sealed class PlaylistUrlRetrieverTests
    {
        const string PageUrl = "http://dummy-url.ro/live/ro";
        const string PlaylistUrl = "http://cdn.nucilandia.ro/live/playmaster.m3u8";

        private Mock<IFileDownloader> downloader;

        [SetUp]
        public void SetUp() => downloader = new Mock<IFileDownloader>();

        [TestCase("\"http://cdn.nucilandia.ro/live/playmaster.m3u8\"")]
        [TestCase("'http://cdn.nucilandia.ro/live/playmaster.m3u8'")]
        [TestCase("<a href=\"http://cdn.nucilandia.ro/live/playmaster.m3u8\">Știrile Pro TV</a>")]
        public async Task GivenAPageReferencingAnAbsolutePlaylist_WhenResolving_ThenTheReferencedPlaylistUrlIsReturned(string page)
        {
            SetPageContent(page);
            SetPlaylistContent(PlaylistUrl, "#EXTM3U\n");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo(PlaylistUrl));
        }

        [Test]
        public async Task GivenAPageReferencingARelativePlaylist_WhenResolving_ThenThePlaylistUrlIsBuiltFromTheStreamBaseUrl()
        {
            SetPageContent("<a href=\"assets/playmaster.m3u8\">Știrile Pro TV</a>");
            SetPlaylistContent("http://base.nucilandia.ro/assets/playmaster.m3u8", "#EXTM3U\n");
            StreamInfo streamInfo = BuildStreamInfo();
            streamInfo.StreamBaseUrl = "http://base.nucilandia.ro";

            string result = await BuildRetriever().GetStreamUrlAsync(streamInfo);

            Assert.That(result, Is.EqualTo("http://base.nucilandia.ro/assets/playmaster.m3u8"));
        }

        [Test]
        public async Task GivenAPageReferencingARelativePlaylistWithoutABaseUrl_WhenResolving_ThenNullIsReturned()
        {
            SetPageContent("<a href=\"assets/playmaster.m3u8\">Știrile Pro TV</a>");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GivenAPageWithoutAPlaylistReference_WhenResolving_ThenNullIsReturned()
        {
            SetPageContent("no playlist reference here");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Null);
        }

        [TestCase("")]
        [TestCase("   ")]
        public async Task GivenAnEmptyPlaylistPage_WhenResolving_ThenANullReferenceExceptionIsThrown(string page)
        {
            SetPageContent(page);

            await Assert.ThrowsAsync<NullReferenceException>(
                () => BuildRetriever().GetStreamUrlAsync(BuildStreamInfo()));
        }

        [Test]
        public async Task GivenANullPlaylistPage_WhenResolving_ThenANullReferenceExceptionIsThrown()
        {
            SetPageContent(null!);

            await Assert.ThrowsAsync<NullReferenceException>(
                () => BuildRetriever().GetStreamUrlAsync(BuildStreamInfo()));
        }

        [TestCase("\"http://googlevideo.com/videoplayback.m3u8?id=613&itag=4\"")]
        [TestCase("'http://googlevideo.com/videoplayback.m3u8?token=613'")]
        public async Task GivenAGoogleVideoPlaylist_WhenResolving_ThenThePlaylistIsNotCrawled(string page)
        {
            string playlistUrl = page[1..^1];
            SetPageContent(page);

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(playlistUrl));
                downloader.Verify(x => x.TryDownloadStringAsync(playlistUrl), Times.Never);
            });
        }

        [Test]
        public async Task GivenAPlaylistWithASingleRelativeStream_WhenResolving_ThenTheAbsoluteStreamUrlIsReturned()
        {
            SetPageContent($"\"{PlaylistUrl}?token=613\"");
            SetPlaylistContent($"{PlaylistUrl}?token=613", "#EXTM3U\nchunklist.m3u8\n");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo("http://cdn.nucilandia.ro/live/chunklist.m3u8"));
        }

        [Test]
        public async Task GivenAPlaylistWithASingleAbsoluteStream_WhenResolving_ThenTheStreamUrlIsReturned()
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(PlaylistUrl, "#EXTM3U\nhttp://cdn.nucilandia.ro/live/chunklist.m3u8\n");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo("http://cdn.nucilandia.ro/live/chunklist.m3u8"));
        }

        [Test]
        public async Task GivenAPlaylistWithMultipleStreamsEndingInAPlaylist_WhenResolving_ThenTheLastStreamUrlIsReturned()
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(
                PlaylistUrl,
                "#EXTM3U\nhttp://cdn.nucilandia.ro/live/low.m3u8\nhttp://cdn.nucilandia.ro/live/high.m3u8\n");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo("http://cdn.nucilandia.ro/live/high.m3u8"));
        }

        [Test]
        public async Task GivenAPlaylistWithMultipleStreamsNotEndingInAPlaylist_WhenResolving_ThenThePlaylistUrlIsReturned()
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(
                PlaylistUrl,
                "#EXTM3U\nhttp://cdn.nucilandia.ro/live/high.m3u8\nhttp://cdn.nucilandia.ro/live/segment.ts\n");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo(PlaylistUrl));
        }

        [Test]
        public async Task GivenAPlaylistWithOnlyComments_WhenResolving_ThenThePlaylistUrlIsReturned()
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(PlaylistUrl, "#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=873\n");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo(PlaylistUrl));
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not a playlist")]
        [TestCase("#EXTINF:-1,Știrile Pro TV\nhttp://cdn.nucilandia.ro/live/high.m3u8\n")]
        public async Task GivenAResponseThatIsNotAPlaylist_WhenResolving_ThenNullIsReturned(string playlistContent)
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(PlaylistUrl, playlistContent);

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GivenAPlaylistThatCannotBeDownloaded_WhenResolving_ThenNullIsReturned()
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(PlaylistUrl, null!);

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GivenAEscapedPageUrl_WhenResolving_ThenTheUrlIsUnescaped()
        {
            SetPageContent("\"http:\\/\\/googlevideo.com\\/videoplayback.m3u8?token=613#038;id=4\"");

            string result = await BuildRetriever().GetStreamUrlAsync(BuildStreamInfo());

            Assert.That(result, Is.EqualTo("http://googlevideo.com/videoplayback.m3u8?token=613id=4"));
        }

        [Test]
        public async Task GivenAChannelStream_WhenResolving_ThenTheSameUrlAsTheEquivalentStreamInfoIsReturned()
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(PlaylistUrl, "#EXTM3U\n#EXT-X-STREAM-INF:BANDWIDTH=873\n");

            PlaylistUrlRetriever retriever = BuildRetriever();

            string streamInfoResult = await retriever.GetStreamUrlAsync(BuildStreamInfo());
            string channelStreamResult = await retriever.GetStreamUrlAsync(BuildChannelStream());

            Assert.That(channelStreamResult, Is.EqualTo(streamInfoResult));
        }

        [Test]
        public async Task GivenAChannelStream_WhenResolving_ThenItsProviderAndUrlAreUsed()
        {
            SetPageContent($"\"{PlaylistUrl}\"");
            SetPlaylistContent(PlaylistUrl, "#EXTM3U\n");

            await BuildRetriever().GetStreamUrlAsync(BuildChannelStream());

            downloader.Verify(x => x.TryDownloadStringAsync(PageUrl), Times.Once);
        }

        private static StreamInfo BuildStreamInfo() => new()
        {
            Provider = StreamProvider.Website,
            ChannelId = "protv",
            Title = "RO: Pro TV",
            Url = PageUrl
        };

        private static ChannelStream BuildChannelStream() => new()
        {
            Id = "ProTV-Website",
            ChannelName = "RO: Pro TV",
            Provider = StreamProvider.Website,
            ChannelId = "protv",
            Title = "RO: Pro TV",
            Url = PageUrl
        };

        private PlaylistUrlRetriever BuildRetriever() => new(downloader.Object);

        private void SetPageContent(string page)
            => downloader
                .Setup(x => x.TryDownloadStringAsync(PageUrl))
                .ReturnsAsync(page);

        private void SetPlaylistContent(string playlistUrl, string content)
            => downloader
                .Setup(x => x.TryDownloadStringAsync(playlistUrl))
                .ReturnsAsync(content);
    }
}
