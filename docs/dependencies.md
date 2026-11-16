# Dependencies

## Application packages

Declared in `StreamToM3U/StreamToM3U.csproj`.

| Package | Version | Kind | Purpose in this repository |
|---------|---------|------|----------------------------|
| `Microsoft.Extensions.Configuration` | 10.0.6 | Framework | Configuration access |
| `Microsoft.Extensions.Configuration.Binder` | 10.0.6 | Framework | `config.Bind(sectionName, object)` |
| `Microsoft.Extensions.Configuration.FileExtensions` | 10.0.6 | Framework | `AddJsonFile` file provider |
| `Microsoft.Extensions.Configuration.Json` | 10.0.6 | Framework | JSON configuration source |
| `Microsoft.Extensions.DependencyInjection` | 10.0.6 | Framework | `ServiceCollection` composition root |
| `NuciCLI` | 3.0.1 | Internal | Shared command-line helpers |
| `NuciDAL` | 3.0.1 | Internal | `IRepository<T>`, `XmlRepository<T>`, `EntityBase` |
| `NuciExtensions` | 5.3.1 | Internal | `RemoveDiacritics`, `RemovePunctuation`, `ToSentenceCase`, `GetRandomElement` |
| `NuciLog` | 1.2.0 | Internal | `NuciLogger`, `NuciLoggerSettings` |
| `NuciLog.Core` | 2.6.2 | Internal | `ILogger`, `Operation`, `OperationStatus`, `LogInfo`, `LogInfoKey` |
| `NuciWeb` | 4.0.0 | Internal | `IWebProcessor`, `Select` |
| `NuciWeb.Automation` | 1.0.0 | Internal | Browser automation abstractions |
| `NuciWeb.Automation.Selenium` | 1.0.2 | Internal | `SeleniumWebProcessor` implementation |

`Selenium.WebDriver` and `OpenQA.Selenium` types (`ChromeDriver`, `ChromeOptions`,
`ChromeDriverService`, `IWebElement`, `By`, `PageLoadStrategy`) are consumed directly and arrive
transitively through `NuciWeb.Automation.Selenium`.

## Test packages

Declared in `StreamToM3U.UnitTests/StreamToM3U.UnitTests.csproj`.

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.NET.Test.Sdk` | 18.12.0 | Test host |
| `Moq` | 4.21.0 | Doubles for `IFileDownloader`, `IPlaylistUrlRetriever`, `IRepository<T>`, `ILogger` |
| `NUnit` | 4.6.1 | Test framework |
| `NUnit.Analyzers` | 4.15.0 | Test-quality analyzers |
| `NUnit3TestAdapter` | 6.3.1 | Adapter for the test host |

The test project references the application project, and the application project never
references the test project.

## External process and service dependencies

| Dependency | Resolved by | Required for | Failure mode |
|------------|-------------|--------------|--------------|
| Chrome or Chromium | Selenium `ChromeDriver` | `TvSportHd`, `AntenaPlay` | Driver creation throws; resolution fails for the channel |
| ChromeDriver | `ChromeDriverService.CreateDefaultService()` (Selenium Manager) | `TvSportHd`, `AntenaPlay` | Same as above |
| `streamlink` executable | `Process.Start` with `FileName = "streamlink"` | `Streamlink` | `Win32Exception` surfaces as `AggregateException` in batch mode |
| Channel websites and CDNs | `HttpClient`, browser | `Website`, `TvSportHd`, `AntenaPlay` and playlist validation | `FileDownloader` returns `null`; resolution fails for the channel |
| TVSportHD site (`http://www.tv-sport-hd.com`) | Browser | `TvSportHd` | DOM mismatch → failed resolution |
| AntenaPlay site (`https://antenaplay.ro`) | Browser | `AntenaPlay` | Registration or stream pattern mismatch → failed resolution |

## Internal dependency graph

```mermaid
flowchart LR
    Program --> Configuration
    Program --> Service
    Program --> DataAccess
    Program --> Logging
    Program --> Utils
    Service --> Configuration
    Service --> DataAccess
    Service --> Logging
    Service --> ServiceProcessors["Service/Processors"]
    ServiceProcessors --> Utils
    ServiceProcessors --> Models["Service/Models"]
    DataAccess --> NuciDAL
    Tests --> Program0["application project"]
```

## Version pinning notes

- Every Nuci package is exactly pinned, so a dependency update is a deliberate change and should
  refresh [documentation-maintenance.md](./documentation-maintenance.md) considerations and the
  README dependency table.
- The release workflow derives the .NET version from `<TargetFramework>` in
  `StreamToM3U/StreamToM3U.csproj`, so changing that value changes the release toolchain too. See
  [build-and-deployment.md](./build-and-deployment.md).
