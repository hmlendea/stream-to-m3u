[![Donate](https://img.shields.io/badge/-%E2%99%A5%20Donate-%23ff69b4)](https://hmlendea.go.ro/fund.html)
[![Latest Release](https://img.shields.io/github/v/release/hmlendea/stream-to-m3u)](https://github.com/hmlendea/stream-to-m3u/releases/latest)
[![Build Status](https://github.com/hmlendea/stream-to-m3u/actions/workflows/dotnet.yml/badge.svg)](https://github.com/hmlendea/stream-to-m3u/actions/workflows/dotnet.yml)
[![License](https://img.shields.io/github/license/hmlendea/stream-to-m3u)](https://github.com/hmlendea/stream-to-m3u/blob/master/LICENSE)

# StreamToM3U

StreamToM3U is a .NET console tool that resolves live stream URLs and generates playlists in M3U format.

## 📑 Table of Contents

- [Capabilities](#-capabilities)
- [Use Cases](#-use-cases)
- [Usage](#-usage)
- [Known Limitations](#-known-limitations)
- [System Requirements](#-system-requirements)
- [Installation](#-installation)
  - [Manual Installation](#manual-installation)
- [Configuration](#-configuration)
  - [Configuration Files](#configuration-files)
  - [Settings](#settings)
  - [Command-Line Options](#command-line-options)
- [Integrations](#-integrations)
- [Privacy and Data](#-privacy-and-data)
  - [Data Locations](#data-locations)
- [Development](#-development)
  - [Requirements](#requirements)
  - [Setup](#setup)
  - [Build](#build)
  - [Run](#run)
  - [Test](#test)
  - [Release](#release)
  - [Dependencies](#dependencies)
- [GitHub Actions](#-github-actions)
- [Project Structure](#-project-structure)
  - [Projects and Packages](#projects-and-packages)
  - [Directories](#directories)
- [Deployment](#-deployment)
  - [Environments](#environments)
  - [Observability](#observability)
- [Architecture](#-architecture)
- [Data Formats and Protocols](#-data-formats-and-protocols)
- [Contributing](#-contributing)
- [Support](#-support)
- [Project Engagement](#-project-engagement)
- [License](#-license)

## ✨ Capabilities

- Resolves the playable URL of a single live stream and prints it to standard output
- Generates a merged M3U playlist, or a directory with an index playlist and one playlist per channel, from an XML channel list
- Resolves streams through website HTML parsing, Streamlink, TVSportHD, or AntenaPlay
- Rewrites relative playlist entries against a configured base URL

## 🎯 Use Cases

- **Single stream resolution:** Obtain a direct, playable URL for one stream on demand
- **IPTV playlist publishing:** Regenerate regional playlists on a schedule and serve them from a web directory
- **Provider fallback:** Reach streams that are exposed only through HTML, Streamlink, TVSportHD, or AntenaPlay

## 🚀 Usage

Resolve a single stream and print its URL:

```bash
dotnet run --project StreamToM3U -- --url "https://live.antena3.ro/"
```

Generate one merged playlist from an XML channel list:

```bash
dotnet run --project StreamToM3U -- --input "Data/input-ro.xml" --output-file "playlist.m3u"
```

Generate an index playlist and one playlist per channel in a directory:

```bash
dotnet run --project StreamToM3U -- --input "Data/input-ro.xml" --output-dir "./out" --url "https://mydomain.example/iptv"
```

Select a provider with its flag, for example Streamlink:

```bash
dotnet run --project StreamToM3U -- --streamlink --url "https://youtube.com/@a7tvlive/live"
```

## ⚠️ Known Limitations

- Providers other than `Website` and `Streamlink` drive a headless Chrome session through Selenium, so they require a local Chrome or Chromium installation
- Channel names and providers are defined through XML input files; no graphical interface is provided
- Failed channels are skipped rather than retried, and are recorded in the log file

## 🖥️ System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| OS | Linux, macOS, or Windows | Linux |
| .NET | .NET 8 SDK | [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Network | Outbound HTTPS access | Outbound HTTPS access |
| Browser | Chrome or Chromium, for the TVSportHD and AntenaPlay providers | Latest stable Chrome or Chromium |
| Streamlink | Streamlink, for the Streamlink provider | Latest stable Streamlink |

## 📦 Installation

[![Obtain it from GitHub](https://raw.githubusercontent.com/hmlendea/readme-assets/master/badges/stores/github.png)](https://github.com/hmlendea/stream-to-m3u/releases)

### Manual Installation

Download the archive matching your platform from [GitHub Releases](https://github.com/hmlendea/stream-to-m3u/releases), extract it, and run the `StreamToM3U` executable. Release archives are published for Linux, macOS, and Windows on x64, arm, and arm64.

## ⚙️ Configuration

Runtime settings are read from `appsettings.json`, which is loaded from the working directory. Channels and providers for batch generation are supplied through an XML input file.

### Configuration Files

| File | Scope | Purpose |
|------|-------|---------|
| `appsettings.json` | Application | HTTP request settings and logging settings |
| `Data/input-*.xml` | Batch input | Channel name, provider, and source URL for each channel |

### Settings

The subsequent settings are recognised:
| Section | Key | Type | Default | Required | Description |
|---------|-----|------|---------|----------|-------------|
| `applicationSettings` | `requestTimeout` | `int` | `8000` | No | HTTP request timeout, in milliseconds |
| `applicationSettings` | `userAgent` | `string` | Chrome user agent | No | `User-Agent` header sent with HTTP requests |
| `nuciLoggerSettings` | `logFilePath` | `string` | `logfile.log` | No | Path of the log file |
| `nuciLoggerSettings` | `minimumLevel` | `string` | `Info` | No | Minimum level written to the log |
| `nuciLoggerSettings` | `isFileOutputEnabled` | `bool` | `true` | No | Whether messages are written to the log file |

### Command-Line Options

| Option | Value | Default | Description |
|--------|-------|---------|-------------|
| `-i`, `--input` | `<path>` | — | Input XML file used for batch playlist generation |
| `-o`, `--output-file` | `<path>` | `playlist.m3u` | Output playlist path used when no output directory is set |
| `-O`, `--output-dir`, `--output-directory` | `<path>` | — | Output directory that receives an index playlist and one playlist per channel |
| `-c`, `--channel` | `<identifier>` | — | Channel identifier used by the TVSportHD and AntenaPlay providers |
| `-t`, `--title` | `<string>` | — | Optional stream title |
| `-u`, `--url` | `<url>` | — | Source page URL |
| `-U`, `--baseurl` | `<url>` | — | Base URL used to build relative playlist entries |
| `--tvs`, `--tvsport`, `--tvshd`, `--tvsporthd` | — | — | Selects the TVSportHD provider |
| `--antena-play`, `--antenaplay`, `--antena`, `--aplay`, `--ap` | — | — | Selects the AntenaPlay provider |
| `--sl`, `--streamlink` | — | — | Selects the Streamlink provider |

## 🔌 Integrations

| Integration | Compatibility | Purpose | Required |
|-------------|---------------|---------|----------|
| Website | Any source page that exposes an `.m3u` or `.m3u8` URL | Default provider; extracts the playlist URL from page HTML | No |
| Streamlink | Streamlink installed and available on `PATH` | Resolves the best stream for a page URL | No |
| TVSportHD | Chrome or Chromium | Reads the playlist URL from a channel page served by the TVSportHD website | No |
| AntenaPlay | Chrome or Chromium | Registers a disposable account and reads the stream URL from an AntenaPlay channel page | No |

## 🛡️ Privacy and Data

StreamToM3U is a stateless command-line tool: it does not collect telemetry and it does not transmit project data to the maintainer. Stream URLs and source pages are requested directly by the tool, and diagnostics are written only to the local log file.

### Data Locations

| Platform or Scope | Location | Contents |
|-------------------|----------|----------|
| Working directory | `logfile.log`, or the path set in `nuciLoggerSettings.logFilePath` | Operational log lines, including resolved URLs |

## 🛠️ Development

### Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Chrome or Chromium, for the TVSportHD and AntenaPlay providers
- Streamlink, for the Streamlink provider

### Setup

```bash
git clone https://github.com/hmlendea/stream-to-m3u.git
cd stream-to-m3u
dotnet restore
```

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run --project StreamToM3U -- [arguments]
```

### Test

```bash
dotnet test
```

### Release

Releases are published by the `github-release.yml` workflow when a GitHub Release is published. Contributors do not need to run the release process locally.

The workflow:
1. Derives the .NET version from `StreamToM3U/StreamToM3U.csproj` and the release version from the published tag
2. Downloads and executes the external .NET release helper from `https://raw.githubusercontent.com/hmlendea/dotnet-release-scripts/refs/heads/master/dotnet/10.0.sh`
3. Produces self-contained archives for `linux-arm`, `linux-arm64`, `linux-x64`, `osx-arm64`, `osx-x64`, `win-arm64`, and `win-x64`
4. Attaches every produced archive to the GitHub Release

**Note:** Piping into `bash` is an intensely controversial topic. Please review any external scripts before running them in your environment!

To publish a release, create and publish a GitHub Release with a semantic-version tag such as `v5.0.6`.

### Dependencies

| Package | Version | Scope | Purpose |
|---------|---------|-------|---------|
| `NuciCLI` | 3.0.1 | Runtime | Shared command-line helpers |
| `NuciDAL` | 3.0.1 | Runtime | File-backed repository abstractions used for XML input |
| `NuciExtensions` | 5.3.1 | Runtime | Shared extension methods |
| `NuciLog` | 1.2.0 | Runtime | Application logging |
| `NuciLog.Core` | 2.6.2 | Runtime | Logging contracts |
| `NuciWeb` | 4.0.0 | Runtime | Web content processing |
| `NuciWeb.Automation` | 1.0.0 | Runtime | Browser automation abstractions |
| `NuciWeb.Automation.Selenium` | 1.0.2 | Runtime | Selenium implementation of the browser automation abstractions |

## ⚙️ GitHub Actions

| Workflow | Purpose | What it does |
|----------|---------|--------------|
| [ dotnet.yml ](https://github.com/hmlendea/stream-to-m3u/blob/master/.github/workflows/dotnet.yml) | Continuous integration | Restores dependencies, builds the solution, and runs the unit tests on every push and pull request to `master` |
| [ github-release.yml ](https://github.com/hmlendea/stream-to-m3u/blob/master/.github/workflows/github-release.yml) | Release asset distribution | Builds the self-contained archives for every supported runtime identifier and attaches them to the GitHub Release when a release is published |

## 🗂️ Project Structure

### Projects and Packages

| Project | Type | Purpose |
|---------|------|---------|
| `StreamToM3U` | Console Application | Main CLI application |
| `StreamToM3U.UnitTests` | Unit Test Project | Unit tests for the application |

### Directories

| Directory | Purpose |
|-----------|---------|
| `.github` | Repository automation configuration and GitHub Actions workflows |
| `StreamToM3U/Configuration` | Startup options, application settings, and provider enumeration |
| `StreamToM3U/Data` | Sample XML channel lists |
| `StreamToM3U/DataAccess` | Data objects used for XML input deserialisation |
| `StreamToM3U/Logging` | Operation and log-key definitions |
| `StreamToM3U/Service` | Playlist URL resolution, playlist generation, and file download services |
| `StreamToM3U/Service/Processors` | Provider-specific stream URL processors |
| `StreamToM3U/Utils` | Shared utilities, including the Selenium web-driver handler |
| `StreamToM3U.UnitTests` | Unit tests and test doubles |

## 🏗️ Architecture

StreamToM3U is a stateless, single-process console pipeline that resolves live stream URLs through provider-specific processors and generates M3U playlists from XML channel definitions.

See the [Architecture Documentation](./ARCHITECTURE.md) for the system context, principal components, runtime flows, ownership boundaries, dependencies, constraints, and extension points.

## 🚢 Deployment

### Environments

| Environment | Purpose | Deployment Method |
|-------------|---------|-------------------|
| Local host | One-off playlist generation or manual execution | Published release archive or `dotnet run` |
| systemd host | Scheduled playlist regeneration served from a web directory | `stream-to-m3u@.service` and `stream-to-m3u.timer` templates |

The following systemd service regenerates a playlist per channel-set and writes it to a served directory:

Create `/usr/lib/systemd/system/stream-to-m3u@.service`:

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

Create `/lib/systemd/system/stream-to-m3u.timer`:

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

### Observability

Runtime diagnostics are written to the log file configured through `nuciLoggerSettings.logFilePath`. Log output is controlled by `nuciLoggerSettings.minimumLevel` and `nuciLoggerSettings.isFileOutputEnabled`.

## 🧾 Data Formats and Protocols

| Format or Protocol | Specification | Purpose |
|--------------------|---------------|---------|
| XML channel list | `ArrayOfChannelStreamEntity` document, matching `StreamToM3U/Data/input-*.xml` | Declares the channels processed in batch mode, with `Id`, `ChannelName`, `Provider`, `Url`, and optional `ChannelId`, `Title`, and `StreamBaseUrl` fields |
| M3U / M3U8 playlist | `#EXTM3U` text playlist | Generated output; contains an `#EXTINF` entry per resolved channel stream |

## 🤝 Contributing

You are welcome to submit any suggestion, feedback, or modification to this project.

When doing so, please:
- Maintain cross-platform compatibility
- Submit focused pull requests that conform to the existing code style
- Maintain your branch synchronised with `master`
- Revise the documentation when functionality changes
- Properly test all modifications, including edge cases and error conditions
- Include tests for additional or modified functionality
- Raise a new [issue](https://github.com/hmlendea/stream-to-m3u/issues) for problems or suggestions

See the [Contributing Guidelines](./CONTRIBUTING.md) for details on how to report issues, suggest enhancements, and submit changes.

## 💬 Support

For support, please [open an issue](https://github.com/hmlendea/stream-to-m3u/issues).

## 💝 Project Engagement

Discovered a problem with StreamToM3U or have a suggestion for it? [Open an issue](https://github.com/hmlendea/stream-to-m3u/issues)!

If you find this project useful, consider [funding it](https://hmlendea.go.ro/fund.html) or starring ⭐️ it on GitHub!

[![Donate](https://raw.githubusercontent.com/hmlendea/readme-assets/master/donate_generic.png)](https://hmlendea.go.ro/fund.html)

## 📄 License

This project is being distributed under the `GNU General Public License v3.0` or later.
See [LICENSE](./LICENSE) for additional information.