# AI Prompts Log

This file documents significant prompts used during development of the Flight Status Tracker, along with notes on key decisions made in response to AI output.

---

## Phase 1 — Analysis & Specification

**Prompt:** "Analyse the requirements from the challenge PDF and create a spec.md covering unified domain models, provider response models, the IFlightStatusProvider interface, status normalisation rules, merge rules, API contracts, validation rules, error-handling behaviour, deterministic stub scenarios, frontend states, and key assumptions."

**Decision notes:**
- Chose winner-takes-all merge strategy (not field-level blending) for simplicity and predictability.
- Defined time-delta check as `≤ 900 seconds = OnTime` to make "within 15 minutes" unambiguous at the boundary.
- Decided time delta overrides raw status label when actual times are present (more reliable data source).
- Added tie-break rule favouring AeroTrack when `lastUpdatedUtc` values are equal (AeroTrack has richer detail).

---

*(Additional prompts will be recorded here during implementation.)*
