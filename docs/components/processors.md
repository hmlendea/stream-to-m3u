# Component: processors

Sources: `StreamToM3U/Service/Processors/`.

## Contract

```csharp
public interface IProcessor
{
    Task<string> GetUrlAsync(StreamInfo streamInfo);
}
```

A processor receives a `StreamInfo` and returns a candidate URL. A failure is expressed by
returning an empty value or by throwing — the caller (`PlaylistUrlRetriever.FindStreamUrl`)
converts any exception into `null`.

Boundary rules: processors must not log, must not write files, must not construct `Options`, and
must release any `WebDriverHandler` lock they acquire.

## Dispatch

`PlaylistUrlRetriever.CreateProcessor`:

| Provider | Processor |
|----------|-----------|
| `TvSportHd` | `TvSportHdProcessor` |
| `AntenaPlay` | `AntenaPlayProcessor` |
| `Streamlink` | `StreamlinkProcessor` |
| anything else (including `Website`) | `WebsiteProcessr(downloader)` |

Processors are created per resolution call, not registered in the container.

## `WebsiteProcessr`

Default provider. Exists under this spelling in source (`WebsiteProcessr`, missing final `o`).

**Algorithm.**

1. Download `streamInfo.Url` through the injected `IFileDownloader`. A `null`, empty, or
   whitespace body returns `null`.
2. Try patterns in order:
   - `"(http[^"']*\.m3u[^"']*)"` — double-quoted absolute playlist URL;
   - `'(http[^"']*\.m3u[^"']*)'` — single-quoted absolute playlist URL.
   The first non-whitespace match wins, so double quotes are preferred.
3. If no absolute match and `StreamBaseUrl` is set, try `["']([^"']*\.m3u[^"']*)["']` and
   `Path.Join(StreamBaseUrl, match)`.
4. Otherwise return an empty string.
5. A successful match is trimmed and has `Environment.NewLine` removed and `&amp;` replaced by
   `&`.

**Behavioural notes.**

- Regexes operate on raw HTML, so quoting and surrounding markup affect the result.
- A relative URL without a base URL yields `string.Empty` (not `null`), which the retriever then
  fails to validate and returns as `null`.
- The `.m3u` pattern matches `.m3u8` too, because `.m3u` is a prefix.

**Tests.** `WebsiteProcessrTests` (18 cases).

## `StreamlinkProcessor`

**Algorithm.**

1. Start `streamlink --stream-url <url> best` with `RedirectStandardOutput = true`,
   `UseShellExecute = false`, `CreateNoWindow = true`.
2. Read standard output to the end and await exit.
3. If the output is empty/whitespace or does not start with `http`, return `string.Empty`.
4. Otherwise trim, remove newlines, and replace `,` with `%2c`.

**Behavioural notes.**

- Requires the `streamlink` executable on `PATH`; if missing, `Process.Start` throws
  `Win32Exception`, which becomes `null` in single mode and `AggregateException` in batch mode.
- Commas are percent-encoded, which suits URLs where a comma is a separator.
- No tests: exercising it requires the external executable.

## `TvSportHdProcessor`

**Algorithm.**

1. `GainLock()` on `WebDriverHandler`.
2. Build `http://www.tv-sport-hd.com/channel/tvs.php?ch=<ChannelId>`.
3. Navigate, wait for the `thatframe` element, and switch into that iframe.
4. Wait for `//*[@id='playerDIV_html5_api']/source` and read its `src`.
5. `ReleaseLock()` and return the URL.

**Behavioural notes.**

- `ReleaseLock` is not in a `finally`, so a DOM mismatch (`WaitForElementToExist` throwing) skips
  it and leaves the lock taken, stalling later browser-driven channels.
- The helper `webProcessor` is created against `WebDriverHandler.WebDriver`, so the first use
  triggers driver creation.
- No tests: exercising it requires a browser and the live site.

## `AntenaPlayProcessor`

**Algorithm.**

1. `GainLock()` on `WebDriverHandler`.
2. Build `https://antenaplay.ro/live/<ChannelId>`.
3. `RegisterAccount()`:
   - navigate to `https://antenaplay.ro/cont-nou`;
   - accept GDPR once per process (static `acceptedGdpr`), clicking the button by absolute XPath;
   - fill `email`, `password`, `firstname`, `lastname`;
   - tick the `agree` checkbox and submit `//form/button`;
   - wait for `js-btn-sms` to be visible.
4. Navigate to the channel URL, click `start-video`, then read the page source and extract
   `streamURL: "([^"]*)"`.
5. `ClearResources()`: navigate to `/logout`, dispose the processor wrapper, and release the lock.

**Generated values.** A ten-character lowercase random string is used for the e-mail local part,
password, first name, and last name; the e-mail always uses `@gmail.com`. Names are
sentence-cased. The values are not persisted by the application.

**Behavioural notes.**

- The account persists with the provider after logout; the application does not delete it.
- `ClearResources` is not in a `finally`, so an exception before it leaves the lock taken and the
  browser session logged in.
- The static `acceptedGdpr` flag means the GDPR banner is only handled once per process, which is
  correct for a reused driver.
- No tests: exercising it requires a browser and the live site.

## Failure-mode summary

| Processor | Failure expression | Retriever result |
|-----------|--------------------|------------------|
| `WebsiteProcessr` | `null` (no page) or `string.Empty` (no match) | `null`, or `NullReferenceException` when the page is empty |
| `StreamlinkProcessor` | `string.Empty`, or a thrown `Win32Exception` | `null` or a valid CLI URL |
| `TvSportHdProcessor` | Thrown Selenium timeout | `null`, lock possibly retained |
| `AntenaPlayProcessor` | Thrown Selenium timeout or no regex match | `null`, lock possibly retained |

## Related documentation

- [../flows/single-stream-resolution.md](../flows/single-stream-resolution.md)
- [browser-automation.md](./browser-automation.md)
- [../integrations.md](../integrations.md)
- [../change-guide.md](../change-guide.md)
