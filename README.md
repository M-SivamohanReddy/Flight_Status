# Flight Status Tracker

A Flight Status lookup feature for the SkyRoute platform built with .NET 8 Minimal API and Angular 18.

## Prerequisites

- .NET 8 SDK
- Node.js 18+ / npm
- Angular CLI (`npm install -g @angular/cli`)

## Setup & Run

### 1. Clone and navigate

```bash
git clone <repo-url>
cd flight-status
```

### 2. Run the API

```bash
cd FlightStatus.Api
dotnet run
# API listens on http://localhost:5000
```

### 3. Run the Angular UI

```bash
cd flight-status-ui
npm install
ng serve
# UI available at http://localhost:4200
```

### 4. Run the tests

```bash
cd FlightStatus.Tests
dotnet test
```

## Architecture Overview

```
flight-status/
├── spec.md                  # Data models, interface contracts — committed before implementation
├── FlightStatus.Api/        # .NET 8 Minimal API
│   ├── Providers/           # IFlightStatusProvider implementations (AeroTrack, QuickFlight stubs)
│   ├── Services/            # FlightStatusQueryService, StatusNormaliser
│   └── Models/              # Domain models, DTOs
├── FlightStatus.Tests/      # xUnit tests covering normalisation, merge, and endpoint behaviour
├── flight-status-ui/        # Angular 18 frontend
├── prompts.md               # AI prompts used during development
└── reflection.md            # Retrospective — what would be improved with more time
```

## Key Design Decisions

- See [spec.md](spec.md) for full data models, interface contracts, and stub scenarios.
- Both providers are queried concurrently (`Task.WhenAll`).
- Provider exceptions are swallowed and treated as no-data, not surfaced to the caller.
- Status 200 is always returned for valid requests — `Unknown` status handles the no-data case.

## Assumptions

See [spec.md — Section 1](spec.md#1-key-assumptions) for the full list.
