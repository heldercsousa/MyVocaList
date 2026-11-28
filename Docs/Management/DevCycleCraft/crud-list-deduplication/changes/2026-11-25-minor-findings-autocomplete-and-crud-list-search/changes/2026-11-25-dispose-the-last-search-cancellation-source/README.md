---
id: dispose-the-last-search-cancellation-source
title: Dispose the last search cancellation source
status: ✅ Done
target: 2026-11-25
section: DevCycleCraft
parent: minor-findings-autocomplete-and-crud-list-search
goal: Every search cancellation source is disposed when it is replaced and when its owner goes away.
gate: Tests pass.
closed: 2026-11
kind: change
---

# Dispose the last search cancellation source

Every search cancellation source is disposed when it is replaced and when its owner goes away.

Minor finding B9 of the code-sharing research of 2026-11-24 ([findings.md](../../../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5). Registered on Helder's request of 2026-11-25, nested under [Minor findings: autocomplete and CRUD list search](../../README.md).

The CRUD list `_searchCts` and the venue map `_tapCts` never dispose the last source.

## Decision (2026-11-25)

Each cancellation source is released when its own search ends, including the last one. Releasing it when the page closes would need page lifecycle hooks, which is more than this minor finding justifies: no source outlives its search.

Decided by Claude Code on 2026-11-25 under Helder's autonomy grant of the same day ("proceed with every needed step"); Helder may revisit it.
