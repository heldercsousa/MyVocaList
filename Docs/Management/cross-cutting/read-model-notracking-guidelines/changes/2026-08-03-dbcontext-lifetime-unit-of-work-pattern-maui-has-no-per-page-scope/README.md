---
id: dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope
title: DbContext lifetime & unit-of-work pattern — MAUI has no per-page scope
status: 📋 Spec
target: 2026-08-03
section: DevCycleCraft
parent: read-model-notracking-guidelines
goal: AddDbContext registers Scoped but MAUI never creates a scope, so one AppDbContext lives for the whole app session and leaks tracked entities between operations (root cause of BUG-068). Establish one correct unit-of-work pattern with minimal repeated code.
gate: Helder's approval of the revised spec, then a plan and plan-review before any implementation.
kind: change
---

# DbContext lifetime & unit-of-work pattern — MAUI has no per-page scope

AddDbContext registers Scoped but MAUI never creates a scope, so one AppDbContext lives for the whole app session and leaks tracked entities between operations (root cause of BUG-068). Establish one correct unit-of-work pattern with minimal repeated code.

> **Spec APPROVED by Helder 2026-08-04.** See `requirements.md` Status banner for the full decision list. Two decisions applied on approval: (1) `IUnitOfWork.ExecuteAsync` opens an explicit transaction, replacing the earlier `ExecuteUpdateAsync`/`ExecuteDeleteAsync` atomicity carve-out (REQ-UOW-33, `design.md § 8`); (2) every remaining "REQUIRES HELDER'S CONFIRMATION" marker in `requirements.md`/`design.md` is now "APPROVED by Helder 2026-08-04" — this does not change REQ-UOW-11's substance, which has a separate open question with Helder.
