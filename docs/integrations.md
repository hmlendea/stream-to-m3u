# Integrations

## Overview

All integrations are outbound. The application initiates every interaction; nothing connects to
it.

| Integration | Direction | Compatibility | Required |
|-------------|-----------|---------------|----------|
| Website (HTTP) | Outbound | Any page exposing an `.m3u`/`.m3u8` URL in quoted HTML | No — default provider |
| Streamlink executable | Outbound process | Streamlink installed and on `PATH` | No — only for the `Streamlink` provider |
| TVSportHD website | Outbound HTTP + browser | Chrome or Chromium; site structure | No — only for the `TvSportHD` provider |
| AntenaPlay website | Outbound HTTP + browser | Chrome or Chromium; site form and stream pattern | No — only for the `AntenaPlay` provider |
| Channel CDNs | Outbound HTTP | Any HTTP source returning a playlist body | No — used for validation and probing |

## Website provider

**Contract.** The page HTML must contain an absolute `.m3u`/`.m3u8` URL inside either single or
double quotes, or a relative one when `StreamBaseUrl` is configured.

**Failure semantics.** No match yields `string.Empty` → the retriever returns `null` (or
`NullReferenceException` when the page itself is empty). A page fetch failure yields `null` from
the downloader.

**Coupling.** Regex-based, so any page restructuring that changes quoting or URL shape breaks the
extraction. See [components/processors.md](./components/processors.md).

## Streamlink

**Contract.** `streamlink --stream-url <pageUrl> best` writes a playable URL to standard output.
Commas are percent-encoded before use.

**Failure semantics.** Empty or non-`http` output becomes an empty string; a missing executable
raises `Win32Exception` (surfacing as `AggregateException` in batch mode).

**Coupling.** Depends on the CLI contract and on Streamlink's own extractors.

## TVSportHD

**Contract.** `http://www.tv-sport-hd.com/channel/tvs.php?ch=<ChannelId>` renders an iframe with
id `thatframe` containing an HTML5 player whose `source` element (`//*[@id='playerDIV_html5_api']/source`)
carries the playlist URL in `src`.

**Failure semantics.** Any missing element produces a Selenium timeout; because `ReleaseLock` is
not in a `finally` block, the lock may remain taken.

**Coupling.** Depends entirely on site structure and identifiers.

## AntenaPlay

**Contract.** `https://antenaplay.ro/cont-nou` registration accepts a generated e-mail, password,
first and last name, and a terms checkbox, then requires SMS validation to be offered;
`https://antenaplay.ro/live/<ChannelId>` exposes `streamURL: "<url>"` in the page source.

**Failure semantics.** A missing `js-btn-sms` element or no regex match fails the resolution and
may retain the lock.

**Privacy.** A disposable account is created per run with random ten-character values, always
using a `@gmail.com` address, and the session logs out at the end. The account remains with the
provider. See [../PRIVACY.md](../PRIVACY.md).

## Channel CDNs

**Contract.** A `GET` on the resolved playlist URL returns a body whose first line is `#EXTM3U`.

**Failure semantics.** A `null` body fails validation and the channel is skipped. In merged mode
an additional probe download occurs even after validation, and a failed probe removes the URL.

## Retry, circuit breaking, and quotas

None. There is no retry, no backoff, no circuit breaker, and no rate limiting. Under batch mode,
requests run in parallel with no concurrency bound, and browser-driven providers serialize one at
a time. See [concurrency-and-scheduling.md](./concurrency-and-scheduling.md).

## Adding an integration

See [change-guide.md](./change-guide.md). New providers must implement
`IProcessor`, be added to `StreamProvider` and the dispatch switch, gain CLI aliases, and be
documented in both the README integrations table and this file.
