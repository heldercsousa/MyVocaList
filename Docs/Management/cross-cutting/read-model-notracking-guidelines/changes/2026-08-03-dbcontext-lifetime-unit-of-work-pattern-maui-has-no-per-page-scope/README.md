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
