# Invariants

Properties that must remain true for the tool to behave correctly. Each entry states the
invariant, where it is enforced, and how it is verified.

## Process and composition

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| Exactly one DI container is built per run, and all services are singletons | `Program.Main` | Source inspection |
| `WebDriverHandler.CloseDriver()` always runs, including on failure | `try/finally` in `Program.Main` | Source inspection |
| The process returns the default exit code regardless of resolution failures | No `Environment.Exit`/return value in `Program.Main` | Source inspection |
| Configuration binding never throws when the file is missing | `AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)` | Source inspection |

## Configuration and CLI

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| With no arguments, `Provider` is `Website` and `OutputFile` is `playlist.m3u` | `Options` static arrays and `GetArgumentIfExists` fallback | `OptionsTests.GivenNoArguments_WhenParsing_ThenAllValuesAreSetToTheirDefaults` |
| Provider priority is `TvSportHd` > `AntenaPlay` > `Streamlink` > `Website` | Order of `if` blocks in `DetermineProviderFromArgs` | `OptionsTests.GivenMultipleProviderFlags_...` |
| The first occurrence of an option wins | `GetOptionValue` returns on match | `OptionsTests.GivenRepeatedOptions_WhenParsing_ThenTheFirstValueIsUsed` |
| `-x=<value>` and `-x <value>` forms both parse | `GetOptionValue` prefix branch | `OptionsTests` inline tests |
| An option followed by another option yields an empty value | `!args[i + 1].StartsWith('-')` guard | `OptionsTests.GivenAnOptionFollowedByAnotherOption_...` |
| Option matching is case-sensitive | Exact string comparison | `OptionsTests.GivenAnUnrecognisedArgument_...` (`--Url`, `--URL`, `-u2`, `url` unused) |

## Enumeration

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| `StreamProvider` numeric values are `Website=0`, `TvSportHd=1`, `AntenaPlay=2`, `Streamlink=3` | Declaration order | `StreamProviderTests.GivenAProvider_WhenReadingItsNumericValue_ThenItMatchesThePersistedContract` |
| Provider names parse case-insensitively | `Enum.Parse<StreamProvider>(value, true)` in `ChannelStreamMapping` | `StreamProviderTests.GivenAProviderName_WhenParsingIt_ThenTheMatchingProviderIsReturned` |

## HTTP retrieval

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| A `null`, empty, whitespace, relative, or non-HTTP(S) URL is never transmitted | `HttpClient.GetAsync` rejects it; `TryDownloadStringAsync` returns `null` | `FileDownloaderTests` unresolvable-URL cases |
| URLs are normalised before use: URL-decoded, `\/` → `/`, `#038;` removed | `FileDownloader.NormaliseUrl`, `PlaylistUrlRetriever.NormaliseUrl` | `FileDownloaderTests` escaped-URL cases |
| Non-success HTTP status codes still return the body | No status check in `GetAsync` | `FileDownloaderTests.GivenAnErrorStatusCode_...` |
| The configured `User-Agent` is sent on every request | `httpClient.DefaultRequestHeaders` | `FileDownloaderTests.GivenAReachablePlaylist_WhenDownloading_ThenTheUserAgentIsSent` |
| A positive timeout is required to construct the downloader | `HttpClient.Timeout` setter | `FileDownloaderTests.GivenAZeroRequestTimeout_ThenAnArgumentOutOfRangeExceptionIsThrown` |

## Resolution

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| A processor exception is translated to `null`, never propagated | `try/catch` in `PlaylistUrlRetriever.FindStreamUrl` | Source inspection |
| Playlist validation is skipped for `Streamlink` and for URLs containing `googlevideo` | Provider/URL guard in `GetStreamUrlAsync` | `PlaylistUrlRetrieverTests` Google Video cases |
| Validation requires a first line beginning with `#EXTM3U` | Content check in `GetStreamUrlAsync` | `PlaylistUrlRetrieverTests.GivenAResponseThatIsNotAPlaylist_ThenNullIsReturned` |
| A single stream entry is selected; otherwise the last entry wins only if it ends in `m3u`/`m3u8` | Selection logic in `GetStreamUrlAsync` | `PlaylistUrlRetrieverTests` entry-selection cases |
| Relative stream entries are resolved against the playlist URL | `ProcessStreamUrl` regex replacement | `PlaylistUrlRetrieverTests.GivenAPlaylistWithASingleRelativeStream_...` |

## Output

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| Every playlist starts with `#EXTM3U` | Both writers seed the line list with it | `PlaylistFileGeneratorTests` header cases |
| Channels are written in ordinal ascending `ChannelName` order | `OrderBy(x => x.Key)` | `PlaylistFileGeneratorTests.GivenResolvableChannelStreams_ThenTheChannelsAreWrittenAlphabetically` |
| A channel resolving to `null`/whitespace is omitted and logged at `Debug`/`Failure` | Filter and `logger.Debug` branch | `PlaylistFileGeneratorTests` skip cases |
| Merged mode drops URLs whose content cannot be downloaded or is empty | Reachability check before writing | `PlaylistFileGeneratorTests.GivenAChannelStreamsDictionary_ThenOnlyDownloadableUrlsAreWritten` |
| Directory mode performs no reachability download | No downloader use in `GeneratePlaylistDirectory` | `PlaylistFileGeneratorTests.GivenChannelUrls_ThenNoBaseUrlStreamIsDownloaded` |
| Channel file names drop the prefix before the first `:`, then diacritics, punctuation, and spaces | `GenerateChannelFileName` | `PlaylistFileGeneratorTests` naming `TestCase` rows |
| The directory index references an absolute path without `--url`, or `<url>/<fileName>` with it | `GeneratePlaylistDirectory` branch | `PlaylistFileGeneratorTests` index-reference cases |
| Channel entries use `#EXTINF:-1,<ChannelName>` and stream lines use `#EXT-X-STREAM-INF:BANDWIDTH=873` | Literal strings in the writers | `PlaylistFileGeneratorTests.GivenChannelUrls_ThenTheChannelFileListsEveryStream` |
| No component other than `PlaylistFileGenerator` writes playlists | Source boundary; `IProcessor` has no file API | Source inspection |

## Browser automation

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| At most one browser-driven provider is active at a time | `GainLock`/`ReleaseLock` spin lock | `WebDriverHandlerTests` lock cases |
| Exactly one `ChromeDriver` exists per process | `webDriver ??= CreateDriver()` under `lock (sync)` | Source inspection |
| The driver is quit at shutdown | `Program.Main` `finally` | Source inspection |
| The browser runs headless with image loading disabled | `ChromeOptions` arguments | Source inspection |

## Logging

| Invariant | Enforced by | Verified by |
|-----------|-------------|-------------|
| Operation names are exactly `FileDownload` and `ChannelStreamFetching` | `MyOperation` statics | `MyOperationTests` |
| Log key names are exactly `Url` and `ChannelStreamId` | `MyLogInfoKey` statics | `MyLogInfoKeyTests` |
| Per-channel outcomes are logged below the default minimum level | `logger.Debug` against `minimumLevel: Info` | Source inspection |

## Known violated or fragile invariants

These are documented because they are **not** currently true, or are true only by luck. See
[ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md).

- The per-channel `List<string>` in the result dictionary is appended to without a lock.
- `ReleaseLock` writes the flag outside the `lock (sync)` block.
- `PlaylistUrlRetriever` raises `NullReferenceException` when a processor returns `null` while
  validation is required.
