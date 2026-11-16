# Component: configuration

Sources:

- `StreamToM3U/Configuration/Options.cs` → `StreamToM3U.Configuration.Options`
- `StreamToM3U/Configuration/ApplicationSettings.cs` → `StreamToM3U.Configuration.ApplicationSettings`
- `StreamToM3U/Configuration/StreamProvider.cs` → `StreamToM3U.Configuration.StreamProvider`

## Purpose

Translate the process inputs (command-line arguments and `appsettings.json`) into strongly typed
objects consumed by the rest of the application.

## `Options`

A sealed class with static alias arrays and a single parsing entry point `FromArguments`.

| Member | Purpose |
|--------|---------|
| `Provider` | Selected `StreamProvider` |
| `InputFile` | Batch input path (also decides the run mode) |
| `OutputFile` | Merged output path, defaulting to `playlist.m3u` |
| `OutputDirectory` | Directory output path; takes precedence when non-whitespace |
| `ChannelId`, `Title`, `Url`, `StreamBaseUrl` | Per-resolution parameters |

Alias arrays: `InputFileOptions`, `OutputFileOptions`, `OutputDirectoryOptions`,
`ChannelIdOptions`, `TitleOptions`, `UrlOptions`, `StreamBaseUrlOptions`,
`TvSportHdProcessorOptions`, `AntenaPlayProccessorOptions`, `StreamlinkProcessorOptions`.

### Parsing internals

| Method | Behaviour |
|--------|-----------|
| `FromArguments` | Builds the instance from all `GetArgumentIfExists` calls |
| `DetermineProviderFromArgs` | Priority: `TvSportHd`, `AntenaPlay`, `Streamlink`, else `Website` |
| `GetArgumentIfExists(args, options[, fallback])` | Returns the value, the fallback, or `null` |
| `HasOption` | True when `GetOptionValue` returns non-`null` |
| `GetOptionValue` | Scans for `option` (returning the next non-`-` token or an empty string) or `option=` (returning the remainder) |

### Boundary rules

- No dependency on `Service`, processors, or data-access types.
- The alias arrays are the single place that defines the CLI contract.
- Matching is exact and case-sensitive.

## `ApplicationSettings`

| Property | Type | Source key |
|----------|------|-----------|
| `RequestTimeout` | `int` | `applicationSettings:requestTimeout` |
| `UserAgent` | `string` | `applicationSettings:userAgent` |

Bound once, passed to `FileDownloader` which uses both values in its constructor. A zero timeout
or empty user agent makes construction throw.

## `StreamProvider`

```
Website, TvSportHd, AntenaPlay, Streamlink
```

| Property | Value |
|----------|-------|
| Persisted contract | Numeric values `0`, `1`, `2`, `3` in declaration order |
| Name contract | The member names are matched against the XML `Provider` value case-insensitively |
| Default | `Website` when no provider flag is present |

Consumers: `Options.DetermineProviderFromArgs`, `ChannelStreamMapping.ToServiceModel`,
`PlaylistUrlRetriever.CreateProcessor` and the validation guard.

## Modification impact

- Adding an alias means extending the relevant array only.
- Adding a provider means extending the enumeration and the dispatch switch together; XML files
  reference members by name.
- Renaming a member or reordering the enumeration breaks published playlist consumption and
  existing XML files.

## Tests

| Fixture | Coverage |
|---------|----------|
| `OptionsTests` | Defaults, every alias form, precedence, empty values, unknown arguments (57 cases) |
| `StreamProviderTests` | Full value set, numeric contract, name parsing (9 cases) |

## Related documentation

- [../configuration.md](../configuration.md)
- [../api-reference/command-line-interface.md](../api-reference/command-line-interface.md)
- [../invariants.md](../invariants.md)
