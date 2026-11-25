---
id: dispose-the-last-search-cancellation-source
title: Dispose the last search cancellation source
status: 🟡 In Progress
target: 2026-11-25
section: DevCycleCraft
parent: minor-findings-autocomplete-and-crud-list-search
goal: Every search cancellation source is disposed when it is replaced and when its owner goes away.
gate: Tests pass.
kind: change
---

# Dispose the last search cancellation source

Every search cancellation source is disposed when it is replaced and when its owner goes away.

Minor finding B9 of the code-sharing research of 2026-11-24 ([findings.md](../../../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5). Registered on Helder's request of 2026-11-25, nested under [Minor findings: autocomplete and CRUD list search](../../README.md).

The CRUD list `_searchCts` and the venue map `_tapCts` never dispose the last source.
