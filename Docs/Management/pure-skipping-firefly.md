# Form Validation — Orchestration Plan

## Context

BACKLOG.md registers a **Form validation** feature (`💡 Pending`, 2026-06-30) whose goal is to *"Establish and apply patterns for all the existing App's form entries validations."* It has 5 nested tasks, to be executed **in the order found in the document**:

1. **01 — Form validation guide**: append/update MyVocaList's Claude internal tooling (`.claude/library/*`, rules) to establish a validation standard across app forms. Requirements live in `Docs/Management/DevCycleCraft/ui-form-validation/01-form-validation-guide.md` (folder name may actually be `ui-form-validation-guide/` — see git status; subagent locates the real file).
2. **02 — Update Venues form** (reference implementation)
3. **03 — Update Singer form**
4. **04 — Update Songs form**
5. **05 — Update Artists form**

**Why now:** the app is approaching MVP; form-input validation is currently inconsistent across entities. Establishing one standard in the internal guidelines, then applying it Venue-first as the canonical pattern and replicating it to the other three forms, prevents validation drift and rework.

## Orchestration constraints (from Helder)

- **Autonomous:** all authorizations (worktree creation, commit, merge, push to `develop`) are pre-granted to the orchestrator and every subagent.
- **Minimal orchestrator context:** the orchestrator reads **only** BACKLOG.md. It does **not** read `.cs`, `.xaml`, `.md` guideline files, or the requirements file — all reading/analysis is delegated (also honors `orchestrator.md § Orchestrator Read-Scope`).
- **Worktree isolation:** every subagent runs in its own git worktree (`isolation: "worktree"`) so `develop` is never locked and parallel terminals never collide.
- **Sequential forms:** the four form updates run one at a time; each merges to `develop` before the next starts, so every form subagent sees the updated guidelines + prior reference form.
- **Reference pattern:** the Venue form is the canonical implementation; Singer, Songs, Artists must replicate the Venue pattern exactly, each obeying the updated guidelines.

## Model assignment

| Step | Model | Rationale |
|------|-------|-----------|
| 1A Guidelines evaluation + update plan | **Opus** | Explicitly required by Helder |
| 1B Fresh review of the update plan | **Opus** | Explicitly required by Helder ("fresh Opus subagent to review") |
| 1C Guidelines implementation | Opus | Constitutional/rules-file authorship; high blast radius |
| 2 Venues form (reference) | Opus | Sets the canonical pattern the other 3 copy |
| 3–5 Singer / Songs / Artists forms | Sonnet | Pattern-following UI work against an established reference |
| Per-task reviews | Opus | Mandatory per-task spec+quality review loop |

---

## Wave 1 — Task 01: Form validation guide (internal guidelines)

### 1A — Evaluate & plan updates (Opus, worktree)
Dispatch one Opus subagent. Brief:
- Locate and read the requirements file (`ui-form-validation/01-form-validation-guide.md` or `ui-form-validation-guide/…` — check git status untracked folder).
- Audit current internal validation guidance: `.claude/library/dialogs-validation.md`, `crud-pages.md`, `devexpress-patterns.md`, `m3-components.md`, and any `.claude/rules/*` touching validation; inspect how the **Venue form** currently validates (reference for gap analysis).
- Produce a **plan** (not an implementation) of the exact edits needed to establish the validation standard: which files to change, what sections to add/replace, the canonical validation pattern (error surfacing, inline vs BottomSheet, DevExpress-first components, MD3 terminology, tuple-return service validation per `code-principles.md`).
- Output the plan to `Docs/Management/DevCycleCraft/ui-form-validation/plan.md` and register any new/moved files in `MyVocaList.sln`.
- Commit on its worktree branch; do **not** merge yet.

### 1B — Review the plan (fresh Opus, worktree)
Dispatch a **fresh** Opus subagent (no prior context) to review 1A's plan for correctness, completeness, consistency, and testability against the requirements file and the project constitution (CLAUDE.md non-negotiables). It returns APPROVE or a concrete change list. If changes required → loop back to 1A briefing with the review notes.

### 1C — Implement the guideline updates (Opus, worktree)
Dispatch an Opus subagent to implement the approved plan: edit the internal guideline/rules files, follow the `amend:` process for any `CLAUDE.md`/`.claude/rules/*` change (rationale + changelog entry), register `.sln` changes, run the subagent exit checklist. Commit, **merge to `develop`, push**. Orchestrator verifies `develop` build is green before proceeding.

**Gate:** guidelines must be on `develop` before any form work starts.

---

## Wave 2 — Tasks 02–05: apply validations to forms (sequential)

For each form in order **Venues → Singer → Songs → Artists**, dispatch one subagent in its own worktree:

1. **Venues form** (Opus) — implement validation per the updated guidelines. This is the **reference**. Its resulting pattern (validation triggers, error display, service-layer validation, tests) is documented in the task-log as the template. Merge to `develop`, push.
2. **Singer form** (Sonnet) — replicate the Venue pattern exactly (note: "Singer" = the Person CRUD form; subagent confirms the actual page/VM). Merge, push.
3. **Songs form** (Sonnet) — replicate the Venue pattern. Merge, push.
4. **Artists form** (Sonnet) — replicate the Venue pattern. Merge, push.

Each form subagent must:
- Read the updated guidelines + the Venue reference implementation (for forms 2–4) before coding.
- Follow TDD per `testing.md` (Service/ViewModel validation is Level A — failing test first).
- Run the subagent exit checklist (build 0 errors, tests green, `.sln`, task-log with Changed files + AC traceability, commit, merge to `develop`, push).
- Update its BACKLOG nested row status is done by the **orchestrator**, not the subagent.

**Between each form:** orchestrator dispatches the mandatory per-task review loop (Opus spec-compliance + code-quality review). Fix findings before starting the next form. Orchestrator runs `dotnet build` + `dotnet test` on `develop` independently after each merge.

---

## Orchestrator responsibilities (between waves)

- Update BACKLOG.md nested row statuses at each milestone (`💡 → 🟡 In Progress → ✅ Done`).
- Run independent `dotnet build` / `dotnet test` after each merge to `develop`.
- Never read source files; delegate all inspection.
- Do not start a form task until the previous form is merged, reviewed, and green on `develop`.

## Verification (end-to-end)

- After each merge: `dotnet build` (0 errors) + `dotnet test` (0 failures) on `develop`, run by the orchestrator.
- Guidelines: confirm the requirements file's standard is fully reflected in the edited `.claude/library`/rules files (1B reviewer confirms).
- Each form: AC traceability matrix in its task-log; validation unit tests (Service + ViewModel) green; Venue pattern demonstrably replicated in Singer/Songs/Artists.
- Final: all 5 BACKLOG nested rows → `✅ Done`; Helder emulator smoke test of the four forms noted as the remaining human gate (validation UX is UI-observable).

## Notes / risks

- **Path discrepancy:** BACKLOG cites `ui-form-validation/01-form-validation-guide.md`; git status shows untracked `ui-form-validation-guide/`. First subagent resolves the real path.
- **"Singer" naming:** maps to the Person CRUD form; subagent confirms actual `PersonFormPage`/VM.
- **Guideline files are write-protected in spirit** (`amend:` process): 1C must produce rationale + changelog, human-authorship note preserved.
