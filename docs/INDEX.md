# StreamToM3U documentation

Master index for the `docs/` knowledge base of the StreamToM3U repository.

## Repository summary

StreamToM3U is a stateless, single-process .NET 10 console application that resolves live
stream URLs and produces M3U/M3U8 playlists. In single-stream mode it resolves one URL from a
source page and writes it to standard output. In batch mode it reads an XML channel list,
resolves every channel through one of four providers (`Website`, `Streamlink`, `TvSportHd`,
`AntenaPlay`), and writes either one merged `playlist.m3u` or a directory containing an index
playlist plus one `.m3u8` file per channel. There is no server, no database, and no inbound
network surface; the only persistence is the local filesystem (configuration, input XML, log
file, and generated playlists).

See [repository-overview.md](./repository-overview.md) for the fuller scope statement.

## Root document map

| Document | Purpose |
|----------|---------|
| [README.md](../README.md) | Installation, usage, configuration, deployment templates, and data formats |
| [ARCHITECTURE.md](../ARCHITECTURE.md) | System context, component responsibilities, runtime procedure, dependency rules, extension points |
| [CONTRIBUTING.md](../CONTRIBUTING.md) | Contribution process and standards |
| [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) | Conduct standards for project spaces |
| [SECURITY.md](../SECURITY.md) | Supported versions, vulnerability reporting, disclosure policy |
| [PRIVACY.md](../PRIVACY.md) | Privacy and personal-data handling |
| [LICENSE](../LICENSE) | GNU General Public License v3.0 |

The `docs/` tree complements these documents and does not reproduce them. This repository has
no `ROADMAP.md`.

## Documentation catalogue

### Orientation

| Document | Description |
|----------|-------------|
| [INDEX.md](./INDEX.md) | This master index and navigation aid |
| [repository-overview.md](./repository-overview.md) | Purpose, scope, boundaries, and entry points |
| [repository-structure.md](./repository-structure.md) | Source tree layout and module organisation |
| [quick-start.md](./quick-start.md) | Getting started for newcomers |
| [faq.md](./faq.md) | Frequently asked questions |

### Understanding

| Document | Description |
|----------|-------------|
| [architecture.md](./architecture.md) | Layered pipeline, provider strategy, and dependency direction |
| [design-decisions.md](./design-decisions.md) | Key design choices and their rationale |
| [dependencies.md](./dependencies.md) | Internal and external dependency inventory |
| [invariants.md](./invariants.md) | System-wide invariants and contracts |

### Reference

| Document | Description |
|----------|-------------|
| [api-reference/INDEX.md](./api-reference/INDEX.md) | Public surface index (command line and process contract) |
| [api-reference/command-line-interface.md](./api-reference/command-line-interface.md) | Complete command-line interface reference |
| [api-usage-examples.md](./api-usage-examples.md) | Runnable command and output examples |
| [configuration.md](./configuration.md) | Configuration keys, sources, precedence, and defaults |
| [data-model.md](./data-model.md) | Domain entities, XML schema, and generated playlist formats |
| [logging.md](./logging.md) | Logging framework, operations, keys, levels, and destinations |
| [integrations.md](./integrations.md) | External systems and their contracts |

### Behaviour and flows

| Document | Description |
|----------|-------------|
| [behaviour/resolve-single-stream.md](./behaviour/resolve-single-stream.md) | Single-URL resolution behaviour |
| [behaviour/generate-merged-playlist.md](./behaviour/generate-merged-playlist.md) | Merged playlist generation behaviour |
| [behaviour/generate-playlist-directory.md](./behaviour/generate-playlist-directory.md) | Directory playlist generation behaviour |
| [flows/startup-and-composition.md](./flows/startup-and-composition.md) | Startup, configuration binding, and composition |
| [flows/single-stream-resolution.md](./flows/single-stream-resolution.md) | Provider dispatch and URL validation for one stream |
| [flows/batch-playlist-generation.md](./flows/batch-playlist-generation.md) | Parallel resolution and file writing for a channel list |
| [flows/playlist-url-validation.md](./flows/playlist-url-validation.md) | Playlist content validation and entry selection |

### Component deep dives

| Document | Description |
|----------|-------------|
| [components/entry-and-composition.md](./components/entry-and-composition.md) | `Program`, the composition root |
| [components/configuration.md](./components/configuration.md) | `Options`, `ApplicationSettings`, `StreamProvider` |
| [components/service-layer.md](./components/service-layer.md) | `FileDownloader`, `PlaylistUrlRetriever`, `PlaylistFileGenerator`, mapping, models |
| [components/processors.md](./components/processors.md) | The four `IProcessor` implementations |
| [components/browser-automation.md](./components/browser-automation.md) | `WebDriverHandler` and NuciWeb automation |
| [components/data-access-and-mapping.md](./components/data-access-and-mapping.md) | `ChannelStreamEntity` and `ChannelStreamMapping` |
| [components/logging-and-observability.md](./components/logging-and-observability.md) | Log vocabulary and diagnostics |

### Operational

| Document | Description |
|----------|-------------|
| [build-and-deployment.md](./build-and-deployment.md) | Build, CI, release, and deployment topology |
| [state-and-persistence.md](./state-and-persistence.md) | Filesystem state and generated artefacts |
| [concurrency-and-scheduling.md](./concurrency-and-scheduling.md) | Parallelism, blocking, and scheduling |
| [error-handling.md](./error-handling.md) | Error taxonomy, swallowing policy, and recovery |
| [troubleshooting.md](./troubleshooting.md) | Common issues and diagnosis |

### Technical

| Document | Description |
|----------|-------------|
| [security.md](./security.md) | Trust boundaries and threat surfaces (complements root `SECURITY.md`) |
| [testing.md](./testing.md) | Test strategy, fixtures, and coverage gaps |

### Maintenance

| Document | Description |
|----------|-------------|
| [change-guide.md](./change-guide.md) | How to modify common areas safely |
| [ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md) | Known gaps and unresolved items |
| [documentation-maintenance.md](./documentation-maintenance.md) | How to keep this knowledge base current |

## Navigation aids

- **Start here (newcomers):** [repository-overview.md](./repository-overview.md) → [quick-start.md](./quick-start.md) → [architecture.md](./architecture.md).
- **Deep dive (component owners):** [components/](./components/) → [invariants.md](./invariants.md) → [change-guide.md](./change-guide.md).
- **Flows (debuggers):** [flows/](./flows/) → [error-handling.md](./error-handling.md) → [troubleshooting.md](./troubleshooting.md).
- **Operators:** [configuration.md](./configuration.md) → [build-and-deployment.md](./build-and-deployment.md) → [logging.md](./logging.md).

## Maintenance metadata

| Field | Value |
|-------|-------|
| Last reviewed | 2026-11-16 |
| Coverage status | Core application, tests, release automation, and configuration documented from source |
| Owner | Repository maintainer (`hmlendea`) |
| Review cadence | Update on any change to providers, CLI options, output formats, or configuration keys; see [documentation-maintenance.md](./documentation-maintenance.md) |
