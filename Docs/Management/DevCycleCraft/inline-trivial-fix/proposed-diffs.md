# Inline Trivial Fix (ITF) Lane — Proposed `amend:` Diffs

> **NOT YET APPLIED.** These diffs are staged for Helder's approval per `CLAUDE.md § Amending These Rules`. Both targets are `[HARD RULE]`s, not `[Unamendable]` constitutional constraints, so Helder can approve them.
>
> Apply order: 1 → 2 → 3 → 4, in a single `amend:` commit on `develop` (rules + docs only; the Guard 3 implementation in `constitutional-guard.py` is a separate worktree task).

---

## Amendment 1 — `.claude/rules/workflow.md` § Rule 2

**What is wrong with the current rule:** Rule 2's "all coding is done by subagents" is correct for every change with design content, but at the bottom of the size distribution it forces a ~25–35k-token subagent round-trip for a fully-diagnosed one-line fix that costs the orchestrator ~500 tokens to apply, because the orchestrator already holds the diagnosis context.

**Backward compatibility:** No existing code is affected. No in-flight task changes behaviour. Existing subagent dispatches remain valid — ITF is an additional permitted path, never a required one.

### Diff

Insert after the "Orchestrator never reads source files `[HARD RULE]`" blockquote and before the "Wave cap" bullet:

```diff
 > **Orchestrator never reads source files `[HARD RULE]`:** the main/orchestrator agent must not read `.cs`, `.xaml`, or any other source file — all code inspection (including plan-mode exploration) is delegated to an Explore/Plan subagent. Allow/deny list + session-start self-check: `.claude/agents/orchestrator.md § Orchestrator Read-Scope`.
 
+### Inline Trivial Fix (ITF) lane `[amended YYYY-MM-DD]`
+
+**Narrow exception to "all coding is done by subagents".** The orchestrator MAY apply a fix directly — no subagent — only when ALL of the following hold. Any single miss = dispatch an implementor; there is no partial qualification.
+
+| # | Condition |
+|---|-----------|
+| C0 | A **declaration** exists in the worktree where the edit occurs, naming this file |
+| C1 | Exactly **1 file**, **≤ 5 changed lines** (guard's upper-bound count) |
+| C2 | Fix **fully diagnosed** — root cause, exact file and exact line already recorded before the file is opened; if finding the defect would need a grep or a second file, it is not fully diagnosed |
+| C3 | Target is **not** `.xaml` / `.xaml.cs` |
+| C4 | Target is **not** a governed component (`component-change-governance.md`) |
+| C5 | Target is **not** in the sequential-only file registry (below) |
+| C6 | Severity ≤ Major **and** no regression test is mandatory per `bug-tracking.md`. In practice: Critical always dispatches; Major dispatches wherever testable. The lane's population is Minor bugs, UI-only Major bugs verified by manual E2E, and non-bug trivia |
+| C7 | Edit is in a **worktree on a task branch** — ITF grants NO worktree exemption |
+| C8 | Build (0 errors) + affected tests green before commit |
+
+**Opt-in is explicit.** Before editing, the orchestrator (a) writes `<worktree>/.itf-active` — worktree root, never repo root — and (b) logs one line in the feature's `task-log.md`:
+`ITF: BUG-050 — SongFormViewModel.cs — root cause: SelectArtist omits IsArtistLocked = true — expected 1 line.`
+The orchestrator **deletes the marker as the final step of the ITF commit**; a 30-minute expiry is the safety net for a dead session.
+
+**Enforcement is opt-in, and bounded once entered.** Without a declaration the lane is inert and ordinary Rule 2 applies — prose-enforced, as today. Once declared, C1/C3/C4/C5 are hook-enforced (`constitutional-guard.py` Guard 3) and cannot be exceeded. C2/C6, and multi-declaration chaining, are prose rules auditable after the fact via the task-log lines and the commit trailer's true file count.
+
+**Commit trailer (required):** append `Lane: ITF (N files, N lines)` to the Bug Fix Pattern message (Rule 3). Audit with `git log --grep "Lane: ITF"`.
+
+**Applies to the orchestrator only.** Implementor subagents are never constrained by ITF bounds.
+
+Full rationale, decisions, and Guard 3 design: `DevCycleCraft/inline-trivial-fix/`.
+
 - **Wave cap `[HARD RULE]`:** max **4** subagents in parallel; dispatch in waves, wait for all, then next wave. Discard a subagent's context after it completes — never reuse the instance.
```

---

## Amendment 2 — `.claude/agents/orchestrator.md` § Orchestrator Read-Scope

**What is wrong with the current rule:** The deny-list forbids reading any source file, including the one file the orchestrator has already been told contains a one-line defect. The rule's own stated rationale is that reading source burns coordination context and causes drift into implementer work — neither effect is produced by opening one pre-identified file for ≤ 5 lines. The blanket form over-shoots its purpose at the smallest change size.

**Backward compatibility:** The deny-list is otherwise unchanged. Grep, neighbour-reads, and exploratory reads remain forbidden. The existing "Narrow exception" (explicit user instruction) is unaffected.

### Diff

```diff
 **Narrow exception:** Reading a specific source file is permitted ONLY when the user explicitly and directly instructs the orchestrator to read that exact file. Absent that explicit instruction, delegate.
 
+**Narrow exception 2 — Inline Trivial Fix lane `[amended YYYY-MM-DD]`:** when an active ITF declaration is in place in the target worktree (`workflow.md § Rule 2 — Inline Trivial Fix lane`), the orchestrator MAY read **the single declared file it is about to edit**, and nothing else. It MAY NOT grep, MAY NOT read neighbouring files, and MAY NOT open the file to *determine whether* a fix is needed — condition C2 requires the diagnosis to already exist. The moment the fix turns out to need a second file, more than 5 lines, or any exploration: stop, clear the declaration, and dispatch an implementor.
+
 ## Post-Wave Verification
```

---

## Amendment 3 — `.claude/rules/workflow.md` § Rule 3 (Bug Fix Pattern)

**What is wrong with the current rule:** The Bug Fix Pattern commit template has no field recording which execution lane produced the fix, so ITF usage would be invisible in git history and the calibration review (requirements.md § Calibration review) would have no data source.

**Backward compatibility:** The trailer is required only for ITF-lane commits. Subagent-produced fixes keep the existing three-field template unchanged.

### Diff

```diff
 ```
 fix: [component] — [symptom]
 
 Root cause: [one sentence]
 Fix: [one sentence]
 Regression risk: [None | Low | Medium — reason]
+Lane: ITF (N files, N lines)     ← required for ITF-lane fixes only; omit otherwise
 ```
```

---

## Amendment 4 — `Docs/Changelog/changelog.md`

Required by `CLAUDE.md § Amending These Rules` step 4.

```markdown
### YYYY-MM-DD — Inline Trivial Fix (ITF) lane

**Old rule** (`workflow.md` Rule 2): All coding is done by subagents; the orchestrator handles shell-only steps. The orchestrator never reads `.cs`/`.xaml` source files (`orchestrator.md § Orchestrator Read-Scope`).

**New rule:** Both hold, with one bounded, opt-in exception. When a fix is fully diagnosed and satisfies C0–C8 (declared, 1 file, ≤ 5 changed lines, not XAML, not a governed component, not sequential-only, severity ≤ Major with no mandatory regression test, in a worktree, build + tests green), the orchestrator may declare an ITF (`<worktree>/.itf-active` + a task-log line), read that single file, apply the fix inline, and commit it with a `Lane: ITF` trailer. Once declared, C1/C3/C4/C5 are hook-enforced by `constitutional-guard.py` Guard 3 and cannot be exceeded; undeclared, the lane is inert and the pre-existing prose rules apply unchanged. The lane grants no exemption from the worktree rule, component governance, regression-test requirements, or any [Unamendable] constitutional constraint, and imposes no bound on implementor subagents.

**Rationale:** A fully-diagnosed one-line fix costs ~25–35k tokens and 2–4 minutes via subagent dispatch versus ~500 tokens inline, because the orchestrator already holds the diagnosis context. The delegation rule is correct in the general case and mis-calibrated at the smallest change size.

**Effective date:** YYYY-MM-DD. Spec: `Docs/Management/DevCycleCraft/inline-trivial-fix/`.
```

---

## Also required at apply time

- **Helder authorship review of the amended rule text** (`CLAUDE.md § Continuous Enhancement — Authorship`) — required before these diffs are applied, and separate from approving the spec.
- **`.gitignore`** — add `.itf-active` (transient per-worktree marker, must never be committed).
- **`MyVocaList.sln`** — register `requirements.md`, `design.md`, and `proposed-diffs.md` under a new `inline-trivial-fix` Solution Folder (`constraints-registry.md § Visual Studio Solution` HARD GATE; next sequential GUID `0042`).
- **BACKLOG.md** — flip the 2026-07-12 Dev Cycle Craft row *"Evaluate guideline update — allow inline trivial-task execution"* from 💡 Pending to 📋 Spec, retitle to name the ITF lane, and repoint at `DevCycleCraft/inline-trivial-fix/`.
- **LEDGER.md** — add the row when the Guard 3 implementation task is dispatched to a worktree.
