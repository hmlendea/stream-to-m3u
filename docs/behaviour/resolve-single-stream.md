# Behaviour: resolve a single stream

## Summary

Resolves one playable URL from a source page and writes it to standard output. Triggered
implicitly whenever `--input` is absent or blank.

## Preconditions

- A source URL is supplied through `--url` (or the provider does not need one).
- For `TvSportHd` and `AntenaPlay`, a `--channel` identifier and a local Chrome or Chromium.
- For `Streamlink`, the `streamlink` executable on `PATH`.

## Behaviour

1. `Program.GetPlaylistForStream` builds a `StreamInfo` from `Options` (`Provider`, `ChannelId`,
   `Title`, `Url`, `StreamBaseUrl`).
2. `PlaylistUrlRetriever.GetStreamUrlAsync(StreamInfo)` runs the resolution and validation
   described in [../flows/single-stream-resolution.md](../flows/single-stream-resolution.md).
3. The result is written to standard output with `Console.WriteLine`. A `null` result prints an
   empty line.

## Observable outcomes

| Condition | Standard output | Log |
|-----------|-----------------|-----|
| URL resolved and valid | The URL | `StartUp`, `FileDownload` records, `ShutDown` |
| Processor returned a valid absolute HTTP(S) URL | The URL | As above |
| Validation failed (not a playlist) | Empty line | `FileDownload` records |
| Processor threw | Empty line | `Fatal` (only if the exception escapes resolution) |
| Provider returned `null` with validation required | Empty line | `Fatal` with `NullReferenceException` |
| Exit code | `0` in all cases | — |

## Notes

- There is no retry. A transient failure yields an empty line.
- The URL is printed without normalisation if it is already a valid absolute HTTP(S) URL;
  otherwise the normalised form is printed.
- Additional output must not be introduced on standard output: scripted consumers parse exactly
  one line. See [../invariants.md](../invariants.md).

## Related documentation

- [../flows/single-stream-resolution.md](../flows/single-stream-resolution.md)
- [../components/service-layer.md](../components/service-layer.md)
- [../components/processors.md](../components/processors.md)
