# Build and deployment

## Local build

| Command | Purpose |
|---------|---------|
| `dotnet restore` | Restore NuGet packages |
| `dotnet build` | Build the solution |
| `dotnet test` | Build and run all unit tests |
| `dotnet run --project StreamToM3U -- [args]` | Run without publishing |

The solution file `StreamToM3U.slnx` lists both projects. The application targets `net10.0`.

## Outputs

A build places the executable `StreamToM3U` (or `StreamToM3U.exe`), `appsettings.json`, and the
`Data/*.xml` files into `bin/<Configuration>/net10.0/`. The XML and settings files are copied
because `StreamToM3U.csproj` marks them `CopyToOutputDirectory=PreserveNewest`.

## Continuous integration

`.github/workflows/dotnet.yml`:

| Property | Value |
|----------|-------|
| Trigger | Push and pull request to `master` |
| Runner | `ubuntu-latest` |
| .NET | `10.0.x` |
| Steps | Checkout, setup .NET, `dotnet restore`, `dotnet build --no-restore`, `dotnet test --no-build --verbosity normal` |

The workflow does not require Chrome, Chromium, or Streamlink, because no test exercises those
paths.

## Security scanning

`.github/workflows/security.yml`:

| Property | Value |
|----------|-------|
| Trigger | Push and pull request to `master`, plus a weekly `schedule` (`0 0 * * 0`) |
| Runner | `ubuntu-latest` |
| .NET | `10.0.x` |
| Permissions | `security-events: write`, `contents: read` |
| Steps | Checkout, setup .NET, `dotnet restore`, `dotnet list package --vulnerable --include-transitive`, CodeQL `init` for `csharp`, `dotnet build --no-restore`, CodeQL `analyze` |

Findings are uploaded as SARIF to GitHub Code Scanning, which requires a public repository.

## Release

`.github/workflows/github-release.yml` runs when a GitHub Release is published:

1. Check out with full history.
2. Derive the .NET version from `<TargetFramework>` in `StreamToM3U/StreamToM3U.csproj`
   (`net10.0` → `10.0`); fail if it cannot be determined.
3. Set up that .NET version.
4. Strip a leading `v` from the release tag to obtain the release version.
5. Normalise the origin remote URL to HTTPS.
6. Download and pipe `https://raw.githubusercontent.com/hmlendea/dotnet-release-scripts/refs/heads/master/dotnet/<version>.sh`
   into `bash`.
7. Attach `StreamToM3U/bin/Release/stream-to-m3u_*` to the release.

Produced artefacts are self-contained archives per runtime identifier: `linux-arm`,
`linux-arm64`, `linux-x64`, `osx-arm64`, `osx-x64`, `win-arm64`, `win-x64`.

> The release step pipes a remotely fetched script into `bash`. Review that script before running
> a release locally, as noted in the [README](../README.md).

Changing `<TargetFramework>` changes the toolchain used by the release workflow, so a framework
upgrade must be coordinated with the `dotnet-release-scripts` repository supporting that version.

## Deployment topology

A single short-lived process per invocation. There is no service, no container image, no
listening socket, and no database.

| Concern | Design |
|---------|--------|
| Cadence | External scheduler; the documented pattern is a systemd timer |
| Working directory | Must contain `appsettings.json` and be writable for the log file |
| Output | A merged file at `--output-file`, or a directory at `--output-dir` |
| Prerequisites | Chrome/Chromium for `TvSportHd`/`AntenaPlay`; Streamlink on `PATH` for `Streamlink` |
| Resource limit | The README's systemd template suggests `MemoryMax=256M` per instance |
| Shutdown | `WebDriverHandler.CloseDriver()` in `finally`, so normal runs do not leak a browser |

The README documents a `stream-to-m3u@.service` template plus a `stream-to-m3u.timer`
(`OnBootSec=3min`, `OnUnitActiveSec=40min`) that regenerate a playlist per channel-set into a
served directory.

## Environments

| Environment | Purpose | Deployment method |
|-------------|---------|-------------------|
| Local host | One-off generation or manual execution | Published release archive or `dotnet run` |
| systemd host | Scheduled regeneration served over HTTP | `stream-to-m3u@.service` and `stream-to-m3u.timer` templates |

There is no staging or production environment operated by the maintainers.

## Observability in deployment

Runtime diagnostics go to the console and to the file configured by
`nuciLoggerSettings.logFilePath`, filtered by `minimumLevel`. There are no metrics, traces,
health endpoints, or dashboards. See [logging.md](./logging.md).

## Backup and rollback

The application persists no state beyond the log file and the playlist output it produces on each
run. A failed run can overwrite a previously valid playlist with a partial or empty one, so
operators who need a valid playlist at all times should retain prior output externally (for
example with a versioned copy step) and restore it manually on failure. There is no built-in
rollback.

## Related documentation

- [testing.md](./testing.md)
- [state-and-persistence.md](./state-and-persistence.md)
- [dependencies.md](./dependencies.md)
