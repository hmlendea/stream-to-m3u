# Logging

## Framework

Logging uses **NuciLog** (`NuciLog` 1.2.4 with contracts in `NuciLog.Core` 3.1.2). The
application supplies `NuciLoggerSettings` and a vocabulary of operations and keys; the library
owns formatting and sinks. Configuration is bound from the `nuciLoggerSettings` section of
`appsettings.json`.

## Levels

| Level | Used for |
|-------|----------|
| `Fatal` | Unhandled top-level exceptions |
| `Info` | Process start/stop and channel-resolution start |
| `Debug` | Download start, channel-resolution outcome |
| `Verbose` | Download success and failure |

`Info` is the shipped minimum. Because channel outcomes and download records are emitted at
`Debug`/`Verbose`, they are hidden by default. See [components/logging-and-observability.md](./components/logging-and-observability.md)
for the full signal matrix.

## Structured fields

Every record carries an operation name and an operation status. Additional key/value pairs:

| Key | Value |
|-----|-------|
| `ChannelStreamId` | The channel `Id` from the XML input |
| `Url` | The normalised download URL |

## Operations

| Operation | Origin | Emitted by |
|-----------|--------|-----------|
| `StartUp` | `NuciLog.Core.Operation` | `Program.Main` |
| `ShutDown` | `NuciLog.Core.Operation` | `Program.Main` |
| `Unknown` | `NuciLog.Core.Operation` | `Program.Main` exception handlers |
| `FileDownload` | `MyOperation` | `FileDownloader` |
| `ChannelStreamFetching` | `MyOperation` | `PlaylistFileGenerator` |

## Sinks

| Sink | Controlled by |
|------|---------------|
| Console | Always active (governed by `minimumLevel`) |
| File at `logFilePath` | `isFileOutputEnabled` and `minimumLevel` |

Default path is `logfile.log` in the working directory.

## Correlation

None. There is no run identifier, trace identifier, or span, and concurrent channel resolutions
interleave records. `ChannelStreamId` is the only per-channel grouping key.

## Redaction and masking

No redaction is performed. Download URLs are logged in normalised but otherwise complete form,
which may include provider-issued session tokens. The log file should be treated as sensitive.

## Retention

No rotation, archival, or automatic deletion is implemented in this repository; retention is
whatever the logging library and the operator's environment provide.

## Sampling

None.

## Error logging

`FileDownload` failures are logged at `Verbose` with the exception and the normalised URL.
Top-level failures are logged at `Fatal`: `AggregateException` instances are unwrapped so each
inner exception produces its own record.

## Audit logging

None. The tool has no authenticated surface and no security-relevant event stream beyond the
download records.

## Performance

All logging is synchronous through the NuciLog abstraction. Because downloads and channel records
are below the default minimum level, the practical logging volume in normal operation is two
records per run.

## Changing levels at runtime

There is no runtime control. `reloadOnChange` is enabled for the JSON file but the bound
`NuciLoggerSettings` instance is a plain singleton with no monitor, so editing
`appsettings.json` during a run has no effect. Set the level for the whole run before starting
the process.

## Related documentation

- [configuration.md](./configuration.md)
- [components/logging-and-observability.md](./components/logging-and-observability.md)
- [troubleshooting.md](./troubleshooting.md)
