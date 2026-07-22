---
id: BUG-026
title: "BUG-026: HWUI native crash (SIGABRT) on render teardown (Major)"
status: "💡 Pending"
severity: Major
target: 2026-07-03
section: BusinessFeatures
parent: cross-cutting
kind: bug
order: 410
goal: "confirm whether the crash is a real defect or debugger-teardown noise (Release logcat investigation first)."
pointer: BusinessFeatures/cross-cutting/bugs/BUG-026-hwui-sigabrt-render-teardown/
---

# BUG-026: HWUI native crash (SIGABRT) on render teardown

A native SIGABRT was observed during render teardown. The first step is a Release-build
logcat investigation to decide whether this is a real defect or debugger-teardown noise.
Detail: the bug note in this folder.

> **Spec updated [2026-07-22]:** the row's pointer moves from the shared cross-cutting log to this
> folder, and its parent grouping row now has its own README (Helder decision 4A,
> spec-evolution-versioning).
