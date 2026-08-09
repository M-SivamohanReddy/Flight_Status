# SkyRoute — Flight Status Tracker

A **Flight Status Tracker** built for the SkyRoute platform challenge.
A support agent enters a flight number and date; the system queries two stub providers (AeroTrack and QuickFlight), normalises their responses into a unified status model, and displays the result.

---

## Prerequisites

| Tool | Minimum | Notes |
|------|---------|-------|
| .NET SDK | 10.0 | [download](https://dotnet.microsoft.com/download) |
| Node.js | 18+ | [download](https://nodejs.org/) |
| Angular CLI | 18+ | `npm install -g @angular/cli` |
| SQL Server LocalDB | Any | Ships with Visual Studio; standalone: [download](https://aka.ms/sqllocaldb) |

**Verify LocalDB is available:**
```powershell
SqlLocalDB info MSSQLLocalDB
```
If missing: `SqlLocalDB create MSSQLLocalDB`

---

## Quick Start

Open **three terminals** from the repo root.

### Terminal 1 — API
```bash
cd FlightStatus.Api
dotnet run --urls "http://localhost:5000"
```
First run automatically:
1. Creates `FlightStatusDb` on `(localdb)\MSSQLLocalDB`
2. Creates all tables (Identity, FlightCatalog, FlightProviderData, FlightBookings)
3. Seeds 14 flights, 2 roles, and 2 demo accounts

### Terminal 2 — Angular UI
```bash
cd flight-status-ui
npm install
ng serve --port 4200
```
Open **http://localhost:4200**

### Terminal 3 — Tests
```bash
cd FlightStatus.Tests
dotnet test
```
Expected output: **21 tests passing, 0 failed**

---

## Demo Accounts

| Email | Password | Role | What they can do |
|-------|----------|------|-----------------|
| `admin@skyroute.com` | `Admin@123` | Admin | Fleet status board + all passenger bookings |
| `user@skyroute.com` | `User@123` | User | Flight search, book flights, view own bookings |
| *(self-registered)* | your choice | User | Same as above |

> **JWT signing key in `appsettings.json`:** The value
> `SkyRoute-JWT-SuperSecret-Key-2026-AtLeast32Chars!`
> is an intentional **demo-only development key** committed to source so the
> application runs from a clean clone without any manual configuration.
> A production deployment would inject this via an environment variable or
> a secrets manager (e.g. Azure Key Vault) and the file would be excluded
> from source control via `.gitignore`.

---

## Available Test Flights

Flight status is **publicly accessible — no login required**.

| Flight | Route | Status | Interesting because… |
|--------|-------|--------|----------------------|
| AA100 | New York -> London | **OnTime** | Both providers agree; AeroTrack wins (later timestamp) |
| AA200 | LA -> Paris | **Delayed** 45 min | Delay reason: "Air Traffic Control" |
| AA300 | Chicago -> Frankfurt | **Cancelled** | Delay reason: "Maintenance" |
| AA400 | Miami -> Amsterdam | **Diverted** | AeroTrack only; QuickFlight has no record |
| AA500 | Dallas -> Dubai | **OnTime** | QuickFlight only; AeroTrack has no record |
| AA600 | Seattle -> Tokyo | **Unknown** | Neither provider responds |
| AA700 | Boston -> Barcelona | **Delayed** | Raw label is ON_SCHEDULE but 20-min delta overrides it |
| AA800 | SF -> Singapore | **OnTime** | QuickFlight wins (later lastUpdatedUtc) |
| AA900 | Denver -> Zurich | **OnTime** | Terminal T5, Gate C14 visible |
| AA1000 | Atlanta -> Sydney | **Delayed** | Delay reason: "Weather" |
| AA1100 | New York -> Tokyo | **OnTime** | Early arrival: actual is 10 min early (delta = -600 s) |
| AA1200 | London -> Singapore | **OnTime** | Exactly 15-min boundary (delta = 900 s, inclusive) |
| AA1300 | Paris -> Sydney | **Delayed** | 16-min delta overrides ON_SCHEDULE provider label |
| AA1400 | Dubai -> LA | **Cancelled** | QuickFlight only |

**Any calendar date** works — stubs return data regardless of the date value.

---

## Project Layout

```
flight-status/
|-- spec.md                # Domain models + interface contracts  (committed before any code)
|-- prompts.md             # AI prompts log with key decision notes
|-- reflection.md          # Post-implementation retrospective
|-- NuGet.config           # Locks restore to nuget.org only
|
|-- FlightStatus.Api/      # .NET 10 Minimal API
|   |-- Data/
|   |   |-- Entities/      # EF Core entities: FlightCatalog, FlightProviderData,
|   |   |                  #   ApplicationUser (extends IdentityUser), FlightBookingEntity
|   |   |-- Repositories/  # IFlightCatalogRepository, IFlightProviderDataRepository, IBookingRepository
|   |   |-- Migrations/    # EF Core migration: InitialCreate
|   |   |-- FlightStatusDbContext.cs   # Extends IdentityDbContext<ApplicationUser>
|   |   |-- DatabaseSeeder.cs          # First-run seed + idempotent SeedDelta()
|   |   +-- IdentitySeeder.cs          # Seeds Admin + User roles and 2 demo accounts
|   |-- Models/            # FlightStatus enum, FlightStatusResult, BookingModels, AuthModels
|   |-- Providers/         # IFlightStatusProvider + two DI-injected stub implementations
|   +-- Services/          # StatusNormaliser, FlightStatusQueryService, AuthService,
|                          #   BookingService, FlightCatalogService
|
|-- FlightStatus.Tests/    # xUnit — 21 meaningful tests (normalisation + merge logic)
|
|-- flight-status-ui/      # Angular 18 standalone application
|   +-- src/app/
|       |-- components/    # landing, login, register, admin-dashboard, user-dashboard,
|       |                  #   search-form, result-card, flight-list, chatbot
|       |-- services/      # AuthService, FlightStatusService, BookingService
|       |-- guards/        # authGuard, adminGuard, userGuard (functional, Angular 18+)
|       +-- interceptors/  # jwtInterceptor: attaches Bearer token, redirects on 401
|
|-- flight-status-mcp/     # MCP server (Node.js/TypeScript)
|   +-- src/index.ts       # Exposes list_flights + check_flight_status tools via stdio
|
+-- docker-compose.yml     # One-command: SQL Server + API + Angular (nginx)
```

---

## API Reference

| Method | Path | Auth required | Description |
|--------|------|--------------|-------------|
| POST | `/auth/register` | None | Register; new accounts get the User role |
| POST | `/auth/login` | None | Returns a signed JWT (60-minute expiry) |
| GET | `/flights` | None | Full flight catalog (14 routes) |
| GET | `/flights/status` | None | Core endpoint — flight number + date -> unified status |
| POST | `/bookings` | User role | Create a booking |
| GET | `/bookings/my` | Authenticated | Caller's own bookings |
| GET | `/admin/bookings` | Admin role | All passenger bookings |

**Returns 400** when `flightNumber` or `date` is missing or fails validation.  
**Returns 200 with `status: "Unknown"`** when no provider has data — never a 404 for missing flights.

---

## MCP Server (optional)

Exposes flight tools to AI agents (Claude Desktop, GitHub Copilot Agent, etc.).

```bash
cd flight-status-mcp
npm install
npm run dev          # stdio transport
```

The `.vscode/mcp.json` is pre-configured — Copilot Agent picks it up automatically.

Try in GitHub Copilot Agent chat:
```
List all SkyRoute flights
What is the status of AA200?
```

---

## Docker (optional)

```bash
docker compose up --build
# SQL Server on :1433  |  API on :5000  |  Angular on :4200
```

---

## Key Assumptions

Full rationale in [spec.md](spec.md). Summary:

1. **15-minute boundary is inclusive** — delta <= 900 seconds is OnTime; > 900 is Delayed.
2. **Time delta overrides raw provider label** — when actual times are available they are more reliable than the status string (demonstrated by AA700: label says ON_SCHEDULE, 20-min delta says Delayed).
3. **Winner-takes-all merge** — the provider with the later `lastUpdatedUtc` supplies all fields. No field-level blending.
4. **AeroTrack tie-break** — equal timestamps favour AeroTrack (it carries terminal, gate, delay reason).
5. **Provider exceptions are silent** — treated as no-data, never surfaced to the API caller.
6. **Beyond-spec additions** — Identity + JWT auth, LocalDB persistence, bookings, `IMemoryCache` (60 s TTL), pagination, MCP chatbot, and Docker Compose go beyond "no auth or persistence". The core challenge requirements are fully met independently of these additions.