# Data model

## XML input schema

The batch input is an `ArrayOfChannelStreamEntity` document. The root element name and the
field names are fixed by the XML serialiser in `ChannelStreamEntity` and must not be renamed
without migrating existing files.

| Field | Required | Description |
|-------|----------|-------------|
| `Id` | Yes | Channel identifier; used for logging only |
| `ChannelName` | Yes | Output identity; groups resolved URLs and names the per-channel file |
| `Provider` | Yes | Free-form string parsed case-insensitively into `StreamProvider` |
| `ChannelId` | Conditional | Provider-specific identifier; required by `TvSportHd` and `AntenaPlay` |
| `Title` | Optional | Optional stream title |
| `Url` | Yes | Source page URL |
| `StreamBaseUrl` | Optional | Base URL used to absolutise relative playlist entries |

Sample files shipped in `StreamToM3U/Data/`:

| File | Channels | Providers used |
|------|----------|----------------|
| `input-ro.xml` | 25 | `Streamlink` (21), `Website` (4) |
| `input-md.xml` | 3 | `Website` (1), `Streamlink` (2) |
| `input-int.xml` | 1 | `Streamlink` (1) |

## Entity and service-model flow

```mermaid
flowchart LR
    XML["Data/input-*.xml"] -->|"XmlRepository.GetAll"| Entity["ChannelStreamEntity"]
    Entity -->|"ChannelStreamMapping.ToServiceModel"| Channel["ChannelStream"]
    Channel -->|"PlaylistUrlRetriever.GetStreamUrlAsync"| Info["StreamInfo"]
    Info -->|"processor dispatch"| Url["Resolved URL"]
```

`ChannelStreamEntity` is a `NuciDAL.DataObjects.EntityBase` subclass and never reaches
processors; it is converted to `ChannelStream` in `Service/Mapping/ChannelStreamMapping.cs`. The
`StreamInfo` request model is used for single-stream resolution and is built from `ChannelStream`
inside `PlaylistUrlRetriever`.

## Generated playlist formats

### Merged mode (`--output-file`)

```
#EXTM3U
#EXTINF:-1,<ChannelName>
<url>
#EXTINF:-1,<ChannelName>
<url>
```

One `#EXTINF` per resolved URL, in ascending `ChannelName` order. A URL whose content cannot be
downloaded or is empty is dropped before writing.

### Directory mode (`--output-dir`)

Index file `playlist.m3u`:

```
#EXTM3U
#EXTINF:-1,<ChannelName>
<absolute-path-or-baseurl>/<fileName>.m3u8
```

Per-channel file `<Channel>.m3u8`:

```
#EXTM3U
#EXT-X-STREAM-INF:BANDWIDTH=873
<url>
#EXT-X-STREAM-INF:BANDWIDTH=873
<url>
```

The channel file name is derived from the channel name: the text before the first `:` is
discarded, diacritics are removed, punctuation is removed, spaces are removed, and `.m3u8` is
appended. The `BANDWIDTH=873` value is a fixed literal.

## Normalisation

URLs are normalised in two places with the same three operations: URL-decode, replace `\/` with
`/`, and remove `#038;`. `FileDownloader.NormaliseUrl` does it before download; the processor
return is normalised in `PlaylistUrlRetriever.NormaliseUrl` only when the URL is not already
valid. See [flows/playlist-url-validation.md](./flows/playlist-url-validation.md).