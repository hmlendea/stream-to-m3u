# Security Policy

This policy states how security issues in StreamToM3U are reported, validated, and disclosed, and which distribution channels receive security maintenance. It covers the application source and its release automation, and it expects vulnerabilities to be reported privately so that maintainers can validate and remediate them before any public disclosure. The latest version distributed through GitHub Releases is the supported configuration; all other channels and preceding versions are unsupported.

## 📑 Table of Contents

- [Supported Versions](#-supported-versions)
- [Reporting a Vulnerability](#-reporting-a-vulnerability)
- [Scope](#-scope)
- [Disclosure Policy](#-disclosure-policy)
- [Automated Security Scanning](#-automated-security-scanning)

## 🛡️ Supported Versions

Use this table to indicate which project versions currently receive security maintenance.

| Version | Distribution Channel | Supported |
|---------|--------------------|-----------|
| Latest version | GitHub Releases | ✅ |
| Latest version | Unofficial third-party distribution channels | ❌ |
| Preceding versions | Any distribution channel | ❌ |

## 🚨 Reporting a Vulnerability

Please do not disclose suspected vulnerabilities publicly before maintainers have had an opportunity to validate and remediate them.

To report a vulnerability:
- [GitHub Security Advisories](https://github.com/hmlendea/stream-to-m3u/security/advisories)
- Contact the maintainers directly

## 📌 Scope

The original maintainers are not responsible for changes, modifications, or security issues introduced by downstream forks.

The subsequent report categories are in scope for this repository:
- Vulnerability in the application source under `StreamToM3U/`, including URL resolution, playlist generation, and browser automation code
- Unsafe handling of downloaded content, including file paths or playlist markers derived from untrusted input
- Exposure of local data by the application, including log files that may contain provider-issued playback tokens
- Vulnerability in the release automation under `.github/workflows/`
- Vulnerable or malicious dependency declared in `StreamToM3U/StreamToM3U.csproj`, `StreamToM3U.ArchitectureTests/StreamToM3U.ArchitectureTests.csproj`, or `StreamToM3U.UnitTests/StreamToM3U.UnitTests.csproj`

The subsequent categories are out of scope unless explicitly stated to the contrary:
- Channel pages, playlist documents, streaming sources, and provider websites operated by third parties, including TVSportHD and AntenaPlay
- The Streamlink executable and the streams it selects
- Chrome, Chromium, ChromeDriver, and Selenium, except where this project configures them unsafely
- Accounts, subscriptions, or access controls on third-party provider websites
- Builds obtained from unofficial third-party distribution channels or modified forks
- Data written to a self-hosted instance's own filesystem, which the instance operator controls

## 📢 Disclosure Policy

This project follows coordinated disclosure:
1. Vulnerabilities are investigated privately.
2. A remediation plan is prepared and validated.
3. Public disclosure is published after a fix, mitigation, or agreed risk decision is available.
4. Credit is attributed in accordance with reporter preference and project policy.

## 🔍 Automated Security Scanning

This repository runs automated security scans on every push and pull request to `master`, and weekly on a schedule. The scan includes:
- **Vulnerable dependencies** Direct and transitive NuGet packages are checked against known vulnerability advisories.
- **Static code analysis** The C# sources are analysed with CodeQL for security-relevant coding patterns.

Results are published to the [Security](https://github.com/hmlendea/stream-to-m3u/security) tab and the [Actions](https://github.com/hmlendea/stream-to-m3u/actions/workflows/security.yml) workflow page.