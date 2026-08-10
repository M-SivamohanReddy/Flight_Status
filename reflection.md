# Reflection

## What Was Delivered

The core challenge (flight status lookup, two providers, normalisation, merge, Angular UI, xUnit tests) was completed in full and committed in the correct order (spec.md first, then implementation).

Beyond the specified scope, the following was added:

| Addition | Rationale |
|----------|-----------|
| ASP.NET Core Identity + JWT (Admin / User roles) | Demonstrates role-based access control and real-world auth patterns |
| SQL Server LocalDB + EF Core 9 | Persistence via Repository Pattern; data survives restarts |
| Flight bookings (create / list / admin view) | Realistic user journey beyond read-only status lookup |
| IMemoryCache (60 s TTL per flight+date) | Prevents redundant provider round-trips on the fleet board |
| Client-side pagination (5 rows/page) | Practical UX for tables with many rows |
| Idempotent SeedDelta() | New stub scenarios can be added without dropping existing data |
| MCP server — initially Node.js, migrated to .NET | `FlightMcpTools.cs` hosted in ASP.NET Core via SSE at `/mcp`; injects `IFlightCatalogService` directly — no separate process or HTTP round-trip |
| Floating chatbot widget (natural language) | In-app search without navigating to a form |
| Docker Compose | One-command startup for reviewers without a .NET or Node environment |
| Layered architecture — Controllers / Services / Repositories with interfaces | `IController`, `IAuthService`, `IFlightStatusQueryService`, `IBookingService`, `IFlightCatalogService`; auto-discovered endpoint registration via `MapControllers()` |
| CQRS via MediatR | Controllers dispatch to `IRequest<T>` handlers; read-side Queries are side-effect-free, write-side Commands mutate state — zero business logic in controllers |
| Custom middleware (`RequestPipelineMiddleware`) | Single `IMiddleware` class covering request/response logging, endpoint existence check (404 guard), JWT structural validation (401 guard), and global exception handling with exception-type→status-code mapping |
| `X-Correlation-Id` response header | Injected via `OnStarting` callback on every response; enables distributed tracing across logs |
| Angular feature modules (lazy loading) | `features/admin` and `features/user` are lazy-loaded chunks; shared components in `shared/components/` |

---

## What I Would Improve With More Time

### Testing

38 unit tests cover StatusNormaliser, FlightStatusQueryService, and RequestPipelineMiddleware. Missing:

- **Integration tests** using `WebApplicationFactory<Program>` — test the full HTTP pipeline (auth middleware, validation, endpoint routing, JSON serialisation) with a real in-memory database.
- **E2E tests** using Playwright — cover the login/logout flow, admin vs user role separation, and the chatbot widget's happy path.
- **Repository tests** against SQLite in-memory — verify EF Core query translations without hitting LocalDB.

### Architecture

- **Retry + circuit-breaker on providers** using `Microsoft.Extensions.Http.Resilience` — the current `FetchSafeAsync` catch-all silently absorbs failures; a circuit-breaker would fail fast after repeated errors rather than always waiting for a provider timeout.
- **Background status pre-fetch** — when a user opens their dashboard, fire concurrent status requests for all booked flights and warm the cache, so "Check Status" is instant.
- **Health checks endpoint** (`/healthz`) reporting DB connectivity and cache state — essential for container orchestration readiness/liveness probes.
- **Structured logging with Serilog + OpenTelemetry** — The `RequestPipelineMiddleware` already emits `X-Correlation-Id` and structured log entries; enriching these with OpenTelemetry spans would let you trace a single `/flights/status` call through both provider queries with one trace ID.

### Security / Operability

- **Move JWT secret to `appsettings.Development.json`** (gitignored) and supply it via an environment variable in CI/CD. The current approach (key in `appsettings.json`) is intentional for a clean-clone demo but would not be acceptable in production.
- **HTTPS enforcement** — the API currently uses HTTP only. Adding a dev certificate and enforcing HTTPS redirect is a one-line change but was omitted to simplify the quick-start instructions.
- **Angular production build** — `ng build --configuration production` reduces the bundle by ~40% through tree-shaking and minification. The Docker image uses the production build; local `ng serve` uses the development build.

---

## Critical Reflection on AI Tooling (GitHub Copilot)

### Where AI accelerated development

- **Boilerplate reduction** — EF Core entity configuration, DI registration blocks, Angular component scaffolding, and CSS token tables were generated quickly and accurately.
- **Spec-first thinking** — prompting Copilot to reason about edge cases (15-min boundary, delta vs label priority, tie-break) *before* writing code produced a specification concrete enough to be directly translated into tests and assertions.
- **Test coverage** — the AI suggested the `ThrowingProvider` test double (provider-exception-treated-as-no-data) which was not in the original test plan but is an important contract guarantee.

### Where AI required correction

- **DI lifetime conflicts** — the captive dependency between a Singleton factory and Scoped `DbContextOptions` required two correction cycles; the AI's first suggestion (`PooledDbContextFactory`) references a semi-internal namespace.
- **Angular template corruption** — several `replace_string_in_file` operations left stale code outside the class body when the search string matched multiple locations; files required manual complete rewrites.
- **`EnsureCreated` vs `Migrate` conflict** — the AI initially suggested running `Database.Migrate()` on a database that was created by `EnsureCreated` (no `__EFMigrationsHistory` table), which would fail at runtime. The correct fix (conditional EnsureDeleted + EnsureCreated when Identity tables are absent) required human reasoning about the boot-time state.

### Honest productivity assessment

AI tooling compressed approximately 60–70% of the implementation time. The remaining 30–40% — design decisions, debugging DI and EF Core lifetime issues, cross-cutting concerns, and fixing AI-introduced errors — required human reasoning that Copilot could *inform* but not *replace*. The most effective pattern throughout was:

> **Human defines the problem precisely → Copilot drafts the implementation → Human reviews, corrects, and accepts.**

The spec-first discipline (writing spec.md before any code) was the single most valuable practice: it gave the AI a precise, unambiguous contract to implement against and eliminated an entire category of back-and-forth.