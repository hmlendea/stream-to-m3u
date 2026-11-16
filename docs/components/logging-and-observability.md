# Component: logging and observability

Sources:

- `StreamToM3U/Logging/MyOperation.cs`
- `StreamToM3U/Logging/MyLogInfoKey.cs`
- Call sites in `Program`, `FileDownloader`, and `PlaylistFileGenerator`
- Package `NuciLog` / `NuciLog.Core`

## Purpose

Emit structured operational records that describe what the run did and which URLs it touched.

## Vocabulary

`MyOperation : Operation` — application operations:

| Operation | Emitted by | Levels |
|-----------|-----------|--------|
| `FileDownload` | `FileDownloader.TryDownloadStringAsync` | `Debug` (Started), `Verbose` (Success/Failure) |
| `ChannelStreamFetching` | `PlaylistFileGenerator.GeneratePlaylist` | `Info` (Started), `Debug` (Success/Failure) |

`Operation.StartUp`, `Operation.ShutDown`, and `Operation.Unknown` come from `NuciLog.Core`.

`MyLogInfoKey : LogInfoKey` — structured keys:

| Key | Value | Attached to |
|-----|-------|-------------|
| `ChannelStreamId` | The channel `Id` | `ChannelStreamFetching` records |
| `Url` | The normalised download URL | `FileDownload` records |

Both are exposed as static properties whose `Name` is the property name, and both are validated
by `MyOperationTests` and `MyLogInfoKeyTests`.

## Signal matrix

| Event | Operation | Status | Level | Payload |
|-------|-----------|--------|-------|---------|
| Process start | `StartUp` | Success | `Info` | none |
| Process end | `ShutDown` | Success | `Info` | none |
| Channel resolution begins | `ChannelStreamFetching` | Started | `Info` | `ChannelStreamId` |
| Channel resolved | `ChannelStreamFetching` | Success | `Debug` | `ChannelStreamId` |
| Channel failed | `ChannelStreamFetching` | Failure | `Debug` | `ChannelStreamId` |
| Download begins | `FileDownload` | Started | `Debug` | `Url` |
| Download succeeded | `FileDownload` | Success | `Verbose` | `Url` |
| Download failed | `FileDownload` | Failure | `Verbose` | `Url` + exception |
| Unhandled exception | `Unknown` | Failure | `Fatal` | exception (+ inner exceptions separately) |

## Diagnosability warning

Per-channel outcomes are logged at `Debug` while the shipped `minimumLevel` is `Info`. At the
default level a run reports only `StartUp` and `ShutDown`, so successful and failed resolutions
are both invisible. Lowering `minimumLevel` to `Debug` or `Verbose` is required to see them. See
[../logging.md](../logging.md) and [../troubleshooting.md](../troubleshooting.md).

## Sinks

Two sinks are configured by `NuciLogger`: the console and a file at
`nuciLoggerSettings.logFilePath` (default `logfile.log`), the latter gated by
`isFileOutputEnabled`. `minimumLevel` filters both.

## Sensitive data

The `Url` key carries the normalised download URL, which for playback URLs can embed provider
issued tokens. The log file is therefore treated as sensitive in
[../security.md](../security.md) and [../../PRIVACY.md](../../PRIVACY.md).

## Correlation

There is none. Records carry no run identifier, timestamp correlation, or request identifier, so
records from concurrent channel resolutions are interleaved without a grouping key other than the
`ChannelStreamId`.

## Related documentation

- [../logging.md](../logging.md)
- [../configuration.md](../configuration.md)
- [../error-handling.md](../error-handling.md)
