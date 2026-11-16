# Component: service layer

Sources:

- `StreamToM3U/Service/IFileDownloader.cs`, `FileDownloader.cs`
- `StreamToM3U/Service/IPlaylistUrlRetriever.cs`, `PlaylistUrlRetriever.cs`
- `StreamToM3U/Service/IPlaylistFileGenerator.cs`, `PlaylistFileGenerator.cs`
- `StreamToM3U/Service/Models/ChannelStream.cs`, `StreamInfo.cs`

## `FileDownloader` / `IFileDownloader`

**Purpose.** Fetch text over HTTP with a configured timeout and user agent.

**Constructor.** Takes `ApplicationSettings` and `ILogger`. Creates one `HttpClient` with
`Timeout = TimeSpan.FromMilliseconds(applicationSettings.RequestTimeout)` and adds the
`User-Agent` header. Both operations throw on invalid settings, so a missing or incomplete
`appsettings.json` fails here rather than later.

**Method.** `TryDownloadStringAsync(url)`:

1. normalises the URL (URL-decode, `\/` → `/`, remove `#038;`); `null` stays `null`;
2. logs `FileDownload`/`Started` at `Debug` with the normalised URL;
3. performs `GET`, reads the full byte array, decodes as UTF-8;
4. logs `FileDownload`/`Success` at `Verbose`;
5. on any exception logs `FileDownload`/`Failure` at `Verbose` and returns `null`.

**Behavioural notes.**

- HTTP error status codes do not throw; the body is returned regardless.
- The single `HttpClient` instance is reused across calls (correct practice).
- All exceptions are swallowed, so callers must treat `null` as "unavailable".

**Tests.** `FileDownloaderTests` (33 cases) plus `LoopbackHttpServer`.

## `PlaylistUrlRetriever` / `IPlaylistUrlRetriever`

**Purpose.** Turn a `StreamInfo` or `ChannelStream` into a validated, playable URL.

**Constructor.** Takes `IFileDownloader`; the downloader is passed on to `WebsiteProcessr` and
used directly for validation.

**Methods.**

| Method | Behaviour |
|--------|-----------|
| `GetStreamUrlAsync(StreamInfo)` | Provider dispatch, validation, normalisation |
| `GetStreamUrlAsync(ChannelStream)` | Projects the channel to a `StreamInfo` and delegates |
| `FindStreamUrl(StreamInfo)` | Creates the processor and swallows its exceptions into `null` |
| `CreateProcessor(StreamProvider)` | Switch; default arm is `WebsiteProcessr` |
| `IsUrlValid(url)` | Non-blank, absolute, `http`/`https` |
| `NormaliseUrl(url)` | URL-decode, `\/` → `/`, remove `#038;` |
| `ProcessStreamUrl(playlistUrl, streamUrl)` | Keeps absolute entries; otherwise regex-replaces the trailing `*.m3u[8]?` segment |

The validation algorithm is documented in detail in
[../flows/playlist-url-validation.md](../flows/playlist-url-validation.md).

**Behavioural notes.**

- Processor exceptions are converted to `null`; `null` propagation into the validation guard
  raises `NullReferenceException`.
- Validation is skipped for `Streamlink` and for `googlevideo` URLs.

**Tests.** `PlaylistUrlRetrieverTests` (24 cases).

## `PlaylistFileGenerator` / `IPlaylistFileGenerator`

**Purpose.** Batch orchestrator and the only component that writes playlists.

**Constructor.** Takes `IFileDownloader`, `IPlaylistUrlRetriever`, `Options`,
`IRepository<ChannelStreamEntity>`, and `ILogger` (primary constructor).

**Methods.**

| Method | Behaviour |
|--------|-----------|
| `GeneratePlaylist()` | Load, map, resolve in parallel, group, order, then delegate to a writer |
| `GeneratePlaylistFile(Dictionary<string, List<string>>)` | Reachability probe per URL, then write the merged file |
| `GeneratePlaylistDirectory(Dictionary<string, List<string>>)` | Create the directory, write per-channel files and the index |
| `GenerateChannelFileName(string)` | Drop the pre-`:` prefix, strip diacritics, punctuation, and spaces, append `.m3u8` |

Both writers are public so tests can drive them directly with a prepared dictionary.

**Behavioural notes.**

- A `Task` list is declared but unused; resolution is awaited synchronously inside the parallel
  body.
- Only `PlaylistFileGenerator` writes playlist files; processors have no file API.

**Tests.** `PlaylistFileGeneratorTests` (23 cases).

## Models

| Model | Fields | Used by |
|-------|--------|---------|
| `ChannelStream` | `Id`, `ChannelName`, `Provider`, `ChannelId`, `Title`, `Url`, `StreamBaseUrl` | Batch mode; carries the parsed provider |
| `StreamInfo` | `Provider`, `ChannelId`, `Title`, `Url`, `StreamBaseUrl` | Single resolution; built by `Program` and by the `ChannelStream` overload |

`ChannelStream` retains `Id` and `ChannelName` for grouping and logging; `StreamInfo` omits them
because processors do not need them.

## Modification impact

- Changing `IFileDownloader` affects `FileDownloader`, `WebsiteProcessr`, and both test fixtures.
- Changing grouping or ordering semantics affects published playlists and both writers.
- Introducing a second file writer would break the single-writer invariant.

## Related documentation

- [../flows/single-stream-resolution.md](../flows/single-stream-resolution.md)
- [../flows/batch-playlist-generation.md](../flows/batch-playlist-generation.md)
- [processors.md](./processors.md)
- [../invariants.md](../invariants.md)
