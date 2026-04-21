[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/stream-to-m3u)](https://github.com/hmlendea/stream-to-m3u/releases/latest)
[![Build Status](https://github.com/hmlendea/stream-to-m3u/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/stream-to-m3u/actions/workflows/dotnet.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://gnu.org/licenses/gpl-3.0)

# StreamToM3U

StreamToM3U is a .NET console tool that resolves live stream URLs and generates playlists in M3U format.

It supports:
- one-off URL resolution for a single stream
- batch playlist generation from an XML input file
- multiple providers: website parsing, Streamlink, TVSportHD, and AntenaPlay

## How It Works

The app has two operating modes:

1. Single stream mode (no input file)
Returns one playable URL to stdout.

2. Batch mode (input file provided)
Reads channels from XML, resolves each stream URL, and generates playlist files.

## Requirements

- .NET SDK (`net10.0` or newer)
- Internet access
- For Streamlink provider: streamlink installed and available in PATH
- For TVSportHD and AntenaPlay providers: Chrome/Chromium available for headless Selenium sessions

## Quick Start

Build:

```bash
dotnet build
```

Run with default options:

```bash
dotnet run -- --url "https://example.com/live"
```

## Command-Line Reference

### General options

| Option | Alias | Description | Default |
|---|---|---|---|
| --input | -i | Input XML path for batch mode | empty |
| --output-file | -o | Output playlist path (when no output directory is set) | playlist.m3u |
| --output-dir, --output-directory | -O | Output directory for per-channel playlists + index | empty |
| --channel | -c | Channel identifier (used by TVSportHD/AntenaPlay) | empty |
| --title | -t | Optional stream title | empty |
| --url | -u | Source page URL | empty |
| --baseurl | -U | Base URL used to build relative playlist links | empty |

### Provider selection flags

If no provider flag is supplied, the app uses the Website provider.

| Provider | Flags |
|---|---|
| TVSportHD | --tvs, --tvsport, --tvshd, --tvsporthd |
| AntenaPlay | --antena-play, --antenaplay, --antena, --aplay, --ap |
| Streamlink | --sl, --streamlink |
| Website (default) | no flag |

## Usage Examples

### 1) Single stream: Website provider (default)

```bash
dotnet run -- --url "https://live.antena3.ro/"
```

### 2) Single stream: Streamlink provider

```bash
dotnet run -- --streamlink --url "https://youtube.com/@a7tvlive/live"
```

### 3) Single stream: TVSportHD provider

```bash
dotnet run -- --tvsporthd --channel "digi1"
```

### 4) Batch generation from XML to one playlist file

```bash
dotnet run -- --input "Data/input-ro.xml" --output-file "playlist.m3u"
```

### 5) Batch generation to directory (index + per-channel .m3u8 files)

```bash
dotnet run -- --input "Data/input-ro.xml" --output-dir "./out" --url "https://mydomain.example/iptv"
```

## Input XML Format

The XML is deserialized into channel stream entities. A typical item contains:

- Id
- ChannelName
- Provider
- Url
- Optional: ChannelId, Title, StreamBaseUrl

Example:

```xml
<?xml version="1.0" encoding="utf-8"?>
<ArrayOfChannelStreamEntity xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
	<ChannelStreamEntity>
		<Id>Antena3-Website</Id>
		<ChannelName>RO: Antena 3</ChannelName>
		<Provider>Website</Provider>
		<Url>https://live.antena3.ro/</Url>
	</ChannelStreamEntity>
</ArrayOfChannelStreamEntity>
```

See sample files in the Data directory.

## Output Behavior

- No input file:
	Prints a single resolved URL to stdout.
- Input file + no output directory:
	Writes one merged playlist file (default: playlist.m3u).
- Input file + output directory:
	Writes one index playlist plus one file per channel.

## Runtime Configuration

Runtime settings are read from appsettings.json.

Notable values:
- applicationSettings.requestTimeout
- applicationSettings.userAgent
- nuciLoggerSettings.logFilePath
- nuciLoggerSettings.minimumLevel
- nuciLoggerSettings.isFileOutputEnabled

## Running as a Linux systemd Service

Create service file: /usr/lib/systemd/system/stream-to-m3u@.service

```ini
[Unit]
Description=Stream to M3U (%i channels)

[Service]
WorkingDirectory=[ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]
ExecStart=[ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]/StreamToM3U -i [ABSOLUTE_PATH_TO_SERVICE_DIRECTORY]/Data/input-%i.xml -O /srv/http/iptv/livestreams/%i -u http://mydomain.com/iptv
MemoryAccounting=yes
MemoryMax=256M

[Install]
WantedBy=multi-user.target
```

Create timer file: /lib/systemd/system/stream-to-m3u.timer

```ini
[Unit]
Description=Periodically creates an M3U playlist out of livestreams (%i channels)

[Timer]
OnBootSec=3min
OnUnitActiveSec=40min

[Install]
WantedBy=timers.target
```

Tune these values as needed:
- OnBootSec: delay after boot
- OnUnitActiveSec: execution interval
- MemoryMax: RAM cap per service instance

## Development

Build:

```bash
dotnet build
```

Run:

```bash
dotnet run -- [arguments]
```

## Contributing

Contributions are welcome.

Please:
- keep the changes cross-platform
- keep the pull requests focused and consistent with the existing style
- update the documentation when the behaviour changes
- add or update the tests for new behaviour

## License

Licensed under GNU General Public License v3.0 or later.
See [LICENSE](./LICENSE) for details.