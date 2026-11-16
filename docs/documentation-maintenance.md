# Documentation maintenance

## Ownership

The `docs/` tree is maintained by the repository maintainer alongside the code. Root documents
(`README.md`, `ARCHITECTURE.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`,
`PRIVACY.md`, `LICENSE`) are governed by their own fidelity rules and are not duplicated under
`docs/`.

## When to update what

| Change | Documents to revise |
|--------|---------------------|
| New or changed provider | `docs/components/processors.md`, `docs/integrations.md`, `docs/architecture.md`, `docs/design-decisions.md`, README integrations/options, `ARCHITECTURE.md` |
| Output format, markers, or naming | `docs/data-model.md`, `docs/invariants.md`, `docs/behaviour/*`, README data formats |
| CLI aliases or defaults | `docs/configuration.md`, `docs/api-reference/command-line-interface.md`, `docs/invariants.md`, README options |
| Configuration keys | `docs/configuration.md`, `docs/logging.md` when logging-related, README settings |
| Resolution or validation rules | `docs/flows/single-stream-resolution.md`, `docs/flows/playlist-url-validation.md`, `docs/invariants.md`, `docs/error-handling.md` |
| Concurrency or lock behaviour | `docs/concurrency-and-scheduling.md`, `docs/components/browser-automation.md`, `docs/error-handling.md`, `docs/invariants.md` |
| Dependencies or framework version | `docs/dependencies.md`, `docs/build-and-deployment.md`, README dependencies |
| CI or release workflow | `docs/build-and-deployment.md`, README GitHub Actions |
| Tests added or removed | `docs/testing.md`, `docs/ambiguities-and-open-questions.md` for resolved gaps |
| New hazard or resolved hazard | `docs/ambiguities-and-open-questions.md`, `docs/error-handling.md`, `docs/security.md` |

## Review cadence

There is no automated staleness check. Revise the affected documents in the same change as the
code, and update the `Last reviewed` field in [INDEX.md](./INDEX.md) after any substantial
revision.

## Conventions

- File and directory names are lower kebab-case; directories that hold collections are plural.
- Headings use sentence case.
- Cross-references use repository-relative Markdown links.
- Code symbols appear in backticks with their namespace or full type name where useful.
- Claims must be evidence-based; label anything inferred, and never present inference as fact.

## Verification before considering a documentation change complete

1. Every new or changed link resolves to a file that exists.
2. Every documented symbol and path exists in the repository.
3. Tables are complete (no empty cells where a value is known).
4. Statements about behaviour match the source; where a hazard is described, the source shows it.
5. The `docs/INDEX.md` catalogue lists every file under `docs/`.
6. The README `## 📚 Documentation` section links the principal `docs/` resources.

## Related documentation

- [INDEX.md](./INDEX.md)
- [change-guide.md](./change-guide.md)
- [repository-structure.md](./repository-structure.md)
