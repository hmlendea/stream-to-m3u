# Design decisions

Each decision below records the choice, the constraint that produced it, and the consequence a
future change must account for.

## Provider strategy behind `IProcessor`

**Decision.** Providers implement `IProcessor.GetUrlAsync(StreamInfo)` and are selected by a
`switch` on `StreamProvider` inside `PlaylistUrlRetriever.CreateProcessor`.

**Rationale.** The providers share one contract (given a page URL and channel identifier,
produce a candidate URL) but have nothing else in common: regular expressions, an external CLI,
and two browser automation flows.

**Consequence.** Adding a provider requires coordinated edits to `StreamProvider`, the
`CreateProcessor` switch, the `Options` alias arrays, and the XML `Provider` values. There is no
plugin or reflection discovery. The `switch` expression's default arm is the website provider,
so an unknown enumeration member silently degrades to HTML extraction rather than failing.

## One shared static `ChromeDriver`

**Decision.** `WebDriverHandler` lazily creates a single headless `ChromeDriver` held in static
state, and browser providers serialize through `GainLock`/`ReleaseLock`.

**Rationale.** Driver startup is slow and Selenium/CDP sessions are not designed for concurrent
control from one instance.

**Consequence.** Browser-driven channels cannot run in parallel; a provider that throws before
releasing the lock stalls all later browser-driven channels. See
`docs/ambiguities-and-open-questions.md`.

## `Parallel.ForEach` with blocking `.Result`

**Decision.** Batch resolution uses `Parallel.ForEach` over the channel list while calling
`urlRetriever.GetStreamUrlAsync(...).Result`.

**Rationale.** It introduced parallelism without turning `Main`, the generator, and the
retriever into a fully asynchronous pipeline.

**Consequence.** Parallelism is thread-pool workers blocked on I/O, and exceptions surface as
`AggregateException` rather than their original type. The newline handling and per-list mutation
in the result dictionary also rely on this shape. See
[concurrency-and-scheduling.md](./concurrency-and-scheduling.md).

## Swallow processor and download exceptions

**Decision.** `PlaylistUrlRetriever.FindStreamUrl` catches every processor exception and returns
`null`; `FileDownloader.TryDownloadStringAsync` catches every exception, logs at `Verbose`, and
returns `null`.

**Rationale.** One dead channel must not fail an entire playlist regeneration.

**Consequence.** Success cannot be inferred from the process exit code, and the only evidence of
a failure is a log record that is emitted at `Debug` (below the default `Info` threshold). See
[error-handling.md](./error-handling.md).

## Two output modes sharing one resolution stage

**Decision.** `PlaylistFileGenerator` writes either a merged `playlist.m3u` or a directory with
an index plus per-channel `.m3u8` files; the choice is `string.IsNullOrWhiteSpace(Options.OutputDirectory)`.

**Rationale.** Consumers either want a single playlist or a publishable directory of per-channel
playlists served over HTTP.

**Consequence.** A single run produces one form only, and the merged form performs an extra
reachability download per URL that the directory form does not. See
[state-and-persistence.md](./state-and-persistence.md).

## Channel identity is `ChannelName`, not `Id`

**Decision.** Output groups resolved URLs by `ChannelName`; `Id` is only used for logging.

**Rationale.** One logical channel may be backed by several XML entries (for example a standard
and an HD source), all of which should appear under the same channel entry.

**Consequence.** Anything consuming the generated playlists sees one `#EXTINF` per URL sharing a
name, and the directory mode's file name is derived from the channel name. See
[flows/batch-playlist-generation.md](./flows/batch-playlist-generation.md).

## Fixed `#EXT-X-STREAM-INF:BANDWIDTH=873`

**Decision.** Every stream line in a per-channel file is preceded by the literal bandwidth
marker `873`.

**Rationale.** The playlist needs a well-formed `#EXT-X-STREAM-INF` line, and the tool does not
measure bandwidth.

**Consequence.** Consumers must not treat the value as meaningful, and any change would alter a
compatibility contract.

## Disposable AntenaPlay account per run

**Decision.** `AntenaPlayProcessor` registers a new account with random ten-character values on
every run, accepts GDPR, resolves the stream, then logs out.

**Rationale.** The provider exposes streams only to authenticated sessions, and the project
stores no credentials.

**Consequence.** Each AntenaPlay channel adds a registration round-trip and depends on the
provider's rate limits and form structure; the account persists with the provider after logout.
See [integrations.md](./integrations.md).

## Synchronous configuration reload is inert

**Decision.** `appsettings.json` is loaded with `optional: true, reloadOnChange: true`, but the
bound objects are plain singletons.

**Rationale.** The tool is short-lived; reload capability came with the default builder options.

**Consequence.** Editing `appsettings.json` during a run has no effect. A missing file leaves
`RequestTimeout` at `0`, which makes `HttpClient.Timeout` assignment throw. See
[configuration.md](./configuration.md).
