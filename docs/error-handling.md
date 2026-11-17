# Error handling

## Policy

Failures are handled at three levels, each of which converts a fault into a degraded but
successful outcome.

| Level | Mechanism | Result |
|-------|-----------|--------|
| Provider | `try/catch` in `PlaylistUrlRetriever.FindStreamUrl` | Any processor exception becomes `null` |
| Download | `try/catch` in `FileDownloader.TryDownloadStringAsync` | Any HTTP exception is logged at `Verbose` and becomes `null` |
| Process | `try/catch` in `Program.Main` | `AggregateException` unwrapped per inner exception; any other exception logged once; the run continues to shutdown |

## Error taxonomy

| Fault | Detected at | Expression | Observable outcome |
|-------|-------------|------------|--------------------|
| Missing/invalid `appsettings.json` | `FileDownloader` constructor | `ArgumentOutOfRangeException`, `FormatException`, `NullReferenceException` | Logged `Fatal`; no output produced |
| Page fetch failure | `FileDownloader` | `null` | Channel skipped; log at `Verbose`/`Failure` |
| No playlist reference in HTML | `WebsiteProcessr` | `string.Empty` | Channel skipped |
| Relative playlist without base URL | `WebsiteProcessr` / retriever | `string.Empty` → invalid | Channel skipped |
| Empty page body | `PlaylistUrlRetriever` guard | `NullReferenceException` | Channel skipped in batch mode (`AggregateException`) |
| Playlist body is not `#EXTM3U` | `PlaylistUrlRetriever` | `null` | Channel skipped |
| `streamlink` missing | `StreamlinkProcessor` | `Win32Exception` | Channel skipped; `AggregateException` in batch mode |
| Streamlink returns non-`http` | `StreamlinkProcessor` | `string.Empty` | Channel skipped |
| Browser element timeout | `TvSportHdProcessor`, `AntenaPlayProcessor` | `WebDriverTimeoutException` | Channel skipped; lock possibly retained |
| Unparsable `Provider` | `ChannelStreamMappingExtensions` | `ArgumentException` | `AggregateException`; logged per inner exception |
| Output path unwritable | `PlaylistFileGenerator` writers | `IOException`/`UnauthorizedAccessException` | Logged `Fatal`; partial output possible |

## Design intent

- **A dead channel must not fail a batch.** This motivates swallowing at provider and download
  levels and skipping unresolvable channels.
- **Validation protects consumers.** A resolved URL is only emitted if it is a plausible
  playlist, which prevents writing broken entries into published playlists.
- **Fatal means "the run could not proceed", not "the run failed".** Only exceptions escaping
  the batch loop reach `Program`'s handlers.

## Consequences to keep in mind

- The process exit code is always `0`; success must be inferred from the output file and the log.
- At the default `minimumLevel` of `Info`, per-channel failures are not visible, so a run that
  produces an empty playlist looks successful.
- Nothing is retried; a transient network failure permanently removes a channel from that run.
- No circuit breaking exists, so a provider-wide outage produces one failed attempt per channel.

## Gaps and hazards

| Hazard | Detail | Reference |
|--------|--------|-----------|
| `NullReferenceException` path | Occurs for any non-Streamlink provider returning `null` when validation is required; pinned by tests as intended behaviour | [flows/playlist-url-validation.md](./flows/playlist-url-validation.md) |
| Lock retention | A browser processor exception skips `ReleaseLock` | [components/browser-automation.md](./components/browser-automation.md) |
| Overwrite on failure | An empty playlist can replace a valid one | [state-and-persistence.md](./state-and-persistence.md) |
| Unsynchronised list mutation | Two XML entries sharing a `ChannelName` may lose a URL | [concurrency-and-scheduling.md](./concurrency-and-scheduling.md) |

## Related documentation

- [troubleshooting.md](./troubleshooting.md)
- [logging.md](./logging.md)
- [ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md)
