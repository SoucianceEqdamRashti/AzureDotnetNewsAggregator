# Source verification

Verified 2026-11-16. All 34 requested sources are retained: 28 enabled and 6 disabled. The published metadata is the authoritative report of the latest run.

| Source | Adapter | Configuration | Ingestion URL | Verification / reason |
| --- | --- | --- | --- | --- |
| Azure Updates | rss | Enabled | [Endpoint](https://www.microsoft.com/releasecommunications/api/v2/azure/rss) | Live ingestion and parsing verified. |
| Microsoft Azure Blog | rss | Enabled | [Endpoint](https://azure.microsoft.com/en-us/blog/feed/) | Live ingestion and parsing verified. |
| Azure SDK Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/azure-sdk/feed/) | Live ingestion and parsing verified. |
| Microsoft Tech Community | custom | Disabled | No verified endpoint | Homepage returned no advertised feed; a stable public feed/API for this broad source was not verified. No HTML scraping. |
| Azure SQL Devs' Corner | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/azure-sql/feed/) | Live ingestion and parsing verified. |
| Azure Cosmos DB Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/cosmosdb/feed/) | Live ingestion and parsing verified. |
| Microsoft Agent Framework Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/agent-framework/feed/) | Live ingestion and parsing verified. |
| Microsoft Foundry Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/foundry/feed/) | Live ingestion and parsing verified. |
| Microsoft Agent Framework Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/microsoft/agent-framework/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| Azure SDK for .NET Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/Azure/azure-sdk-for-net/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| .NET Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/dotnet/feed/) | Live ingestion and parsing verified. |
| .NET Runtime Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/dotnet/runtime/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| ASP.NET Core Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/dotnet/aspnetcore/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| Visual Studio Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/visualstudio/feed/) | Live ingestion and parsing verified. |
| EF Core Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/dotnet/efcore/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| Npgsql | custom | Disabled | No verified endpoint | Homepage has no advertised dated feed. A reliable news endpoint was not verified; no homepage scraping is implemented. |
| Thinktecture Blog | custom | Disabled | No verified endpoint | Homepage has no advertised RSS/Atom feed. No supported dated API was verified; no fragile HTML scraping. |
| Aspire Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/aspire/feed/) | Live ingestion and parsing verified. |
| Aspire Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/dotnet/aspire/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| Microsoft Orleans Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/dotnet/orleans/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| Andrew Lock's Blog | rss | Enabled | [Endpoint](https://andrewlock.net/rss.xml) | Live ingestion and parsing verified. |
| Milan Jovanović Blog | rss | Enabled | [Endpoint](https://milanjovanovic.tech/rss/feed.xml) | Live ingestion and parsing verified. |
| Gérald Barré (Meziantou) | rss | Enabled | [Endpoint](https://www.meziantou.net/feed.rss) | Live ingestion and parsing verified. |
| Microsoft Security Blog | rss | Enabled | [Endpoint](https://www.microsoft.com/en-us/security/blog/feed/) | Live ingestion and parsing verified. |
| Microsoft Entra Blog | rss | Disabled | [Endpoint](https://techcommunity.microsoft.com/gxcuf89792/rss/board?board.id=Identity) | Publisher-linked legacy Identity RSS endpoint returned HTTP 404. Current blog navigation redirected to sign-in; no replacement feed verified. |
| Microsoft Security Response Center | custom (msrc) | Enabled | [Endpoint](https://api.msrc.microsoft.com/cvrf/v3.0/updates) | Live ingestion and parsing verified. |
| Azure DevOps Blog | rss | Enabled | [Endpoint](https://devblogs.microsoft.com/devops/feed/) | Live ingestion and parsing verified. |
| GitHub Blog | rss | Enabled | [Endpoint](https://github.blog/feed/) | Live ingestion and parsing verified. |
| Azure Bicep Releases | github-release | Enabled | [Endpoint](https://api.github.com/repos/Azure/bicep/releases?per_page=100) | API parsing verified; latest ingestion rate-limited. |
| HashiCorp Blog | custom | Disabled | No verified endpoint | Publisher homepage returned HTTP 429 during verification. No feed endpoint was verified; retry discovery when accessible. |
| InfoQ .NET | rss | Enabled | [Endpoint](https://feed.infoq.com/dotnet/) | Live ingestion and parsing verified. |
| InfoQ Cloud Computing | rss | Enabled | [Endpoint](https://feed.infoq.com/cloud-computing/) | Live ingestion and parsing verified. |
| The Register | rss | Enabled | [Endpoint](https://www.theregister.com/?lab_viewport=rss) | Live ingestion and parsing verified. |
| Microsoft Azure Customer Stories | custom | Disabled | No verified endpoint | Inspected official search HTML: no advertised feed or dated structured story listing. No supported public story-search API verified. Disabled to avoid fragile scraping. |

## Evidence and interpretation

- Azure Updates advertised its releasecommunications RSS endpoint in the supplied publisher page. Azure Blog and Microsoft developer blogs advertised the configured feeds in XML alternate links or RSS anchors.
- Andrew Lock, Milan Jovanović, and InfoQ advertised their endpoints through feed autodiscovery. Meziantou advertises unquoted HTML alternate links to feed.rss and feed.atom; feed.rss was fetched and parsed.
- The Register's [official feed directory](https://www.theregister.com/design/page/feeds) links to the configured RSS view. Broad-source includeKeywords apply to title/summary text.
- GitHub sources use the documented public [list releases API](https://docs.github.com/en/rest/releases/releases#list-releases), published_at timestamps, and Link pagination. API parsing was checked live before the shared unauthenticated quota was exhausted. The workflow supplies GITHUB_TOKEN. The SDK Atom alternative advertises only updated dates, so the API is used for publication reliability.
- MSRC uses the official [Microsoft CVRF API definition](https://github.com/microsoft/MSRC-Microsoft-Security-Updates-API/blob/main/docs/swagger.yaml). InitialReleaseDate is the publication date; CurrentReleaseDate does not make an old bulletin new. Original links point to the official CVRF document.
- Customer Stories: inspected the supplied official search page and searched for an official API. No advertised feed or dependable dated structured listing was verified. Unrelated site/auth APIs are not story-ingestion contracts. It remains disabled; no scraper or guessed endpoint was added.
- Disabled means no reliable endpoint was verified in this investigation, not proof that the publisher has no feed. All original homepage URLs remain in sources.json.

## Latest captured live run

Timestamp: 2026-11-16T09:22:42.159031+00:00. Successful sources: 20. Articles: 206. Temporarily failed sources: agent-framework, aspire, aspnetcore, azure-sdk-net, bicep, efcore, orleans, runtime. These failures were HTTP 403 rate limits; other sources continued.

Run `dotnet run --project NewsCollector -- validate-sources data` from src for current endpoint health. Validation does not update published data. Use `--include-disabled` after the data-directory argument to probe disabled entries that have candidate endpoints, without enabling them.
