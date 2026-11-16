# API reference index

StreamToM3U exposes no network API — there are no controllers, routes, or listening sockets.
Its public surface is the **process contract**: the command-line arguments it accepts, the
standard output it emits, the files it writes, and the exit code it returns.

## Interface summary

| Interface | Kind | Contract | Documented in |
|-----------|------|----------|---------------|
| Command line | Inbound | Aliased flags parsed by `Options.FromArguments` | [command-line-interface.md](./command-line-interface.md) |
| Standard output | Outbound | One resolved URL when no `--input` is supplied | [command-line-interface.md](./command-line-interface.md) |
| Configuration file | Inbound | `appsettings.json` sections `applicationSettings`, `nuciLoggerSettings` | [../configuration.md](../configuration.md) |
| XML channel list | Inbound | `ArrayOfChannelStreamEntity` document | [../data-model.md](../data-model.md) |
| Playlist files | Outbound | `playlist.m3u` and per-channel `.m3u8` | [../data-model.md](../data-model.md) |
| Log file | Outbound | Structured lines at the configured path | [../logging.md](../logging.md) |
| Exit code | Outbound | Always `0` | [command-line-interface.md](./command-line-interface.md) |

## Endpoints

None. There is no HTTP, gRPC, GraphQL, or message-bus surface.

## Authentication

None. The tool makes outbound requests only, holds no credentials, and exposes no inbound
endpoint. The AntenaPlay provider registers a disposable account with the third-party site; it
is not an authentication mechanism of this application. See [../security.md](../security.md).

## Versioning

There is no wire protocol to version. The **stable contracts** that consumers depend on are:

- Command-line aliases and defaults.
- The XML element and field names.
- Playlist markers (`#EXTM3U`, `#EXTINF:-1,<name>`, `#EXT-X-STREAM-INF:BANDWIDTH=873`) and
  channel file naming.
- `StreamProvider` names and numeric values.
- `nuciLoggerSettings` and `applicationSettings` key names.

These are recorded in [../invariants.md](../invariants.md).

## Rate limiting and throttling

The tool applies no rate limiting of its own. It uses one `HttpClient` instance and processes
channels in parallel with no concurrency bound other than the thread pool, so a large channel
list can issue many concurrent requests. Browser-driven providers serialize one at a time. See
[../concurrency-and-scheduling.md](../concurrency-and-scheduling.md).

## Related documentation

- [../flows/](./../flows/) — end-to-end execution flows.
- [../components/service-layer.md](./../components/service-layer.md) — orchestration internals.
- [../data-model.md](./../data-model.md) — input and output data shapes.
