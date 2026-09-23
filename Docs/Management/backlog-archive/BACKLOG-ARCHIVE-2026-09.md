# BACKLOG Archive — 2026-09

> Closed backlog rows completed in 2026-09, moved out of `Docs/Management/BACKLOG.md`. Rows use the slim PO template: Goal + one-sentence outcome + pointer. **Past BUG-NNN / feature lookups must grep all `backlog-archive/` files.**

## Business Features

<!-- BACKLOG:GENERATED:BEGIN archive-business -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
<!-- BACKLOG:GENERATED:END archive-business -->

## Dev Cycle Craft

<!-- BACKLOG:GENERATED:BEGIN archive-craft -->
| Target | Feature/Item | Status | Notes |
|--------|--------------|--------|-------|
| 2026-07-15 | **String trimming on persistence — centralized normalization analysis** | ✅ Done | Goal: strings persisted to the DB should be trimmed (extension of BUG-046's query-side trimming) via one centralized Services-layer helper (search) + EF Core `ValueConverter`s (persistence). Gate: All code merged to develop 2026-08-04 — search normalization and the persistence ValueConverters are both live. Only Helder's on-device E2E sign-off remains before this goes terminal. Pointer: `DevCycleCraft/persisted-string-trimming/`. |
| 2026-08-24 | Scope all service reads through IUnitOfWork (under: **Read Model + Global NoTracking Pattern — Guidelines Update**) | ✅ Done | Goal: Route every service read path through IUnitOfWork so no read touches the app-lifetime AppDbContext, fixing BUG-078 and satisfying REQ-UOW-29 rationale (1). Gate: Helder approves the spec; then reads scoped, BUG-078 regression test red-then-green, page-load-frozen suite green without DbLoadGate, and Phase 4.7 closed or formally deferred. Pointer: `cross-cutting/read-model-notracking-guidelines/changes/2026-08-24-scope-all-service-reads-through-iunitofwork/`. |
<!-- BACKLOG:GENERATED:END archive-craft -->
