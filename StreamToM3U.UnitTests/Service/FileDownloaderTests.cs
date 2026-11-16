using System;
using System.Linq;
using System.Threading.Tasks;

using Moq;

using NuciLog.Core;

using NUnit.Framework;

using StreamToM3U.Configuration;
using StreamToM3U.Logging;
using StreamToM3U.Service;
using StreamToM3U.UnitTests.TestDoubles;

namespace StreamToM3U.UnitTests.Service
{
    [TestFixture]
    public sealed class FileDownloaderTests
    {
        const int RequestTimeout = 8000;
        const string UserAgent = "Mozilla/5.0 (X11; Linux x86_64) NucilandiaTestBrowser/1.0";
        const string DownloadedContent = "#EXTM3U\n#EXTINF:-1,Știrile Pro TV\n";

        private Mock<ILogger> logger;

        [SetUp]
        public void SetUp() => logger = new Mock<ILogger>();

        [Test]
        public void GivenAZeroRequestTimeout_WhenConstructing_ThenAnArgumentOutOfRangeExceptionIsThrown()
            => Assert.Throws<ArgumentOutOfRangeException>(() => BuildFileDownloader(0));

        [TestCase(-1)]
        [TestCase(RequestTimeout)]
        public void GivenANonPositiveOrPositiveRequestTimeout_WhenConstructing_ThenNoExceptionIsThrown(int requestTimeout)
            => Assert.That(BuildFileDownloader(requestTimeout), Is.Not.Null);

        [TestCase("")]
        [TestCase("   ")]
        public void GivenAnInvalidUserAgent_WhenConstructing_ThenAFormatExceptionIsThrown(string userAgent)
            => Assert.Throws<FormatException>(() => BuildFileDownloader(RequestTimeout, userAgent));

        [Test]
        public void GivenANullApplicationSettings_WhenConstructing_ThenANullReferenceExceptionIsThrown()
            => Assert.Throws<NullReferenceException>(() => new FileDownloader(null!, logger.Object));

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not a url")]
        [TestCase("/relative/playlist.m3u8")]
        [TestCase("ftp://cdn.nucilandia.ro/live/playmaster.m3u8")]
        [TestCase("file:///tmp/playmaster.m3u8")]
        [TestCase("http://[::1/broken.m3u8")]
        public async Task GivenAnUnresolvableUrl_WhenDownloading_ThenNullIsReturned(string url)
        {
            FileDownloader fileDownloader = BuildFileDownloader();

            string result = await fileDownloader.TryDownloadStringAsync(url);

            Assert.That(result, Is.Null);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase("not a url")]
        [TestCase("ftp://cdn.nucilandia.ro/live/playmaster.m3u8")]
        public async Task GivenANullOrUnresolvableUrl_WhenDownloading_ThenTheUriIsNeverDownloaded(string url)
        {
            using LoopbackHttpServer server = new(200, DownloadedContent);
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync(url);

            Assert.That(server.Requests, Is.Empty);
        }

        [Test]
        public async Task GivenANullUrl_WhenDownloading_ThenNullIsReturned()
        {
            FileDownloader fileDownloader = BuildFileDownloader();

            string result = await fileDownloader.TryDownloadStringAsync(null!);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task GivenANullUrl_WhenDownloading_ThenTheStartedOperationIsLoggedWithoutAnyUrlValue()
        {
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync(null!);

            logger.Verify(
                l => l.Debug(
                    It.Is<Operation>(operation => operation.Name == nameof(MyOperation.FileDownload)),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Started)),
                    It.Is<LogInfo[]>(details => details.Length == 1 && details[0].Value == null)),
                Times.Once);
        }

        [TestCase("ftp://cdn.nucilandia.ro/live/playmaster.m3u8")]
        [TestCase("not a url")]
        public async Task GivenAFailingDownload_WhenDownloading_ThenAFailureIsLoggedWithTheNormalisedUrl(string url)
        {
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync(url);

            logger.Verify(
                l => l.Verbose(
                    It.Is<Operation>(operation => operation.Name == nameof(MyOperation.FileDownload)),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Failure)),
                    It.IsAny<Exception>(),
                    It.Is<LogInfo[]>(
                        details =>
                            details.Length == 1 &&
                            details[0].Key.Name == nameof(MyLogInfoKey.Url) &&
                            details[0].Value == url)),
                Times.Once);
        }

        [Test]
        public async Task GivenAFailingDownload_WhenDownloading_ThenNoSuccessIsLogged()
        {
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync("ftp://cdn.nucilandia.ro/live/playmaster.m3u8");

            logger.Verify(
                l => l.Verbose(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Success)),
                    It.IsAny<LogInfo[]>()),
                Times.Never);
        }

        [TestCase("ftp:\\/\\/cdn.nucilandia.ro\\/live\\/playmaster.m3u8", "ftp://cdn.nucilandia.ro/live/playmaster.m3u8")]
        [TestCase("ftp://cdn.nucilandia.ro/live/playmaster.m3u8#038;token=613", "ftp://cdn.nucilandia.ro/live/playmaster.m3u8token=613")]
        [TestCase("ftp://cdn.nucilandia.ro/live/play%20master.m3u8", "ftp://cdn.nucilandia.ro/live/play master.m3u8")]
        public async Task GivenAnEscapedUrl_WhenDownloading_ThenTheNormalisedUrlIsLogged(
            string url,
            string normalisedUrl)
        {
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync(url);

            logger.Verify(
                l => l.Debug(
                    It.Is<Operation>(operation => operation.Name == nameof(MyOperation.FileDownload)),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Started)),
                    It.Is<LogInfo[]>(details => details.Length == 1 && details[0].Value == normalisedUrl)),
                Times.Once);
        }

        [Test]
        public async Task GivenAReachablePlaylist_WhenDownloading_ThenTheDecodedContentIsReturned()
        {
            using LoopbackHttpServer server = new(200, DownloadedContent);
            FileDownloader fileDownloader = BuildFileDownloader();

            string result = await fileDownloader.TryDownloadStringAsync(server.Url);

            Assert.That(result, Is.EqualTo(DownloadedContent));
        }

        [Test]
        public async Task GivenAReachablePlaylist_WhenDownloading_ThenASuccessIsLogged()
        {
            using LoopbackHttpServer server = new(200, DownloadedContent);
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync(server.Url);

            logger.Verify(
                l => l.Verbose(
                    It.Is<Operation>(operation => operation.Name == nameof(MyOperation.FileDownload)),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Success)),
                    It.Is<LogInfo[]>(details => details.Length == 1 && details[0].Value == server.Url)),
                Times.Once);
        }

        [Test]
        public async Task GivenAReachablePlaylist_WhenDownloading_ThenNoFailureIsLogged()
        {
            using LoopbackHttpServer server = new(200, DownloadedContent);
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync(server.Url);

            logger.Verify(
                l => l.Verbose(
                    It.IsAny<Operation>(),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Failure)),
                    It.IsAny<Exception>(),
                    It.IsAny<LogInfo[]>()),
                Times.Never);
        }

        [Test]
        public async Task GivenAReachablePlaylist_WhenDownloading_ThenTheUserAgentIsSent()
        {
            using LoopbackHttpServer server = new(200, DownloadedContent);
            FileDownloader fileDownloader = BuildFileDownloader();

            await fileDownloader.TryDownloadStringAsync(server.Url);

            Assert.That(server.Requests.Single(), Does.Contain($"User-Agent: {UserAgent}"));
        }

        [TestCase(404)]
        [TestCase(500)]
        public async Task GivenAnErrorStatusCode_WhenDownloading_ThenTheResponseBodyIsStillReturned(int statusCode)
        {
            using LoopbackHttpServer server = new(statusCode, DownloadedContent);
            FileDownloader fileDownloader = BuildFileDownloader();

            string result = await fileDownloader.TryDownloadStringAsync(server.Url);

            Assert.That(result, Is.EqualTo(DownloadedContent));
        }

        [Test]
        public async Task GivenAnEmptyResponseBody_WhenDownloading_ThenAnEmptyStringIsReturned()
        {
            using LoopbackHttpServer server = new(200, string.Empty);
            FileDownloader fileDownloader = BuildFileDownloader();

            string result = await fileDownloader.TryDownloadStringAsync(server.Url);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GivenAnEscapedReachableUrl_WhenDownloading_ThenTheUnescapedUrlIsRequested()
        {
            using LoopbackHttpServer server = new(200, DownloadedContent);
            FileDownloader fileDownloader = BuildFileDownloader();
            string escapedUrl = server.Url.Replace("//", "\\/\\/");

            string result = await fileDownloader.TryDownloadStringAsync(escapedUrl);

            Assert.That(result, Is.EqualTo(DownloadedContent));
        }

        private FileDownloader BuildFileDownloader()
            => BuildFileDownloader(RequestTimeout, UserAgent);

        private FileDownloader BuildFileDownloader(int requestTimeout)
            => BuildFileDownloader(requestTimeout, UserAgent);

        private FileDownloader BuildFileDownloader(int requestTimeout, string userAgent)
            => new(
                new ApplicationSettings
                {
                    RequestTimeout = requestTimeout,
                    UserAgent = userAgent
                },
                logger.Object);
    }
}
