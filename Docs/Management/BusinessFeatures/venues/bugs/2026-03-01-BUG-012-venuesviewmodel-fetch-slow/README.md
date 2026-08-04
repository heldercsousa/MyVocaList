---
id: BUG-012
title: "Bug: Venues list fetch slow — 2.2s paged query (BUG-012)"
status: "💡 Pending"
target: 2026-03
section: BusinessFeatures
kind: bug
order: 10
goal: "restore fast venue list loading (N+1 query suspected)."
pointer: BusinessFeatures/venues/bugs/2026-03-01-BUG-012-venuesviewmodel-fetch-slow/
---

> **Spec updated [2026-07-22]:** this file was the flat
> `bugs/BUG-012-venuesviewmodel-fetch-slow.md`; `git mv`-ed into a REQ-SEV-01 item folder
> (day `-01` per REQ-SEV-00, the row's target being the bare month `2026-03`). Content below is
> the original file byte-for-byte. `severity:` is deliberately unset: the legacy header records
> "Medium", which is not a value in `model.SEVERITIES`, and neither the BACKLOG row nor this file
> states a Critical/Major/Minor severity — inventing one would be fabrication (T11c).

# BUG-012 — VenuesViewModel fetch 2268ms — slow paged query

**Filed:** 2026-06-11
**Feature area:** Venues CRUD
**Severity:** Medium — 2.2s fetch on app start, visible shimmer delay for users with any venue data
**Status:** 💡 Pending
**Recommended model:** `claude-sonnet-4-6` — EF Core repository query rewrite; requires SQLite MCP + EF Core query analysis but no new architecture

## Symptom

Serilog instrumentation on Galaxy S23 Ultra (Release build) shows:

```
VenuesViewModel fetch=2268ms
```

The paged query that populates the venues list takes 2.2 seconds — far above the expected <100ms for a simple paged SQLite query on a modern device.

## Root cause (hypothesis)

`VenueRepository.GetPagedAsync` (or `GetPagedWithEventInfoAsync`) likely executes a correlated COUNT subquery per row (e.g. `SELECT COUNT(*) FROM QueueEntries WHERE VenueId = v.Id`) inside the EF Core LINQ projection. When EF Core translates this for SQLite, each row triggers a separate synchronous DB round-trip. With N venues on screen, this becomes N+1 queries.

Additionally, the Microsoft.Data.Sqlite provider executes "async" EF methods synchronously on the calling thread (no real async I/O). The `Task.Run` offload in `CrudListViewModelBase` moves this to the thread pool, but the query itself is still single-threaded and blocks the thread pool worker for 2.2s.

## Affected files (to investigate)

- `MyVocaList.Infra/Repositories/VenueRepository.cs`
- `MyVocaList.Services/VenueService.cs`

## Fix approach

1. Run `SELECT * FROM ...` equivalent via SQLite MCP (`.claude/MyVocaList.db`) to confirm actual venue count on the test device.
2. Enable EF Core query logging (one session) to capture the generated SQL and count round-trips.
3. If N+1 confirmed: rewrite the projection to use a single JOIN or a single GROUP BY COUNT — eliminate correlated subqueries.
4. If single query but slow: check for missing indexes on `VenueId` FK columns in join tables; add index if absent.
5. Measure with Serilog instrumentation post-fix: `VenuesViewModel fetch` should be ≤ 100ms.

## Acceptance criteria

- AC-BUG012-1: `VenuesViewModel fetch` Serilog log line shows ≤ 100ms on Galaxy S23 Ultra with the same dataset.
- AC-BUG012-2: EF Core query log shows a single SQL statement (no N+1 pattern) for the paged venue list.
- AC-BUG012-3: Venues list page behavior (data, pagination, search) is unchanged.

## Out of scope

- Other ViewModels with similar patterns (investigate separately once this is fixed)
- SQLite → MSSQL migration (tracked separately; this fix targets current SQLite infra)
