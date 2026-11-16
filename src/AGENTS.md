# AGENTS.md — .NET and React / TypeScript Engineering Standards

> Scope: All C# source, ASP.NET Core APIs, background services, EF Core persistence, tests, and infrastructure code in this repository. Repository-specific requirements override preferences here, but never override security or correctness requirements. Follow existing conventions when modifying an established codebase unless a change is explicitly requested.

> Frontend scope: Section 16 applies to React / TypeScript code and frontend tests under this directory. Shared engineering priorities and delivery principles also apply; C# syntax and tooling rules apply only to .NET code.

> Project scope: This aggregator uses a .NET console collector, a static React frontend, and repository-backed JSON. API, authentication, database, Azure hosting, and telemetry infrastructure examples below are conditional guidance, not requirements to add those technologies. Keep the project within its stated lightweight scope.

## 1. Engineering priorities

1. Correctness, security, maintainability, observability, and simplicity take priority over cleverness.
2. Prefer small, cohesive changes and working vertical slices over speculative frameworks.
3. Do not introduce dependencies, layers, interfaces, patterns, or configuration without a concrete need.
4. Before coding, inspect the solution, target frameworks, package versions, `.editorconfig`, existing tests, and feature conventions.
5. For new projects, default to supported .NET LTS (currently .NET 10) and its stable default C# language version (C# 14 for .NET 10). For existing projects, respect their target framework; never silently upgrade. Do not use preview features or `LangVersion=preview` unless requested. Avoid `LangVersion=latest`; allow SDK/TFM defaults.
6. Prefer official Microsoft and library documentation when uncertain about APIs or version-specific behavior. Never invent APIs.

## 2. C# style and language constructs

- Enable nullable reference types, implicit usings, and analyzers; treat meaningful warnings as actionable.
- Prefer file-scoped namespaces, one primary type per file, clear naming, and small methods.
- Prefer `record`/`record struct` for immutable value-like models where value equality is intended; use classes for entities with identity and mutable lifecycle.
- Prefer `required` members, constructors, and immutable state when they improve invariants. Use primary constructors where they improve clarity, not mechanically.
- Use modern collection expressions (`[]`, `[item1, item2]`) when target typing and semantics are clear; use collection initializers when clearer or required. Do not assume collection expressions preserve concrete collection behavior.
- Prefer pattern matching, switch expressions, property patterns, `nameof`, null-coalescing, null-conditional access, and `is null` where clearer.
- Use `var` when the type is obvious from the right-hand side; otherwise prefer explicit types for readability.
- Prefer `readonly`, `const`, and `static` where appropriate. Avoid unnecessary allocations, defensive copying, and LINQ chains in hot paths.
- Use `DateTimeOffset` for timestamps representing instants, UTC for persisted timestamps, and `TimeProvider` for testable clock behavior.
- Use `Guid`/strongly typed IDs as appropriate; avoid using display names as durable identifiers.
- Use `CancellationToken` on I/O and long-running operations and propagate it to downstream calls.
- Avoid `async void` except event handlers; never block on async work with `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()`.
- Use `await using`/`IAsyncDisposable` for async resources when supported. Do not dispose DI-owned services manually.
- Do not add `ConfigureAwait(false)` mechanically to ASP.NET Core application code; evaluate separately in reusable libraries.
- Use comments to explain *why*, invariants, and non-obvious tradeoffs; avoid narrating obvious code. Document public APIs where helpful.

### Readable layout and line wrapping

- Put each statement on its own line. Never combine an assignment, logging call, return, or throw on the same line as another statement.
- Always use braces for control-flow bodies, including single-statement `if`, `else`, loops, and `using` blocks. Put opening and closing braces on separate lines (Allman style). Do not compress `try`, `catch`, or `finally` blocks onto one line.
- Expand guard clauses into multiline blocks. Do not write `if (condition) { Log(); return result; }` or `if (condition) return result;`.
- Use a target line length of 120 characters. Wrap long conditions, parameter lists, method calls, and fluent chains at logical boundaries. Do not split protocol values, URLs, or string literals merely to meet the target.
- For wrapped argument and parameter lists, put one argument or parameter per line with consistent indentation. Expand complex object/collection initializers and anonymous response objects so each member is on its own line.
- Separate validation, mapping, scheduling, logging, and response construction with blank lines where this makes the flow easier to follow. Avoid dense methods containing unrelated operations without visual separation.
- Keep simple expression-bodied members only when the complete member is short and immediately readable; expand complex or multiline expressions into block bodies.
- Prefer methods responsible for one action or a small, cohesive sequence of closely related actions. Keep orchestration methods readable at a consistent level of abstraction; extract well-named methods when validation, mapping, persistence, or response handling obscures the main flow.
- Avoid high cyclomatic complexity. Reduce deeply nested conditionals, large branching structures, and intertwined error paths using clear guard clauses, focused methods, and pattern matching where appropriate. Do not split methods mechanically or introduce abstractions solely to satisfy a complexity metric; each extraction must improve cohesion, readability, or testability.
- Apply these rules to production code and tests. Configure available `.editorconfig` rules for braces and multiline blocks/statements, and run `dotnet format --verify-no-changes`. The formatter does not enforce every line-wrapping rule; review long calls and conditions explicitly rather than assuming formatter success proves readability.
- Keep formatting-only changes separate from behavioral refactoring. Preserve statement order, evaluation order, exception handling, and public contracts.

Example:

```csharp
if (!DevelopmentAccess.Allowed(environment.EnvironmentName, options))
{
    status = StatusCodes.Status404NotFound;
    PipelineLog.Emit(logger, "EndpointDisabled");

    return new NotFoundResult();
}

var activity = await JsonSerializer.DeserializeAsync<InboundActivity>(
    req.Body,
    serializerOptions,
    ct);
```

## 3. Architecture: Vertical Slice first

Organize by business capability rather than technical layer. A typical API layout:

```text
src/
  MyApp.Api/
    Features/
      Orders/
        CreateOrder/
          Endpoint.cs
          Request.cs
          Response.cs
          Handler.cs
          Validator.cs
        GetOrder/
          Endpoint.cs
          Response.cs
          Handler.cs
    Common/
      Behaviors/
      Errors/
      Extensions/
    Domain/
      Orders/
        Order.cs
    Infrastructure/
      Persistence/
        AppDbContext.cs
        Configurations/
      Integrations/
    Program.cs
tests/
  MyApp.UnitTests/
  MyApp.IntegrationTests/
```

- Each slice owns its endpoint, input/output contracts, validation, and application behavior. Keep closely related code together; small slices may use fewer files.
- Keep cross-slice shared code in `Common/` or `Shared/` only when it is genuinely reused and stable. Avoid dumping unrelated helpers into a generic `Utils` folder.
- Keep infrastructure implementations in `Infrastructure/`; domain rules should not depend on ASP.NET Core, EF Core, or transport DTOs.
- Prefer direct, understandable feature handlers over mandatory mediator libraries. Add MediatR or similar only when its benefits justify the dependency.
- Do not create a generic repository or Unit of Work abstraction over EF Core by default; `DbContext` already provides these semantics. Add focused abstractions only for meaningful boundaries or testability.
- Avoid unnecessary service-to-service forwarding layers, anemic wrapper classes, and broad `BaseService` inheritance trees.
- Do not leak EF entities directly as public API contracts. Keep API contracts versionable.
- Keep dependency direction intentional; do not make `Common` depend on individual features.

## 4. ASP.NET Core API development

- Prefer Minimal APIs for small/new vertical-slice HTTP endpoints; controllers are acceptable for established projects or when their features are advantageous. Be consistent within the project.
- Use route groups and endpoint extension methods to register slices; keep `Program.cs` focused on composition.
- Use explicit request/response DTOs and typed results (`TypedResults`, `Results<...>`) when helpful.
- Validate inputs at the boundary, including length, range, required fields, formats, and domain constraints. Return consistent validation errors.
- Use correct HTTP semantics: 201 + Location for creation where appropriate, 204 for successful no-content operations, 400 for malformed input, 401 for unauthenticated, 403 for forbidden, 404 for missing resources, and 409 for conflicts.
- Centralize exception handling with `IExceptionHandler`/Problem Details. Never expose stack traces, connection strings, internal exception details, or sensitive payloads to clients.
- Apply authentication and resource-based authorization deliberately. Deny by default for protected endpoints; do not trust client-provided tenant/user IDs without authorization checks.
- Use API versioning only when needed; maintain backward compatibility of published contracts.
- Apply request size limits, timeouts, rate limiting, pagination, and idempotency where relevant. Avoid unbounded result sets.
- Generate and maintain OpenAPI metadata. Define health/readiness checks appropriate to dependencies.
- Use `IHttpClientFactory` or typed HTTP clients for outbound HTTP; configure timeouts, retries/backoff and resilience only where safe. Never blindly retry non-idempotent operations.
- Prefer `System.Text.Json` with source generation for high-throughput/AOT scenarios when justified. Do not switch to Newtonsoft.Json without a feature requirement.

## 5. Dependency injection and configuration

- Use built-in `Microsoft.Extensions.DependencyInjection` and constructor injection by default.
- Choose lifetimes correctly: singleton for thread-safe stateless/shared services, scoped for request/unit-of-work state and EF `DbContext`, transient for lightweight services when justified.
- Never inject a scoped service into a singleton directly. Use scopes or `IDbContextFactory<TContext>` for background/concurrent work where appropriate.
- Avoid service locator patterns (`IServiceProvider.GetService` in business logic), static mutable service state, and manual `new HttpClient()` per request.
- Bind configuration to strongly typed Options classes. Validate on startup (`ValidateOnStart`) and use `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>` based on lifecycle.
- Use constants, enums, typed options, or dedicated value objects instead of magic strings and numbers. Keep protocol-defined literal values where they are clearer, and centralize repeated values.
- Do not store secrets in source control or normal appsettings files. Use environment/secret stores and managed identity where possible.
- Avoid excessive interfaces: introduce one for external boundaries, multiple implementations, or meaningful test seams—not for every concrete class.

## 6. Serialization and allocations

- Never create `new JsonSerializerOptions(...)` on every request or inside loops. Prefer shared, immutable, preconfigured options via DI (`IOptions<JsonOptions>` or configured ASP.NET Core JSON options), static cached options, or source-generated `JsonSerializerContext`.
- Do not mutate shared `JsonSerializerOptions` after first use. Ensure naming policies and converters match the API contract.
- Reuse appropriately scoped `HttpClient`, serializers, regexes, and other expensive objects. Use `[GeneratedRegex]` for suitable constant regex patterns.
- Avoid premature optimization. Benchmark hot paths before introducing pooling, `Span<T>`, `ArrayPool<T>`, or unsafe code.
- Prefer streaming over loading large payloads into memory when practical.
- Use `StringComparison.Ordinal`/`OrdinalIgnoreCase` for nonlinguistic identifiers; specify culture intentionally for user-facing text.

## 7. Entity Framework Core and databases

- Prefer EF Core for relational persistence. Use provider-supported capabilities and check SQL translation of nontrivial queries.
- Register `DbContext` with the appropriate scope. `DbContext` is **not thread-safe**; never share one instance across parallel tasks.
- Use `AsNoTracking()` for read-only queries, project to DTOs using `Select`, and fetch only needed columns. Avoid unnecessary `Include` and N+1 queries.
- Use async database APIs and propagate cancellation tokens (`ToListAsync(ct)`, `SaveChangesAsync(ct)`).
- Configure mappings explicitly with `IEntityTypeConfiguration<T>` for nontrivial entities; define constraints, precision, indexes, relationships, and delete behavior.
- Manage schema with reviewed EF migrations. Do not automatically apply production migrations at app startup without an explicit deployment policy.
- Use database-enforced uniqueness, foreign keys, and constraints; application validation is not a substitute.
- Use optimistic concurrency tokens where concurrent updates matter; handle conflicts intentionally.
- Prefer transactions for multi-write atomic operations; keep them short. For reliable integration events, consider the transactional outbox pattern.
- Use parameterized queries. Never interpolate untrusted input into raw SQL. Review bulk operations and `ExecuteUpdateAsync`/`ExecuteDeleteAsync` for change-tracker and business-rule implications.
- Avoid client-side evaluation, excessive materialization, and unbounded queries. Inspect generated SQL and query plans for performance-sensitive endpoints.
- Do not parallelize EF operations on a single context. For independent parallel database operations, use separate contexts/scopes and only when concurrency is beneficial.
- For integration tests, prefer the real database engine in a disposable container. Do not treat EF InMemory as a faithful relational database substitute.

## 8. Asynchrony, concurrency, and resilience

- For independent I/O operations, prefer `Task.WhenAll` to sequential awaits when latency benefits justify concurrency and each operation uses independent resources.
- Use bounded concurrency (`SemaphoreSlim`, channels, or `Parallel.ForEachAsync`) for large workloads; avoid unbounded fan-out.
- Do not use `Task.Run` to wrap naturally async I/O in ASP.NET Core request handlers.
- Use `Parallel.ForEachAsync` primarily for independent workloads with controlled concurrency; measure before using parallelism for CPU-bound work.
- Propagate cancellation and distinguish user cancellation from genuine failures.
- Handle partial failure and exceptions from parallel operations deliberately. Do not fire-and-forget tasks from request handlers; use a durable queue/background worker where needed.
- Set timeouts and use retries with exponential backoff and jitter only for transient failures; respect idempotency and provider throttling.
- For messaging, consider duplicate delivery, ordering, poison messages, and idempotent consumers.

## 9. Logging, telemetry, and diagnostics

- Use `ILogger<T>` and structured message templates: `logger.LogInformation("Order {OrderId} created", orderId)`; do not use string interpolation for logging templates.
- Prefer source-generated logging (`[LoggerMessage]`) for frequently executed or performance-sensitive log statements.
- Use appropriate levels: Trace/Debug for diagnosis, Information for meaningful lifecycle events, Warning for recoverable anomalies, Error for failures, Critical for severe outages.
- Record exceptions with their exception object. Do not swallow errors, log-and-rethrow redundantly, or expose sensitive information.
- Never log passwords, tokens, connection strings, personal data, or full request/response bodies by default. Apply explicit redaction and retention policies.
- Use OpenTelemetry for traces, metrics, and logs where the stack supports it. Propagate W3C trace context across HTTP and messaging boundaries.
- Add meaningful spans/activities around external calls and important operations; use stable low-cardinality metric labels. Never use order IDs, user IDs, or arbitrary URLs as metric dimensions.
- Correlate logs with trace/span IDs and relevant safe business identifiers. Prefer standard instrumentation before custom telemetry.
- Expose health checks without flooding production logs; separate liveness from readiness.

## 10. Testing: xUnit and TDD

- Prefer **xUnit.net v3** for new test projects where supported. Respect the existing xUnit major version in established repositories.
- Follow Red → Green → Refactor for each behavior: write failing test(s), run and verify they fail for the expected reason, implement the minimum change, rerun, then refactor while tests remain green.
- Use test names in `Given_<Precondition>_When_<Action>_Then_<ExpectedResult>` format; use PascalCase words separated by underscores. Keep names descriptive, not excessively long.
- Add `[Fact(DisplayName = "Given ..., when ..., then ...")]` or `[Theory(DisplayName = "...")]` with clear, natural English descriptions. For xUnit v3 theories with multiple data rows, use data-row labels/display names where useful and supported by the installed version.
- Use Arrange / Act / Assert organization. Assert observable behavior and side effects rather than implementation details.
- Prefer `Assert` APIs from xUnit. Use FluentAssertions only if already approved/installed and license requirements are acceptable.
- Use `[Theory]` with inline/member/class data for meaningful variations. Avoid test duplication.
- Prefer real objects and lightweight fakes for domain logic; use mocks at external boundaries when necessary. Do not mock EF LINQ behavior as a substitute for database integration tests.
- Unit tests should be deterministic, isolated, and fast. Avoid real time delays, randomness without a fixed seed, shared mutable state, and dependence on execution order.
- Use `TimeProvider`/fake time and seeded test data. Keep test builders and fixtures reusable but simple.
- For API integration tests, use `WebApplicationFactory<Program>` and real HTTP requests; verify status, response contract, auth, validation, and persistence.
- Use Testcontainers for relational database integration tests when feasible. Isolate tests via disposable databases, transactions, or reset strategies; ensure parallel test safety.
- Test success, validation, not-found, unauthorized/forbidden, conflict, cancellation, and infrastructure failure paths as appropriate.
- Prefer coverage of critical behavior over a numeric coverage target. Never write meaningless tests solely to raise coverage.

Example:

```csharp
[Fact(DisplayName = "Given an unknown order, when it is requested, then return not found")]
public async Task Given_UnknownOrder_When_GettingOrder_Then_ReturnsNotFound()
{
    // Arrange
    var orderId = Guid.NewGuid();

    // Act
    var result = await handler.HandleAsync(orderId, TestContext.Current.CancellationToken);

    // Assert
    Assert.Null(result);
}
```

Adapt the example to the actual handler contract and installed xUnit version; avoid inventing dependencies.

## 11. Security and reliability

- Use least-privilege identity and authorization. Prefer managed identities over embedded credentials in Azure-hosted workloads.
- Validate untrusted input and output-encode where applicable. Avoid SQL injection, SSRF, unsafe file paths, insecure deserialization, and mass assignment.
- Treat external API responses, files, and messages as untrusted. Do not leak secrets through logs or exception messages.
- Use HTTPS, secure defaults, and appropriate CORS policies. Do not use wildcard CORS with credentials.
- Use standard cryptographic APIs and secure random generators; do not implement custom cryptography.
- Keep packages patched; review advisories, transitive dependencies, and license implications. Prefer fewer dependencies.
- Make state-changing operations idempotent where retries or at-least-once delivery are possible.

## 12. Tooling, quality gates, and delivery

- Keep `Directory.Build.props`, `Directory.Packages.props` (when central package management is used), `.editorconfig`, and `global.json` consistent with repository conventions.
- Prefer `dotnet format`, built-in .NET analyzers, nullable analysis, and xUnit analyzers. Do not disable diagnostics globally merely to make builds pass.
- Run the narrowest relevant tests while iterating, then broader unit/integration suites before declaring completion.
- When applicable, run:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

- Adjust commands to the actual SDK, solution, test runner, and environment. If Docker, databases, credentials, or cloud resources are unavailable, report the blocked checks explicitly; do not claim they passed.
- Review changes for API compatibility, database migrations, security, performance, cancellation, and observability.
- Do not modify unrelated files or generate large boilerplate changes without a reason.
- Do not commit secrets, build artifacts, generated temporary files, or environment-specific configuration.

## 13. Agent execution protocol

For every nontrivial change:

1. **Understand:** Summarize requirements, acceptance criteria, affected slices, and unknowns. Ask only when an ambiguity materially affects implementation.
2. **Inspect:** Read nearby code, configuration, package versions, test patterns, and architecture before editing.
3. **Plan:** Propose the smallest coherent vertical slice and test strategy. Identify dependencies and migration/compatibility risks.
4. **Red:** Add focused failing unit/integration tests and execute them. Verify the failure demonstrates missing behavior, not broken test setup.
5. **Green:** Implement the smallest production change. Follow established patterns and this file.
6. **Refactor:** Improve clarity, remove duplication, and ensure no regression. Add tests for discovered edge cases.
7. **Verify:** Build, run relevant tests, inspect analyzer/formatting output, and review logs and generated SQL where applicable.
8. **Report:** Summarize changed behavior, tests actually run and their results, design tradeoffs, remaining risks, and any checks that could not run.

Never claim that code compiles, tests pass, or a cloud integration works without actually verifying it. Never replace failing tests with weaker assertions just to get green results. When tests cannot be executed, provide exact commands and explain the blocker.

## 14. Patterns to avoid

- Repeated `new JsonSerializerOptions()` in request paths.
- Per-request `new HttpClient()` and manual lifetime management of DI-owned services.
- Blocking asynchronous operations or using `async void` outside event handlers.
- Sharing one `DbContext` across parallel tasks.
- Generic repositories and needless layers over EF Core.
- Unbounded `Task.WhenAll` fan-out or unobserved fire-and-forget tasks.
- Magic strings for configuration keys, roles, policies, queue names, and event names.
- Logging via interpolated strings, `Console.WriteLine` in production services, or sensitive payload dumps.
- Catching `Exception` only to ignore it or return success.
- Returning EF entities directly from external APIs.
- Giant service classes, bloated controllers, circular dependencies, and shared folders full of unrelated helpers.
- Mocking everything, testing private implementation details, or using EF InMemory as proof of relational behavior.
- Introducing preview syntax, unreviewed packages, or wholesale refactors without a requirement.

## 15. Decision principles

When choices are not specified, optimize for a maintainable production implementation with the fewest moving parts. Prefer established .NET platform features to third-party frameworks. Explain meaningful deviations from these guidelines rather than silently ignoring them.

## 16. React / TypeScript frontend

- Use React, TypeScript with strict checking, and Vite. Prefer small function components and focused hooks. Avoid unnecessary UI frameworks, global state libraries, abstractions, and dependencies.
- Organize related presentation and behavior together. Keep filtering, sorting, article validation, and browser persistence logic independently testable; do not put all behavior into a single dashboard component.
- Define explicit article, category, source, and metadata contracts consistent with collector JSON. Treat fetched JSON and localStorage contents as untrusted runtime values; TypeScript types alone do not validate them. Avoid `any` and unchecked type assertions.
- Use stable article IDs for React keys and persisted read/bookmark state. Derive filtered and sorted results from source data without mutating it or storing redundant derived state.
- Use effects for external synchronization, not ordinary calculations. Clean up effects and cancel fetches with AbortController where appropriate. Follow the Rules of Hooks and keep dependency lists accurate.
- Handle loading, fetch failures, missing or malformed data, empty datasets, and empty filter results explicitly. Handle unavailable, malformed, or full localStorage without crashing the dashboard.
- Capture the previous-visit baseline once per visit before updating its persisted value. Keep timestamps in UTC and format dates for display with browser locale APIs.
- Render external summaries as plain text. Do not use dangerouslySetInnerHTML for feed content. Accept only HTTP(S) article links and use appropriate rel attributes when opening new tabs.
- Use semantic HTML, labeled inputs, keyboard-accessible controls, visible focus states, and readable responsive layouts. Avoid unnecessary animation and styling complexity.
- Resolve data URLs relative to Vite's configured base path. Verify both assets and JSON loading under the GitHub Pages repository subpath; do not hardcode root-relative data paths.
- Follow Red → Green → Refactor for significant frontend behavior using Vitest and React Testing Library. Use descriptive Given / When / Then test descriptions and assert user-visible behavior through accessible roles and labels.
- Prioritize combined category/source/search filtering, date ordering, read and bookmark persistence, previous-visit highlighting, and invalid/empty data. Mock HTTP and browser storage boundaries; use controlled clocks rather than live services or time delays. Avoid tests for trivial markup, styling, private implementation details, and broad snapshots.
- Follow the established frontend formatter and ESLint configuration, including React Hooks rules. Use braces for control flow and readable multiline code; JavaScript/TypeScript formatting need not follow C# Allman style.
- Before declaring frontend work complete, run the project's non-watch tests, TypeScript type check, lint, and production build. Establish corresponding package scripts during scaffolding, document exact commands, and report any checks that could not run.
