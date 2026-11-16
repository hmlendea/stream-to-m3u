# Frequently asked questions

## Why does the process always exit with code 0?

Because `Program.Main` catches every exception and never returns a failure code. Failures are
recorded in the log and manifest as missing playlist entries. See
[error-handling.md](./error-handling.md).

## Why does my playlist only contain `#EXTM3U`?

Either no channel resolved, or every resolved URL was dropped. In merged mode a URL is also
dropped when its content cannot be re-downloaded. Lower `minimumLevel` to `Debug` to see the
per-channel records, then see [troubleshooting.md](./troubleshooting.md).

## Why are there no per-channel log lines at the default settings?

Per-channel outcomes are logged at `Debug` while the shipped `minimumLevel` is `Info`. See
[logging.md](./logging.md).

## How do I run both output modes in one run?

You cannot. `--output-dir` takes precedence when non-whitespace; otherwise `--output-file` is
used. Run the tool twice, or consume the directory form and build your own merged playlist.

## Why is one channel represented by several entries?

Grouping is by `ChannelName`, so several XML entries sharing that name (for example standard and
HD sources) all appear under the same channel. `Id` is only used for logging.

## Why does a per-channel file start with `#EXT-X-STREAM-INF:BANDWIDTH=873`?

That is a fixed literal used to produce well-formed playlist entries; the value carries no
measured meaning. See [data-model.md](./data-model.md).

## Why does merged mode make extra HTTP requests?

It probes each resolved URL to confirm reachability and discards the body. Directory mode does
not. See [behaviour/generate-merged-playlist.md](./behaviour/generate-merged-playlist.md).

## How do I select a provider for a single channel?

Use a provider flag (`--streamlink`, `--tvshd`, `--antena`) or set `Provider` in the XML file for
batch mode. Flags are mutually exclusive with a fixed priority. See
[configuration.md](./configuration.md).

## Can I run several channels concurrently through the browser?

No. All browser-driven providers share one `ChromeDriver` and serialize through a lock. See
[concurrency-and-scheduling.md](./concurrency-and-scheduling.md).

## Does the tool store or send my credentials?

It stores no credentials and reads none from configuration. The AntenaPlay provider generates a
disposable account per run locally. See [security.md](./security.md) and
[../PRIVACY.md](../PRIVACY.md).

## Is the log file safe to share?

Not necessarily. It records normalised download URLs, which may embed provider-issued session
tokens, and it reveals which channels an instance consumes. See [security.md](./security.md).

## Why does editing `appsettings.json` mid-run change nothing?

`reloadOnChange` is enabled, but the bound settings are plain singletons with no monitor. Restart
the process after editing the file.

## Why do some channel files disappear or overwrite each other?

Channel names are normalised into file names, so two names that normalise identically share one
file and the later write wins. See
[behaviour/generate-playlist-directory.md](./behaviour/generate-playlist-directory.md).

## Where is the roadmap?

The repository has no `ROADMAP.md`. Planned work is tracked through GitHub issues and pull
requests. See [ambiguities-and-open-questions.md](./ambiguities-and-open-questions.md).