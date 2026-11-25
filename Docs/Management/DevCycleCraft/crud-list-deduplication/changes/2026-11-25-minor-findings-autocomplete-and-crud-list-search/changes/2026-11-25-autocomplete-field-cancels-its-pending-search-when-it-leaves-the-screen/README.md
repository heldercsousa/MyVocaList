---
id: autocomplete-field-cancels-its-pending-search-when-it-leaves-the-screen
title: Autocomplete field cancels its pending search when it leaves the screen
status: 🟡 In Progress
target: 2026-11-25
section: DevCycleCraft
parent: minor-findings-autocomplete-and-crud-list-search
goal: A pending autocomplete search is cancelled when the field is unloaded.
gate: Tests pass, or the Syncfusion field is shown not to have the defect.
kind: change
---

# Autocomplete field cancels its pending search when it leaves the screen

A pending autocomplete search is cancelled when the field is unloaded.

Minor finding B13 of the code-sharing research of 2026-11-24 ([findings.md](../../../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5). Registered on Helder's request of 2026-11-25, nested under [Minor findings: autocomplete and CRUD list search](../../README.md).

The DevExpress field does not cancel a pending search on Unloaded. Check and fix the Syncfusion field (`AutocompleteFieldSf`).
