# Privacy and Personal Data

This document describes the privacy and personal-data handling conduct of the StreamToM3U command-line application, as implemented in this repository and verified as of 16 November 2026. StreamToM3U is a stateless, self-hosted tool that requests channel pages and playlists over the network and writes only local output files; it operates no service on behalf of the maintainers, holds no user accounts, and sends no telemetry to the project maintainers.

## 📑 Table of Contents

- [What This Document Covers](#-what-this-document-covers)
- [Self-Hosted Deployments](#-self-hosted-deployments)
- [Data We Handle](#-data-we-handle)
  - [Data Provided to the Application](#data-provided-to-the-application)
  - [Data Generated or Collected by the Application](#data-generated-or-collected-by-the-application)
  - [Data Received from Integrations](#data-received-from-integrations)
- [Processing and Use](#-processing-and-use)
- [Storage, Retention, and Deletion](#-storage-retention-and-deletion)
- [External Processing and Integrations](#-external-processing-and-integrations)
- [User Controls and Requests](#-user-controls-and-requests)
- [International Transfers](#-international-transfers)
- [Data Protection and Security](#-data-protection-and-security)
- [Document Changes](#-document-changes)
- [Contact](#-contact)

## 🔎 What This Document Covers

This document describes how StreamToM3U at https://github.com/hmlendea/stream-to-m3u manages personal data. It covers the application conduct and verified integrations described below. Where the software is self-hosted, the instance operator may have separate responsibilities described below.

## 🏠 Self-Hosted Deployments

StreamToM3U is distributed as self-contained release archives and is run by each operator on their own machine or server. This document therefore covers both the project maintainers, who publish the software, and each instance operator, who runs it. The maintainers do not operate any instance on behalf of users and have no access to operator instances or their data.

Operators control their instance's configuration, local storage, logs, backups, access controls, preservation, and request handling unless the project directly controls those functions. In this project the maintainers control none of those functions for an operator instance: the operator decides where the software runs, which channel lists it processes, which paths its logs and playlists are written to, who can read those paths, and for how long anything is kept.

The application does not send any data from a self-hosted instance to the project maintainers. There is no telemetry, no update check, no crash reporting, no licence check, and no maintainer-operated account or authentication service. The only outbound data flows are the requests the operator's own run makes to the channel pages, streaming sources, and provider websites it is configured to reach, as described under [External Processing and Integrations](#-external-processing-and-integrations).

## 📥 Data We Handle

### Data Provided to the Application

- The application requests no personal data and provides no account, profile, or sign-up interface.
- Operators do not supply personal data through the documented inputs, which are command-line arguments and the XML channel list fields `Id`, `ChannelName`, `Provider`, `ChannelId`, `Title`, `Url`, and `StreamBaseUrl`. These values describe channels, not persons, and the repository's own sample files contain only channel definitions.

### Data Generated or Collected by the Application

- The application does not collect analytics, telemetry, crash reports, audit records, device identifiers, or usage statistics. No data is transmitted to the project maintainers.
- The application writes an operational log to `logfile.log`, or to the path configured through `nuciLoggerSettings.logFilePath`. These lines record operations and, for downloads, the normalised request URL. Resolved playback URLs may embed session or access tokens issued by the source provider.
- The application writes the generated M3U and M3U8 playlist files to the output file or output directory supplied on the command line. These files contain channel names and stream URLs.

### Data Received from Integrations

- The application receives channel pages, playlist documents, and stream URLs from the source websites and streaming sources that the operator configures. This content is published by those third parties and is not provided by any user of this project.
- When the AntenaPlay provider is selected, the application registers a disposable account on antenaplay.ro using a locally generated e-mail local part, password, first name, and last name, each a randomly generated ten-character lowercase string, with the e-mail always using the `gmail.com` domain. These generated values are personal data only in form; they are not derived from, and are not linked to, any real person, and the application does not ask the operator for any such details.
- No personal data is received from any other integration or third party.

## 🧭 Processing and Use

The application processes the data described above for these verified functions:
- Resolving a single stream URL and printing it to standard output — the source page URL, the source page content, the resolved stream URL, and the optional channel, title, and base URL arguments.
- Resolving every channel in an XML channel list and generating playlists — the channel definitions from the input file, the resolved stream URLs, and the download URL of each resolved playlist.
- Downloading channel pages and playlist documents — the request URL sent to the source website or streaming source, and the response content.
- Extracting a playlist URL from page HTML — the downloaded page content and the optional configured base URL.
- Delegating stream selection to Streamlink — the source page URL passed on the command line to the `streamlink` executable, and its standard output.
- Driving a browser session for the TVSportHD and AntenaPlay providers — the channel URL, the page content, the DOM elements and iframe contents read during extraction, and the resolved stream URL.
- Creating a disposable AntenaPlay account — the randomly generated e-mail address, password, first name, and last name, submitted to the registration form, followed by a logout request at the end of the attempt.
- Writing operational diagnostics — the operation name, outcome, channel stream identifier, and normalised download URL of each logged event.
- Writing playlist output — the resolved channel names and stream URLs.

## 🗄️ Storage, Retention, and Deletion

The application holds all runtime data in memory and releases it when the process exits. It maintains no database, no cache, no queue, and no user profile. The only persistence surfaces are the files described below, all of which are owned and located by the instance operator.

| Data | Location | Retention conduct |
|------|----------|-------------------|
| Operational log | `logfile.log` in the working directory, or the path set in `nuciLoggerSettings.logFilePath` | Appended or rewritten by the logging library according to its own behaviour; the application implements no rotation, archival, or automatic deletion |
| Generated merged playlist | The path supplied through `--output-file`, defaulting to `playlist.m3u` | Overwritten on every run that uses merged mode |
| Generated index playlist and per-channel playlists | The directory supplied through `--output-dir`, written as `playlist.m3u` plus one `.m3u8` file per channel | Overwritten on every run that uses directory mode; the directory is created when absent |
| Configuration and channel lists | `appsettings.json` and the XML input file in the operator's working directory | Operator-managed files; the application never writes them |

Because the software is self-hosted, the instance operator controls storage, deletion, and backups for every item above. The maintainers hold no copy of any operator's log file, playlists, configuration, or channel lists. Deleting a run's data is performed by deleting the files the operator configured the run to produce.

Where the AntenaPlay provider registers an account, the resulting account and any session associated with it are held by antenaplay.ro under that service's own terms, not by this project. The application logs out at the end of the attempt but does not delete the account.

## 🔗 External Processing and Integrations

Each outbound flow below is initiated by the operator's own run, directed to a destination the operator's channel list specifies, and governed by the destination's own privacy practices. The maintainers operate none of these destinations and receive none of this data.

| Service or integration | Purpose | Data involved | Configuration or documentation |
|-----------------------|---------|---------------|--------------------------------|
| Configured channel websites and streaming sources | Fetch channel pages, playlist documents, and stream URLs | Request URL, request headers including the configured user agent, and response content | `Url` and `StreamBaseUrl` fields in the XML channel list; `applicationSettings.userAgent` and `applicationSettings.requestTimeout` in `appsettings.json` |
| Streamlink executable | Resolve the best stream for a page URL through a local process | Source page URL passed as a command-line argument; resolved URL returned on standard output | `Streamlink` provider entry in the XML channel list; requires Streamlink on `PATH` |
| TVSportHD website | Read the playlist URL from a channel page | Channel URL and page content, fetched through a locally driven browser | `TvSportHd` provider entry in the XML channel list; see [README.md](./README.md) |
| AntenaPlay website | Register a disposable account and read the stream URL from a channel page | Randomly generated e-mail address, password, first name, and last name; channel URL; page content | `AntenaPlay` provider entry in the XML channel list; see [README.md](./README.md) |
| Chrome or Chromium through WebDriver | Render the pages required by the browser-driven providers | Page and DOM content requested from the configured sites | Local browser installation required for the TVSportHD and AntenaPlay providers |

The application has no built-in transfer of data to the project maintainers, and no analytics, advertising, or tracking integration is present.

## ⚙️ User Controls and Requests

The following documented controls or request procedures are available:
- Choosing the log destination — set `nuciLoggerSettings.logFilePath` in `appsettings.json` to place the log file where the operator wants it.
- Reducing or disabling diagnostics — set `nuciLoggerSettings.minimumLevel` to raise the log threshold, or set `nuciLoggerSettings.isFileOutputEnabled` to `false` to stop writing the log file.
- Avoiding the browser-driven providers — omit or change the `Provider` value in the channel list so that no browser session and no AntenaPlay registration is created.
- Deleting produced data — remove the log file, the output file, and the output directory that the run was configured to write.
- Reviewing generated output before publishing — inspect the generated playlists and log file before serving them, because resolved stream URLs may embed provider-issued tokens.

## 🌍 International Transfers

The application itself transfers no data to the maintainers, and the deployment location is controlled entirely by the instance operator. Data leaves the operator's machine or server only as requests to the destinations named in the operator's channel list, so the countries and regions involved are determined by where those third-party services and their content delivery networks operate. Operators who need to constrain those destinations should select only providers hosted in acceptable regions and review the privacy documentation of each destination they configure.

## 🛡️ Data Protection and Security

The application applies the following safeguards: it stores no credentials, reading configuration only from `appsettings.json` and the command line and never from a secret store; it requests no personal data from the operator; it holds runtime data in memory only; and it does not send anything to the maintainers. Downloads use the configured user agent and a configurable request timeout, and the application never executes downloaded page or playlist content.

Because the software is self-hosted, the operator is responsible for updates, secrets, access controls, backups, network exposure, and log protection. The log file and the generated playlists are the surfaces that warrant protection, because resolved stream URLs may contain provider-issued tokens and the log may disclose which channels an instance consumes. Operators should restrict filesystem permissions on the working directory, avoid serving the log file publicly, and review generated output before publishing it. No software can guarantee absolute security, and this document makes no such promise.

The original maintainers are not responsible for changes, modifications, or privacy issues introduced by downstream forks.

## 🔄 Document Changes

Update this document when application data processes, storage, integrations, or deployment responsibilities change. The current version is published at https://github.com/hmlendea/stream-to-m3u/blob/master/PRIVACY.md.

## 📬 Contact

For questions about application data handling, contact the project maintainers through https://github.com/hmlendea/stream-to-m3u/issues. For a self-hosted instance, contact the instance operator, unless the project explicitly manages the request. Include the application version, the provider in use, and a description of the data process concerned; do not send passwords, access tokens, or other secrets.