# Behaviour: generate a merged playlist

## Summary

Reads an XML channel list, resolves every channel in parallel, and writes a single
`playlist.m3u` containing one entry per resolved stream. Selected when `--input` is supplied and
`--output-dir` is blank or absent.

## Preconditions

- `--input` points to a readable `ArrayOfChannelStreamEntity` document.
- Every `Provider` value in the file parses into `StreamProvider` (case-insensitive).
- `appsettings.json` provides a positive `requestTimeout` and a valid `userAgent`.

## Behaviour

1. `PlaylistFileGenerator.GeneratePlaylist` loads all entities and maps them to `ChannelStream`.
2. Each channel is resolved in parallel (`Parallel.ForEach`); a resolution that yields `null` or
   whitespace is logged at `Debug`/`Failure` and discarded, otherwise logged at `Debug`/`Success`
   and appended under its `ChannelName`.
3. The result dictionary is ordered by channel name.
4. For each URL, the content is downloaded as a reachability probe; if the content is `null`,
   empty, or whitespace, the URL is dropped.
5. `playlist.m3u` is written with `#EXTM3U`, then `#EXTINF:-1,<ChannelName>` and the URL for each
   surviving stream.

## Output

```
#EXTM3U
#EXTINF:-1,<ChannelName>
<url>
```

## Observable outcomes

| Condition | Output |
|-----------|--------|
| No channels in the input | A file containing only `#EXTM3U` |
| All channels fail | A file containing only `#EXTM3U`; exit code still `0` |
| One URL unreachable but resolvable | That URL omitted, others written |
| Multiple entries share a `ChannelName` | All their URLs appear under that name |

## Side effects

- One extra HTTP request per surviving URL (the reachability probe); the body is discarded.
- The output file is overwritten, never merged with prior content.
- A new file is not created if the parent directory does not exist; writing throws.

## Related documentation

- [../flows/batch-playlist-generation.md](../flows/batch-playlist-generation.md)
- [generate-playlist-directory.md](./generate-playlist-directory.md)
- [../state-and-persistence.md](../state-and-persistence.md)
