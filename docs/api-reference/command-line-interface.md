# Command-line interface

The interface header is the process invocation: `StreamToM3U <arguments>`.

## `Main (console entry)` / `-i` etc.

### Invocation

```
StreamToM3U [options]
```

Parsed by `StreamToM3U.Configuration.Options.FromArguments`.

### Authentication or authorisation

None.

### Arguments

| Option | Value type | Required | Default | Description |
|--------|-----------|----------|---------|-------------|
| `-i`, `--input` | Path | No | none | Input XML file. Presence switches to batch mode |
| `-o`, `--output-file` | Path | No | `playlist.m3u` | Output file for merged mode |
| `-O`, `--output-dir`, `--output-directory` | Path | No | none | Output directory for directory mode; takes effect only when non-whitespace |
| `-c`, `--channel` | Identifier | For `TvSportHd`/`AntenaPlay` | none | Provider channel identifier |
| `-t`, `--title` | String | No | none | Optional stream title |
| `-u`, `--url` | URL | For single-stream mode; also the index base URL in directory mode | none | Source page URL, or base URL for index entries |
| `-U`, `--baseurl` | URL | No | none | Base URL used to absolutise relative entries in website extraction |
| `--tvs`, `--tvsport`, `--tvshd`, `--tvsporthd` | Flag | No | — | Selects `TvSportHd` |
| `--antena-play`, `--antenaplay`, `--antena`, `--aplay`, `--ap` | Flag | No | — | Selects `AntenaPlay` |
| `--sl`, `--streamlink` | Flag | No | — | Selects `Streamlink` |

### Argument syntax

- Both `-x value` and `-x=value` forms are accepted.
- The first occurrence of an option wins; later occurrences are ignored.
- An option immediately followed by another option (`-` prefix) yields an empty string value.
- Unknown arguments are ignored; matching is case-sensitive.
- Provider flags are mutually exclusive, with priority `TvSportHd` > `AntenaPlay` > `Streamlink`
  > `Website`.

### Behaviour

| Mode | Trigger | Effect |
|------|---------|--------|
| Single stream | `--input` blank/absent | Resolves one URL and writes it to standard output |
| Batch | `--input` set | Resolves the channel list and writes playlists |

Mode precedence within batch: a non-whitespace `--output-dir` selects directory mode; otherwise
merged mode uses `--output-file`.

### Response

| Output | Form | Notes |
|--------|------|-------|
| Standard output | The resolved URL, or an empty line | Written exactly once in single-stream mode |
| Merged playlist | A text file at `--output-file` | `#EXTM3U` plus one `#EXTINF`/URL block per stream |
| Directory playlists | `playlist.m3u` plus one `.m3u8` per channel | Created under `--output-dir` |
| Log file | Text at `nuciLoggerSettings.logFilePath` | See [../logging.md](../logging.md) |
| Exit code | `0` | Always, including when every channel fails |

### Flow references

- [../flows/startup-and-composition.md](../flows/startup-and-composition.md)
- [../flows/single-stream-resolution.md](../flows/single-stream-resolution.md)
- [../flows/batch-playlist-generation.md](../flows/batch-playlist-generation.md)

### Error scenarios

| Condition | Observable effect |
|-----------|-------------------|
| `appsettings.json` missing | `requestTimeout` is `0`; constructing `FileDownloader` throws `ArgumentOutOfRangeException`, logged at `Fatal` |
| `appsettings.json` has an empty `userAgent` | `DefaultRequestHeaders.Add` throws `FormatException`, logged at `Fatal` |
| No `--url` in single-stream mode | Standard output is an empty line |
| XML `Provider` value cannot be parsed | `ArgumentException` inside `Parallel.ForEach`, surfaced as `AggregateException`, logged per inner exception at `Fatal` |
| A channel cannot be resolved | Channel omitted; the run still exits `0` |