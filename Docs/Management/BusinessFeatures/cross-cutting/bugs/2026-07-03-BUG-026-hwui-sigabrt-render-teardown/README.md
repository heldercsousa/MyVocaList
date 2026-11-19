---
id: BUG-026
title: "BUG-026: HWUI native crash (SIGABRT) on render teardown (Major)"
status: 🔵 Superseded
severity: Major
target: 2026-07-03
section: BusinessFeatures
parent: cross-cutting
goal: confirm whether the crash is a real defect or debugger-teardown noise (Release logcat investigation first).
pointer: BusinessFeatures/cross-cutting/bugs/2026-07-03-BUG-026-hwui-sigabrt-render-teardown/
closed: 2026-11
order: 410
kind: bug
---

# BUG-026: HWUI native crash (SIGABRT) on render teardown

A native SIGABRT was observed during render teardown. The first step is a Release-build
logcat investigation to decide whether this is a real defect or debugger-teardown noise.
Detail: the bug note in this folder.

> **Spec updated [2026-07-22]:** the row's pointer moves from the shared cross-cutting log to this
> folder, and its parent grouping row now has its own README (Helder decision 4A,
> spec-evolution-versioning).


## Observation

Closed on 2026-11-16 at Helder's explicit request during the backlog cleanup. It was never reproduced in a Release build. Reopen it by registering a new item if the work is wanted again.
