---
id: scope-all-service-reads-through-iunitofwork
title: Scope all service reads through IUnitOfWork
status: 📋 Spec
target: 2026-08-24
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: Route every service read path through IUnitOfWork so no read touches the app-lifetime AppDbContext, fixing BUG-078 and satisfying REQ-UOW-29 rationale (1).
gate: Helder approves the spec; then reads scoped, BUG-078 regression test red-then-green, page-load-frozen suite green without DbLoadGate, and Phase 4.7 closed or formally deferred.
kind: change
---

# Scope all service reads through IUnitOfWork

Route every service read path through IUnitOfWork so no read touches the app-lifetime AppDbContext, fixing BUG-078 and satisfying REQ-UOW-29 rationale (1).
