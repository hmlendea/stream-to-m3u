# Quick start

## Prerequisites

| Requirement | Needed for |
|-------------|-----------|
| [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | Building and running from source |
| Network access to the configured sources | Any resolution |
| Chrome or Chromium | `TvSportHd` and `AntenaPlay` providers |
| Streamlink on `PATH` | `Streamlink` provider |

## Build

```bash
git clone https://github.com/hmlendea/stream-to-m3u.git
cd stream-to-m3u
dotnet restore
dotnet build
```

## Resolve a single stream

```bash
dotnet run --project StreamToM3U -- --url "https://live.antena3.ro/"
```

The resolved URL is written to standard output. Failures are logged, not thrown to the console,
so an empty line means the resolution did not succeed.

## Generate a merged playlist

```bash
dotnet run --project StreamToM3U -- --input "Data/input-ro.xml" --output-file "playlist.m3u"
```

## Generate a servable playlist directory

```bash
dotnet run --project StreamToM3U -- \
  --input "Data/input-ro.xml" \
  --output-dir "./out" \
  --url "https://mydomain.example/iptv"
```

This writes `out/playlist.m3u` (index) plus one `out/<Channel>.m3u8` per channel. The `--url`
value becomes the base URL used in the index entries.

## Select a provider

Provider flags are mutually exclusive with a fixed priority (`TvSportHd` > `AntenaPlay` >
`Streamlink` > `Website`). For example:

```bash
dotnet run --project StreamToM3U -- --streamlink --url "https://youtube.com/@a7tvlive/live"
```

## Run the tests

```bash
dotnet test
```

## Where to go next

- [configuration.md](./configuration.md) — settings and command-line options.
- [data-model.md](./data-model.md) — XML input and playlist output formats.
- [flows/startup-and-composition.md](./flows/startup-and-composition.md) — what the process does first.
- [troubleshooting.md](./troubleshooting.md) — when nothing is resolved.
