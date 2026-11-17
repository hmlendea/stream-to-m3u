# Change guide

Safe modification procedures for the areas most likely to change. Each section lists the files,
the steps, the contracts to preserve, and the tests to extend.

## Adding a stream provider

**Files.** `Service/Processors/` (new processor), `Configuration/StreamProvider.cs`,
`Configuration/Options.cs`, `Service/PlaylistUrlRetriever.cs`.

**Steps.**

1. Implement `IProcessor.GetUrlAsync` in `StreamToM3U/Service/Processors/`. Return the candidate
   URL; express failure by returning an empty value or throwing.
2. Add the enumeration member to `StreamProvider`. Do **not** reorder existing members (the
   numeric values are a persisted contract).
3. Add the case to the `CreateProcessor` switch.
4. Add a CLI alias array and a branch in `DetermineProviderFromArgs`, observing the priority
   order.
5. If the provider needs a browser, acquire and release `WebDriverHandler` locks — ideally in a
   `try/finally`.
6. Extend `PlaylistUrlRetrieverTests` for any validation bypass the provider needs.
7. Update the README integrations and option tables, `docs/integrations.md`, and
   `docs/components/processors.md`.

**Preserve.** Processor must not log, must not write files, and must not construct `Options`.

## Changing an output format

**Files.** `Service/PlaylistFileGenerator.cs`.

**Steps.**

1. Revise the writers. Keep both modes producing a valid `#EXTM3U` header.
2. If markers, ordering, or file naming change, update `docs/data-model.md`,
   `docs/invariants.md`, and the README data-format section, and note the consumer impact.
3. Extend `PlaylistFileGeneratorTests` for both modes and the index reference forms.

**Preserve.** Only `PlaylistFileGenerator` writes playlists; alphabetical ordering by
`ChannelName`.

## Changing the command-line interface

**Files.** `Configuration/Options.cs`.

**Steps.**

1. Add or adjust an alias array entry; do not repurpose an existing alias.
2. Add a `TestCase` row to `OptionsTests` for each accepted form and for the negative cases.
3. Update `docs/configuration.md`, `docs/api-reference/command-line-interface.md`, and the README
   option table.

**Preserve.** Existing aliases, defaults (`playlist.m3u`, `Website`), and provider priority.

## Changing configuration keys

**Files.** `Configuration/ApplicationSettings.cs`, `Program.cs`, `StreamToM3U/appsettings.json`.

**Steps.**

1. Add the property and bind it; keep new keys optional with sensible defaults.
2. Update `docs/configuration.md`, `docs/dependencies.md` if the package set changes, and the
   README settings table.
3. If a key affects logging level or path, update `docs/logging.md`.

**Preserve.** Section and property names are contracts; a rename needs a migration path.

## Changing resolution or validation

**Files.** `Service/PlaylistUrlRetriever.cs`, `Service/FileDownloader.cs`,
`Service/Processors/WebsiteProcessr.cs`.

**Steps.**

1. Change the behaviour, then update `docs/flows/playlist-url-validation.md` and
   `docs/flows/single-stream-resolution.md`.
2. Update the affected invariant entries, especially the `googlevideo`/Streamlink bypass and the
   entry-selection policy.
3. Extend `PlaylistUrlRetrieverTests`; if a previously pinned hazard (for example the
   `NullReferenceException`) is fixed, replace the pinning test deliberately.

**Preserve.** Playlist URLs published previously must remain resolvable where possible.

## Adding a service

**Files.** `Program.cs`, `Service/` (interface and implementation).

**Steps.**

1. Define the interface in `Service/`.
2. Register it as a singleton in the container.
3. Ensure it is safe as a singleton (no per-run state assumptions) and that the application
   project still does not reference the test project.
4. Add a fixture under `StreamToM3U.UnitTests/Service/`.

## Hardening the browser lock

**Files.** `Utils/WebDriverHandler.cs`, browser processors.

**Steps.**

1. Replace the spin lock so waiting does not hold the monitor, or keep the monitor but poll
   outside it.
2. Make release idempotent and guarantee it on the failure path (for example `try/finally` in
   each processor).
3. Extend `WebDriverHandlerTests` for release-on-failure and re-entrancy semantics.
4. Update `docs/components/browser-automation.md`, `docs/concurrency-and-scheduling.md`, and the
   related hazards in `docs/invariants.md` and `docs/error-handling.md`.

## Dependency updates

**Files.** the two `.csproj` files.

1. Update the pin for the affected packages.
2. Re-run `dotnet build` and `dotnet test`.
3. Update the README dependency table and `docs/dependencies.md`.
4. If `<TargetFramework>` changes, coordinate with the release workflow, which derives the toolchain
   from it; update `docs/build-and-deployment.md`.

## Documentation synchronisation

Any change to behaviour, contracts, configuration, or structure requires the corresponding
documents to be updated. The mapping is in
[documentation-maintenance.md](./documentation-maintenance.md). Root documents are governed by
the README/ARCHITECTURE fidelity rules and must be revised only where genuinely affected.

## Related documentation

- [invariants.md](./invariants.md)
- [testing.md](./testing.md)
- [documentation-maintenance.md](./documentation-maintenance.md)
