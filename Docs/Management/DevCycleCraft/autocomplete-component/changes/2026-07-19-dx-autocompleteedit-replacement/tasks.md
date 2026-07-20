# Tasks — DX `AutoCompleteEdit` replacement

> Ordering: API pinning first (doc-source gap), then one XAML file per task (incremental-edits constraint), exclusions last before verification. All code tasks run in a worktree branched from `develop` (Rule 2).

- [ ] **T1 — Pin DX 25.2.4 `AutoCompleteEdit` API surface** `[SPIKE-lite]`
  Produces: `findings.md` section — exact member names for async suggestions (provider/event), suggestion delay/debounce, client-filter disable, error display, item template, text-retention behavior. Sources: DevExpress MCP (verify index health first — MCP Availability Gate), Context7 (version-pinned), installed 25.2.4 package XML docs. Escalate to Helder if unconfirmable. Consumes: —. Risk: doc gap. Files owned: `findings.md`. Demo: `findings.md` contains a pinned-name table + explicit Option A/B wiring decision. Review lane: Standard.
- [ ] **T2 — MaterialStyles.xaml: AutoCompleteEdit form style** (REQ-DXAC-12)
  Consumes: T1. Files owned: `MyVocaList/Resources/Styles/MaterialStyles.xaml`. Demo: style resource compiles; matches Outlined TextEdit convention. Review lane: Standard.
- [ ] **T3 — SongFormPage Artist field swap** (REQ-DXAC-01/03/04/05/06/07)
  Consumes: T1, T2. Files owned: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` (+ `SongFormViewModel.cs` only if debounce glue is needed — with unit tests). Demo: typing shows artist suggestions; selection locks field; blur validation intact; typed text never cleared. Review lane: Elevated (constitutional UI constraints + BUG-027 criticality).
- [ ] **T4 — PersonFormPage Full Name field swap** (REQ-DXAC-02/03/04/05/06/07)
  Consumes: T3 (pattern proven). Files owned: `MyVocaList/UI/Pages/People/PersonFormPage.xaml`, `MyVocaList/UI/Pages/People/PersonFormPage.xaml.cs` (+ `PersonFormViewModel.cs` same condition). Demo: dedup suggestions from 2 chars; selection + blur validation intact. Review lane: Elevated (BUG-044/045/047 residual-defect surface).
- [ ] **T5 — Exclude frozen component family from build** (REQ-DXAC-11)
  Consumes: T3, T4 (no remaining references). Files owned: `MyVocaList/MyVocaList.csproj`, `MyVocaList.Tests/MyVocaList.Tests.csproj`, new `UI/Components/AutocompleteField/README-FROZEN.md`. Demo: solution builds 0 errors; 6 component test files no longer executed (record the before/after test-count delta in the task-log as evidence). Review lane: Standard.
- [ ] **T6 — Full test suite + BUG-044/045/047 evaluation checklist** (REQ-DXAC-08/09)
  Consumes: T5. `dotnet test` unchanged VM suites green; write the on-device evaluation checklist into `task-log.md` for Helder's run (include an explicit REQ-DXAC-06 item: suggestions shown exactly as the Service returned, e.g. diacritic-mismatch query); register BUG rows + regression tests for any survivor. Review lane: Standard.
- [ ] **T7 — Helder device verification: smoke 16C.1 + checklist run** (REQ-DXAC-09/10) `[MANUAL — Helder]`
  Consumes: T6. Green → BACKLOG updates: this row ✅; BUG-027 unblocked (fix direction satisfied — re-verify BUG-027 symptoms in 16C.1); residual-evaluation row closed with results.
