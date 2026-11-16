# Implementation checkpoints

Updated: 2026-11-16. No commits or remote deployment have been performed by the implementation agent.

## Completed

- Contracts, .NET 10 solution, xUnit v3, strict TypeScript/React/Vite, Vitest/RTL, and formatting/lint configuration.
- URL normalization, stable IDs, duplicate merging, inclusive rolling retention, category rules and broad-source filtering.
- Independent RSS/Atom, GitHub API, and MSRC adapters; source validation CLI; bounded fetch/retry/cancellation behavior.
- Atomic per-file persistence and unchanged-byte detection; total-failure preservation; partial-source merging and metadata.
- All 34 sources configured, with unverified/unsupported sources disabled and explained in `docs/SOURCES.md`.
- Responsive dashboard, combined filters/search, read/unread, bookmarks, previous-visit highlights, loading/error/empty states.
- PR validation and scheduled/manual publishing workflows placed at the repository root, with serialized data updates and same-run Pages deployment.

## TDD evidence

- Pipeline: 6 expected failures on unimplemented methods → 6 passes.
- Feed parsing/categories: 5 new expected failures → 12 total passes; malformed-feed assertion tightened to its specific exception.
- Source isolation/persistence: 2 expected failures → 14 passes.
- Total ingestion failure: expected failure → 15 passes.
- Release pagination: reproduced unbounded historical pagination → regression passed.
- Frontend filters/runtime validation: 3 expected failures → 3 passes.
- Dashboard/storage: 4 expected failures → 7 frontend passes.
- Loading/error/empty application states: 2 expected failures → 9 frontend passes.
- MSRC initial publication dates: expected failure → passed.
- Robustness: reproduced missing-link and missing-required-field failures → fixed; encoded tracking regression also passed.
- Topic filtering: reproduced `AI` matching inside `email` → fixed with keyword boundaries.
- Retained data: reproduced stale source rules on stored articles → fixed by reclassification.
- Repeated query parameters: reproduced reordered values changing URL meaning → fixed by stable key sorting.
- Refactored adapters into separate files and ran suites/formatting between changes. Additional acceptance tests cover partial success, retries and cancellation.

## Verification and remaining work

Final backend checks passed on SDK **10.0.404**: locked restore, Release build with **0 warnings / 0 errors**, **26 xUnit tests**, and `dotnet format --verify-no-changes --no-restore`.

Frontend checks passed: 9 Vitest/RTL tests, TypeScript, ESLint, frozen dependency install, production builds at both `/` and `/AzureDotnetNewsAggregator/`, matching packaged JSON, and asset-path verification. Production dependency audit reported no known vulnerabilities. Actionlint 1.7.12 passed both workflow files (shellcheck/pyflakes integrations unavailable and disabled). Local browser loaded the production subpath and displayed articles; the mobile layout stacked filters without horizontal overflow.

Live ingestion was verified: the latest run produced 206 articles from 20 successful sources after current topic rules were reapplied. All eight enabled GitHub release sources were temporarily rate-limited in that run. Individual release API parsing was verified earlier, including paginated Azure SDK responses. All 34 source definitions are present: 28 enabled and 6 disabled. CI supplies its token; authenticated GitHub Actions execution remains unverified.

Recovery sequence:

1. Read this file, `AGENTS.md`, and `docs/SOURCES.md`; inspect local changes before editing.
2. Install .NET 10 (global.json permits stable 10.0 feature bands), Node 24, and the pinned pnpm version. The implementation environment originally had only SDK 11; a checksum-verified SDK 10 was placed in ignored `.tools/dotnet10` for final verification.
3. Run README checks. For a failed behavior, reproduce it with an offline fixture and continue Red → Green → Refactor.
4. Run `validate-sources data`; for rate limits use an environment token or wait for reset. Disable a persistently broken source with a precise reason; do not replace it with guessed URLs or scraping.
5. Run ingestion and review its metadata, then rebuild. For interrupted writes, rerun before publishing. For concurrent branch changes, rerun from the latest default branch rather than force pushing.
6. Push/merge through the normal repository process, configure Pages, and dispatch the update workflow. Check that the data commit and deployed artifact match. This is the outstanding external acceptance checkpoint.

Known deliberate limits: no title similarity, no fragile Customer Stories scraping, no guarantee of complete historical backfill, and no cross-device preference sync. Metadata-only commits are expected when a successful check advances the timestamp.
