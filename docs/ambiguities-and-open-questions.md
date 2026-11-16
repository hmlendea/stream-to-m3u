# Ambiguities and open questions

Items that could not be resolved definitively from repository evidence, or that are known gaps.
Each entry states what is known, what is uncertain, and what evidence would settle it.

## Behavioural hazards confirmed in source

| Item | Evidence | Status |
|------|----------|--------|
| Browser lock is not released on the failure path | Neither `TvSportHdProcessor.GetUrlAsync` nor `AntenaPlayProcessor.GetUrlAsync` uses `try/finally` around `ReleaseLock`; `GetPlaylistUrlFromPage` can throw | Confirmed in source; consequence inferred |
| Unsynchronised per-channel list mutation | `foundChannelUrls[name].Add(url)` executes inside `Parallel.ForEach` with no lock on the list | Confirmed in source |
| `ReleaseLock` writes the flag outside the lock and the field is not `volatile` | `public static void ReleaseLock() => IsWebDriverLocked = false;` | Confirmed in source |
| `NullReferenceException` on an empty page with validation required | `url.Contains("googlevideo")` throws when `url` is `null`; pinned by tests | Confirmed, and asserted as intended |
| Unused `Task` list | `List<Task> tasks = [];` in `GeneratePlaylist` is never used | Confirmed in source; no behavioural effect |
| Configuration reload is inert | `reloadOnChange: true` with plain singleton settings | Confirmed in source |

## Unresolved questions

| Question | Why it matters | What would settle it |
|----------|----------------|----------------------|
| Is the `NullReferenceException` path intended, or an accident later pinned by tests? | Tests assert it, which suggests acceptance, but the guard's shape suggests an oversight | Maintainer intent, a changelog entry, or a commit message |
| Should `AntenaPlay` accounts be deleted rather than only logged out of? | The provider accumulates disposable accounts; PRIVACY.md documents logout only | Provider capability or a maintainer decision |
| Should merged mode probe reachability at all, given directory mode does not? | The probe costs one request per stream and discards the body; the asymmetry is unexplained | A commit rationale or an issue |
| Why is `BANDWIDTH=873` used? | The value is arbitrary and consumers must not interpret it | A commit rationale |
| Is the CI workflow's `dotnet-version: 10.0.x` guaranteed to match the project framework? | The release workflow derives the version; CI hardcodes it, so the two can drift | A workflow refactor deriving both from the `.csproj` |
| Is `WebsiteProcessr`'s misspelling deliberate or a typo? | Renaming would touch tests and user-visible logs | A maintainer decision; renaming is a safe refactor if desired |
| Are `Data/input-*.xml` files maintained as content or are they stale samples? | They are copied to the output directory and referenced by the README | Maintainer intent |
| Should the tool bound outbound concurrency? | Unbounded parallelism can overwhelm a source; no throttling exists | A requirement or an observed provider reaction |

## Known documentation gaps

| Gap | Detail |
|-----|--------|
| No `ROADMAP.md` | The repository has no roadmap document; planned work lives in issues and pull requests |
| No changelog | Release history is available through Git tags and GitHub Releases only |
| No architecture tests | Nothing enforces the layering rules recorded in [architecture.md](./architecture.md) or [invariants.md](./invariants.md) |
| `Program` untested | Orchestration, container construction, and shutdown are verified only by inspection |

## Scope notes

- `Vendor`, `tools`, and `scripts` directories do not exist; there is no local tooling to document.
- The repository contains no database, queue, cache, or scheduled job of its own.
- `bin/` and `obj/` are SDK outputs and are out of scope for documentation.

## Related documentation

- [invariants.md](./invariants.md)
- [error-handling.md](./error-handling.md)
- [testing.md](./testing.md)
- [change-guide.md](./change-guide.md)
