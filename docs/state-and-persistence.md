# State and persistence

## Principle

The application is stateless across runs. Nothing is cached, checkpointed, or resumed: every run
resolves from scratch and overwrites its outputs.

## Persistence surfaces

| Data | Owner | Location | Lifecycle |
|------|-------|----------|-----------|
| `appsettings.json` | Operator | Working directory | Read-only to the application; never written |
| XML channel list | Operator | Path supplied through `--input` | Read once through `GetAll()`; never written |
| Log file | `NuciLogger` | `nuciLoggerSettings.logFilePath` (default `logfile.log`) | Written by the logging library; no rotation implemented here |
| Merged playlist | `PlaylistFileGenerator` | `--output-file` (default `playlist.m3u`) | Overwritten on every merged run |
| Index playlist | `PlaylistFileGenerator` | `<--output-dir>/playlist.m3u` | Overwritten on every directory run |
| Per-channel playlists | `PlaylistFileGenerator` | `<--output-dir>/<Channel>.m3u8` | Overwritten; the directory is created when absent |

## In-memory state

| State | Owner | Lifetime | Concurrency |
|-------|-------|----------|-------------|
| `ConcurrentDictionary<string, List<string>>` of resolved URLs | `PlaylistFileGenerator` | One `GeneratePlaylist` call | Populated in parallel; per-list mutation is unsynchronised |
| Ordered `Dictionary<string, List<string>>` | `PlaylistFileGenerator` | Writing phase | Single-threaded |
| `ChromeDriver` | `WebDriverHandler` | Process lifetime, created lazily | Guarded by a spin lock |
| `acceptedGdpr` | `AntenaPlayProcessor` | Process lifetime | Written by browser-serialised code |
| `HttpClient` | `FileDownloader` | Process lifetime | Safe for concurrent use |

## Run-to-run consistency

- A run never merges with previous output; the file is replaced wholesale.
- A run that resolves nothing still writes a file containing only `#EXTM3U`, so a previously
  valid playlist is destroyed by a failed run. This is the principal operational risk; see
  [build-and-deployment.md](./build-and-deployment.md#backup-and-rollback).
- Channel files from a previous run that are no longer produced are **not** deleted from the
  output directory, so a served directory can accumulate stale playlists.

## Migrations

There are none: no schema, no database, no versioned state. The only compatibility migrations
are the XML element names and the playlist markers, both treated as contracts in
[invariants.md](./invariants.md).

## Related documentation

- [data-model.md](./data-model.md)
- [behaviour/generate-merged-playlist.md](./behaviour/generate-merged-playlist.md)
- [behaviour/generate-playlist-directory.md](./behaviour/generate-playlist-directory.md)
