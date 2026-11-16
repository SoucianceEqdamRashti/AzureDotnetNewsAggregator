# Azure & .NET News Aggregator

A lightweight news dashboard powered by a .NET 10 collector, React/TypeScript, repository-backed JSON, and GitHub Pages.

See [setup, configuration, tests, and deployment](src/README.md), [engineering instructions](src/AGENTS.md), [source verification](src/docs/SOURCES.md), and [implementation checkpoints](src/IMPLEMENTATION_STATUS.md).

Quick start (requires .NET 10 SDK, Node.js 24, and pnpm 11.25.0):

```sh
cd src
dotnet test
dotnet run --project NewsCollector -- ingest data
cd web
pnpm install --frozen-lockfile
pnpm dev
```

GitHub Pages deployment is configured in the root `.github/workflows/update-news.yml`. Select **GitHub Actions** in Pages settings, then dispatch **Update news and publish** on the default branch. The workflow also runs at approximately 08:00 and 20:00 Europe/Stockholm with daylight saving time handling.

Live ingestion and a local production build were verified. A remote GitHub Actions run and Pages deployment have not yet been verified.
