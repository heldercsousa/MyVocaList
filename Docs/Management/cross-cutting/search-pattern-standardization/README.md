---
id: search-pattern-standardization
title: **Search Pattern Standardization + Navigation Result Service**
status: 🔵 Superseded
target: 2026-06
section: DevCycleCraft
goal: reconcile the app's two search patterns into one canonical choice + migration plan.
gate: blocks any new search surface until at least 📋 Spec.
pointer: cross-cutting/search-pattern-standardization/
closed: 2026-08
order: 60
kind: feature
---

# Search Pattern Standardization + Navigation Result Service

Migrated from the pre-migration BACKLOG.md row (no prior spec folder).

Back-reference: `Docs/Management/cross-cutting-log.md` (retained; not migrated).

> **Closed 🔵 Superseded [2026-08-04] -- cancelled (Helder).** Retired without being executed.
> Its stated gate -- *"blocks any new search surface until at least Spec"* -- is void: it did
> not in fact block anything, and search surfaces have since been standardised in practice by
> the AppBar / SearchAppBar Interaction Redesign, which made a persistent MD3 `SearchBar` in
> `CrudListView` the single search pattern and added `SearchBar` to the governed-component
> list.
>
> If a genuine second search pattern reappears, register a fresh item against the state of the
> code at that time rather than reviving this one.
