# Repository overview

## Purpose

StreamToM3U resolves playable live-stream URLs and assembles them into M3U playlists. It exists
because many stream providers publish a browser page, an iframe, or a CLI-resolvable source
rather than a stable playlist URL. The tool performs that resolution in one of four ways —
HTML extraction, the Streamlink executable, or browser automation of the TVSportHD and
AntenaPlay sites — and then normalises the result into playlist files that a media player or a
web server can serve directly.

## Scope

The system owns:

- Command-line parsing and run-mode selection (single stream versus batch).
- Configuration binding from `appsettings.json`.
- Resolution of a playlist or stream URL for a provider.
- Validation and normalisation of the resolved playlist content.
- Generation of merged (`playlist.m3u`) and per-channel (`.m3u8`) playlists.
- HTTP text retrieval, browser automation, and structured operational logging.
- Reading the XML channel list from disk.

## Boundary — what the system deliberately does not own

- **Streaming.** The tool never proxies, transcodes, or serves stream bytes; it only resolves
  and records URLs.
- **Accounts.** No credentials are stored. The AntenaPlay provider creates a disposable,
  randomly generated account per run and logs out.
- **Page structure.** Channel websites, CDNs, Streamlink, and the browser are external
  dependencies whose behaviour and availability the project does not control.
- **Scheduling.** There is no daemon or internal timer; cadence is imposed externally, as
  documented with systemd in the [README](../README.md).
- **Guaranteed success.** Failed channels are skipped; the process still exits with the default
  exit code.

## Entry points

| Entry point | Location | Description |
|-------------|----------|-------------|
| Process entry | `StreamToM3U/Program.cs` → `Program.Main` | Loads configuration, builds the DI container, and dispatches to a run mode |
| Single-stream mode | `Program.GetPlaylistForStream` | Resolves one URL and writes it to standard output |
| Batch mode | `Program.GetPlaylistForInputFile` → `PlaylistFileGenerator.GeneratePlaylist` | Resolves a channel list and writes playlists |
| Test entry | `StreamToM3U.UnitTests` | NUnit fixtures and a loopback HTTP test double |

## Principal capabilities

| Capability | Implementing area | Behaviour document |
|-----------|-------------------|--------------------|
| Single stream URL resolution | `PlaylistUrlRetriever`, `IProcessor` | [behaviour/resolve-single-stream.md](./behaviour/resolve-single-stream.md) |
| Merged playlist generation | `PlaylistFileGenerator.GeneratePlaylistFile` | [behaviour/generate-merged-playlist.md](./behaviour/generate-merged-playlist.md) |
| Directory playlist generation | `PlaylistFileGenerator.GeneratePlaylistDirectory` | [behaviour/generate-playlist-directory.md](./behaviour/generate-playlist-directory.md) |
| Provider dispatch | `PlaylistUrlRetriever.CreateProcessor` | [components/processors.md](./components/processors.md) |
| Playlist validation | `PlaylistUrlRetriever.GetStreamUrlAsync` | [flows/playlist-url-validation.md](./flows/playlist-url-validation.md) |
| Structured logging | `NuciLogger`, `MyOperation`, `MyLogInfoKey` | [logging.md](./logging.md) |

## Runtime topology

A single short-lived process, invoked per run, with no listening socket and no persistent
service state. It reads configuration and input from the working directory, performs outbound
HTTP and (for two providers) browser automation, writes playlists and a log file, then exits.
See [architecture.md](./architecture.md) and [flows/startup-and-composition.md](./flows/startup-and-composition.md).

## Technology

- .NET 10.0, C# with file-scoped namespaces and primary constructors.
- `Microsoft.Extensions.DependencyInjection` for a singleton-based composition root.
- `Microsoft.Extensions.Configuration` for JSON binding.
- Nuci-prefixed packages for repositories (`NuciDAL`), logging (`NuciLog`/`NuciLog.Core`), web
  extraction (`NuciWeb`) and browser automation (`NuciWeb.Automation[.Selenium]`), plus
  `NuciCLI` and `NuciExtensions`.
- NUnit 4 with Moq for tests.

Full inventory: [dependencies.md](./dependencies.md).
