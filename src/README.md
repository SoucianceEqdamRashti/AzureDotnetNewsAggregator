# Azure & .NET News Aggregator

A .NET 10 console collector publishes repository-backed JSON to a React + TypeScript dashboard on GitHub Pages. No database, API server, Azure resources, login, or paid services are required.

## Local setup

Install the **.NET 10 SDK**, **Node.js 24**, and **pnpm 11.25.0** (`npm install --global pnpm@11.25.0`). Run these commands from the repository's `src` directory:

```sh
dotnet restore
dotnet test
dotnet run --project NewsCollector -- validate-sources data
dotnet run --project NewsCollector -- ingest data
cd web
pnpm install --frozen-lockfile
pnpm dev
```

The collector accepts an optional absolute or relative data-directory argument. Validation checks configured endpoints and parses their responses without changing published data. Add `--include-disabled` after the directory to check disabled entries that have a candidate endpoint. Disabled entries and reasons are always reported. Exit codes: `0` success, `1` failure, `2` invalid command, `130` cancellation. An empty but valid feed counts as a successful fetch; the validation report includes the number of dated entries.

Optionally provide `GITHUB_TOKEN` through the environment for public GitHub release API requests. Unauthenticated requests can hit the shared-IP rate limit. The token is sent only through the dedicated GitHub client; never put it in configuration or commit it.

## Configuration

- `data/sources.json`: all 34 requested sources. `homepage` preserves the supplied URL; `url` is a verified ingestion endpoint, or `null` when none was verified. Set `enabled` to add or disable ingestion. IDs are stable keys, independent of display names.
- Existing adapter types are `rss`, `atom`, and `github-release` (GitHub REST API). The `custom` type with `adapter: "msrc"` handles the official MSRC CVRF update-summary API. Other custom sources remain disabled until a reliable adapter exists.
- `defaultCategories` contains category IDs. Optional `includeKeywords` keeps only matching titles or summaries for broad publishers. Keywords match case-insensitively at word boundaries; add plural forms explicitly. Keywords ending in punctuation, such as `CVE-`, can match prefixes.
- `data/categories.json`: category IDs, display labels, and keyword rules. Multiple categories can match. Source defaults and matching rules are combined. Rules are reapplied to stored articles on successful ingestion, including articles from temporarily failing sources.
- Add/remove source entries or change rules without code changes for supported adapters. Validate new endpoints before enabling them. See [source verification and disabled reasons](docs/SOURCES.md).

## Data and failure policy

Articles use a SHA-256 ID of their normalized HTTP(S) URL. Normalization removes fragments and known tracking parameters and sorts remaining query parameters. It preserves path casing, meaningful parameters, and HTTP/HTTPS distinctions. Exact normalized URLs deduplicate across sources; title-similarity deduplication is intentionally omitted. Incoming data wins duplicate text/source conflicts, and categories are combined. Source order determines precedence; outputs sort by UTC publication time descending and ID for ties.

Only reliable publication dates in the inclusive `[now - 30 days, now]` window survive merging. Atom entries with only an `updated` timestamp are excluded. Summaries are plain text, capped at 1,000 characters. Browser links accept only HTTP(S). GitHub prereleases are included, drafts are excluded. Release pagination stops after a page with no publications in the retention window, with a 20-page safety limit. This assumes recent releases appear before historical pages; unusual backdated/delayed publications may need source-specific handling. Feeds also expose limited publisher-controlled history: the initial dataset is not guaranteed to backfill every article from the last 30 days.

Sources fetch independently with concurrency 4, a 25-second HTTP timeout, a 90-second source budget, bounded retries for network failures/429/5xx, and a 10 MB response limit. Longer retry requests are deferred to a later run. Failed sources preserve their retained articles. If all sources fail, existing files and their timestamp remain untouched and the collector exits unsuccessfully. A valid empty feed counts as success. Any successful run advances `lastSuccessfulIngestion`; `failedSources` reports partial failures.

JSON replacement is atomic per file, and identical bytes are not rewritten. Articles and metadata are separate files, not a filesystem transaction: after a local interruption, rerun ingestion before publishing. The publishing workflow only builds/deploys after successful ingestion. Timestamp-only metadata changes legitimately produce a commit even when article data is unchanged; exact no-op output produces no commit.

Read/unread status, bookmarks, and the previous-visit baseline stay in browser localStorage. Blocked or corrupt storage degrades to the current visit. These preferences do not sync between browsers.

## GitHub Pages

1. Push the implementation to the default branch. Under repository **Settings → Pages**, select **GitHub Actions** as the build source.
2. Allow the update workflow's `GITHUB_TOKEN` to write repository contents. Branch rules must permit the bot's data-only commits; a rejected push fails the workflow before deployment. No force pushes are used.
3. Run **Update news and publish** manually from Actions on the default branch. It runs ingestion, all tests/checks, builds, commits only article/metadata files, uploads the exact build, and deploys with official Pages Actions.
4. Verify the deployed URL and inspect the workflow's source failures. No deployment has been verified merely by a successful local build.

The schedule uses `0 8,20 * * *` with `timezone: Europe/Stockholm`, accounting for daylight saving time. GitHub scheduling is approximate and may be delayed. Publication runs are serialized and not cancelled midway. Build uses the Pages `base_path` output, and data fetching uses Vite's base URL. Data commits made with `GITHUB_TOKEN` do not need to trigger another workflow: deployment happens in the same run. A deployment-only failure can be retried by rerunning the failed deployment job while its artifact is available, or by dispatching a fresh run.

The separate PR validation workflow runs offline fixture/unit tests and a repository-subpath build with read-only repository permissions. It never ingests live sources, commits data, or deploys. Code/config changes publish on the next manual or scheduled update; there is no push-triggered publication.

## Verification and recovery

```sh
# From src
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet format --verify-no-changes --no-restore
# From src/web
pnpm test
pnpm typecheck
pnpm lint
pnpm build
node scripts/verify-build.mjs
```

For a project-path build, set `BASE_PATH=/AzureDotnetNewsAggregator/` before both the build and verification commands. In PowerShell use `$env:BASE_PATH='/AzureDotnetNewsAggregator/'`. CI sets it automatically. `pnpm preview` serves the production build; set the same `BASE_PATH` when previewing a subpath build.

Follow [AGENTS.md](AGENTS.md) for .NET and frontend engineering standards. [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) records checkpoints, test evidence, limitations, and the next recovery steps. Tests use inline fixtures and fake HTTP responses, with no live external dependencies.
