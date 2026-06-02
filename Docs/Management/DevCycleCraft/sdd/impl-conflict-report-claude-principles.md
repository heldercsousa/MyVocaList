# Conflict Report — CLAUDE.md + code-principles.md Analysis
**Date:** 2026-05-06
**Analyst:** Agent C
**Scope:** `CLAUDE.md` (project root) and `.claude/rules/code-principles.md` — internal duplications/contradictions/inconsistencies within each file, cross-file analysis between the two, and cross-reference against already-captured findings in `conflict_report_workflow.md` and `conflict_report_review_testing.md`.

Files already captured in prior reports are **not** repeated. Where a finding extends a prior finding, it is noted.

---

## Summary
**9 duplications, 3 contradictions, 5 inconsistencies, 3 structural issues**

---

## Duplications

### D-01: Architecture constraints duplicated verbatim across CLAUDE.md and code-principles.md
- **Location 1:** `CLAUDE.md § Architecture` — "Business logic **only** in Services", "Repository interfaces in **Domain** — implementations in **Infra**", "Only MAUI depends on Infra", "Services depends only on Domain interfaces", "DTOs as records in Contracts"
- **Location 2:** `code-principles.md § Architecture Constraints` — near-identical list with minor wording variations ("Services depend only on Domain interfaces — never on Infra directly", "DTOs are records in the **Contracts** project")
- **Assessment:** CLAUDE.md states these as the project architecture; code-principles.md re-states them as coding rules. The same rules appear in two files that are both always loaded. Any update to one must be mirrored to the other or drift occurs.
- **Recommended action:** Keep the authoritative statement in `CLAUDE.md § Architecture` (constitutional document). In `code-principles.md § Architecture Constraints`, replace the list with a single line: "Architecture layer constraints are defined in `CLAUDE.md § Architecture` — they apply equally to code." This eliminates the maintenance surface without losing the rule.

### D-02: "Language: English only" stated in two files
- **Location 1:** `CLAUDE.md § Constitutional Constraints` — "Language: Code, comments, logs, UI text — English only. Translate any non-English text immediately."
- **Location 2:** `code-principles.md § Language` — "All code, comments, logs, and UI text must be English only."
- **Assessment:** Near-identical rules in two files. CLAUDE.md is the constitutional document and the canonical home for Constitutional Constraints. code-principles.md repeats it without adding new content.
- **Recommended action:** Remove `code-principles.md § Language`. Add a line at the top of code-principles.md: "Language rule: see `CLAUDE.md § Constitutional Constraints` (English only — applies to all code in this file)." The rule enforcement is already in review.md + hooks via CLAUDE.md; the code-principles.md copy adds no enforcement value.

### D-03: "DisplayAlert prohibition" stated twice within CLAUDE.md
- **Location 1:** `CLAUDE.md § Constitutional Constraints` — "Native dialogs: NEVER use `DisplayAlert`, `DisplayActionSheet`, `DisplayPromptAsync`. Use `dx:BottomSheet` — see `myvocalist-coding` skill."
- **Location 2:** `CLAUDE.md § Rule Authority Hierarchy — Unamendable constraints` — "Never use `DisplayAlert` for dialogs"
- **Assessment:** Both lines are in the same file, eleven lines apart. The Constitutional Constraints entry is the full statement with reason; the Unamendable entry is a summary. This creates two places to update if the rule changes.
- **Recommended action:** In the Unamendable constraints list, replace the repeated text with "See Constitutional Constraints — Native dialogs rule." The list communicates the tier; the full rule stays in Constitutional Constraints.

### D-04: "DevExpress first" stated twice within CLAUDE.md
- **Location 1:** `CLAUDE.md § Constitutional Constraints` — "UI Component Priority: DevExpress first, always. Use stock MAUI only when DevExpress has no equivalent."
- **Location 2:** `CLAUDE.md § Rule Authority Hierarchy — Unamendable constraints` — "DevExpress components before stock MAUI"
- **Assessment:** Same pattern as D-03. Constitutional Constraints is the authoritative form.
- **Recommended action:** Same pattern as D-03 — the Unamendable list entry should reference the Constitutional Constraints entry rather than restate it.

### D-05: "Business logic in Services" stated in four locations
- **Location 1:** `CLAUDE.md § Architecture` — "Business logic **only** in Services"
- **Location 2:** `CLAUDE.md § Constitutional Role` — "Business logic only in Services" listed as a convention to verify before writing any spec
- **Location 3:** `CLAUDE.md § Rule Authority Hierarchy — Unamendable constraints` — "Business logic lives in Services only"
- **Location 4:** `code-principles.md § Architecture Constraints` — "Business logic lives in **Services** only — never in ViewModels or pages"
- **Assessment:** The most-repeated rule in the project. Four statements all say the same thing. The first (Architecture section) is canonical; the other three are references with different emphasis.
- **Recommended action:** Apply the same resolution as D-01 and D-03: keep the canonical statement in `CLAUDE.md § Architecture`. Make all other locations explicit references: "Business logic constraint: see `CLAUDE.md § Architecture`." The Unamendable list may retain the rule name for scannability, but add "(defined in § Architecture)."

### D-06: EF Core / SQLite constraints duplicated across code-principles.md and constraints-registry.md
- **Location 1:** `code-principles.md § EF Core / SQLite` — "`__EFMigrationsLock` row is cleared before each `MigrateAsync()` call (SQLite single-user workaround)", "`CollationInterceptor` applied globally for case-insensitive search"
- **Location 2:** `.claude/rules/constraints-registry.md § EF Core / SQLite` — "MigrationsLock: The `__EFMigrationsLock` row must be cleared before each `MigrateAsync()` call", "CollationInterceptor: Must be applied globally for case-insensitive search"
- **Assessment:** Both files document the same two EF Core constraints. code-principles.md adds a third bullet (migrations via `Task.Run`). constraints-registry.md adds "First-run table absence" catch. Neither file is a complete subset of the other. An agent reading only one file gets an incomplete picture.
- **Recommended action:** Consolidate under constraints-registry.md (it is the designated home for discovered runtime constraints). In code-principles.md, replace `§ EF Core / SQLite` with: "EF Core / SQLite constraints: see `.claude/rules/constraints-registry.md § EF Core / SQLite`."

### D-07: ObservableRangeCollection rules duplicated across code-principles.md and constraints-registry.md
- **Location 1:** `code-principles.md § UI Thread Performance — ObservableRangeCollection` — full section with rules and code examples (correct and wrong patterns)
- **Location 2:** `.claude/rules/constraints-registry.md § DevExpress / UI` — "DXCollectionView reset events: `ReplaceRange` and `ClearRange` each fire `CollectionChanged(Reset)`, triggering a full re-render … Never call both in the same `RunOnUiThread` block"
- **Assessment:** The same constraint lives in two places. code-principles.md has the detailed version with code examples. constraints-registry.md has the summary. The summary adds no content not in code-principles.md.
- **Recommended action:** Remove the constraints-registry.md entry for DXCollectionView reset events and add: "ObservableRangeCollection / DXCollectionView reset: see `code-principles.md § UI Thread Performance`." The detailed form belongs in code-principles.md; constraints-registry.md should cross-reference it rather than duplicate it.

### D-08: "DTOs are records in Contracts" duplicated
- **Location 1:** `CLAUDE.md § Architecture` — "DTOs as records in Contracts"
- **Location 2:** `code-principles.md § Architecture Constraints` — "DTOs are records in the **Contracts** project"
- **Assessment:** Part of the broader D-01 finding (architecture constraints duplicated). Calling it out separately because it is a design decision that warrants a single canonical statement.
- **Recommended action:** Resolved by the same action as D-01 — code-principles.md defers to CLAUDE.md Architecture for all layer constraints.

### D-09: CLAUDE.md testing.md reference perpetuates historical "Step 3" language
- **Location:** `CLAUDE.md § Rules Files` — "testing.md — read before writing any test or setting up the test project. Covers test types, naming, TDD workflow, and **prerequisites for Step 3**."
- **Prior capture:** `conflict_report_review_testing.md I-03` recommends removing the "Step 3" preamble from testing.md itself.
- **Assessment:** When testing.md is updated to remove its historical step references (per I-03 recommendation), CLAUDE.md's pointer will reference a section that no longer exists. The CLAUDE.md description will become stale simultaneously.
- **Recommended action:** Update the CLAUDE.md reference to: "testing.md — read before writing any test. Covers test types, naming, TDD workflow, and test project setup." Remove the "Step 3" qualifier now, regardless of when testing.md is updated.

---

## Contradictions

### C-01: Constitutional Constraints list and Unamendable constraints list overlap without a stated selection criterion
- **Rule A:** `CLAUDE.md § Constitutional Constraints` — 6 items: Language, Native dialogs, UI Component Priority, MD3 terminology, SafeAreaEdges, Incremental edits. Header: "Enforced via `review.md` checklist + hooks — these are not advisory."
- **Rule B:** `CLAUDE.md § Rule Authority Hierarchy — Unamendable constraints` — 3 items: "Business logic lives in Services only", "Never use `DisplayAlert`", "DevExpress components before stock MAUI." Header: "require architecture review to change."
- **Assessment:** The Unamendable list contains only 2 of the 6 Constitutional Constraints (DisplayAlert and DevExpress). It also includes one item *not* in Constitutional Constraints (Business logic in Services). Agents reading both sections cannot determine: (a) why Language, MD3 terminology, SafeAreaEdges, and Incremental edits are Constitutional but not Unamendable, and (b) why Business logic is Unamendable but not Constitutional. There is no stated selection criterion that explains the asymmetry.
- **Recommended action:** Merge the two concepts. Either: (a) extend Constitutional Constraints to be the single list, adding `[Unamendable]` markers to those 3 items, and adding "Business logic in Services" to the list; or (b) define "Constitutional Constraint" ≡ "Unamendable" and verify all 6 + 1 = 7 items appear in both lists. Add a sentence explaining what makes a rule unamendable vs. a regular constitutional rule.

### C-02: "Spec Language — Determinism" is a spec-writing rule that lives in a coding standards file
- **Rule A:** `code-principles.md § Spec Language — Determinism` — "In spec files (`requirements.md`, `design.md`) and in task descriptions, vague quality adjectives are **prohibited**." Contains the prohibited terms list and table of replacements.
- **Rule B:** `workflow.md` is the canonical home for spec-writing rules. All spec quality gates, spec format rules, and spec vocabulary rules are documented there.
- **Assessment:** An agent loading code-principles.md for coding guidance will find spec-writing rules mixed in. An agent loading workflow.md for spec guidance will miss the prohibited-terms rule unless also loading code-principles.md. The rule's location (code-principles.md) does not match its subject (spec quality).
- **Recommended action:** Move `§ Spec Language — Determinism` from code-principles.md to workflow.md Rule 1 (Spec-First) — specifically into or adjacent to the "Spec quality four-gate review / Testability gate" or "Acceptance criteria format" sections where spec language quality is already discussed. In code-principles.md, add a one-line reference: "Spec language determinism rules (prohibited terms): see `workflow.md § Spec quality four-gate review`."

### C-03: `/project:review` trigger scope in CLAUDE.md is broader than the subagent exit checklist
- **Rule A:** `CLAUDE.md § Commands` — "Review: `/project:review` — run after every completed task **and after creating or updating any spec or plan file**"
- **Rule B:** `workflow.md` subagent exit checklist (8 steps) — no mention of `/project:review`
- **Prior capture:** `conflict_report_review_testing.md C-03` identifies the "every completed task" discrepancy between CLAUDE.md and the subagent exit checklist.
- **Assessment:** This report extends the prior finding: CLAUDE.md adds a second trigger ("creating or updating any spec or plan file") that is not captured anywhere in the workflow. Neither the main agent session protocol nor the subagent exit checklist mentions running review after spec file changes. The trigger is stated only in CLAUDE.md and has no corresponding enforcement mechanism.
- **Recommended action:** The "spec or plan file" trigger should be added to the subagent exit checklist's Living spec check step, or explicitly assigned as a main-agent responsibility in workflow.md Rule 3a (Session-End Spec Update Ritual). Without a location in the workflow, this trigger will be missed. (The "every completed task" half is already tracked in prior report C-03.)

---

## Inconsistencies

### I-01: "Prefer composition over inheritance" lives in CLAUDE.md Architecture but not in code-principles.md
- **Occurrence:** `CLAUDE.md § Architecture` — "Prefer composition over inheritance"
- **Assessment:** All other coding principles (naming, async, return patterns, exception handling, usings, pagination, DI, UI thread) are in code-principles.md. This design principle is stranded in CLAUDE.md with architecture-layer rules, making it easy to miss when reading code-principles.md for coding guidance.
- **Recommended action:** Move "Prefer composition over inheritance" to `code-principles.md § C# Style` (or as a new "Design Principles" subsection). Remove it from the CLAUDE.md Architecture list (where it is the only stylistic rule among structural rules).

### I-02: "Incremental edits" is a Constitutional Constraint with no elaboration anywhere in the rules files
- **Occurrence:** `CLAUDE.md § Constitutional Constraints` — "Incremental edits: For XAML/UI work, edit ONE file → build → fix → then next file."
- **Assessment:** All other Constitutional Constraints are either elaborated in code-principles.md (Language → § Language section) or in constraints-registry.md (SafeAreaEdges) or point to a skill. "Incremental edits" has no elaboration, no rationale beyond the inline sentence, and no cross-reference to any tool or skill that enforces or explains it. It is also absent from the constraints-registry.md and code-principles.md.
- **Recommended action:** Add a brief elaboration to constraints-registry.md: "Incremental XAML edits: Edit one XAML file → build → fix before editing the next. Rationale: XAML parser errors cascade across files, making the source of the error ambiguous when batched." This makes the constraint discoverable alongside other runtime/build constraints.

### I-03: code-principles.md § Global Usings lists specific namespace names that will drift from the codebase
- **Occurrence:** `code-principles.md § Global Usings` — full namespace lists for each project (e.g., `MyVocaList.UI.Pages.*`, `MyVocaList.Navigation`).
- **Assessment:** These lists are accurate as of the file's last edit, but will drift as new namespaces are added or existing ones are renamed. There is no "verify against current GlobalUsings.cs before using" note. An agent reading this section may add usings that conflict with the actual GlobalUsings.cs, or miss recently-added ones.
- **Recommended action:** Add a note after each list: "Verify against the project's `GlobalUsings.cs` — this list is a snapshot, not the authoritative source." The rule for when to use GlobalUsings.cs (2+ types in one project) is the stable part; the specific namespace lists are volatile.

### I-04: "Spec Quality Check (Rebuild Test)" in CLAUDE.md and "Rebuild test — feature close-out spec quality check" in workflow.md use different activation phrasing
- **Location 1:** `CLAUDE.md § Spec Quality Check (Rebuild Test)` — "When closing out a feature, ask: 'Could a fresh agent regenerate this feature from the spec files + test suite alone…'"
- **Location 2:** `workflow.md § Rebuild test` — "When a feature is considered complete (all tasks checked in `tasks.md`, final review passed), perform the rebuild test."
- **Prior capture:** `conflict_report_workflow.md D-12` identifies the "Regeneration test practice" / "Rebuild test" duplication within workflow.md. CLAUDE.md's version is a third partial statement of the same concept.
- **Assessment:** CLAUDE.md frames the test as a question; workflow.md frames it as a formal protocol. CLAUDE.md's description adds "test suite" as a context source which is not mentioned in workflow.md's protocol. This is a minor inconsistency in scope (spec only vs spec + test suite). CLAUDE.md correctly says "See `workflow.md` for the full rebuild test protocol," which is the right cross-reference.
- **Recommended action:** Add "and the test suite" to the workflow.md rebuild test description (as CLAUDE.md correctly includes this). Remove the question framing from CLAUDE.md and replace with: "When closing out a feature, run the Rebuild Test (see `workflow.md § Rebuild test — feature close-out spec quality check`). Include the test suite as context alongside the spec files." This makes the CLAUDE.md entry a pointer only.

### I-05: `CLAUDE.md § Continuous Enhancement` and `CLAUDE.md § Amending These Rules` describe overlapping update processes without cross-referencing each other
- **Location 1:** `CLAUDE.md § Continuous Enhancement` — "After every task, always ask: 'What was learned…?' Add, Update, Replace, Delete rules."
- **Location 2:** `CLAUDE.md § Amending These Rules` — formal process: document what's wrong, note backward compatibility, commit with `amend:` prefix, update changelog.
- **Assessment:** Both sections describe how to update CLAUDE.md and rules files. Continuous Enhancement is an informal ongoing practice; Amending These Rules is a formal process with a commit convention and changelog requirement. Neither section references the other. An agent following only Continuous Enhancement could update rules without the `amend:` prefix or changelog entry required by the formal amendment process.
- **Recommended action:** Add a cross-reference to Continuous Enhancement: "Note: changes to CLAUDE.md or `.claude/rules/*.md` must follow the Amending These Rules process (see below) — including `amend:` commit prefix and changelog entry." This connects the informal "when to update" with the formal "how to update."

---

## Structural Issues

### S-01: code-principles.md has no table of contents or internal cross-references, making it hard to navigate from CLAUDE.md's § Constitutional Role
- **Issue:** `CLAUDE.md § Constitutional Role` says: "Before writing any spec, verify that the proposed design is consistent with … Naming conventions (entities, services, ViewModels, commands) … DI registration rules (Singleton / Scoped / Transient) … Error handling idioms (tuple returns, no exceptions for business failures)." None of these items have a pointer to their location in code-principles.md. A reader must know that naming is in `§ C# Style / Naming`, DI is in `§ DI Registration Conventions`, and error handling is in `§ Exception Handling` and `§ C# Style / Service Return Patterns`.
- **Recommended action:** Add file + section references to each item in `§ Constitutional Role`: "Naming conventions — see `code-principles.md § C# Style / Naming`", "DI registration rules — see `code-principles.md § DI Registration Conventions`", etc. This makes the constitutional verification process actionable rather than conceptual.

### S-02: CLAUDE.md § Constitutional Constraints and § Rule Authority Hierarchy are structurally adjacent but describe overlapping domains without stated relationship
- **Issue:** The two sections are 5 lines apart. Constitutional Constraints defines 6 enforced rules. Rule Authority Hierarchy defines the layer structure and lists 3 Unamendable constraints. A reader encounters the 6 rules, then a table about layers, then 3 rules that partially overlap the 6. There is no explanation of the relationship: are the Unamendable rules a subset of Constitutional Constraints? Are they a different tier? The sections don't reference each other.
- **Recommended action:** Add a sentence at the start of the Unamendable constraints subsection: "The following Constitutional Constraints additionally require architecture review to change — they are not relaxable by Helder alone." Then ensure the list matches the full Constitutional Constraints list (or explain why it doesn't).

### S-03: "Spec Language — Determinism" in code-principles.md is the only non-coding rule in a coding standards file
- **Issue:** code-principles.md covers: Language, Spec Language (for spec files), XML doc comments, Nullable types, Architecture Constraints, C# Style, Exception Handling, Global Usings, Pagination, DI, UI Thread, EF Core, Static Analysis. Every section except "Spec Language — Determinism" is about code. The spec language section is about spec quality — it governs what agents write in requirements.md and design.md, not what they write in .cs files.
- **Assessment:** This is the structural dimension of C-02. A file named/described as "code principles" should contain code principles. The spec language rule's presence here is a structural misfit that may cause it to be missed by agents loading code-principles.md for coding guidance but not for spec writing.
- **Recommended action:** Move `§ Spec Language — Determinism` to workflow.md (as recommended in C-02) and update code-principles.md's header comment to accurately describe its scope.

---

## Priority Summary

| ID | File(s) | Type | Severity | Recommended action |
|----|---------|------|----------|--------------------|
| C-01 | CLAUDE.md | Contradiction | High | Merge Constitutional Constraints and Unamendable list; add selection criterion |
| C-02 | code-principles.md + workflow.md | Contradiction | High | Move "Spec Language — Determinism" to workflow.md |
| D-01 | CLAUDE.md + code-principles.md | Duplication | High | Defer code-principles.md Architecture Constraints to CLAUDE.md Architecture |
| D-05 | CLAUDE.md (×3) + code-principles.md | Duplication | High | Consolidate "Business logic in Services" to one canonical statement |
| C-03 | CLAUDE.md + workflow.md | Contradiction | Medium | Add "spec or plan file" trigger to workflow.md Rule 3a or subagent exit checklist (extends review_testing C-03) |
| D-02 | CLAUDE.md + code-principles.md | Duplication | Medium | Remove Language section from code-principles.md; reference CLAUDE.md |
| D-06 | code-principles.md + constraints-registry.md | Duplication | Medium | Consolidate EF Core constraints under constraints-registry.md |
| S-01 | CLAUDE.md + code-principles.md | Structural | Medium | Add file+section pointers to CLAUDE.md § Constitutional Role items |
| S-02 | CLAUDE.md | Structural | Medium | Clarify relationship between Constitutional Constraints and Unamendable list |
| I-05 | CLAUDE.md | Inconsistency | Medium | Cross-reference Continuous Enhancement → Amending These Rules |
| D-03 | CLAUDE.md | Duplication | Low | Unamendable list references Constitutional Constraints for DisplayAlert |
| D-04 | CLAUDE.md | Duplication | Low | Unamendable list references Constitutional Constraints for DevExpress |
| D-07 | code-principles.md + constraints-registry.md | Duplication | Low | Remove ObservableRangeCollection entry from constraints-registry.md; reference code-principles.md |
| D-08 | CLAUDE.md + code-principles.md | Duplication | Low | Resolved by D-01 action |
| D-09 | CLAUDE.md | Duplication | Low | Remove "prerequisites for Step 3" from CLAUDE.md testing.md reference (extends review_testing I-03) |
| I-01 | CLAUDE.md + code-principles.md | Inconsistency | Low | Move "Prefer composition over inheritance" to code-principles.md § C# Style |
| I-02 | CLAUDE.md + constraints-registry.md | Inconsistency | Low | Add "Incremental edits" entry to constraints-registry.md with rationale |
| I-03 | code-principles.md | Inconsistency | Low | Add "verify against GlobalUsings.cs" note to § Global Usings |
| I-04 | CLAUDE.md + workflow.md | Inconsistency | Low | Add "test suite" to workflow.md rebuild test; make CLAUDE.md entry a pointer only |
| S-03 | code-principles.md | Structural | Low | "Spec Language — Determinism" is structurally misplaced; resolved by C-02 action |
