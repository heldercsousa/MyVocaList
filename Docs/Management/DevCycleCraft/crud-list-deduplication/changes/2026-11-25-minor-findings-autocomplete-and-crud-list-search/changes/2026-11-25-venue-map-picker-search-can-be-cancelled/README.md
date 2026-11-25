---
id: venue-map-picker-search-can-be-cancelled
title: Venue map picker search can be cancelled
status: 🟡 In Progress
target: 2026-11-25
section: DevCycleCraft
parent: minor-findings-autocomplete-and-crud-list-search
goal: A venue map search is cancelled by a newer map tap or search and when the page is left.
gate: Tests pass.
kind: change
---

# Venue map picker search can be cancelled

A venue map search is cancelled by a newer map tap or search and when the page is left.

Minor finding B10 of the code-sharing research of 2026-11-24 ([findings.md](../../../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5). Registered on Helder's request of 2026-11-25, nested under [Minor findings: autocomplete and CRUD list search](../../README.md).

`VenueMapPickerViewModel.cs:138-169`: the submit search has no cancellation. A map tap during a search gets overwritten, and leaving the page does not cancel it.
