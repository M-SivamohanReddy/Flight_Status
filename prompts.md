# AI Prompts Log

Significant GitHub Copilot prompts used during development of the SkyRoute Flight Status Tracker, with decision notes on key judgement calls.

---

## Phase 1 — Analysis & Specification

**Prompt:** "Analyse the requirements from the challenge PDF and create a spec.md covering unified domain models, provider response models, the IFlightStatusProvider interface, status normalisation rules, merge rules, API contracts, validation rules, error-handling behaviour, deterministic stub scenarios, frontend states, and key assumptions."

**Decisions made:**
- Chose **winner-takes-all merge** (not field-level blending) — simpler, predictable, and avoids the ambiguity of which provider's terminal/gate is "correct" when both respond.
- Defined **?t = 900 s = OnTime** so the "within 15 minutes" boundary is explicit and inclusive.
- Decided **time delta overrides raw status label** — if actual times are present, they are more reliable than a provider's status string. This handles the AA700 edge case where the label says ON_SCHEDULE but the flight is 20 minutes late.
- Added **AeroTrack tie-break** when `lastUpdatedUtc` values are equal, because AeroTrack carries more detail (terminal, gate, delay reason).

---

## Phase 2 — Backend Core (Providers, Normaliser, Merge, Tests)

**Prompt:** "Implement IFlightStatusProvider, AeroTrackStubProvider, QuickFlightStubProvider, StatusNormaliser, and FlightStatusQueryService following the rules defined in spec.md. The providers must be deterministic and cover all 10 stub scenarios."

**Decisions made:**
- `StatusNormaliser` checks Cancelled/Diverted first (no delta check), then departure delta, then arrival delta, then falls back to raw string — this priority order was chosen deliberately so terminal statuses are never accidentally downgraded by a time delta.
- `FlightStatusQueryService` uses `Task.WhenAll` for concurrent provider calls. Provider exceptions are caught in `FetchSafeAsync` and logged as warnings — never surfaced to the caller.
- Both stubs are registered as **Scoped** (later promoted to use `IDbContextFactory` when EF Core was added).

**Prompt:** "Write xUnit tests for StatusNormaliser and FlightStatusQueryService covering all 10 stub scenarios plus edge cases: early arrival, exactly 15-minute boundary, delta-overrides-label, and provider-exception-treated-as-no-data."

**Decision:** Tests use in-memory test doubles (NullProvider, FixedProvider, ThrowingProvider) rather than mocking frameworks — simpler, no extra dependency, and proves the interface contract directly.

---

## Phase 3 — Minimal API Endpoint + Angular UI

**Prompt:** "Create the GET /flights/status minimal API endpoint with validation (400 for missing/invalid params), wire up DI, add CORS for localhost:4200, and configure JSON serialisation to use camelCase and string enums."

**Decision:** Validation is done inline in the endpoint handler (not via attributes) because Minimal APIs don't support `[ApiController]` model validation — keeping it explicit and visible.

**Prompt:** "Create the Angular 18 standalone components: SearchFormComponent (flight number + date inputs with validation), ResultCardComponent (colour-coded by status, AeroTrack-only fields shown only when non-null), FlightListComponent (fleet board with 9 UI states, column filters, pagination). Use the existing FlightStatusService."

**Decision:** Used Angular `signal()` for the auth user state in AuthService rather than BehaviorSubject — signals are the Angular 17+ idiomatic approach and avoid manual subscription management.

---

## Phase 4 — Database (EF Core + Repository Pattern)

**Prompt:** "Add EF Core 9 with SQL Server LocalDB. Implement the Repository Pattern with IFlightCatalogRepository and IFlightProviderDataRepository. Use IDbContextFactory<T> as a Singleton factory for repositories so Task.WhenAll concurrent provider queries each get their own DbContext and avoid the 'second operation on same DbContext' error."

**Decision:** `AddDbContext` is called **before** `AddDbContextFactory` so that Identity's Scoped DbContext registration wins the TryAdd race. The Singleton factory uses a `DirectDbContextFactory` with its own options, completely independent of the DI-registered `DbContextOptions`, avoiding the captive dependency error.

**Prompt:** "Add ASP.NET Core Identity with JWT Bearer auth. Seed 2 roles (Admin, User) and 2 demo users. Admin sees all flight details and bookings; User can book flights and see their own bookings."

**Decision:** `EnsureDeleted + EnsureCreated` is used on first boot when Identity tables are missing, rather than `Migrate()`, because the project started with `EnsureCreated` for the pre-auth schema and adding a migration on top of a manually-created DB causes conflicts. `IdentitySeeder.SeedAsync` is idempotent and runs every boot.

---

## Phase 5 — Beyond-Spec Features

**Prompt:** "Add IMemoryCache to FlightStatusQueryService to cache status results per flight+date for 60 seconds. Add pagination (5 per page) to the admin and user bookings tables. Add 4 new edge-case stub scenarios via an idempotent delta seeder."

**Decision:** Cache key is `fstatus:{flightNumber}:{date:yyyyMMdd}` — date-keyed so status changes on different dates are never cross-contaminated.

**Prompt:** "Create a floating Angular chatbot widget that processes natural language flight queries using pattern matching and calls the public API. Also create an MCP server using @modelcontextprotocol/server v2 SDK exposing list_flights and check_flight_status tools over stdio."

**Decision:** The chatbot uses **rule-based regex**, not an LLM, so it works offline without API keys. Swapping in LLM completions later only requires replacing the `processMessage()` method — the rest of the UI is unchanged.

**Prompt:** "Redesign SearchFormComponent and ResultCardComponent with a polished CSS grid layout, gradient button, animated status banner, times grid, info chips for terminal/gate, and delay reason warning banner."

**Decision:** `ResultCardComponent.statusIcon` returns Unicode emoji rather than SVG icons to avoid an icon library dependency. The status banner uses CSS gradients rather than solid colours for visual depth.

---

## Key Judgement Calls (cross-cutting)

| Decision | Rationale |
|----------|-----------|
| Added auth + persistence beyond spec | Demonstrates production-grade thinking; core requirements are fully met independently |
| Public `GET /flights/status` | Flight status is public information; enables MCP chatbot and landing-page search without requiring login |
| `DatabaseSeeder.SeedDelta()` always runs | Idempotent — new stub scenarios can be added without dropping data |
| `::ng-deep` in LandingComponent | The SearchFormComponent grid must collapse to single-column inside the landing card without altering the component's own CSS |
