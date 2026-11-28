---
id: research-shared-code-between-the-autocomplete-field-and-the-crud-list-inline-or-full-screen-configurable-tap
title: "Research: shared code between the autocomplete field and the CRUD list (inline or full-screen, configurable tap)"
status: ✅ Done
target: 2026-11-24
section: DevCycleCraft
parent: crud-list-deduplication
goal: Decide whether the autocomplete and the CRUD list should share code, given the Syncfusion migration.
gate: Decided by Helder; follow-ups are registered as their own items.
closed: 2026-11
kind: change
---

# Research: shared code between the autocomplete field and the CRUD list (inline or full-screen, configurable tap)

Decide whether the autocomplete and the CRUD list should share code, given the Syncfusion migration.

Research only. No code changes. Results: [findings.md](findings.md). Helder decided on 2026-11-24; see [Decision](#decision).

## Decision

Helder's decisions of 2026-11-24, after the follow-up discussion:

1. **No merged component and no shared search helper.** The autocomplete field and the CRUD list stay separate, as the findings recommend (section 9, option D). The configurable tap already exists in the form ViewModels.
2. **No full-screen search.** It is not wanted for now.
3. **No code changes now.** Another session is migrating DevExpress to Syncfusion in the same files. Every follow-up waits for the migration to land, and targets the Syncfusion versions.
4. **Follow-ups registered:**
   - Suspected races B6, B7 and B8: [BUG-089](../../../../BusinessFeatures/cross-cutting/bugs/2026-11-24-BUG-089-crud-list-search-load-more-and-pull-to-refresh-can-overwrite-newer-search-results-suspected-race/README.md), its own bug, not nested under another item.
   - Search errors, B5: [search error handling](../2026-11-24-search-error-handling-short-message-for-users-full-detail-for-developers-only/README.md). Users get no verbose errors; full detail is for developers only. Helder and Claude Code agree the approach in a discussion first.
   - Dead code, B12 and B11, with B1 and B2 in the same file: [remove dead search code](../2026-11-24-remove-dead-search-code-after-confirming-nothing-uses-it/README.md), only after confirming nothing uses it.
   - Timing tests: [timing tests for the autocomplete search](../2026-11-24-timing-tests-for-the-autocomplete-search/README.md).
   - Search delay, B3: [one search delay for all searches](../2026-11-24-one-search-delay-for-all-searches-300-ms/README.md). Helder allowed 300 ms or 400 ms if it makes sense; 300 ms was chosen because the autocomplete already has a named, tested 300 ms constant.
5. **Not scheduled:** B4 (possible SQLite on the UI thread; measure first), B9, B10 and B13 stay recorded in the findings only.

## Request

Helder's request of 2026-11-24, quoted word for word:

> I'm not sure you can just inspect files. If not, spawn an Opus model subagent to check the following research inside the app's 2 functionalities : I want to compare the DX autocomplete component we've being using inside @MyVocaList/UI/Pages/Songs/SongFormPage.xaml, in the Title field, to the @MyVocaList/UI/Pages/Base/CrudListPageBase.cs + @MyVocaList/UI/Components/CrudListView.xaml.cs  and @MyVocaList/UI/Components/CrudListView.xaml. I guess both have pratically the very same behavior, and may be a kind of oportunity for sharing code instead of duplicating some code portions that are, by the end, producing the same result. The differences I see in between autocomplete under usage and all crud list page are that crud list have an AppBar on top of screen, the bottom floating ToolBar, and the action performed when a listItem is picked (autocomplete loads the picked item into the form field(s) while the crud list executes another action, usually navigating to another page). Having that said, I would like to figure out if the is there any oportunity to clean the code and end up having an autocomplete component that is able to run in full screen, or inline like the current autocomplete component. Also, if would be possible to define how an item tap behaves, allowing to behave like the current autocomplete or like the current crud list view. Note that there is a massive work running now in parallel for migrating the DX components to SyncFusion components. It means that we're going start using the SyncFusion autocomplete over the current one based on the DX autocomplete component - so, perhaps it worths compare to syncfusion autocomplete, but having in mind that this one isn't tested yet and is still under migration process. So, you must decide what one will be the comparison target. Run it under plan mode. Also, such research must be registered under @Docs/Management/BACKLOG.md tasks, and must be recorded both my ask and the research results in the task dedicated folder. After your complete analysis, we're going to chat about it and decide if it worth to tackle around sharing code, and having a common source of code between them.
