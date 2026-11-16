# Testing

## Strategy

The suite is a hermetic unit-test set. It verifies resolution, generation, configuration defaults,
logging vocabulary, and browser-lock semantics with Moq doubles, and it never touches the network,
a browser, or Streamlink. Integration and end-to-end verification is manual.

## Framework and conventions

| Aspect | Choice |
|--------|--------|
| Framework | NUnit 5.0.0 |
| Doubles | Moq 4.21.0 |
| Adapter | NUnit3TestAdapter 6.3.1 |
| Analyzers | NUnit.Analyzers 4.15.0 |
| Naming | `Given<Context>_When<Action>_Then<Outcome>` |
| Assertions | `Assert.Multiple` for related assertions; `[TestCase]` for data-driven rows |
| Structure | `SetUp`/`TearDown` per fixture; temporary directories created and deleted per test |

## Fixtures

| Fixture | Cases | Covers |
|---------|-------|--------|
| `OptionsTests` | 57 | Defaults, every alias form, precedence, empty values, unknown arguments, provider priority |
| `StreamProviderTests` | 9 | Value set, numeric contract, name parsing |
| `MyOperationTests` | 2 | Operation names |
| `MyLogInfoKeyTests` | 3 | Key names and inequality |
| `FileDownloaderTests` | 33 | Construction guards, URL normalisation, non-transmission of invalid URLs, logging, loopback download, status codes, user agent |
| `PlaylistFileGeneratorTests` | 23 | Empty input, alphabetical order, multiple streams, skipping, logging, directory creation, naming, index forms, merged filtering |
| `PlaylistUrlRetrieverTests` | 24 | Absolute and relative references, base URL joining, `googlevideo` bypass, entry selection, non-playlist bodies, escaped URLs, `NullReferenceException` |
| `WebsiteProcessorTests` | 18 | Quoting preference, base URL joining, empty pages, sanitisation, single download |
| `WebDriverHandlerTests` | 3 | Lock free/taken/released |

Total: **172 cases**.

## Test doubles

`LoopbackHttpServer` (in `TestDoubles`) binds a `TcpListener` to `127.0.0.1` on an ephemeral port
and serves a fixed status, `Content-Type: application/vnd.apple.mpegurl`, and body. It records
received request headers in a `ConcurrentQueue`, which lets tests prove that the configured
`User-Agent` is sent and that invalid URLs are never transmitted — without patching `HttpClient`.

The class is `internal` and implements `IDisposable`; `Dispose` cancels the accept loop and stops
the listener.

## What the tests pin

- Configuration: defaults, alias forms, priority, empty-value semantics, case sensitivity.
- Download: constructor guards, normalisation, error-status passthrough, header transmission.
- Resolution: validation rules, entry selection, bypass conditions, and the `NullReferenceException`
  on empty pages.
- Output: markers, ordering, naming, index forms, filtering in merged mode, and the absence of
  downloads in directory mode.
- Browser lock: the three lock transitions only.

## Coverage gaps

| Area | Reason |
|------|--------|
| `Program` orchestration | No test constructs the container or drives `Main` |
| `StreamlinkProcessor` | Requires the external executable |
| `TvSportHdProcessor` | Requires a browser and the live site |
| `AntenaPlayProcessor` | Requires a browser and the live site |
| `WebDriverHandler.CreateDriver` | Requires Chrome |
| Concurrent resolution behaviour | No test drives two XML entries sharing a `ChannelName` in parallel |
| Release workflow | Not exercised by any automated check |
| Configuration binding from `appsettings.json` | Settings are constructed directly in tests |

## Running

```bash
dotnet test
dotnet test --filter "FullyQualifiedName~PlaylistFileGeneratorTests"
```

CI runs `dotnet test --no-build --verbosity normal` on `ubuntu-latest` after `dotnet build`.

## Guidance for new tests

- Prefer the canonical fixture values in the existing suite for consistency.
- Keep tests hermetic: no real network, no browser, no Streamlink.
- Assert the observable contract (output lines, log records, file names) rather than internals.
- When a hazard listed in [invariants.md](./invariants.md) is fixed, replace the pinning test
  rather than deleting it silently.

## Related documentation

- [invariants.md](./invariants.md)
- [change-guide.md](./change-guide.md)
- [build-and-deployment.md](./build-and-deployment.md)
