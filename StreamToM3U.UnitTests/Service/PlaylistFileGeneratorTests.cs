using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Moq;

using NuciDAL.Repositories;
using NuciLog.Core;

using NUnit.Framework;

using StreamToM3U.Configuration;
using StreamToM3U.DataAccess.DataObjects;
using StreamToM3U.Logging;
using StreamToM3U.Service;
using StreamToM3U.Service.Models;

namespace StreamToM3U.UnitTests.Service
{
    [TestFixture]
    public sealed class PlaylistFileGeneratorTests
    {
        const string ChannelStreamFetchingOperationName = nameof(MyOperation.ChannelStreamFetching);

        private Mock<IFileDownloader> fileDownloader;
        private Mock<IPlaylistUrlRetriever> urlRetriever;
        private Mock<IRepository<ChannelStreamEntity>> channelStreamRepository;
        private Mock<ILogger> logger;
        private string temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            fileDownloader = new Mock<IFileDownloader>();
            urlRetriever = new Mock<IPlaylistUrlRetriever>();
            channelStreamRepository = new Mock<IRepository<ChannelStreamEntity>>();
            logger = new Mock<ILogger>();

            temporaryDirectory = Path.Combine(Path.GetTempPath(), $"stream-to-m3u-{Path.GetRandomFileName()}");

            Directory.CreateDirectory(temporaryDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(temporaryDirectory, true);
            }
        }

        [Test]
        public void GivenNoChannelStreams_WhenGeneratingPlaylist_ThenOnlyTheHeaderIsWritten()
        {
            SetChannelStreams([]);
            string outputFile = BuildTemporaryFilePath("playlist.m3u");

            BuildGenerator(BuildFileOptions(outputFile)).GeneratePlaylist();

            Assert.That(File.ReadAllLines(outputFile), Is.EqualTo(new[] { "#EXTM3U" }));
        }

        [Test]
        public void GivenResolvableChannelStreams_WhenGeneratingPlaylist_ThenTheChannelsAreWrittenAlphabetically()
        {
            SetChannelStreams(
            [
                BuildEntity("ProTV", "RO: Pro TV", "protv"),
                BuildEntity("Antena1", "RO: Antena 1", "antena1"),
                BuildEntity("Stirile", "RO: Știrile Pro TV", "stirile")
            ]);
            SetResolvedUrl(stream => $"http://cdn.nucilandia.ro/live/{stream.ChannelId}.m3u8");
            SetDownloadedContent("#EXTM3U");

            string outputFile = BuildTemporaryFilePath("playlist.m3u");
            BuildGenerator(BuildFileOptions(outputFile)).GeneratePlaylist();

            Assert.That(
                ExtractChannelNames(outputFile),
                Is.EqualTo(new[] { "RO: Antena 1", "RO: Pro TV", "RO: Știrile Pro TV" }));
        }

        [Test]
        public void GivenAChannelWithMultipleStreams_WhenGeneratingPlaylist_ThenAllStreamsAreWritten()
        {
            SetChannelStreams(
            [
                BuildEntity("ProTV-Sd", "RO: Pro TV", "protv.sd"),
                BuildEntity("ProTV-Hd", "RO: Pro TV", "protv.hd")
            ]);
            SetResolvedUrl(stream => $"http://cdn.nucilandia.ro/live/{stream.ChannelId}.m3u8");
            SetDownloadedContent("#EXTM3U");

            string outputFile = BuildTemporaryFilePath("playlist.m3u");
            BuildGenerator(BuildFileOptions(outputFile)).GeneratePlaylist();

            string[] lines = File.ReadAllLines(outputFile);

            Assert.Multiple(() =>
            {
                Assert.That(ExtractChannelNames(outputFile), Is.EqualTo(new[] { "RO: Pro TV", "RO: Pro TV" }));
                Assert.That(lines, Does.Contain("http://cdn.nucilandia.ro/live/protv.sd.m3u8"));
                Assert.That(lines, Does.Contain("http://cdn.nucilandia.ro/live/protv.hd.m3u8"));
            });
        }

        [Test]
        public void GivenAChannelStreamWithoutAUrl_WhenGeneratingPlaylist_ThenTheChannelIsSkipped()
        {
            SetChannelStreams(
            [
                BuildEntity("ProTV", "RO: Pro TV", "protv"),
                BuildEntity("Antena1", "RO: Antena 1", "antena1")
            ]);
            SetResolvedUrl(stream => stream.ChannelId == "protv" ? null! : "http://cdn.nucilandia.ro/live/antena1.m3u8");
            SetDownloadedContent("#EXTM3U");

            string outputFile = BuildTemporaryFilePath("playlist.m3u");
            BuildGenerator(BuildFileOptions(outputFile)).GeneratePlaylist();

            Assert.Multiple(() =>
            {
                Assert.That(ExtractChannelNames(outputFile), Is.EqualTo(new[] { "RO: Antena 1" }));
                Assert.That(File.ReadAllLines(outputFile), Does.Not.Contain("http://cdn.nucilandia.ro/live/protv.m3u8"));
            });
        }

        [Test]
        public void GivenAChannelStreamWithoutAUrl_WhenGeneratingPlaylist_ThenAFailureIsLogged()
        {
            SetChannelStreams([BuildEntity("ProTV", "RO: Pro TV", "protv")]);
            SetResolvedUrl(stream => null!);
            SetDownloadedContent("#EXTM3U");

            BuildGenerator(BuildFileOptions(BuildTemporaryFilePath("playlist.m3u"))).GeneratePlaylist();

            logger.Verify(
                l => l.Debug(
                    It.Is<Operation>(operation => operation.Name == ChannelStreamFetchingOperationName),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Failure)),
                    It.Is<LogInfo[]>(details => details.Length == 1 && details[0].Key.Name == nameof(MyLogInfoKey.ChannelStreamId))),
                Times.Once);
        }

        [Test]
        public void GivenResolvableChannelStreams_WhenGeneratingPlaylist_ThenEachChannelStreamIsLoggedAsStarted()
        {
            SetChannelStreams(
            [
                BuildEntity("ProTV", "RO: Pro TV", "protv"),
                BuildEntity("Antena1", "RO: Antena 1", "antena1"),
                BuildEntity("Stirile", "RO: Știrile Pro TV", "stirile")
            ]);
            SetResolvedUrl(stream => $"http://cdn.nucilandia.ro/live/{stream.ChannelId}.m3u8");
            SetDownloadedContent("#EXTM3U");

            BuildGenerator(BuildFileOptions(BuildTemporaryFilePath("playlist.m3u"))).GeneratePlaylist();

            logger.Verify(
                l => l.Info(
                    It.Is<Operation>(operation => operation.Name == ChannelStreamFetchingOperationName),
                    It.Is<OperationStatus>(status => status.Name == nameof(OperationStatus.Started)),
                    It.IsAny<LogInfo[]>()),
                Times.Exactly(3));
        }

        [Test]
        public void GivenAnUnresolvableProvider_WhenGeneratingPlaylist_ThenAnAggregateExceptionIsThrown()
        {
            ChannelStreamEntity entity = BuildEntity("ProTV", "RO: Pro TV", "protv");
            entity.Provider = "Nucilandia";
            SetChannelStreams([entity]);
            SetResolvedUrl(stream => null!);
            SetDownloadedContent("#EXTM3U");

            PlaylistFileGenerator generator = BuildGenerator(BuildFileOptions(BuildTemporaryFilePath("playlist.m3u")));

            Assert.That(() => generator.GeneratePlaylist(), Throws.TypeOf<AggregateException>());
        }

        [Test]
        public void GivenAWhitespaceOutputDirectory_WhenGeneratingPlaylist_ThenAMergedPlaylistFileIsWritten()
        {
            SetChannelStreams([BuildEntity("ProTV", "RO: Pro TV", "protv")]);
            SetResolvedUrl(stream => "http://cdn.nucilandia.ro/live/protv.m3u8");
            SetDownloadedContent("#EXTM3U");

            string outputFile = BuildTemporaryFilePath("playlist.m3u");
            Options options = BuildFileOptions(outputFile);
            options.OutputDirectory = "   ";

            BuildGenerator(options).GeneratePlaylist();

            Assert.That(File.Exists(outputFile));
        }

        [Test]
        public void GivenAChannelStreamsDictionary_WhenGeneratingPlaylistFile_ThenOnlyDownloadableUrlsAreWritten()
        {
            SetDownloadedContentFor(
                ("http://cdn.nucilandia.ro/live/available.m3u8", "#EXTM3U"),
                ("http://cdn.nucilandia.ro/live/unavailable.m3u8", null!));
            string outputFile = BuildTemporaryFilePath("playlist.m3u");

            BuildGenerator(BuildFileOptions(outputFile)).GeneratePlaylistFile(
                new Dictionary<string, List<string>>
                {
                    ["RO: Pro TV"] = ["http://cdn.nucilandia.ro/live/available.m3u8", "http://cdn.nucilandia.ro/live/unavailable.m3u8"]
                });

            Assert.That(
                File.ReadAllLines(outputFile),
                Is.EqualTo(
                [
                    "#EXTM3U",
                    "#EXTINF:-1,RO: Pro TV",
                    "http://cdn.nucilandia.ro/live/available.m3u8"
                ]));
        }

        [Test]
        public void GivenNoChannelStreams_WhenGeneratingPlaylistFile_ThenOnlyTheHeaderIsWritten()
        {
            string outputFile = BuildTemporaryFilePath("playlist.m3u");

            BuildGenerator(BuildFileOptions(outputFile)).GeneratePlaylistFile([]);

            Assert.That(File.ReadAllLines(outputFile), Is.EqualTo(new[] { "#EXTM3U" }));
        }

        [Test]
        public void GivenANonExistentOutputDirectory_WhenGeneratingPlaylistDirectory_ThenTheDirectoryIsCreated()
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "nested", "playlists");

            BuildGenerator(BuildDirectoryOptions(outputDirectory, null!)).GeneratePlaylistDirectory([]);

            Assert.That(Directory.Exists(outputDirectory));
        }

        [TestCase("RO: Știrile Pro TV", "StirileProTV.m3u8")]
        [TestCase("MD: Canal 2 (Test)", "Canal2Test.m3u8")]
        [TestCase("RO: TVR Mureș", "TVRMures.m3u8")]
        [TestCase("RO: Prima Sport 1: XX", "PrimaSport1XX.m3u8")]
        [TestCase("RO: Élysée-France24", "ElyseeFrance24.m3u8")]
        [TestCase("Pro TV", "ProTV.m3u8")]
        public void GivenAChannelName_WhenGeneratingPlaylistDirectory_ThenTheChannelFileIsNamedNormally(
            string channelName,
            string expectedFileName)
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "playlists");

            BuildGenerator(BuildDirectoryOptions(outputDirectory, null!)).GeneratePlaylistDirectory(
                new Dictionary<string, List<string>>
                {
                    [channelName] = ["http://cdn.nucilandia.ro/live/playmaster.m3u8"]
                });

            Assert.That(File.Exists(Path.Combine(outputDirectory, expectedFileName)));
        }

        [Test]
        public void GivenChannelUrlsWithoutABaseUrl_WhenGeneratingPlaylistDirectory_ThenTheIndexReferencesAbsolutePaths()
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "playlists");

            BuildGenerator(BuildDirectoryOptions(outputDirectory, null!)).GeneratePlaylistDirectory(
                new Dictionary<string, List<string>>
                {
                    ["RO: Pro TV"] = ["http://cdn.nucilandia.ro/live/playmaster.m3u8"]
                });

            Assert.That(
                File.ReadAllLines(Path.Combine(outputDirectory, "playlist.m3u")),
                Is.EqualTo(
                [
                    "#EXTM3U",
                    "#EXTINF:-1,RO: Pro TV",
                    Path.Combine(Path.GetFullPath(outputDirectory), "ProTV.m3u8")
                ]));
        }

        [Test]
        public void GivenABaseUrl_WhenGeneratingPlaylistDirectory_ThenTheIndexReferencesTheBaseUrl()
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "playlists");

            BuildGenerator(BuildDirectoryOptions(outputDirectory, "http://mydomain.example/iptv"))
                .GeneratePlaylistDirectory(
                    new Dictionary<string, List<string>>
                    {
                        ["RO: Pro TV"] = ["http://cdn.nucilandia.ro/live/playmaster.m3u8"]
                    });

            Assert.That(
                File.ReadAllLines(Path.Combine(outputDirectory, "playlist.m3u")),
                Is.EqualTo(
                [
                    "#EXTM3U",
                    "#EXTINF:-1,RO: Pro TV",
                    "http://mydomain.example/iptv/ProTV.m3u8"
                ]));
        }

        [Test]
        public void GivenChannelUrls_WhenGeneratingPlaylistDirectory_ThenTheChannelFileListsEveryStream()
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "playlists");

            BuildGenerator(BuildDirectoryOptions(outputDirectory, null!)).GeneratePlaylistDirectory(
                new Dictionary<string, List<string>>
                {
                    ["RO: Pro TV"] =
                    [
                        "http://cdn.nucilandia.ro/live/low.m3u8",
                        "http://cdn.nucilandia.ro/live/high.m3u8"
                    ]
                });

            Assert.That(
                File.ReadAllLines(Path.Combine(outputDirectory, "ProTV.m3u8")),
                Is.EqualTo(
                [
                    "#EXTM3U",
                    "#EXT-X-STREAM-INF:BANDWIDTH=873",
                    "http://cdn.nucilandia.ro/live/low.m3u8",
                    "#EXT-X-STREAM-INF:BANDWIDTH=873",
                    "http://cdn.nucilandia.ro/live/high.m3u8"
                ]));
        }

        [Test]
        public void GivenAChannelWithoutUrls_WhenGeneratingPlaylistDirectory_ThenAnEmptyChannelPlaylistIsWritten()
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "playlists");

            BuildGenerator(BuildDirectoryOptions(outputDirectory, null!)).GeneratePlaylistDirectory(
                new Dictionary<string, List<string>>
                {
                    ["RO: Pro TV"] = []
                });

            Assert.Multiple(() =>
            {
                Assert.That(File.ReadAllLines(Path.Combine(outputDirectory, "ProTV.m3u8")), Is.EqualTo(new[] { "#EXTM3U" }));
                Assert.That(File.ReadAllLines(Path.Combine(outputDirectory, "playlist.m3u")), Has.Length.EqualTo(3));
            });
        }

        [Test]
        public void GivenChannelUrlsAndAnExistingDirectory_WhenGeneratingPlaylistDirectory_ThenTheExistingDirectoryIsReused()
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "playlists");
            Directory.CreateDirectory(outputDirectory);

            BuildGenerator(BuildDirectoryOptions(outputDirectory, null!)).GeneratePlaylistDirectory(
                new Dictionary<string, List<string>>
                {
                    ["RO: Pro TV"] = ["http://cdn.nucilandia.ro/live/playmaster.m3u8"]
                });

            Assert.That(File.Exists(Path.Combine(outputDirectory, "playlist.m3u")));
        }

        [Test]
        public void GivenChannelUrls_WhenGeneratingPlaylistDirectory_ThenNoBaseUrlStreamIsDownloaded()
        {
            string outputDirectory = Path.Combine(temporaryDirectory, "playlists");

            BuildGenerator(BuildDirectoryOptions(outputDirectory, null!)).GeneratePlaylistDirectory(
                new Dictionary<string, List<string>>
                {
                    ["RO: Pro TV"] = ["http://cdn.nucilandia.ro/live/playmaster.m3u8"]
                });

            fileDownloader.Verify(x => x.TryDownloadStringAsync(It.IsAny<string>()), Times.Never);
        }

        private static IEnumerable<string> ExtractChannelNames(string playlistFile)
            => File
                .ReadAllLines(playlistFile)
                .Where(line => line.StartsWith("#EXTINF"))
                .Select(line => line[(line.IndexOf(',') + 1)..]);

        private static ChannelStreamEntity BuildEntity(string id, string channelName, string channelId) => new()
        {
            Id = id,
            ChannelName = channelName,
            Provider = nameof(StreamProvider.Website),
            ChannelId = channelId,
            Url = "http://dummy-url.ro/live/ro"
        };

        private static Options BuildFileOptions(string outputFile) => new()
        {
            OutputFile = outputFile
        };

        private static Options BuildDirectoryOptions(string outputDirectory, string baseUrl)
        {
            Options options = new()
            {
                OutputDirectory = outputDirectory
            };

            if (baseUrl is not null)
            {
                options.Url = baseUrl;
            }

            return options;
        }

        private PlaylistFileGenerator BuildGenerator(Options options)
            => new(
                fileDownloader.Object,
                urlRetriever.Object,
                options,
                channelStreamRepository.Object,
                logger.Object);

        private string BuildTemporaryFilePath(string fileName)
            => Path.Combine(temporaryDirectory, fileName);

        private void SetChannelStreams(IEnumerable<ChannelStreamEntity> channelStreams)
            => channelStreamRepository
                .Setup(x => x.GetAll())
                .Returns(channelStreams);

        private void SetResolvedUrl(Func<ChannelStream, string> resolve)
            => urlRetriever
                .Setup(x => x.GetStreamUrlAsync(It.IsAny<ChannelStream>()))
                .ReturnsAsync(resolve);

        private void SetDownloadedContent(string content)
            => fileDownloader
                .Setup(x => x.TryDownloadStringAsync(It.IsAny<string>()))
                .ReturnsAsync(content);

        private void SetDownloadedContentFor(params (string Url, string Content)[] downloads)
        {
            foreach ((string url, string content) in downloads)
            {
                fileDownloader
                    .Setup(x => x.TryDownloadStringAsync(url))
                    .ReturnsAsync(content);
            }
        }
    }
}
