# Flight Status Tracker — Specification

**Project:** SkyRoute Flight Status Lookup  
**Stack:** .NET 8 Minimal API (C#) · Angular 18 · xUnit  
**Date authored:** 2026-08-08  

---

## Table of Contents

1. [Key Assumptions](#1-key-assumptions)
2. [Domain Concepts](#2-domain-concepts)
3. [Unified Domain Models](#3-unified-domain-models)
4. [Provider Response Models](#4-provider-response-models)
5. [IFlightStatusProvider Interface](#5-iflightstatusprovider-interface)
6. [Status Normalisation Rules](#6-status-normalisation-rules)
7. [Merge Rules](#7-merge-rules)
8. [API Request and Response Contracts](#8-api-request-and-response-contracts)
9. [Validation Rules](#9-validation-rules)
10. [Error-Handling Behaviour](#10-error-handling-behaviour)
11. [Deterministic Stub Scenarios](#11-deterministic-stub-scenarios)
12. [Frontend States](#12-frontend-states)

---

## 1. Key Assumptions

| # | Assumption |
|---|-----------|
| A1 | "Within 15 minutes" is an inclusive boundary: `Δt ≤ 900 seconds` → OnTime; `Δt > 900 seconds` → Delayed. |
| A2 | The 15-minute check applies to **departure** first; if actual departure is absent, use **arrival** delta; if both are absent, fall back to the provider's raw status string. |
| A3 | `Cancelled` and `Diverted` statuses are accepted at face value from any provider — time deltas are irrelevant. |
| A4 | When both providers return data and `lastUpdatedUtc` values are **equal**, **AeroTrack** is preferred because it carries more detail. |
| A5 | The `date` query parameter refers to the **scheduled departure date** in the local timezone of the flight's origin. Because no real timezone data is available from stubs, all stub times are expressed as UTC. |
| A6 | Stubs return data regardless of whether the date is past, present, or future. |
| A7 | A provider that **throws an exception** (e.g., connectivity or serialisation error) is treated identically to one that returns `null` — it contributes no result and a warning is logged. |
| A8 | Flight number format accepted: 2–3 uppercase letters followed by 1–4 digits (regex `^[A-Z]{2,3}\d{1,4}$`). The API normalises the input to uppercase before matching. |
| A9 | There is no authentication, authorisation, rate-limiting, or persistence. The application is intended for local, offline execution only. |
| A10 | The Angular UI is served separately (port 4200) and calls the API on port 5000 during development. CORS is enabled on the API for `localhost:4200` in development. |
| A11 | `ProviderFlightStatus` is the **internal intermediate DTO** produced after deserialising each provider's proprietary response. The API never exposes raw provider models to callers. |
| A12 | `FlightStatusResult` returned to the caller always contains a `Status` value — never throws for a data-not-found scenario; it returns `Unknown` with an explanatory `Message`. |

---

## 2. Domain Concepts

| Concept | Definition |
|---------|-----------|
| **Flight number** | An IATA-style alphanumeric code identifying a scheduled flight (e.g., `BA123`, `UA2341`). |
| **Scheduled time** | The published departure or arrival time of a flight. |
| **Actual time** | The observed or estimated real-world departure/arrival time. |
| **Delay delta** | The difference `Actual − Scheduled`. Positive means late; negative means early. |
| **Provider** | An external system (stubbed) that vends flight status data in its own vocabulary and schema. |
| **Normalisation** | Converting a provider's proprietary status vocabulary to the unified `FlightStatus` enum. |
| **Merge** | Selecting the single authoritative `FlightStatusResult` from the set of (0, 1, or 2) normalised provider results. |

---

## 3. Unified Domain Models

### 3.1 FlightStatus Enum

```csharp
public enum FlightStatus
{
    OnTime,    // Δt ≤ 15 min on departure or arrival
    Delayed,   // Δt > 15 min on departure or arrival
    Cancelled, // Flight will not operate
    Diverted,  // Flight landed at a different airport
    Unknown    // No usable data from any provider
}
```

### 3.2 FlightStatusResult (Unified Response)

This is the **single canonical record** returned from the query service and serialised to the API response.

```csharp
public sealed record FlightStatusResult
{
    public required string FlightNumber { get; init; }
    public required DateOnly Date { get; init; }
    public required FlightStatus Status { get; init; }
    public DateTimeOffset? ScheduledDeparture { get; init; }
    public DateTimeOffset? ActualDeparture { get; init; }
    public DateTimeOffset? ScheduledArrival { get; init; }
    public DateTimeOffset? ActualArrival { get; init; }

    // AeroTrack-only fields — null when sourced from QuickFlight or Unknown
    public string? Terminal { get; init; }
    public string? Gate { get; init; }
    public string? DelayReason { get; init; }

    public DateTimeOffset LastUpdatedUtc { get; init; }
    public required string SourceProvider { get; init; }  // "AeroTrack" | "QuickFlight" | "None"
    public string? Message { get; init; }  // Human-readable note; populated for Unknown
}
```

### 3.3 ProviderFlightStatus (Internal Intermediate DTO)

Produced by each provider implementation after deserialising its proprietary response. The normalisation service consumes this model.

```csharp
public sealed record ProviderFlightStatus
{
    public required string FlightNumber { get; init; }
    public required string ProviderName { get; init; }
    public required string RawStatus { get; init; }  // Provider's own status string
    public DateTimeOffset? ScheduledDeparture { get; init; }
    public DateTimeOffset? ActualDeparture { get; init; }
    public DateTimeOffset? ScheduledArrival { get; init; }
    public DateTimeOffset? ActualArrival { get; init; }
    public string? Terminal { get; init; }
    public string? Gate { get; init; }
    public string? DelayReason { get; init; }
    public required DateTimeOffset LastUpdatedUtc { get; init; }
}
```

---

## 4. Provider Response Models

These represent the raw JSON shapes deserialised from each provider. They are **not exposed** to API callers.

### 4.1 AeroTrack Response Shape

AeroTrack returns full detail including actual times, terminal, gate, and delay reason.

**Status vocabulary:**

| AeroTrack `flightStatus` | Meaning |
|--------------------------|---------|
| `ON_SCHEDULE` | Flight is on or near schedule (actual times may still cause Delayed after delta check) |
| `DELAYED` | Provider explicitly marks as delayed |
| `CANCELLED` | Flight cancelled |
| `DIVERTED` | Flight diverted |
| `UNKNOWN` | Provider has no reliable data |

**Deserialisation model:**

```csharp
public sealed record AeroTrackFlightResponse
{
    public required string FlightId { get; init; }          // e.g., "BA123"
    public required string FlightStatus { get; init; }      // vocabulary above
    public DateTimeOffset ScheduledDeparture { get; init; }
    public DateTimeOffset ActualDeparture { get; init; }    // may equal ScheduledDeparture when no delay
    public DateTimeOffset ScheduledArrival { get; init; }
    public DateTimeOffset ActualArrival { get; init; }
    public string? Terminal { get; init; }
    public string? Gate { get; init; }
    public string? DelayReason { get; init; }
    public DateTimeOffset LastUpdatedUtc { get; init; }
}
```

**Mapping to ProviderFlightStatus:**

| `AeroTrackFlightResponse` field | → `ProviderFlightStatus` field |
|---------------------------------|-------------------------------|
| `FlightId` | `FlightNumber` |
| `FlightStatus` | `RawStatus` |
| `ScheduledDeparture` | `ScheduledDeparture` |
| `ActualDeparture` | `ActualDeparture` |
| `ScheduledArrival` | `ScheduledArrival` |
| `ActualArrival` | `ActualArrival` |
| `Terminal` | `Terminal` |
| `Gate` | `Gate` |
| `DelayReason` | `DelayReason` |
| `LastUpdatedUtc` | `LastUpdatedUtc` |
| _(provider name)_ | `ProviderName = "AeroTrack"` |

---

### 4.2 QuickFlight Response Shape

QuickFlight returns only status and scheduled times. No actual times, terminal, gate, or delay reason.

**Status vocabulary:**

| QuickFlight `status` | Meaning |
|----------------------|---------|
| `on-time` | On schedule |
| `delayed` | Late |
| `cancelled` | Cancelled |
| `unknown` | No data |

**Deserialisation model:**

```csharp
public sealed record QuickFlightResponse
{
    public required string Code { get; init; }              // e.g., "BA123"
    public required string Status { get; init; }            // vocabulary above
    public DateTimeOffset ScheduledDeparture { get; init; }
    public DateTimeOffset ScheduledArrival { get; init; }
    public DateTimeOffset LastUpdatedUtc { get; init; }
}
```

**Mapping to ProviderFlightStatus:**

| `QuickFlightResponse` field | → `ProviderFlightStatus` field |
|-----------------------------|-------------------------------|
| `Code` | `FlightNumber` |
| `Status` | `RawStatus` |
| `ScheduledDeparture` | `ScheduledDeparture` |
| `ScheduledArrival` | `ScheduledArrival` |
| _(absent)_ | `ActualDeparture = null` |
| _(absent)_ | `ActualArrival = null` |
| _(absent)_ | `Terminal = null`, `Gate = null`, `DelayReason = null` |
| `LastUpdatedUtc` | `LastUpdatedUtc` |
| _(provider name)_ | `ProviderName = "QuickFlight"` |

---

## 5. IFlightStatusProvider Interface

```csharp
/// <summary>
/// Abstraction over a single flight-status data provider.
/// Implementations query a specific provider and map its response to the
/// internal ProviderFlightStatus DTO. Returns null when the provider has
/// no data for the requested flight/date.
/// </summary>
public interface IFlightStatusProvider
{
    /// <summary>Human-readable provider identifier used in logs and SourceProvider.</summary>
    string ProviderName { get; }

    /// <summary>
    /// Retrieves flight status from this provider.
    /// Returns null if the flight is not found in this provider's data set.
    /// Throws only on unrecoverable internal errors (callers catch and treat as null).
    /// </summary>
    Task<ProviderFlightStatus?> GetFlightStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
```

**Contract rules:**
- Return `null` when the flight number is not in the stub data set (not-found is not an error).
- Do **not** return `null` for any other reason; throw if there is a genuine internal fault.
- Implementations must be **stateless** — they hold only injected configuration.
- `flightNumber` is passed already normalised to uppercase.

---

## 6. Status Normalisation Rules

The `IStatusNormaliser` (or equivalent static utility) converts a `ProviderFlightStatus` into a `FlightStatus` enum value.

### 6.1 Step-by-step Algorithm

```
Given: ProviderFlightStatus p

1. Resolve terminal statuses first (no time check needed):
   - if p.RawStatus (case-insensitive, trimmed) is in {"cancelled", "cancel"} → return Cancelled
   - if p.RawStatus is in {"diverted"}                                         → return Diverted

2. Compute departure delta:
   - if p.ActualDeparture != null AND p.ScheduledDeparture != null:
       Δdep = p.ActualDeparture - p.ScheduledDeparture  (in whole seconds)
   - else:
       Δdep = null

3. Compute arrival delta:
   - if p.ActualArrival != null AND p.ScheduledArrival != null:
       Δarr = p.ActualArrival - p.ScheduledArrival  (in whole seconds)
   - else:
       Δarr = null

4. Resolve from deltas (departure takes precedence):
   - if Δdep != null: return (Δdep ≤ 900s ? OnTime : Delayed)
   - if Δarr != null: return (Δarr ≤ 900s ? OnTime : Delayed)

5. Fall back to raw status string:
   - if p.RawStatus in {"on_schedule", "on-time", "ontime"}  → OnTime
   - if p.RawStatus in {"delayed", "delay"}                  → Delayed
   - else                                                     → Unknown
```

### 6.2 Normalisation Table

| Raw status | Actual times available? | Δ ≤ 15 min? | Normalised |
|-----------|------------------------|------------|-----------|
| `ON_SCHEDULE` | Yes | Yes | **OnTime** |
| `ON_SCHEDULE` | Yes | No | **Delayed** |
| `ON_SCHEDULE` | No | — | **OnTime** |
| `DELAYED` | Yes | — | Delayed (delta may confirm or override) |
| `DELAYED` | No | — | **Delayed** |
| `CANCELLED` | — | — | **Cancelled** |
| `DIVERTED` | — | — | **Diverted** |
| `UNKNOWN` | — | — | **Unknown** |
| `on-time` | No | — | **OnTime** |
| `delayed` | No | — | **Delayed** |
| `cancelled` | — | — | **Cancelled** |
| `unknown` | — | — | **Unknown** |

> **Edge case:** If `RawStatus = DELAYED` but actual times show Δ ≤ 15 min, the time-delta result (**OnTime**) takes precedence over the raw status string, because objective timing data is more reliable than provider label.

---

## 7. Merge Rules

The `FlightStatusQueryService` collects 0–2 `ProviderFlightStatus?` results (one per provider) and applies the following merge algorithm.

```
Given: results = list of non-null ProviderFlightStatus values (0, 1, or 2 entries)

Case A — 0 results:
  Return FlightStatusResult {
      Status = Unknown,
      SourceProvider = "None",
      Message = "No flight data returned by either provider."
      (all time/terminal fields = null)
  }

Case B — 1 result:
  Normalise the single result → FlightStatus
  Return FlightStatusResult built from that provider's data.

Case C — 2 results:
  Normalise both → statusA, statusB
  winner = result with the later LastUpdatedUtc
  (tie-break: prefer AeroTrack, per Assumption A4)
  Return FlightStatusResult built from winner's data,
         using winner's normalised status.
```

**Merge does not blend fields** — winner-takes-all. The winning `ProviderFlightStatus` provides all fields, including any nulls.

---

## 8. API Request and Response Contracts

### 8.1 Endpoint

```
GET /flights/status?flightNumber={code}&date={yyyy-MM-dd}
```

### 8.2 Request Parameters

| Parameter | Type | Required | Format | Example |
|-----------|------|----------|--------|---------|
| `flightNumber` | string | Yes | 2–3 uppercase letters + 1–4 digits | `BA123` |
| `date` | string | Yes | `yyyy-MM-dd` | `2026-06-15` |

### 8.3 Success Response — HTTP 200

```json
{
  "flightNumber": "BA123",
  "date": "2026-06-15",
  "status": "OnTime",
  "scheduledDeparture": "2026-06-15T08:00:00Z",
  "actualDeparture": "2026-06-15T08:08:00Z",
  "scheduledArrival": "2026-06-15T10:00:00Z",
  "actualArrival": null,
  "terminal": "T5",
  "gate": "B22",
  "delayReason": null,
  "lastUpdatedUtc": "2026-06-15T07:45:00Z",
  "sourceProvider": "AeroTrack",
  "message": null
}
```

**Status returns 200** even when `status = "Unknown"` — the call itself succeeded.

### 8.4 Unknown Result — HTTP 200

```json
{
  "flightNumber": "XX999",
  "date": "2026-06-15",
  "status": "Unknown",
  "scheduledDeparture": null,
  "actualDeparture": null,
  "scheduledArrival": null,
  "actualArrival": null,
  "terminal": null,
  "gate": null,
  "delayReason": null,
  "lastUpdatedUtc": "0001-01-01T00:00:00Z",
  "sourceProvider": "None",
  "message": "No flight data returned by either provider."
}
```

### 8.5 Validation Error — HTTP 400

```json
{
  "errors": {
    "flightNumber": ["flightNumber is required."],
    "date": ["date must be in yyyy-MM-dd format."]
  }
}
```

### 8.6 JSON Serialisation Rules

- `FlightStatus` enum serialised as **string** (not integer): `"OnTime"`, `"Delayed"`, etc.
- `DateTimeOffset` serialised as **ISO 8601 UTC**: `"2026-06-15T08:00:00Z"`.
- `DateOnly` serialised as **`"yyyy-MM-dd"`** string.
- Null fields are included with `null` value (no `[JsonIgnore]` on response model).
- Property names use **camelCase**.

---

## 9. Validation Rules

| Field | Rule | Error Message |
|-------|------|--------------|
| `flightNumber` | Must be present and non-empty | `"flightNumber is required."` |
| `flightNumber` | Must match `^[A-Za-z]{2,3}\d{1,4}$` (case-insensitive) | `"flightNumber must be 2–3 letters followed by 1–4 digits (e.g., BA123)."` |
| `date` | Must be present and non-empty | `"date is required."` |
| `date` | Must parse as a valid calendar date in `yyyy-MM-dd` format | `"date must be in yyyy-MM-dd format."` |

Validation is performed **before** calling any provider. Invalid requests return `HTTP 400` immediately.

---

## 10. Error-Handling Behaviour

| Scenario | Behaviour | HTTP Status |
|----------|-----------|-------------|
| Missing `flightNumber` | Return 400 with error detail | 400 |
| Invalid `flightNumber` format | Return 400 with error detail | 400 |
| Missing `date` | Return 400 with error detail | 400 |
| Invalid `date` format | Return 400 with error detail | 400 |
| AeroTrack stub throws exception | Log warning; treat as null (no result from AeroTrack) | — |
| QuickFlight stub throws exception | Log warning; treat as null (no result from QuickFlight) | — |
| Both providers return null (flight not found) | Return 200 with `status = Unknown` and message | 200 |
| Both providers throw exceptions | Return 200 with `status = Unknown` and message | 200 |
| Unexpected unhandled exception in API | Return 500 with generic `{ "error": "An unexpected error occurred." }` | 500 |

Provider exceptions are **never surfaced** to the caller. The API contract guarantees a `FlightStatusResult` (or a 400/500) — never a provider-specific error.

Both providers are queried **concurrently** using `Task.WhenAll` to minimise latency, even though stubs complete synchronously.

---

## 11. Deterministic Stub Scenarios

Stubs match on `flightNumber` (case-insensitive). Unrecognised flight numbers return `null` from both providers (→ `Unknown` result).

### 11.1 Scenario Table

| Scenario | FlightNumber | AeroTrack | QuickFlight | Expected Merged Status | Notes |
|----------|-------------|-----------|-------------|----------------------|-------|
| S1 — Both on time | `AA100` | ON_SCHEDULE, Δdep=+5min | on-time | **OnTime** | AeroTrack wins (later updated) |
| S2 — Both delayed | `AA200` | DELAYED, Δdep=+45min | delayed | **Delayed** | AeroTrack wins; delay reason present |
| S3 — Both cancelled | `AA300` | CANCELLED | cancelled | **Cancelled** | AeroTrack wins |
| S4 — AeroTrack only (diverted) | `AA400` | DIVERTED | _(null)_ | **Diverted** | Single provider |
| S5 — QuickFlight only (on time) | `AA500` | _(null)_ | on-time | **OnTime** | Single provider |
| S6 — No providers respond | `AA600` | _(null)_ | _(null)_ | **Unknown** | Message populated |
| S7 — ON_SCHEDULE but 20-min delta | `AA700` | ON_SCHEDULE, Δdep=+20min | on-time | **Delayed** | Time delta overrides raw label |
| S8 — QuickFlight wins (later ts) | `AA800` | on-time, updated 07:00 | on-time, updated 09:00 | **OnTime** (QF) | QuickFlight has later `lastUpdatedUtc` |
| S9 — Gate change, on time | `AA900` | ON_SCHEDULE, gate "C14" | on-time | **OnTime** | Gate/terminal present in result |
| S10 — Delayed with reason | `AA1000` | DELAYED, reason "Weather" | delayed | **Delayed** | `delayReason` = "Weather" |

### 11.2 AeroTrack Stub Data

```
AA100 → status=ON_SCHEDULE, schedDep=T+0:00, actDep=T+0:05, terminal=T1, gate=A10, delayReason=null, lastUpdated=T+1:00
AA200 → status=DELAYED,     schedDep=T+0:00, actDep=T+0:45, terminal=T2, gate=B5,  delayReason="Air Traffic Control", lastUpdated=T+0:45
AA300 → status=CANCELLED,   schedDep=T+0:00, actDep=null,   terminal=T3, gate=null, delayReason="Maintenance", lastUpdated=T-2:00
AA400 → status=DIVERTED,    schedDep=T+0:00, actDep=T+0:10, terminal=T1, gate=D3,  delayReason=null, lastUpdated=T+2:00
AA700 → status=ON_SCHEDULE, schedDep=T+0:00, actDep=T+0:20, terminal=T4, gate=E7,  delayReason=null, lastUpdated=T+0:30
AA800 → status=ON_SCHEDULE, schedDep=T+0:00, actDep=T+0:05, terminal=T1, gate=F1,  delayReason=null, lastUpdated=07:00 UTC fixed
AA900 → status=ON_SCHEDULE, schedDep=T+0:00, actDep=T+0:08, terminal=T5, gate=C14, delayReason=null, lastUpdated=T+0:50
AA1000→ status=DELAYED,     schedDep=T+0:00, actDep=T+1:30, terminal=T2, gate=B8,  delayReason="Weather", lastUpdated=T+1:00
(T = 2026-01-01T10:00:00Z as base; offsets are minutes)
```

### 11.3 QuickFlight Stub Data

```
AA100  → status=on-time,   schedDep=T+0:00, schedArr=T+2:00, lastUpdated=T+0:45
AA200  → status=delayed,   schedDep=T+0:00, schedArr=T+2:00, lastUpdated=T+0:30
AA300  → status=cancelled, schedDep=T+0:00, schedArr=T+2:00, lastUpdated=T-2:30
AA500  → status=on-time,   schedDep=T+0:00, schedArr=T+2:00, lastUpdated=T+0:20
AA700  → status=on-time,   schedDep=T+0:00, schedArr=T+2:00, lastUpdated=T+0:10
AA800  → status=on-time,   schedDep=T+0:00, schedArr=T+2:00, lastUpdated=09:00 UTC fixed
AA900  → status=on-time,   schedDep=T+0:00, schedArr=T+2:00, lastUpdated=T+0:30
AA1000 → status=delayed,   schedDep=T+0:00, schedArr=T+2:00, lastUpdated=T+0:45
```

---

## 12. Frontend States

The Angular UI has nine distinct states, each mapping to a specific UI representation.

| State | Trigger | Visual Treatment |
|-------|---------|-----------------|
| **Initial** | Page load, no search performed | Search form only; no result card visible |
| **Loading** | Search submitted; awaiting API response | Spinner or skeleton overlay on the form; search button disabled |
| **OnTime** | API returns `status = OnTime` | Green result card; all available fields displayed |
| **Delayed** | API returns `status = Delayed` | Amber result card; `delayReason` shown if non-null |
| **Cancelled** | API returns `status = Cancelled` | Red result card; `delayReason` shown if non-null |
| **Diverted** | API returns `status = Diverted` | Red result card; explicit "Diverted" badge |
| **Unknown** | API returns `status = Unknown` | Grey result card; `message` field displayed prominently |
| **Validation Error** | Client-side form validation fails | Inline field errors; no API call made |
| **API Error** | Non-2xx HTTP response or network failure | Red error banner with message; search form remains interactive |

### 12.1 Result Card Fields

| Field | Source | Shown when |
|-------|--------|-----------|
| Status badge | `status` | Always |
| Flight number | `flightNumber` | Always |
| Date | `date` | Always |
| Scheduled departure | `scheduledDeparture` | Non-null |
| Actual departure | `actualDeparture` | Non-null |
| Scheduled arrival | `scheduledArrival` | Non-null |
| Actual arrival | `actualArrival` | Non-null |
| Terminal | `terminal` | Non-null |
| Gate | `gate` | Non-null |
| Delay reason | `delayReason` | Non-null |
| Last updated | `lastUpdatedUtc` | Always (when status ≠ Unknown) |
| Source provider | `sourceProvider` | Always |
| Message | `message` | `status = Unknown` |

### 12.2 Colour Coding

| Status | Badge colour | CSS class |
|--------|-------------|-----------|
| OnTime | Green | `status-on-time` |
| Delayed | Amber / Orange | `status-delayed` |
| Cancelled | Red | `status-cancelled` |
| Diverted | Red | `status-diverted` |
| Unknown | Grey | `status-unknown` |

### 12.3 Form Validation (Client-side)

| Field | Rule | Error message |
|-------|------|--------------|
| Flight number | Required | "Flight number is required." |
| Flight number | Pattern `^[A-Za-z]{2,3}\d{1,4}$` | "Enter a valid flight number (e.g., BA123)." |
| Date | Required | "Date is required." |
| Date | Valid calendar date | "Enter a valid date." |

---

*End of specification.*
