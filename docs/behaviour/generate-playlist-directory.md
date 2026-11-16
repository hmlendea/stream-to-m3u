# Behaviour: generate a playlist directory

## Summary

Reads an XML channel list, resolves every channel, and writes an index playlist plus one
playlist per channel into a directory. Selected when `--input` is supplied and `--output-dir` is
non-whitespace.

## Preconditions

- As for merged mode, plus a writable output directory path.
- `--url`, when supplied, is the base URL used in the index entries.

## Behaviour

1. Resolution proceeds exactly as in merged mode; grouping and ordering are identical.
2. The output directory is created when absent.
3. For each channel, in ascending name order:
   - the file name is derived from the channel name;
   - an `#EXTINF:-1,<ChannelName>` line and a reference are added to the index, in memory;
   - the channel file is written with `#EXTM3U` and a `#EXT-X-STREAM-INF:BANDWIDTH=873`/URL pair
     per stream.
4. The index is written to `<dir>/playlist.m3u`.

## Channel file naming

The name before the first `:` is discarded, then diacritics, punctuation, and spaces are
removed, and `.m3u8` is appended.

| Channel name | File name |
|--------------|-----------|
| `RO: Știrile Pro TV` | `StirileProTV.m3u8` |
| `MD: Canal 2 (Test)` | `Canal2Test.m3u8` |
| `RO: TVR Mureș` | `TVRMures.m3u8` |
| `RO: Prima Sport 1: XX` | `PrimaSport1XX.m3u8` |
| `RO: Élysée-France24` | `ElyseeFrance24.m3u8` |
| `Pro TV` | `ProTV.m3u8` |

Two channels normalising to the same name share one file; the later write wins.

## Index reference form

| `--url` | Index entry |
|---------|-------------|
| Absent/blank | Absolute filesystem path, e.g. `/srv/http/iptv/live/ProTV.m3u8` |
| Set | `<url>/<fileName>`, e.g. `http://mydomain.example/iptv/ProTV.m3u8` |

## Observable outcomes

| Condition | Directory contents |
|-----------|--------------------|
| No resolvable channels | `playlist.m3u` containing only `#EXTM3U`; no channel files |
| Channels resolved | `playlist.m3u` plus one `<Channel>.m3u8` per channel |
| Channel with no URLs | A channel file containing only `#EXTM3U`, still referenced from the index |

## Differences from merged mode

- **No reachability probe.** Directory mode does not download each URL, so unreachable-but-
  resolvable URLs are still written.
- Channel files use the `#EXT-X-STREAM-INF` marker rather than a bare URL.

## Related documentation

- [../flows/batch-playlist-generation.md](../flows/batch-playlist-generation.md)
- [generate-merged-playlist.md](./generate-merged-playlist.md)
- [../data-model.md](../data-model.md)
