# API usage examples

There is no network API. These examples exercise the process contract.

## Resolve one URL

```bash
dotnet run --project StreamToM3U -- --url "https://live.antena3.ro/" -o /dev/stdout
```

Standard output (shape):

```
https://cdn.example.net/live/antena3/index.m3u8
```

An empty line means the resolution failed; the reason is in the log.

## Resolve one URL through the Streamlink provider

```bash
dotnet run --project StreamToM3U -- --streamlink --url "https://youtube.com/@a7tvlive/live"
```

## Generate a merged playlist

```bash
dotnet run --project StreamToM3U -- --input "Data/input-ro.xml" --output-file "playlist.m3u"
```

Resulting `playlist.m3u`:

```
#EXTM3U
#EXTINF:-1,RO: Antena 1
https://cdn.example.net/live/antena1/index.m3u8
#EXTINF:-1,RO: Pro TV
https://cdn.example.net/live/protv/index.m3u8
```

Channels are ordered alphabetically by name and any URL that could not be re-downloaded is
omitted.

## Generate a servable directory

```bash
dotnet run --project StreamToM3U -- \
  --input "Data/input-ro.xml" \
  --output-dir "/srv/http/iptv/livestreams/ro" \
  --url "http://mydomain.example/iptv"
```

Produces:

| File | Content |
|------|---------|
| `playlist.m3u` | Index with `#EXTINF:-1,<name>` plus `http://mydomain.example/iptv/<name>.m3u8` |
| `Antena1.m3u8` | `#EXTM3U` plus a `#EXT-X-STREAM-INF:BANDWIDTH=873`/URL pair per stream |
| `ProTV.m3u8` | Same shape |

Without `--url`, the index references absolute filesystem paths instead of a base URL.

## TVSportHD provider

```bash
dotnet run --project StreamToM3U -- --tvshd --channel "protv" --url "http://www.tv-sport-hd.com"
```

## AntenaPlay provider

```bash
dotnet run --project StreamToM3U -- --antena --channel "antena1" --url "https://antenaplay.ro"
```

This registers a disposable account, resolves the stream, and logs out. It requires Chrome or
Chromium.

## Exit-code note

Every example above returns `0`. To detect failures, inspect the log file or the produced
playlist rather than the exit code. See [troubleshooting.md](./troubleshooting.md).