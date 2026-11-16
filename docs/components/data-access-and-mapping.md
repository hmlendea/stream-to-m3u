# Component: data access and mapping

Sources:

- `StreamToM3U/DataAccess/DataObjects/ChannelStreamEntity.cs`
- `StreamToM3U/Service/Mapping/ChannelStreamMapping.cs`

## `ChannelStreamEntity`

A sealed XML-serialisable class deriving from `NuciDAL.DataObjects.EntityBase`.

| Property | Type | XML element | Purpose |
|----------|------|-------------|---------|
| `Id` (inherited) | `string` | `Id` | Identifier used for logging |
| `ChannelName` | `string` | `ChannelName` | Output identity and grouping key |
| `Provider` | `string` | `Provider` | Free-form provider name, parsed into `StreamProvider` |
| `ChannelId` | `string` | `ChannelId` | Provider-specific identifier |
| `Title` | `string` | `Title` | Optional stream title |
| `Url` | `string` | `Url` | Source page URL |
| `StreamBaseUrl` | `string` | `StreamBaseUrl` | Base URL for relative entries |

The property names define the XML contract; renaming any of them invalidates existing
`Data/input-*.xml` files. The document root is `ArrayOfChannelStreamEntity`.

## `ChannelStreamMapping`

A static internal mapping class with two extension methods:

| Method | Behaviour |
|--------|-----------|
| `ToServiceModel(this ChannelStreamEntity)` | Projects to `ChannelStream`, parsing `Provider` with `Enum.Parse<StreamProvider>(value, true)` |
| `ToServiceModels(this IEnumerable<ChannelStreamEntity>)` | `Select(ToServiceModel)` |

## Why the mapping exists

Data-access objects must not leak into processors or the service layer. The mapping is the only
place where the string provider is converted to the enumeration, which keeps the enumeration and
XML contracts coupled in one location. A provider value that does not parse throws
`ArgumentException` here — inside the parallel body in batch mode.

## Storage

The repository is a `NuciDAL.Repositories.XmlRepository<ChannelStreamEntity>` constructed with
`options.InputFile`. It is read-only in this application: only `GetAll()` is used. Nothing writes
the XML files.

Note that the repository is constructed at container build time regardless of run mode, so even a
single-stream run holds an instance (unused) whose path may be `null`.

## Related documentation

- [../data-model.md](../data-model.md)
- [../state-and-persistence.md](../state-and-persistence.md)
- [configuration.md](./configuration.md)
