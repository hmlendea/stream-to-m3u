# Configuration

Configuration has two sources with different owners and precedence.

## Sources

| Source | Owner | Precedence | When it applies |
|--------|-------|------------|-----------------|
| `appsettings.json` | `Program.LoadConfiguration` | Base | Always loaded; the file is optional |
| Command-line arguments | `Options.FromArguments` | Absolute | Always parsed; overrides nothing in the file because the file does not contain run-mode or output settings |

`appsettings.json` is loaded with `optional: true` and `reloadOnChange: true`, but the bound
objects are plain singletons with no `IOptionsMonitor`, so a reload has no effect during a run.

## `applicationSettings` section

| Key | Type | Default | Required | Description |
|-----|------|---------|----------|-------------|
| `requestTimeout` | `int` | `8000` | No | HTTP request timeout in milliseconds, applied to `HttpClient.Timeout` |
| `userAgent` | `string` | Chrome user agent | No | `User-Agent` header sent with every HTTP request |

A missing file leaves `requestTimeout` at `0`, which makes `HttpClient.Timeout =
TimeSpan.FromMilliseconds(0)` throw. A missing or empty `userAgent` makes the
`DefaultRequestHeaders.Add` call throw `FormatException`.

Bound to `ApplicationSettings` in `StreamToM3U/Configuration/ApplicationSettings.cs`.

## `nuciLoggerSettings` section

| Key | Type | Default | Required | Description |
|-----|------|---------|----------|-------------|
| `logFilePath` | `string` | `logfile.log` | No | Path of the log file |
| `minimumLevel` | `string` | `Info` | No | Minimum level written to the log |
| `isFileOutputEnabled` | `bool` | `true` | No | Whether messages are written to the log file |

Bound to `NuciLoggerSettings` in `Program.Main`. Per-channel outcomes are logged at `Debug`, so
they are invisible unless `minimumLevel` is lowered. See [logging.md](./logging.md).

## Command-line options

| Option | Value | Default | Description |
|--------|-------|---------|-------------|
| `-i`, `--input` | `<path>` | — | Input XML file used for batch playlist generation |
| `-o`, `--output-file` | `<path>` | `playlist.m3u` | Output playlist path used when no output directory is set |
| `-O`, `--output-dir`, `--output-directory` | `<path>` | — | Output directory that receives an index playlist and one playlist per channel |
| `-c`, `--channel` | `<identifier>` | — | Channel identifier used by the TVSportHD and AntenaPlay providers |
| `-t`, `--title` | `<string>` | — | Optional stream title |
| `-u`, `--url` | `<url>` | — | Source page URL |
| `-U`, `--baseurl` | `<url>` | — | Base URL used to build relative playlist entries |
| `--tvs`, `--tvsport`, `--tvshd`, `--tvsporthd` | — | — | Selects the TVSportHD provider |
| `--antena-play`, `--antenaplay`, `--antena`, `--aplay`, `--ap` | — | — | Selects the AntenaPlay provider |
| `--sl`, `--streamlink` | — | — | Selects the Streamlink provider |

Both `-x value` and `-x=value` forms are accepted. The first occurrence of an option wins. An
option followed by another option yields an empty value. Unknown arguments are ignored. Provider
flags are mutually exclusive with the priority `TvSportHd` > `AntenaPlay` > `Streamlink` >
`Website`. See [api-reference/command-line-interface.md](./api-reference/command-line-interface.md).

## Run-mode selection

| Condition | Mode | Output |
|-----------|------|--------|
| `--input` is blank or absent | Single stream | One URL on standard output |
| `--input` is set | Batch | Merged file or directory |

## Secrets

The repository contains no secret values. Configuration is read only from `appsettings.json` and
the command line; nothing is read from environment variables. The log file may contain provider
issued tokens in playback URLs, so it is treated as sensitive. See [security.md](./security.md).