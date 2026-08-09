# AI Prompts Log

Significant GitHub Copilot prompts used during development, with notes on key decisions.

---

## Phase 1 — Analysis & Specification

**Prompt:**
"Analyse the challenge PDF and create spec.md covering: unified domain models,
provider response models, IFlightStatusProvider interface, status normalisation rules,
merge rules, API contracts, validation rules, error-handling behaviour, deterministic
stub scenarios, frontend states, and key assumptions. Do not create implementation
files until spec.md is complete."

**Decisions made:**
- **Winner-takes-all merge** — simpler than field-level blending and avoids ambiguity when providers disagree on terminal/gate.
- **Delta <= 900 s = OnTime** — makes the "within 15 minutes" boundary explicit and inclusive.
- **Time delta overrides raw label** — actual times are more reliable than a provider's status string; handles the AA700 edge case (ON_SCHEDULE label, 20-min late actual).
- **AeroTrack tie-break** on equal timestamps — AeroTrack carries terminal, gate, delay reason, so it is the richer source.
- **spec.md committed first** — enforced by the challenge; confirmed by git log showing only spec.md in the initial commit.

---

## Phase 2 — Backend Core (Providers, Normaliser, Merge)

**Prompt:**
"Implement IFlightStatusProvider, AeroTrackStubProvider, QuickFlightStubProvider,
StatusNormaliser, and FlightStatusQueryService following spec.md. Stubs must be
deterministic in-memory; cover all 10 scenarios from the spec."

**Decisions made:**
- StatusNormaliser checks Cancelled/Diverted first (no delta check), then departure delta, then arrival delta, then raw string fallback. This priority order prevents terminal statuses from being overridden by a time calculation.
- FlightStatusQueryService uses `Task.WhenAll` for concurrent provider queries. Exceptions are caught in `FetchSafeAsync` and logged as warnings — never surfaced to the caller.

**Prompt:**
"Write xUnit tests for StatusNormaliser and FlightStatusQueryService covering the
10 stub scenarios plus: early arrival (negative delta), exactly 15-min boundary,
delta overrides label, provider exception treated as no-data."

**Decision:** Tests use hand-written test doubles (NullProvider, FixedProvider, ThrowingProvider) rather than a mocking library — zero extra dependency, and the test doubles prove the interface contract rather than testing internal implementation details.

---

## Phase 3 — Minimal API + Angular UI

**Prompt:**
"Create GET /flights/status with inline validation (400 for missing/invalid params),
JSON options (camelCase, string enums), and CORS for localhost:4200."

**Decision:** Validation is inline in the endpoint handler. Minimal APIs do not support [ApiController] automatic model validation, so keeping validation explicit and readable was the right choice.

**Prompt:**
"Create Angular 18 standalone components: SearchFormComponent, ResultCardComponent,
FlightListComponent with column filters and the 9 UI states from the spec. Use
reactive forms for validation."

**Decision:** Used Angular `signal()` for the auth user state instead of BehaviorSubject — signals are the idiomatic Angular 17+ approach and eliminate explicit subscription teardown.

---

## Phase 4 — Database, Repository Pattern, Identity, JWT

**Prompt:**
"Add EF Core 9 with SQL Server LocalDB. Implement the Repository Pattern:
IFlightCatalogRepository and IFlightProviderDataRepository. Use IDbContextFactory<T>
as a Singleton for repositories so Task.WhenAll concurrent provider queries each get
their own DbContext — avoids the 'second operation on same context' EF Core error."

**Decision:** Two registrations are needed: `AddDbContext<T>` (Scoped) for Identity, and a `DirectDbContextFactory` (Singleton) for repositories. The factory carries its own `DbContextOptions`, avoiding the captive dependency error that occurs when a Singleton depends on a Scoped options object.

**Prompt:**
"Add ASP.NET Core Identity + JWT Bearer auth. Seed Admin and User roles plus 2 demo
accounts. Admin sees all flight details and bookings; User can book flights and view
their own bookings only."

**Decision:** `EnsureDeleted + EnsureCreated` runs on first boot (when Identity tables are missing) rather than `Database.Migrate()`, because the project pre-dates the migration file and applying a migration on top of a manually-created schema causes a conflict. `IdentitySeeder.SeedAsync` is idempotent and runs every boot.

**Prompt:**
"Implement the Repository Pattern with interfaces for all data access. Register
IFlightCatalogRepository and IFlightProviderDataRepository as Singleton (factory-backed),
IBookingRepository as Scoped (uses Scoped DbContext)."

**Decision:** Booking writes have no concurrent-call requirement within a single request, so a Scoped repository using the Scoped DbContext is correct and simpler than the factory approach.

---

## Phase 5 — Beyond-Spec Enhancements

**Prompt:**
"Add IMemoryCache to FlightStatusQueryService. Cache per flightNumber+date for 60 s
with 30 s sliding expiry. Add 5-per-page pagination to admin and user booking tables.
Add 4 new edge-case stub scenarios via an idempotent SeedDelta() method."

**Decisions made:**
- Cache key `fstatus:{flightNumber}:{date:yyyyMMdd}` is date-scoped so different travel dates are never cross-contaminated.
- SeedDelta() checks each row individually before inserting — safe to run on every boot without data loss on an existing database.

**Prompt:**
"Create a floating Angular chatbot widget. Pattern-match natural language (list flights,
check AA100, AA200 on 2026-08-10) and call the public API. Also create an MCP server
using @modelcontextprotocol/server v2 SDK with list_flights and check_flight_status
tools over stdio."

**Decisions made:**
- Chatbot uses regex pattern matching, not an LLM — works offline without API keys; replacing the NLP layer later only requires changing `processMessage()`.
- MCP server calls the same public REST endpoints the Angular app uses — no duplication of data access logic.
- `.vscode/mcp.json` pre-configured so Copilot Agent can call the tools immediately without manual setup.

**Prompt:**
"Redesign SearchFormComponent and ResultCardComponent with CSS grid layout, gradient
button, animated status banner, times grid, and info chips for terminal/gate."

**Decision:** Status icons are Unicode emoji rather than SVG icons — avoids an icon library dependency while still conveying visual meaning clearly.

---

## Key Judgement Calls

| Decision | Why |
|----------|-----|
| Auth + persistence added beyond spec | Demonstrates production-grade thinking; core requirements are satisfied independently |
| Public GET /flights/status (no auth) | Flight status is public information; enables landing-page search and MCP chatbot without requiring a JWT |
| `DirectDbContextFactory` over `PooledDbContextFactory` | `PooledDbContextFactory` is in `Microsoft.EntityFrameworkCore.Internal` (semi-private); the custom implementation is explicit and dependency-free |
| `::ng-deep` in LandingComponent CSS | Collapses the two-column SearchForm grid to single-column inside the landing card without modifying the component's own CSS — parent-context styling without breaking encapsulation |
| `EnsureDeleted + EnsureCreated` instead of Migrate | Project started before migrations; conditionally runs only when Identity tables are missing, so existing bookings survive restarts |