---
id: measure-whether-autocomplete-field-searches-block-the-ui-thread
title: Measure whether autocomplete field searches block the UI thread
status: 🟡 In Progress
target: 2026-11-25
section: DevCycleCraft
parent: minor-findings-autocomplete-and-crud-list-search
goal: Field searches are measured on device; they are moved off the UI thread only if they block it.
gate: Measured on device first.
kind: change
---

# Measure whether autocomplete field searches block the UI thread

Field searches are measured on device; they are moved off the UI thread only if they block it.

Minor finding B4 of the code-sharing research of 2026-11-24 ([findings.md](../../../2026-11-24-research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap/findings.md), section 5). Registered on Helder's request of 2026-11-25, nested under [Minor findings: autocomplete and CRUD list search](../../README.md).

No `Task.Run`, and `UnitOfWork.ExecuteReadAsync` does not offload. By the SQLITE-WORKAROUND note (`CrudListViewModelBase.cs:189-193`), the field option sources may run SQLite on the UI thread. Measure first; offload only if there is visible jank.
