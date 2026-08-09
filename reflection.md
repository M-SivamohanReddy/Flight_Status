# Reflection

## What Was Built

The core challenge (flight status lookup with two providers, normalisation, merge, Angular UI) was completed in full. Beyond the specification, the following was added to demonstrate production-grade engineering:

- **ASP.NET Core Identity + JWT auth** with Admin and User roles
- **SQL Server LocalDB persistence** via EF Core 9 with the Repository Pattern
- **Flight bookings** (users can book and view their own; admins see all)
- **IMemoryCache** on the status query service (60-second TTL per flight+date)
- **Client-side pagination** (5 rows per page) on all booking tables
- **MCP server** (`@modelcontextprotocol/server` v2) exposing `list_flights` and `check_flight_status` tools over stdio
- **Floating chatbot widget** in Angular with natural-language flight queries
- **Docker Compose** for one-command full-stack startup
- **14 deterministic stub scenarios** covering early arrival, boundary (exactly 15 min), label-overridden-by-delta, and single-provider cases

---

## What Would I Improve With More Time

### Testing

The 21 unit tests cover the core business logic (StatusNormaliser, FlightStatusQueryService). Given more time I would add:

- **Integration tests** using `WebApplicationFactory<Program>` to test the full HTTP pipeline (validation, auth, endpoint routing)
- **E2E tests** using Playwright — especially the search form, login/logout flow, and the admin/user dashboard role separation
- **Repository tests** against an in-memory SQLite provider to verify the EF Core query translations

### Architecture

- **Retry + circuit-breaker on providers** using Microsoft.Extensions.Http.Resilience — the current `FetchSafeAsync` catch-all silently suppresses provider failures; a circuit-breaker would fail fast after repeated errors rather than always waiting for a timeout
- **Background status pre-fetch** — when the user opens their dashboard, fire `GET /flights/status` for each booked flight in the background and cache the results, so "Check Status" is instant
- **Outbox pattern for bookings** — currently a booking write and its confirmation are not atomic; adding an outbox table would guarantee at-least-once delivery if the process crashes mid-request

### DX / Operability

- **Health checks** (`/healthz`) reporting DB connectivity and cache state
- **Structured logging** with Serilog ? OpenTelemetry ? Jaeger for distributed tracing across the two provider calls
- **`appsettings.Development.json` for secrets** — the JWT signing key should be gitignored in development and injected via environment variable in CI; currently it is committed as a demo key

---

## Critical Reflection on AI Tooling

### What worked well

GitHub Copilot accelerated the implementation of repetitive but error-prone code: EF Core entity configuration, the `StatusNormaliser` switch tables, the Angular CSS grid and status badge tokens, and all the DI wiring. Each suggestion was reviewed before acceptance — the AI never replaced judgement, it removed friction.

The most valuable AI interaction was during **Phase 1 (spec.md)**. Prompting the AI to reason about edge cases (exactly-15-minute boundary, raw-label vs delta override, tie-break strategy) before writing any code produced a specification that the implementation could be mechanically derived from. This reduced rework significantly.

### Where AI fell short

- **DI lifetime conflicts** — the captive dependency issue (`AddDbContext` + `AddDbContextFactory` ordering, `PooledDbContextFactory` namespace) required iterative manual debugging that the AI could not resolve without multiple correction cycles
- **Angular template corruption** — several `replace_string_in_file` operations left duplicate class definitions when the search string matched multiple positions; the AI needed manual intervention to rewrite entire files cleanly
- **Concurrent DbContext bug** — the original suggestion to use a Singleton repository with a Scoped DbContext directly (rather than `IDbContextFactory`) caused `Task.WhenAll` failures; the AI's first fix attempt used `PooledDbContextFactory` which is in an internal namespace, requiring a second correction

### Honest assessment

AI tooling compressed approximately 60–70% of the typing work. The remaining 30–40% — design decisions, debugging DI lifetime conflicts, fixing template corruption, and cross-cutting concerns like the `EnsureCreated` vs `Migrate` conflict — required human reasoning that the AI could support but not replace. The most productive pattern was: **human defines the problem precisely ? AI drafts the implementation ? human reviews and corrects**.
