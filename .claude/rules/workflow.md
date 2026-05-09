# Development Workflow

> These rules are enforced by hooks. Violating them costs rework. Follow them exactly.

---

## Hook Enforcement Notes

The hooks in `.claude/settings.json` enforce specific rules from this document.

### Hook-enforced rules (automatic warnings or blocks)

| Hook | Trigger | Rule enforced |
|------|---------|---------------|
| `Stop` hook | Session ends with uncommitted changes | Rule 3 — Commit After Every Task; also triggers Verifier dispatch reminder |
| `PostCompact` hook | Context compaction event | Session resume — re-read spec reminder |
| `PostToolUse` hook (Services files) | Edit to a Services/*.cs file | testing.md — TDD reminder for service changes |
| `SessionStart` hook | New session begins | Hook health verification |

### Self-enforced rules (no hook — agent must apply consciously)

- Pre-dispatch validation checklist (Rule 2 / `agents/orchestrator.md`)
- DRY Onion task ordering (Rule 4)
- Single-writer rule for hotspot files (Rule 2)
- Spec freshness gate before dispatching a wave (`agents/orchestrator.md`)
- Multi-wave checkpoint every second wave (`agents/orchestrator.md`)
- Session-end spec update ritual (Rule 3 subsection)
- AC traceability matrix in task-log (Rule 5)
- E2E emulator gate before To Review (`agents/implementor.md`)

### Hook health verification

At the start of each session, verify that hooks are operational:
1. Check that `.claude/settings.json` exists and is valid JSON
2. Confirm the `Stop` hook is present and references the correct script
3. If a hook is misconfigured: fix it before dispatching any subagent

---

## SDD Invariant

> **Spec changes before code changes.**

- If a new requirement arises during implementation, update the spec first — then update the code.
- If code contradicts the spec, the code is wrong — the spec is not wrong.
- If the spec is incomplete, stop and clarify with Helder — do not improvise.
- A subagent that modifies behavior not described in the spec has violated this invariant.

This invariant applies to all agents (main and sub) at all times.

---

## Rule 1 — Spec-First

**Before writing any implementation code for a feature, read `Docs/specs/[feature]/design.md`.**

No exceptions. Code written without reading the spec is code that may contradict it.

### Spec as source of truth

The spec is the authoritative description of intended behavior. When spec and code disagree:

- **If the spec is complete and was approved:** the code is wrong. Fix the code.
- **If the spec has a gap or is ambiguous:** stop, clarify with Helder, update the spec, then fix the code.
- **Never:** silently fix the code and leave the spec describing something that no longer exists.

### Spec structure
| File | What it answers |
|------|----------------|
| `requirements.md` | User stories, acceptance criteria, validation rules, out-of-scope |
| `design.md` | Architecture, interfaces, page structure, interaction flows, key decisions |
| `tasks.md` | Ordered checkboxed tasks — check off as each completes |

> **Spec-writing detail:** For AC format (Given/When/Then, EARS), spec language rules, requirements.md and design.md mandatory sections, reversibility documentation, demo statement format, spec ownership constraints, tacit knowledge capture, over-specification guard, versioning discipline, rebuild test, and functional vs technical separation — see `.claude/library/spec-writing-guide.md`.

### Spec decision table — ceremony, scope, and required artifacts

| Task type | Estimated effort | Spec required? | Ceremony level | Required artifacts |
|-----------|-----------------|----------------|----------------|-------------------|
| Typo fix, comment update | < 5 min | No | None | Descriptive commit message |
| Single-file cosmetic change (color, padding, label) | < 15 min | No | None | Descriptive commit message |
| Single-file logic fix (bug with known cause) | < 30 min | No | Minimal | Commit message as spec (Bug Fix Pattern) |
| Docs/rules/config update | < 30 min | No | Minimal | Commit message |
| Small isolated change (1 file, no interface change, < 1 hour) | 30–60 min | No | Light | `tasks.md` entry + commit message (only if task is tracked in an active feature plan) |
| Multi-file change within one layer | 1–2 hours | No | Standard | `tasks.md` + inline design notes in commit |
| Cross-layer feature (any two of: Domain, Infra, Services, UI) | 2–8 hours | **Yes** | Full | All three spec files |
| Multi-session feature | > 8 hours | **Yes** | Full + Decision log | All three spec files + `decisions.md` |
| New feature (any complexity) | Any | **Yes** | Full | All three spec files |
| Non-trivial refactor (cross-layer, affects interfaces) | Any | **Yes** | Full | `design.md` + `tasks.md` |
| Bug fix | Any | No | Minimal | Commit message as spec (Bug Fix Pattern) |
| Spike / discovery work | Any | No | Minimal | `findings.md` artifact |
| Architectural change (new pattern, new dependency, schema change) | Any | **Yes** | Full + Helder review | All three spec files + Helder sign-off |

**Key thresholds:**
- **≥ 2 layers OR > 2 hours** → Full ceremony with all three spec files. No exceptions.
- **Single file, < 1 hour** → Light ceremony; `tasks.md` entry only if tracked in an active plan.
- **Typo / cosmetic / bug fix** → No spec required; commit message is the artifact.

**Blast radius principle:** Ceremony level must be proportional to the blast radius — how widely the change's consequences spread if it turns out to be wrong.

**When in doubt:** Write a spec. A 10-minute spec prevents a 2-hour rewrite.

### New feature workflow
1. **Brainstorm** — invoke `superpowers:brainstorming`
2. **Write spec** — write all three files; user reviews and approves
   - **2a. Constitution check** — verify the feature does not violate any Non-Negotiable rule in CLAUDE.md before writing the spec
3. **Write plan** — invoke `superpowers:writing-plans`
4. **Implement** — delegate to a subagent (see Rule 2)
5. **Phase-gate review** — invoke `/project:review` after each phase before starting the next

### Spec quality gate (mandatory before implementation)

**No subagent may be dispatched to implement a feature until this gate is passed:**

- [ ] All user stories have at least one acceptance criterion in Given/When/Then or EARS format
- [ ] "Out of Scope" section is present and non-empty
- [ ] Domain Vocabulary defines every domain term used in the spec
- [ ] Validation rules cover all input fields and business constraints
- [ ] `design.md` includes all interface signatures (not just names)
- [ ] `design.md` lists all layers affected
- [ ] Invariants & Postconditions are documented
- [ ] No acceptance criterion is vague or untestable
- [ ] Spec quality four-gate has been applied (Correctness, Completeness, Consistency, Testability)
- [ ] Helder has reviewed and approved the spec

### Spec quality four-gate review

Before marking a spec as ready for implementation, it must pass all four gates:

1. **Correctness gate** — does the spec match what Helder described? (no hallucinated requirements)
2. **Completeness gate** — does every story have a criterion? Are error paths covered?
3. **Consistency gate** — do the requirements and design agree with each other? No contradictions?
4. **Testability gate** — can a developer write a test from every acceptance criterion without asking questions?

### SDD decision table for medium-complexity tasks

| Signal | SDD action |
|--------|-----------|
| Change touches ≥ 2 layers (e.g. Domain + UI) | All three spec files |
| Change introduces a new repository interface | Write `design.md` + update `requirements.md` |
| Change affects an existing public contract (DTO, interface signature) | Write `design.md`; flag downstream consumers in `tasks.md` |
| Change is reversible and affects only one file | Commit message spec is sufficient |
| You find yourself asking "where should this logic live?" | Stop — write a `design.md` |
| Estimated time > 2 hours | Full three-file spec required |

### Spike validation task pattern

A **spike** is a time-boxed exploration task used when the right implementation approach is genuinely unknown. Spikes produce a findings artifact, not production code.

**When to use a spike:**
- A library integration has never been used in this codebase and its behavior is uncertain
- Two valid approaches exist and the trade-offs cannot be evaluated without trying both
- An external API or MCP must be called and the response shape is unknown
- A performance concern exists but its magnitude is unquantified

**Spike task format in `tasks.md`:**
```markdown
- [ ] **[SPIKE] Validate [approach/library/integration]**
  - Time-box: [30 min | 60 min | 2 hours — hard stop]
  - Question: [one sentence: what must the spike answer?]
  - Success criterion: [what finding would confirm the approach is viable?]
  - Failure criterion: [what finding would reject the approach?]
  - Artifact: `Docs/specs/[feature]/findings.md`
  - Files owned: throwaway only — no production code created or modified
  - Demo: N/A (spike produces findings, not user-facing behavior)
```

**Spike rules:**
1. Spike code is throwaway — no production files may be edited
2. The time-box is a hard stop
3. If the spike's success criterion is met: proceed to spec writing using findings
4. If the spike's failure criterion is met: escalate to Helder; do not unilaterally choose an alternative
5. A spike that ends without clear findings must be documented as `inconclusive` with a recommendation

**After the spike:** Main agent reads `findings.md` and updates the spec before any implementation tasks are dispatched. See `.claude/library/session-ops.md` for the findings file format.

### Discovery mode

When the right solution is unknown and exploration is needed before committing to a spec:

1. **Create a spike task** in `tasks.md` with the prefix `[SPIKE]`.
2. Work freely — write throwaway code, try approaches, read docs.
3. At the end of the spike, create `Docs/specs/[feature]/findings.md` (see `session-ops.md`).
4. Delete all throwaway code before transitioning to spec-first implementation.
5. Write the spec based on findings — do not skip spec-writing because "we already know the solution."

### Bug fix pattern — commit message as spec

Bug fixes do not require a three-file spec. The commit message IS the specification.

**Required commit message format:**
```
fix: [component] — [symptom]

Root cause: [one sentence]
Fix: [one sentence]
Regression risk: [None | Low | Medium — reason]
```

If the bug reveals a missing acceptance criterion, add it to `requirements.md` as part of the fix commit.

### Brownfield rule — spec new code only

Write specs only for code you are about to write or significantly change. Do not spec code that is already in production and not being touched.

### When to update specs (Spec-Anchored maintenance)

**Update a spec when:**
- A new requirement is added to an existing feature
- A bug fix reveals a gap in the spec's error path coverage
- A design decision changes during implementation (update before committing the code)
- A review reveals spec/code divergence
- A new constraint is discovered that affects behavior

**Do NOT update a spec when:**
- Refactoring internal implementation details with no observable behavior change
- Renaming variables or moving code within the same layer
- Adding test coverage for already-specified behavior

### ROI J-Curve awareness

The SDD workflow has a **J-Curve ROI profile**: it costs more time upfront and returns that investment later (fewer rewrites, faster subagent execution, less debugging).

- The first 1–2 features using SDD will feel slower than coding without it
- The return starts showing on the 3rd–4th feature
- By the 5th+ feature, SDD overhead is approximately break-even with ad-hoc coding

**J-Curve trap:** Abandoning SDD during the "this takes longer" phase before reaching the return phase. **Counter-measure:** Commit to SDD for a minimum of 3 complete features before evaluating its ROI.

---

## Rule 2 — Subagent Delegation

**All coding is done by subagents. The main agent handles shell-only steps.**

| Main agent does | Subagent does |
|----------------|---------------|
| `dotnet build` | Any file creation or edit |
| `dotnet test` | ViewModels, pages, services, repositories |
| `dotnet ef migrations add` | XAML, code-behind, DI registration |
| `git status`, `git add`, `git commit` | Route additions, AppShell registration |
| Reading spec before briefing subagent | Everything in `crud-pages.md` |

> **Orchestrator-side protocols** (pre-dispatch checklist, pre-wave dependency check, briefing protocol, role scope declaration, adversarial critic, wave handoff, discovery briefs, multi-wave checkpoint, merge sequencing, worktrees, kill criteria, approval authority matrix, review lanes, DGI classification): see `.claude/agents/orchestrator.md`.

> **Implementor-side protocols** (pre-task context gate, intent verification, E2E emulator gate, bounded autonomy rule, spec gap escalation format, subagent return protocol, scope constraints, living spec protocol, post-edit re-read, MCP isolation): see `.claude/agents/implementor.md`.

### Task sizing limits — context window budget

| Task type | Max files touched | Max estimated effort |
|-----------|-------------------|----------------------|
| New service method + tests | 4 files | 90 min |
| New ViewModel + page + tests | 5 files | 2 hours |
| Migration + repository + tests | 4 files | 90 min |
| UI page only (XAML + code-behind) | 3 files | 60 min |
| Full CRUD feature (all layers) | Split into 2+ tasks | — |

**Rule:** If a proposed task exceeds these limits, split it before dispatching.

**Warning sign:** A subagent briefing that lists > 5 files or > 2 hours of estimated work is a sizing violation.

### Wave-based parallelism — hard cap

- **Maximum 4 subagents may run in parallel at any one time.**
- Work is dispatched in waves: spawn up to 4 subagents, wait for all to complete, then start the next wave.
- Never spawn a 5th concurrent subagent — stagger instead.
- After a subagent completes, its context is discarded. Do not reuse the same subagent instance for a second task.

### Single-writer rule for hotspot files

**At any given moment, each file in the repository must have at most one active writer.**

Before dispatching any wave, the main agent performs the file overlap check. If two tasks in the wave list the same file in `Files owned`, they cannot run in parallel — serialize them.

### Sequential-only file registry

Some files must never be edited by more than one agent at a time:

| File | Reason |
|------|--------|
| `MauiProgram.cs` | DI registration — ordering matters; parallel edits produce conflicts |
| `AppShell.xaml` / `AppShell.xaml.cs` | Route registration — one canonical route table |
| `AppDbContext.cs` | EF Core model config — entity set definitions must be coherent |
| Any `*Migration.cs` files | EF migrations are sequential by design |
| `GlobalUsings.cs` (any project) | Global using declarations — merge conflicts produce duplicate errors |
| `Directory.Build.props` | Shared MSBuild properties — parallel edits produce conflicts |
| `tasks.md` (any spec) | Task status tracking — parallel checkbox edits produce divergent state |

**How to add entries:** If a session discovers a new hotspot file, add it to this registry before ending the session.

### Subagent exit checklist (mandatory before returning)

Every subagent must complete ALL of these steps in order before stopping:

1. **Invoke `superpowers:verification-before-completion`** — catches non-negotiable violations (DevExpress-first, SafeAreaEdges, English-only, no DisplayAlert, etc.)
2. **Build:** Run `dotnet build` and confirm 0 errors. If build fails, apply the build retry cap (max 3 attempts). Document result in Verification evidence.
3. **Test:** If any `.cs` implementation file was changed, run `dotnet test` and confirm 0 failures. Document result. Skip only if no code files were modified.
4. **Post-edit re-read:** Re-read the affected section of every edited file and confirm correctness.
5. **Living spec check:** Review decisions made during implementation — write back any undocumented decisions to the spec.
6. **Task-log:** Complete the task-log entry including Changed files, Verification evidence, and AC traceability matrix (if applicable).
7. **Commit:** Commit all changed files including any spec updates.
8. **Push:** `git push origin HEAD`

**The `Stop` hook warns if uncommitted changes remain. Treat it as a hard gate.**

A subagent that stops without completing all 8 steps has not finished the task.

### Post-wave verification — main agent runs build independently

After every wave completes, the main agent must run the build and tests independently:

1. Run `dotnet build` — confirm 0 errors. Do not proceed to the next wave if there are errors.
2. Run `dotnet test` — confirm 0 failures. Investigate any new failures before proceeding.
3. Review the task-log entries from the wave — confirm all entries have Verification evidence and Changed files.
4. If a subagent reported `Build failure` or `blocked: spec gap`, resolve before dispatching the next wave.

---

## Rule 3 — Commit After Every Task

**Run `/project:commit` after every task from `tasks.md` is complete.**

A session that ends with uncommitted changes is a session where progress is at risk. The `Stop` hook warns you — treat it as a hard gate, not a suggestion.

> `/project:review` is a main-agent command invoked after each completed task and after creating or updating any spec or plan file. Subagents do not invoke `/project:review`.

### What counts as "task complete"
- The code builds with no errors
- Tests pass (if the task touched tested code)
- The checkbox in `tasks.md` is checked

### Task completion verification gates

Before checking the box and committing:

**1. Demo statement verification**
If the task has a demo statement, confirm it can be executed. A task whose demo statement cannot be verified is NOT complete.

**2. DI registration check**
If the task introduces a new service, repository, ViewModel, or page, confirm that it is registered in `MauiProgram.cs`. An unregistered type will compile but fail at runtime.

**3. Acceptance criteria check**
For every acceptance criterion the task was supposed to satisfy: confirm it is satisfied. Record evidence in the task-log's AC traceability matrix.

### Session-End Spec Update Ritual

Before ending any session in which implementation occurred:

1. **Review every spec file touched this session** (`requirements.md`, `design.md`, `tasks.md`)
2. For each spec file, ask: "Does this file still accurately describe what was built?"
3. If the answer is "no" or "partially": add a `> **Spec updated [YYYY-MM-DD]:**` note; update ACs, signatures, or invariants to reflect delivered behavior
4. **Update `tasks.md`**: check off all completed tasks; add `[CANCELLED: reason]` to tasks no longer needed
5. Commit all spec updates in the session's final commit

**Trigger questions (ask before ending any session):**
- "Did I implement something that the spec does not describe?"
- "Did I discover a constraint that is not in the spec?"
- "Did I make a decision that future agents will need to know?"
- "Is the spec now more ambiguous than before my session?"

---

## Rule 4 — Tasks.md Is the Source of Truth

Check off each task in `Docs/specs/[feature]/tasks.md` as it completes.

**Sequential constraint:** Never start a task that depends on the output of an incomplete task.

**Parallel exception:** Tasks marked `[P]` may be dispatched simultaneously as a wave per Rule 2.

### In-progress marker — [~] for claimed tasks

```markdown
- [~] **Implement ISingerService** [SEQUENTIAL]  ← claimed — do not reassign
- [ ] **Implement SingersViewModel** [P]          ← available
- [x] **Define SingerEntry entity**               ← done
```

| Marker | Meaning |
|--------|---------|
| `[ ]` | Available — not started |
| `[~]` | In progress — claimed by a dispatched subagent |
| `[x]` | Done — committed |
| `[CANCELLED: reason]` | Removed from scope |

**Rule:** Never dispatch a task marked `[~]`. If a subagent was killed without completing a `[~]` task, reset it to `[ ]` before re-dispatching.

### Task atomization checklist

A task is **atomic** if it passes this checklist:

- [ ] The task produces a single, clearly named artifact (one method, one ViewModel, one page, one migration)
- [ ] The task does not require knowledge of the output of another in-progress task
- [ ] The task can be described in one sentence without using "and" more than once
- [ ] The task fits within the sizing limits (see Rule 2)
- [ ] The task has a `Demo:` statement or a clear acceptance criterion it satisfies
- [ ] A new developer could implement this task correctly using only the spec + `Files owned` declaration

### DRY Onion task ordering rule

Tasks must be ordered from the inside of the architecture outward — Domain first, then Infra, then Services, then UI.

```
Wave 1 (innermost):  Domain entities + repository interfaces
Wave 2:              EF Core migrations + repository implementations
Wave 3:              Service methods
Wave 4 (outermost):  ViewModels + pages
```

**Rule:** Do NOT dispatch a task in Wave N+1 until all tasks in Wave N that produce types consumed by Wave N+1 have been committed.

### Task entry format — structured fields

```markdown
- [ ] **Task title** [P | SEQUENTIAL]
  - **Produces:** [list of new files, interfaces, or types this task creates]
  - **Consumes:** [list of files, interfaces, or types this task depends on being committed first]
  - **Risk:** [Low | Medium | High — one-line reason]
  - **Files owned:** [exact file paths this subagent may create or edit]
  - **Demo:** [one sentence — what a human observer sees when this is done]
  - **Review lane:** [Standard | Elevated | Architectural]
```

### Dependency ordering example — phases template

```markdown
## Phase 1 — Domain (no dependencies)
- [ ] **Define entity** [P]
  - Produces: `MyVocaList.Domain/Entities/SingerEntry.cs`
  - Consumes: nothing
  - Files owned: `MyVocaList.Domain/Entities/SingerEntry.cs`

- [ ] **Define repository interface** [P]
  - Produces: `MyVocaList.Domain/Interfaces/ISingerRepository.cs`
  - Consumes: `SingerEntry.cs`
  - Files owned: `MyVocaList.Domain/Interfaces/ISingerRepository.cs`

## Phase 2 — Infra [SEQUENTIAL — waits for Phase 1]
- [ ] **Add EF Core migration** [SEQUENTIAL]
  - Produces: `*_AddSingerEntry.cs` migration
  - Consumes: `SingerEntry.cs`
  - Files owned: `MyVocaList.Infra/Migrations/*.cs`, `AppDbContext.cs`

- [ ] **Implement repository** [SEQUENTIAL — waits for interface]
  - Produces: `MyVocaList.Infra/Repositories/SingerRepository.cs`
  - Consumes: `ISingerRepository.cs`
  - Files owned: `SingerRepository.cs`

## Phase 3 — Services [SEQUENTIAL — waits for Phase 2]
- [ ] **Implement ISingerService + SingerService** [SEQUENTIAL]
  - Produces: `ISingerService.cs`, `SingerService.cs`
  - Consumes: `ISingerRepository.cs`
  - Files owned: both service files

## Phase 4 — UI [SEQUENTIAL — waits for Phase 3]
- [ ] **ViewModel** [P]
  - Produces: `SingersViewModel.cs`
  - Consumes: `ISingerService.cs`
  - Files owned: `SingersViewModel.cs`

- [ ] **Page + XAML** [P]
  - Produces: `SingersPage.xaml`, `SingersPage.xaml.cs`
  - Consumes: `SingersViewModel.cs`
  - Files owned: both page files
```

---

## Rule 5 — Task Status Registration

Agents record task outcomes manually in the task-log file. The `Stop` hook warns if uncommitted changes remain when a session ends.

### Proof of action — Changed files is mandatory

A task-log entry that claims `To Review` without a `### Changed files` section is **invalid**.

**Rule:** Every task-log entry that represents completed implementation work must include an explicit list of every file that was created or modified.

### Task-log file location

Task-log files live **beside the plan file** in `Docs/superpowers/plans/`, named `<plan-name>-task-log.md`.
Tasks without a plan association are logged to `Docs/superpowers/plans/unassigned-task-log.md`.

### Task-log format (per task entry)
```
---
## Task: <title>
**Plan:** <plan file relative path>
**Status:** in progress | Check build | To Review | Build failure | blocked: spec gap | Spec updated — re-planning required | Early task done | Review task done
**Started:** MM/DD/YYYY
**Completed:** MM/DD/YYYY

### Changed files:
- `relative/path/to/file.cs` — reason (e.g. "added GetPagedAsync method")
- `relative/path/to/test.cs` — reason (e.g. "added 3 test cases")

### Build notes
[Only present if build was checked — records error summary and diagnosis]

### Verification evidence
- Build: [PASS / FAIL — error summary if FAIL]
- Tests: [PASS (N tests) / FAIL (N failures) / SKIPPED (no test files changed)]
- Post-edit re-read: [confirmed / N/A — no code files changed]
- Spec compliance: [confirmed — [spec file] section checked / divergence noted: [one line]]
```

### Acceptance criteria traceability matrix

For tasks that implement user-facing behavior, include an **AC traceability matrix** in the task-log entry:

```
### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-1 | Singer added appears in queue | VenueService.AddSingerAsync | AddSingerAsync_ValidInput_ReturnsSuccess |
```

Missing rows = missing tests = incomplete feature.

### Task statuses
| Status | Meaning |
|--------|---------|
| `in progress` | Task started, work underway |
| `Check build` | Code changed — build verification pending |
| `To Review` | Build passed — task ready for code review |
| `Build failure` | Build failed after 3 attempts — needs investigation |
| `blocked: spec gap` | Spec ambiguity found — question + options + recommendation documented |
| `Spec updated — re-planning required` | Implementation revealed a spec gap; spec updated; tasks.md may need re-ordering |
| `Early task done` | New asset/enhancement completed and committed |
| `Review task done` | Review task completed |

---

## Rule 6 — Research Tool Gate (Context7 → Exa → WebSearch)

Before any web research query, follow this three-tier hierarchy:

1. **Library / framework / SDK / API docs** → Context7 (`mcp__context7__resolve-library-id` → `mcp__context7__query-docs`)
2. **General web research** (comparisons, news, tool evaluations, articles) → Exa MCP (`exa_search`)
3. **Raw `WebSearch` / `WebFetch`** → last-resort fallback only when both Context7 and Exa return no useful result

This applies to **both the main agent and all subagents.**

---

## Rule 7 — Session Start Protocol

Every session that involves implementation or planning must begin with this reading order before any code is written or any subagent is dispatched.

### Session start reading order

Read in this order — do not skip items, do not resume from memory alone:

0. **Hook health verification** — confirm hooks are operational (see Hook Enforcement Notes at the top of this file). Fix any misconfigured hooks before proceeding.
1. **`Docs/DevEnv/plans/impl/MASTER_PLAN.md`** — find the first non-Done step; that is the current position
2. **Active session handoff file** (if one exists): `Docs/superpowers/plans/<plan-name>-handoff.md` — overrides MASTER_PLAN for exact continuation point
3. **`ACTIVE-CONSIDERATIONS.md`** (if it exists) — read the priority stack and open items
4. **`Docs/specs/[feature]/tasks.md`** — confirm which tasks are done, in-progress (`[~]`), and pending
5. **`Docs/specs/[feature]/requirements.md`** — refresh acceptance criteria (do not rely on previous-session memory)
6. **`Docs/specs/[feature]/design.md`** — refresh architecture and interface signatures
7. **`Docs/superpowers/plans/<plan-name>-task-log.md`** — check for unresolved `blocked:` statuses or `Spec updated — re-planning required` entries

**Rule:** Steps 1–7 are mandatory. Steps 4–7 may be scoped to the specific feature being worked on if multiple features are in flight.

**Anti-glob rule:** Never call `Glob("Docs/**")` or equivalent open-ended scans during session start or briefing. Read only the 7 files listed above plus the active feature spec files.

> **Session operations detail** (ACTIVE-CONSIDERATIONS.md format, findings.md format, handoff artifact format, context exhaustion warning signs, tiered memory governance, session start constraint capture): see `.claude/library/session-ops.md`.

---

## Rule 8 — GitHub MCP Pre-Task Collision Check

Before dispatching any wave that modifies files in the repository, confirm that no other agent or branch is currently modifying the same files.

### Pre-task collision check protocol

If the GitHub MCP is available:
1. **Check open PRs:** Query open PRs on the current branch base. If any open PR touches a file in the current wave's `Files owned` list, a collision risk exists.
2. **Check recent commits:** Review the last 10 commits on the branch. Confirm the current spec reflects those changes.
3. **Check in-progress `[~]` tasks:** Confirm no task marked `[~]` is being worked on by another agent in a different session.

**If the GitHub MCP is NOT available:**
- Run `git log --oneline -10` to check recent commits
- Run `git status` to confirm no uncommitted changes from a previous interrupted session
- Check `tasks.md` for any tasks marked `[~]` that should not be in-progress

**Collision types and responses:**

| Collision type | Response |
|----------------|----------|
| Another open PR modifies a file in `Files owned` | Do NOT start the wave. Resolve the PR first. |
| Recent commit from another agent changed an interface the current wave consumes | Re-read the changed interface before briefing. Update briefings if signatures changed. |
| `[~]` task exists but no agent is known to be running it | Reset to `[ ]` and re-dispatch. |
| No collision detected | Proceed with wave dispatch |
