# Component: entry and composition

Source: `StreamToM3U/Program.cs`. Symbol: `StreamToM3U.Program`.

## Purpose

The composition root and the only component permitted to terminate or fail the process. It
loads configuration, builds the singleton container, selects the run mode, handles top-level
exceptions, and shuts down the browser.

## Scope

- Configuration loading and binding.
- Container construction.
- Run-mode dispatch.
- Top-level exception handling and browser shutdown.

## Boundary — what it does not own

- Resolution policy (owned by `PlaylistUrlRetriever`).
- File writing (owned by `PlaylistFileGenerator`).
- HTTP access (owned by `FileDownloader`).

## Static state

| Field | Type | Purpose |
|-------|------|---------|
| `applicationSettings` | `ApplicationSettings` | Bound HTTP settings, also registered in the container |
| `nuciLoggerSettings` | `NuciLoggerSettings` | Bound logging settings |
| `serviceProvider` | `IServiceProvider` | The container, captured so static helper methods can resolve services |

## Methods

| Method | Responsibility |
|--------|----------------|
| `Main(string[] args)` | Full startup, dispatch, error handling, and shutdown |
| `GetPlaylist(Options)` | Branches on `string.IsNullOrWhiteSpace(options.InputFile)` |
| `GetPlaylistForStream(Options)` | Builds `StreamInfo`, resolves one URL, writes it to standard output |
| `GetPlaylistForInputFile()` | Resolves `IPlaylistFileGenerator` and calls `GeneratePlaylist()` |
| `LoadConfiguration()` | `ConfigurationBuilder().AddJsonFile("appsettings.json", true, true)` |

## Data crossing the boundary

| Direction | Data |
|-----------|------|
| In | `string[] args` |
| Out | One line on standard output (single-stream mode) |
| Out | Playlist files (batch mode, via the generator) |
| Out | Log file and console log lines |
| Out | Exit code (always `0`) |

## Configuration consumed

`appsettings.json` sections `applicationSettings` and `nuciLoggerSettings`, bound by the
lower-camel-cased type name.

## Failure behaviour

| Exception | Handling |
|-----------|----------|
| `AggregateException` | Each inner exception logged separately at `Fatal` |
| Any other `Exception` | Logged once at `Fatal` |
| None | Normal shutdown |

Nothing is rethrown, so the process always exits `0`. See
[../error-handling.md](../error-handling.md).

## Extension points

Adding a service means adding a registration in `Main`. Adding a run mode means extending
`GetPlaylist`. Any new service must be safe as a singleton.

## Modification impact

- Changing registration lifetime or order affects `FileDownloader` construction (it reads
  settings eagerly) and `XmlRepository` (it receives the input path eagerly).
- Introducing an asynchronous entry point would change how exceptions surface and would affect
  the `finally` shutdown guarantee.

## Tests

`Program` itself is untested; its observable effects are exercised indirectly through
`PlaylistFileGeneratorTests`, `FileDownloaderTests`, and `PlaylistUrlRetrieverTests`. See
[../testing.md](../testing.md).

## Related documentation

- [../flows/startup-and-composition.md](../flows/startup-and-composition.md)
- [configuration.md](./configuration.md)
- [service-layer.md](./service-layer.md)
