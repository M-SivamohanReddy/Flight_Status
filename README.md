# SkyRoute — Flight Status Tracker

A **Flight Status Tracker** built for the SkyRoute platform challenge.  
Enter a flight number and date; the system queries two stub providers (AeroTrack and QuickFlight), normalises their responses into a unified status model, and displays the result.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 10.0+ | [download](https://dotnet.microsoft.com/download) |
| Node.js | 18+ | [download](https://nodejs.org/) |
| Angular CLI | 18+ | `npm install -g @angular/cli` |
| SQL Server LocalDB | Any | Included with Visual Studio; standalone: [download](https://aka.ms/sqllocaldb) |

Verify LocalDB is available:
```powershell
SqlLocalDB info MSSQLLocalDB
```
If not present: `SqlLocalDB create MSSQLLocalDB`

---

## Quick Start (3 terminals)

### Terminal 1 — API
```bash
cd flight-status/FlightStatus.Api
dotnet run --urls "http://localhost:5000"
```
On first run the API automatically:
1. Creates `FlightStatusDb` on `(localdb)\MSSQLLocalDB`
2. Creates all tables (flights, providers, Identity, bookings)
3. Seeds 14 flights, 2 roles, and 2 demo accounts

### Terminal 2 — Angular UI
```bash
cd flight-status/flight-status-ui
npm install
ng serve --port 4200
```
Open **http://localhost:4200**

### Terminal 3 — Tests
```bash
cd flight-status/FlightStatus.Tests
dotnet test
```
Expected: **21 tests passing**

---

## Demo Accounts

| Email | Password | Role | Access |
|-------|----------|------|--------|
| admin@skyroute.com | Admin@123 | Admin | Full fleet board + all passenger bookings |
| user@skyroute.com | User@123 | User | Flight search + book flights + my bookings |
| *(any registered)* | *(your choice)* | User | Same as user above |

> **Note on the JWT signing key in `appsettings.json`:** The key `SkyRoute-JWT-SuperSecret-Key-2026-AtLeast32Chars!` is an intentional **demo-only** development key committed so the app runs from a clean clone without manual configuration. In a production deployment this would be injected via environment variable or a secrets manager.

---

## Available Test Flights

Flight status is publicly accessible — no login required. Try these:

| Flight | Route | Expected Status | Notes |
|--------|-------|----------------|-------|
| AA100 | New York ? London | **OnTime** | Both providers agree; AeroTrack wins (later timestamp) |
| AA200 | LA ? Paris | **Delayed** (45 min) | Delay reason: "Air Traffic Control" |
| AA300 | Chicago ? Frankfurt | **Cancelled** | Delay reason: "Maintenance" |
| AA400 | Miami ? Amsterdam | **Diverted** | AeroTrack only |
| AA500 | Dallas ? Dubai | **OnTime** | QuickFlight only (AeroTrack has no record) |
| AA600 | Seattle ? Tokyo | **Unknown** | Neither provider has data |
| AA700 | Boston ? Barcelona | **Delayed** | Raw label says ON_SCHEDULE but 20-min delta overrides it |
| AA800 | San Francisco ? Singapore | **OnTime** | QuickFlight wins (later timestamp) |
| AA900 | Denver ? Zurich | **OnTime** | Gate C14, Terminal T5 |
| AA1000 | Atlanta ? Sydney | **Delayed** | Delay reason: "Weather" |
| AA1100 | New York ? Tokyo | **OnTime** | Early arrival (-10 min delta) |
| AA1200 | London ? Singapore | **OnTime** | Exactly 15-min boundary (inclusive) |
| AA1300 | Paris ? Sydney | **Delayed** | 16-min delta overrides ON_SCHEDULE label |
| AA1400 | Dubai ? Los Angeles | **Cancelled** | QuickFlight only |

**Any date** works — stubs return data regardless of date.

---

## Project Structure

```
flight-status/
+-- spec.md                     # Data models and interface contracts — committed before implementation
+-- prompts.md                  # AI prompts log with decision notes
+-- reflection.md               # Post-implementation retrospective
+-- NuGet.config                # Locks restore to nuget.org (avoids private feed errors)
+-- FlightStatus.Api/           # .NET 10 Minimal API
¦   +-- Data/
¦   ¦   +-- Entities/           # EF Core entities (FlightCatalog, FlightProviderData, ApplicationUser, FlightBooking)
¦   ¦   +-- Repositories/       # IFlightCatalogRepository, IFlightProviderDataRepository, IBookingRepository
¦   ¦   +-- Migrations/         # EF Core migration (InitialCreate)
¦   ¦   +-- FlightStatusDbContext.cs   # Extends IdentityDbContext<ApplicationUser>
¦   ¦   +-- DatabaseSeeder.cs          # First-run seed + idempotent delta seeder
¦   ¦   +-- IdentitySeeder.cs          # Seeds roles and 2 demo users
¦   +-- Models/                 # FlightStatus enum, FlightStatusResult, BookingRequest/Response, Auth DTOs
¦   +-- Providers/              # IFlightStatusProvider, AeroTrackStubProvider, QuickFlightStubProvider
¦   +-- Services/               # StatusNormaliser, FlightStatusQueryService, AuthService, BookingService, FlightCatalogService
+-- FlightStatus.Tests/         # xUnit — 21 tests covering normalisation and merge logic
+-- flight-status-ui/           # Angular 18 standalone
¦   +-- src/app/
¦       +-- components/         # landing, login, register, admin-dashboard, user-dashboard,
¦       ¦                       # search-form, result-card, flight-list, chatbot
¦       +-- services/           # AuthService, FlightStatusService, BookingService
¦       +-- guards/             # authGuard, adminGuard, userGuard
¦       +-- interceptors/       # jwtInterceptor (adds Bearer token + handles 401)
+-- flight-status-mcp/          # MCP server (Node.js) — exposes list_flights + check_flight_status tools
¦   +-- src/index.ts
+-- docker-compose.yml          # SQL Server + API + Angular in one command
```

---

## API Endpoints

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| POST | `/auth/register` | Public | Register a new user (auto-assigned User role) |
| POST | `/auth/login` | Public | Login, returns JWT |
| GET | `/flights` | Public | All flights in catalog |
| GET | `/flights/status?flightNumber=AA100&date=2026-08-10` | Public | Flight status (core endpoint) |
| POST | `/bookings` | User role | Create a booking |
| GET | `/bookings/my` | Authenticated | User's own bookings |
| GET | `/admin/bookings` | Admin role | All passenger bookings |

**400 is returned** if `flightNumber` or `date` is missing/invalid.  
**200 with `status: Unknown`** is returned when no provider has data (never throws for missing flights).

---

## Running the MCP Server (optional)

The MCP server lets Claude Desktop or GitHub Copilot Agent query flights via natural language.

```bash
cd flight-status/flight-status-mcp
npm install
npm run dev   # starts on stdio
```

For VS Code Agent: the `.vscode/mcp.json` is pre-configured — it starts automatically when you use GitHub Copilot Agent mode.

Example agent queries:
- "List all available SkyRoute flights"
- "What is the status of flight AA200 today?"

---

## One-Command Docker Start

Requires Docker Desktop.

```bash
docker compose up --build
```

- SQL Server ? port 1433  
- API ? port 5000  
- Angular (nginx) ? port 4200

---

## Key Design Decisions & Assumptions

Full details in [spec.md](spec.md). Highlights:

1. **"Within 15 minutes"** is inclusive: ?t = 900 seconds ? OnTime; ?t > 900 seconds ? Delayed.
2. **Time delta overrides raw label** — if actual times are present, they are more reliable than the provider's status string (see AA700 scenario).
3. **Winner-takes-all merge** — the provider with the later `lastUpdatedUtc` supplies all fields. No field-level blending.
4. **AeroTrack tie-break** — when both providers have the same `lastUpdatedUtc`, AeroTrack is preferred (richer data).
5. **Provider exceptions are swallowed** — a provider that throws is treated identically to one returning null. The API always returns a `FlightStatusResult`, never a provider error.
6. **Beyond-spec additions** — Auth (Identity + JWT), persistence (LocalDB), bookings, caching, pagination, MCP chatbot, and Docker Compose were added beyond the "no auth or persistence" scope to demonstrate production-grade engineering practices. All core challenge requirements are fully met independently of these additions.
