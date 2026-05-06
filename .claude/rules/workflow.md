# Development Workflow

> These rules are enforced by hooks. Violating them costs rework. Follow them exactly.

---

## SDD Invariant

> **Spec changes before code changes.**

This is the single invariant that governs all development in MyVocaList. It cannot be overridden by time pressure, perceived simplicity, or subagent autonomy.

- If a new requirement arises during implementation, update the spec first — then update the code.
- If code contradicts the spec, the code is wrong — the spec is not wrong.
- If the spec is incomplete, stop and clarify with Helder — do not improvise.
- A subagent that modifies behavior not described in the spec has violated this invariant, regardless of whether the change "makes sense."

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

This rule prevents the spec from becoming a historical artifact. A spec that no longer matches the code is worse than no spec — it actively misleads future agents and reviewers.

### Spec structure (copy from `Docs/specs/venues/`)
| File | What it answers |
|------|----------------|
| `requirements.md` | User stories, acceptance criteria, validation rules, out-of-scope |
| `design.md` | Architecture, interfaces, page structure, interaction flows, key decisions |
| `tasks.md` | Ordered checkboxed tasks — check off as each completes |

### Acceptance criteria format

All acceptance criteria in `requirements.md` must use one of these two formats:

**Given/When/Then (for behavior-driven scenarios):**
```
Given [precondition]
When [action]
Then [expected outcome]
And [additional outcome if needed]
```

**EARS (Easy Approach to Requirements Syntax — for system-level rules):**
```
WHEN [trigger], the system SHALL [response]
WHILE [state], the system SHALL [behavior]
IF [condition], THEN the system SHALL [action]
The system SHALL [always-on constraint]
```

Use Given/When/Then for user-facing flows. Use EARS for background rules, constraints, and invariants.

**Never write vague acceptance criteria** such as "the system should work correctly" or "users can manage X." Every criterion must be falsifiable — a test must be writable from it.

#### requirements.md — mandatory sections
- **User stories** — "As a [role], I want [action] so that [value]"
- **Acceptance criteria** — one per user story (see Given/When/Then format below)
- **Validation rules** — field constraints, business invariants
- **Out of Scope** — explicit list of what this feature does NOT do; prevents scope creep during implementation
- **Domain Vocabulary** — define every domain term used in the spec (e.g. "Round", "Queue Entry", "Absence"). All stakeholders and agents must use these exact terms — no synonyms.

### Architecture reversibility documentation

For every significant architectural decision in `design.md`, document its **reversibility**:

| Reversibility level | Description | Example |
|--------------------|-------------|---------|
| **Easily reversible** | Change with no data migration, no interface change | Switching a sort algorithm |
| **Reversible with effort** | Requires migration or interface update | Adding a required column to an existing table |
| **Hard to reverse** | Requires data migration + downstream consumers to change | Changing an entity's primary key type |
| **Irreversible** | Cannot be undone without data loss or breaking changes | Removing a feature that users rely on |

**Rule:** Any decision rated "Hard to reverse" or "Irreversible" must be explicitly flagged in `design.md` under Key Decisions, and must be approved by Helder before implementation begins. Subagents must not make hard-to-reverse decisions unilaterally — they must escalate to the main agent.

### Capture architectural decisions in design.md

**Architectural decisions belong in `design.md`, not in code comments.**

When a significant design choice is made — "why did we pick approach A over B?" — document it in the **Key Decisions** section of `design.md`. Code comments are for explaining what code does; `design.md` is for explaining why the design is structured the way it is.

**What counts as an architectural decision:**
- Choosing between two valid approaches (e.g., "we store queue position as an integer ordinal rather than a linked list because...")
- Accepting a known limitation (e.g., "we do not support concurrent queue edits because the MVP scope is single-admin")
- A trade-off that affects future extensibility (e.g., "this schema does not support multi-venue queues yet")

If an architectural decision is only in a developer's head or in a Slack message, it will be reinvented — incorrectly — the next time someone touches that area.

#### design.md — mandatory sections
- **Architecture** — which layers are affected, how they interact
- **Interfaces** — new or modified service/repository interfaces with signatures
- **Page structure** — screens, navigation flows
- **Interaction flows** — sequence of user actions and system responses
- **Invariants & Postconditions** — system invariants that must hold after every operation (e.g. "Queue always has at least one active singer", "Round number is monotonically increasing")
- **Key Decisions** — one entry per significant design choice, using this format:

  ```
  ### Decision: [short title]
  **Chosen approach:** [what was decided]
  **Alternatives considered:** [what was rejected and why]
  **Reversibility:** [Easily reversible | Reversible with effort | Hard to reverse | Irreversible]
  **Rationale:** [why this approach was chosen]
  ```

#### design.md — optional but recommended sections (for complex features)
- **State machine** — if the feature introduces entity state transitions, document the full state diagram: states, transitions, triggering events, guards. Example: `QueueEntry` states: `Waiting → Singing → Done | Absent`. Without this, subagents invent their own state models.
- **Integration contracts** — if the feature calls external systems (APIs, MCPs, platform services), document the request/response contracts, error modes, and retry behavior. Never leave integration assumptions implicit.

### Functional vs technical separation

`requirements.md` and `design.md` serve different audiences and must not be mixed.

| Belongs in `requirements.md` | Belongs in `design.md` |
|------------------------------|------------------------|
| User goals and intent | Interface signatures |
| What the system must do | How the system does it |
| Acceptance criteria (Given/When/Then) | Layer responsibilities |
| Validation rules from user perspective | EF Core entity configuration |
| Business invariants | Navigation stack design |
| Out of scope statements | Repository method signatures |
| Domain vocabulary | ViewModel state machine |

**Anti-pattern to avoid:** Writing `design.md`-style content in `requirements.md` (e.g. "the VenueRepository will use a parameterized query") or `requirements.md` content in `design.md` (e.g. "users need to see an error message").

**Rule:** If you cannot decide which file a piece of information belongs in, ask: "Is this about what the user needs, or how the system is built?" User need → `requirements.md`. System construction → `design.md`.

### Tacit knowledge capture

Specs only capture what people consciously describe. Tacit knowledge — "of course it works that way" assumptions — is the primary source of spec gaps.

**Protocol:** When writing a spec, explicitly ask these questions before finalizing:
1. What would break if a new developer implemented this from scratch using only the spec?
2. What do I know about this feature that isn't written down yet?
3. What edge cases have I seen in similar features in this codebase?
4. What integrations or dependencies are assumed but not stated?

**LLM-assisted extraction technique:** After drafting a spec, prompt Claude with:
> "What assumptions are implicit in this spec that a developer would need to know but aren't written here? What edge cases are unaddressed?"

Review Claude's output and add any valid tacit knowledge to the spec before implementation starts. This technique surfaces hidden constraints before they become bugs.

### Spec size calibration

Spec size should match task complexity. Over-speccing small tasks wastes time; under-speccing large tasks causes rework.

| Task size | Estimated effort | Spec size target |
|-----------|-----------------|-----------------|
| Tiny | < 30 min | Commit message only |
| Small | 30 min – 2 hours | `tasks.md` + inline notes |
| Medium | 2 – 8 hours | All three files, concise |
| Large | 1 – 3 days | All three files, full detail |
| Epic | > 3 days | Split into sub-features; spec each separately |

**Two-tier spec trigger:** Any task estimated at > 2 hours OR touching ≥ 2 layers automatically requires a full three-file spec. No exceptions.

### Failure-mode analysis

Before finalizing a spec, perform a brief failure-mode analysis:

1. **For each acceptance criterion:** What happens if the operation fails? Is the failure mode documented in the spec?
2. **For each integration point:** What happens if the external system is unavailable or returns an error?
3. **For each state transition:** What happens if the transition is attempted from an invalid state?

Failure modes that are not in the spec will be handled inconsistently by subagents. Document them explicitly.

### Regeneration test practice

After a feature spec is complete, validate it using the **regeneration test**:

> Give the spec (without the existing implementation) to a fresh Claude session and ask it to implement the feature. If the output contradicts your intended design in more than 2 places, the spec has gaps. Fix the spec — do not patch the implementation.

This is a lightweight quality diagnostic, not a required step for every feature. Apply it when a spec feels ambiguous or when a previous implementation diverged from intent.

### Demo statement requirement

Every task in `tasks.md` that touches user-facing behavior must include a **demo statement**: a one-sentence description of what a human observer would see when the task is complete.

Format: `Demo: [actor] can [observable action] and sees [observable result].`

Examples:
- `Demo: Admin taps "Add Singer" and sees the new singer appear at the bottom of the queue list immediately.`
- `Demo: The queue page loads within 500ms with all singers in their correct round-based order.`
- `Demo: Tapping an absent singer shows a bottom sheet with "Mark as Returned" and "Remove" options.`

**Purpose:** Demo statements prevent tasks from being marked "done" when the code compiles but the feature doesn't work as intended. A subagent that cannot write a demo statement does not understand the task.

### Spec ownership constraint

**Specs are written by Helder (Architect) — not by subagents.**

Subagents implement what the spec says. They do not write, rewrite, or significantly alter specs.

| Allowed for subagents | Not allowed for subagents |
|-----------------------|--------------------------|
| Read the spec | Create `requirements.md` or `design.md` from scratch |
| Note a spec gap in the task-log (status: `blocked: spec gap`) | Fill in the spec gap unilaterally |
| Add a change note to a spec when implementation reveals a discrepancy | Rewrite acceptance criteria to match their implementation |
| Flag an ambiguous requirement with options + recommendation | Choose between ambiguous interpretations without escalating |

**Why:** Specs written by subagents reflect what the subagent found convenient to implement, not what the user actually needs. The spec is Helder's voice — it must come from Helder.

**Exception:** A subagent may add a `> **Spec updated [date]:** one-line note` to an existing spec file when updating it per the spec versioning discipline — but only to reflect a decision that was explicitly authorized by the main agent.

### Spec quality gate (mandatory before implementation)

**No subagent may be dispatched to implement a feature until this gate is passed.**

The main agent (Helder or orchestrator) must confirm all of the following before dispatching:

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

Dispatching a subagent before this gate is passed transfers the spec's ambiguity into the implementation — it will manifest as rework.

### Spec quality four-gate review

Before marking a spec as ready for implementation, it must pass all four gates:

1. **Correctness gate** — does the spec match what Helder described? (no hallucinated requirements)
2. **Completeness gate** — does every story have a criterion? Are error paths covered?
3. **Consistency gate** — do the requirements and design agree with each other? No contradictions?
4. **Testability gate** — can a developer write a test from every acceptance criterion without asking questions?

A spec that fails any gate must be revised before implementation proceeds.

### Decision log — fourth optional spec file

For features with many architectural trade-offs, a fourth spec file `decisions.md` may be created alongside the three standard files. This file is a chronological log of decisions made during the feature's lifetime.

**Format: `Docs/specs/[feature]/decisions.md`**

```markdown
# Decision Log — [Feature Name]

## [YYYY-MM-DD] [Short title]
**Context:** [what situation required a decision]
**Decision:** [what was decided]
**Consequences:** [what this enables or constrains going forward]
```

**When to use it:** Create `decisions.md` when:
- A feature has more than 3 Key Decisions in `design.md`
- Decisions evolve over multiple sessions
- The feature is a long-lived component likely to be revisited

**When not to use it:** Do not create `decisions.md` for simple CRUD features. Architectural overhead must be proportional to complexity.

### Spec versioning discipline

When a spec is updated after implementation has started:

1. **Add a change note at the top of the updated file** using this format:
   ```
   > **Spec updated [YYYY-MM-DD]:** [one sentence describing what changed and why]
   ```
2. **Do not delete old content** — mark superseded sections with `~~strikethrough~~` and add a note explaining what replaced them.
3. **Update `tasks.md`** to reflect the change: add new tasks, mark any tasks that are now irrelevant as `[CANCELLED: reason]`.
4. **Notify the main agent** by setting the task-log status to `Spec updated — re-planning required` before stopping.

Rationale: Versioned specs allow the main agent to understand what changed mid-flight and assess the impact on in-progress or pending tasks.

### Spec-update gate — after implementation

When a subagent's work reveals a discrepancy between the spec and the delivered code (even a "minor" one), the following must happen before the task is marked `To Review`:

1. Update `requirements.md` or `design.md` to reflect what was actually built.
2. Note the change in the task-log as `Spec updated — re-planning required` if it affects subsequent tasks.
3. Never leave the spec stale at the end of a task. A stale spec is technical debt that compounds with every subsequent wave.

> **Staleness prevention:** Every implementation task must end with a brief spec-review question: "Does the spec still accurately describe what was built?" If the answer is no, fix the spec before committing.

### New feature workflow
1. **Brainstorm** — invoke `superpowers:brainstorming`
2. **Write spec** — write all three files; user reviews and approves
   - **2a. Constitution check** — before writing the spec, verify the feature does not violate any Non-Negotiable rule in CLAUDE.md (e.g., no `DisplayAlert`, DevExpress-first, English-only). If a conflict exists, flag it to Helder before proceeding — do not silently design around it.
3. **Write plan** — invoke `superpowers:writing-plans`
4. **Implement** — delegate to a subagent (see Rule 2)
5. **Phase-gate review** — invoke `/project:review` after each phase before starting the next
   - After spec writing: review spec for completeness before writing the plan
   - After plan writing: review plan for coherence before dispatching subagents
   - After each implementation wave: review output before dispatching the next wave
   - At feature close-out: final review to confirm spec matches delivered behavior

### Discovery mode

When the right solution is unknown and exploration is needed before committing to a spec, use **discovery mode**:

1. **Create a spike task** in `tasks.md` with the prefix `[SPIKE]`.
2. Work freely — write throwaway code, try approaches, read docs.
3. At the end of the spike, create `Docs/specs/[feature]/findings.md` documenting:
   - What was tried
   - What worked and what didn't
   - Recommended approach with rationale
   - Known constraints or risks discovered
4. Delete all throwaway code before transitioning to spec-first implementation.
5. Write the spec based on findings — do not skip spec-writing because "we already know the solution."

**Discovery mode is not an excuse to skip the spec.** It is a structured path to a better spec.

### Bug fix pattern — commit message as spec

Bug fixes do not require a three-file spec. The commit message IS the specification.

**Required commit message format for bug fixes:**
```
fix: [component] — [symptom]

Root cause: [one sentence]
Fix: [one sentence]
Regression risk: [None | Low | Medium — reason]
```

Example:
```
fix: VenueRepository.ExistsByNameAsync — returns false for mixed-case duplicates

Root cause: CollationInterceptor not applied to this query path.
Fix: Added COLLATE NOCASE to the LIKE clause via EF.Functions.Collate.
Regression risk: Low — affects search queries; covered by new integration test.
```

If the bug reveals a missing acceptance criterion, add it to `requirements.md` as part of the fix commit.

### Brownfield rule — spec new code only

Existing code that predates the SDD workflow does not require retroactive spec creation. Writing specs for already-working code is waste.

**Rule:** Write specs only for code you are about to write or significantly change. Do not spec code that is already in production and not being touched.

### When to update specs (Spec-Anchored maintenance)

Update a spec when:
- A new requirement is added to an existing feature
- A bug fix reveals a gap in the spec's error path coverage
- A design decision changes during implementation (update before committing the code)
- A review reveals spec/code divergence
- A new constraint is discovered that affects behavior (add to Invariants section)

Do NOT update a spec when:
- Refactoring internal implementation details with no observable behavior change
- Renaming variables or moving code within the same layer
- Adding test coverage for already-specified behavior

### SDD decision table for medium-complexity tasks

For tasks that don't fit cleanly into "small isolated" or "new feature," use this decision table:

| Signal | SDD action |
|--------|-----------|
| Change touches ≥ 2 layers (e.g. Domain + UI) | Write `design.md` before starting |
| Change introduces a new repository interface | Write `design.md` + update `requirements.md` |
| Change affects an existing public contract (DTO, interface signature) | Write `design.md`; flag downstream consumers in `tasks.md` |
| Change is reversible and affects only one file | Commit message spec is sufficient |
| You find yourself asking "where should this logic live?" | Stop — write a `design.md` |
| Estimated time > 2 hours | Full three-file spec required |

When uncertain: start with a two-sentence design note in the task-log. If it grows beyond 5 lines, promote it to `design.md`.

### Over-specification guard

A spec that is too long is as harmful as one that is too short. Over-specified specs:
- Take longer to maintain than to implement
- Constrain implementation details that should be left to the developer's judgment
- Become stale faster because they describe the how, not the what

**Thin spec standard:** A good spec specifies outcomes, not implementations.

| Over-specified (avoid) | Thin (prefer) |
|------------------------|---------------|
| "The VenueRepository will use a LEFT JOIN with parameterized WHERE clause" | "Venues are searchable by name (case-insensitive)" |
| "The ViewModel will call `ReplaceRange` in a `RunOnUiThread` block" | "The list updates immediately after a venue is added" |
| "The button will have a 48dp minimum touch target" | "All touch targets meet platform UX standards" |

**Spec length guideline:** `requirements.md` should not exceed 2 pages. `design.md` should not exceed 3 pages. If you find yourself writing more, split the feature into sub-features.

### When to skip SDD (spec bypass rule)

Not every change requires a full three-file spec. Use this table:

| Task type | Spec required? | Minimum artifact |
|-----------|---------------|-----------------|
| New feature (any complexity) | Yes | All three files: `requirements.md`, `design.md`, `tasks.md` |
| Non-trivial refactor (cross-layer, affects interfaces) | Yes | `design.md` + `tasks.md` |
| Small isolated change (< 1 hour, single file, no interface change) | No | Descriptive commit message |
| Bug fix | No | Commit message as spec (see Bug Fix Pattern) |
| Docs/rules/config update | No | Commit message |
| Spike / discovery work | No | `findings.md` artifact (see Discovery Mode) |

**Rule:** When in doubt, write a spec. A 10-minute spec prevents a 2-hour rewrite.

**Spec bypass guard:** Even when skipping a full spec, the SDD Invariant still applies. "No spec" does not mean "no constraints" — it means the commit message, the task description, or a brief inline note serves as the specification.

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

### Wave-based parallelism — hard cap
- **Maximum 4 subagents may run in parallel at any one time.**
- Work is dispatched in waves: spawn up to 4 subagents, wait for all to complete, then start the next wave.
- Never spawn a 5th concurrent subagent — stagger instead.
- After a subagent completes, its context is discarded. Do not reuse the same subagent instance for a second task.

### Briefing protocol — paths only, never paste content
- Subagent briefings must reference **file paths**, not paste file content inline.
- Tell the subagent which files to read; let its own `Read` calls bring the content into its context.
- Pasting rule file content into a briefing multiplies token cost by the number of subagents — never do it.
- Pre-read the spec yourself and hand the subagent concrete, scoped instructions (not "based on what you find").

#### Role scope declaration

Every subagent briefing must begin with a **role scope block** that declares:

```
Role: Implementor
Scope: [one sentence describing the exact task — e.g. "implement VenueRepository.GetPagedAsync"]
Files owned: [list of files this subagent may create or edit]
Files off-limits: [list of files this subagent must NOT modify]
Spec source: [path to design.md and requirements.md]
```

**Purpose:** Role scope blocks prevent scope bleed — a subagent that is not told what it owns will make assumptions about what it is allowed to change. Ambiguous ownership leads to file conflicts in parallel waves.

**Rule:** A subagent that receives a briefing without a role scope block must stop and request one from the main agent before proceeding.

---

#### Mandatory spec reads at session start

Before briefing ANY subagent, the main agent must read (in its own context):

1. `Docs/specs/[feature]/requirements.md` — acceptance criteria, validation rules, out-of-scope
2. `Docs/specs/[feature]/design.md` — interfaces, layers affected, key decisions
3. `Docs/specs/[feature]/tasks.md` — task list and current checkpoint

**Rule:** The main agent must not brief a subagent based on memory from a previous session. Re-read the spec fresh at the start of each session. Context windows reset — assume nothing was retained.

This prevents the most common drift source: a subagent receiving a stale or incomplete briefing because the orchestrator relied on earlier-session memory that was compacted or lost.

### Subagent return protocol — status signal only
Subagents communicate completion **only** by:
1. Updating the task-log beside the plan file (see Rule 5) with the task status:
   - `To Review` — build passed; task ready for review
   - `Build failure` — build failed after 3 attempts; one-line reason appended
   - `blocked: spec gap` — spec ambiguity found; question + options + recommendation documented; agent stops and does NOT choose unilaterally
2. Committing and pushing all changes (`git push origin HEAD`)
3. Stopping (exiting their session)

Subagents must **not** return summaries, explanations, or diffs to the caller.
The caller reads the task-log if it needs outcome details — never the subagent's session context.

### How to brief a subagent
Give it: the spec file paths, the tasks to complete, the rules files to read (paths only), and the
constraint that it must build and fix errors before returning.

### When to take back control
- After the subagent returns: run `dotnet build` and `dotnet test` as main agent
- If a shell command is needed mid-way (migrations, file moves): do it inline, then re-delegate

### Subagent exit checklist (mandatory before returning)
Every subagent must, in this order:
1. Invoke `superpowers:verification-before-completion` — catches non-negotiable violations
2. Build (0 errors)
3. Commit changed files
4. Push (`git push origin HEAD`)

The `Stop` hook warns if uncommitted changes remain.

---

## Rule 3 — Commit After Every Task

**Run `/project:commit` after every task from `tasks.md` is complete.**

A session that ends with uncommitted changes is a session where progress is at risk.
The `Stop` hook warns you — treat it as a hard gate, not a suggestion.

### What counts as "task complete"
- The code builds with no errors
- Tests pass (if the task touched tested code)
- The checkbox in `tasks.md` is checked

---

## Rule 4 — Tasks.md Is the Source of Truth

Check off each task in `Docs/specs/[feature]/tasks.md` as it completes.
The task list is the audit trail for the feature — keep it accurate.

**Sequential constraint:** Never start a task that depends on the output of an incomplete task. Tasks marked `[SEQUENTIAL]` in tasks.md must wait for their predecessor to be committed before starting.

**Parallel exception:** Tasks marked `[P]` (independent, different files/layers) may be dispatched simultaneously as a wave per Rule 2. All tasks in a wave must complete and commit before the next wave begins.

---

## Rule 6 — Research Tool Gate (Context7 → Exa → WebSearch)

Before any web research query, follow this three-tier hierarchy:

1. **Library / framework / SDK / API docs** → Context7 (`mcp__context7__resolve-library-id` → `mcp__context7__query-docs`)
2. **General web research** (comparisons, news, tool evaluations, articles, anything non-library) → Exa MCP (`exa_search`)
3. **Raw `WebSearch` / `WebFetch`** → last-resort fallback only when both Context7 and Exa return no useful result

This applies to **both the main agent and all subagents.**
Reason: `WebFetch` pulls 5,000–15,000 tokens of raw HTML per page; Context7 and Exa return structured results at a fraction of that cost. Exa's query-dependent highlights reduce output tokens by 50–75% vs raw web search.

---

## Rule 5 — Task Status Registration

Agents record task outcomes manually in the task-log file. The `Stop` hook warns if uncommitted changes remain when a session ends.

### Task-log file location
Task-log files live **beside the plan file** in `Docs/superpowers/plans/`, named `<plan-name>-task-log.md`.
Example: plan at `Docs/superpowers/plans/2026-04-23-artists-songs-catalog.md` → log at `Docs/superpowers/plans/2026-04-23-artists-songs-catalog-task-log.md`.
Tasks without a plan association are logged to `Docs/superpowers/plans/unassigned-task-log.md`.

> `Docs/DevEnv/plans/` is for SDD research files only — do not place task-logs there.

### Task-log format (per task entry)
```
---
## Task: <title>
**Plan:** <plan file relative path>
**Status:** in progress | Check build | To Review | Build failure | blocked: spec gap | Spec updated — re-planning required | Early task done | Review task done
**Started:** MM/DD/YYYY
**Completed:** MM/DD/YYYY

### Changed files:
- `relative/path/to/file.cs` [— optional business reason if non-obvious]

### Build notes
[Only present if build was checked — records error summary and diagnosis]
```

### Task statuses
| Status | Meaning |
|--------|---------|
| `in progress` | Task started, work underway |
| `Check build` | Code changed — build verification pending (set on task completion if code files were modified) |
| `To Review` | Build passed — task ready for code review (subagent writes this on successful exit) |
| `Build failure` | Build failed after 3 attempts — needs investigation (subagent writes this on exit) |
| `blocked: spec gap` | Spec ambiguity found — question + options + recommendation documented; waiting for clarification |
| `Spec updated — re-planning required` | Implementation revealed a spec gap; spec updated; tasks.md may need re-ordering |
| `Early task done` | New asset/enhancement completed and committed |
| `Review task done` | Review task completed |
