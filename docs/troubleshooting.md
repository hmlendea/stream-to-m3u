# Troubleshooting

## No URL printed in single-stream mode

**Symptom.** `Console.WriteLine` emits an empty line.

**Causes and checks.**

1. No source URL was supplied — pass `--url`.
2. The provider returned `null` or `string.Empty`. Lower `nuciLoggerSettings.minimumLevel` to
   `Debug` (or `Verbose`) and re-run to see `FileDownload` records.
3. The page is empty when fetched — a `NullReferenceException` is raised and logged at `Fatal`.
4. For browser providers, Chrome or Chromium is missing or the page structure changed.

## The playlist contains only `#EXTM3U`

**Symptom.** Batch mode produced an empty playlist.

**Causes and checks.**

1. No channel resolved. Lower `minimumLevel` to `Debug` and search for
   `ChannelStreamFetching` records with a `Failure` status.
2. The XML `Provider` values do not match the enumeration names.
3. `appsettings.json` is missing, so every download failed; a construction failure would also be
   logged at `Fatal`.

## Everything fails with `Fatal` before any channel is touched

**Symptom.** A `Fatal` record with `ArgumentOutOfRangeException` or `FormatException`.

**Cause.** `appsettings.json` is missing or incomplete: `requestTimeout` defaults to `0`, which
`HttpClient.Timeout` rejects, and an empty `userAgent` is rejected by
`DefaultRequestHeaders.Add`. Ensure the file is in the working directory with a positive timeout
and a non-empty user agent.

## A run appears to hang

**Symptom.** The process never finishes and produces no further log output.

**Cause.** The browser lock was acquired and never released, because a browser processor threw
before `ReleaseLock`. Every subsequent browser-driven channel spins on the lock. Kill the process;
avoid combining `TvSportHd` and `AntenaPlay` channels in a single long-running batch until the
release path is hardened.

## A published playlist lost its content after a run

**Symptom.** A previously valid playlist now contains only the header.

**Cause.** A run with failing channels still overwrites the output file. Retain prior output
externally (for example a versioned copy) and restore on failure; see
[build-and-deployment.md](./build-and-deployment.md).

## Channel file names do not match expectations

**Symptom.** A channel's `.m3u8` file has an unexpected name.

**Cause.** The name is derived by discarding the text before the first `:`, then removing
diacritics, punctuation, and spaces. Two channels that normalise identically share one file, and
the later write wins. Review
[behaviour/generate-playlist-directory.md](./behaviour/generate-playlist-directory.md).

## Stale channel files remain in the output directory

**Symptom.** Files exist for channels no longer in the XML list.

**Cause.** The generator never deletes files it did not write in the current run. Clean the
directory before publishing, or serve only files referenced by the current index.

## `streamlink` failures

**Symptom.** `Streamlink` channels never resolve.

**Cause.** The executable is not on `PATH`. Confirm with `streamlink --version`. A missing
executable produces a `Win32Exception`, seen as part of an `AggregateException` in batch mode.

## TVSportHD or AntenaPlay never resolves

**Symptom.** Browser-driven channels fail while `Website` and `Streamlink` channels succeed.

**Causes and checks.**

1. No Chrome/Chromium installed, so driver creation fails.
2. Site structure changed; the element identifiers used by the processor no longer match.
3. AntenaPlay requires a `--channel` identifier, and its registration flow must offer the SMS
   validation element.
4. See [components/processors.md](./components/processors.md) for the exact selectors.

## Diagnosing with the log file

Set `nuciLoggerSettings.minimumLevel` to `Debug` (or `Verbose` for download successes) and re-run,
then read `logfile.log`. Per-channel records carry `ChannelStreamId`, and download records carry
the normalised `Url`. See [logging.md](./logging.md).

## Related documentation

- [error-handling.md](./error-handling.md)
- [faq.md](./faq.md)
- [ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md)
