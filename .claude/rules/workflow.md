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

### Task sizing limits — context window budget

Each subagent task must fit within one context window without compaction. A task that exhausts the context window produces unreliable output: the agent forgets earlier decisions, contradicts itself, or silently drops requirements.

**Sizing limits per subagent task:**

| Task type | Max files touched | Max estimated effort |
|-----------|-------------------|----------------------|
| New service method + tests | 4 files | 90 min |
| New ViewModel + page + tests | 5 files | 2 hours |
| Migration + repository + tests | 4 files | 90 min |
| UI page only (XAML + code-behind) | 3 files | 60 min |
| Full CRUD feature (all layers) | Split into 2+ tasks | — |

**Rule:** If a proposed task exceeds these limits, split it into independent sub-tasks before dispatching. Do not attempt to complete a full CRUD feature in a single subagent.

**Warning sign:** A subagent briefing that lists > 5 files or > 2 hours of estimated work is a sizing violation. Decompose before dispatching.

### Sequential-only file registry

Some files must never be edited by more than one agent at a time. These are **hotspot files** — editing them concurrently causes merge conflicts or produces incoherent results.

**Sequential-only files (one writer at a time, always):**

| File | Reason |
|------|--------|
| `MauiProgram.cs` | DI registration — ordering matters; parallel edits produce conflicts |
| `AppShell.xaml` / `AppShell.xaml.cs` | Route registration — one canonical route table |
| `AppDbContext.cs` | EF Core model config — entity set definitions must be coherent |
| Any `*Migration.cs` files | EF migrations are sequential by design |
| `GlobalUsings.cs` (any project) | Global using declarations — merge conflicts produce duplicate errors |
| `Directory.Build.props` | Shared MSBuild properties — parallel edits produce conflicts |
| `tasks.md` (any spec) | Task status tracking — parallel checkbox edits produce divergent state |

**Rule:** If a wave requires two or more subagents to touch the same sequential-only file, serialize those tasks — do not parallelize them. Complete one task, commit, then dispatch the next.

**How to add entries:** If a session discovers a new hotspot file (a file where parallel edits caused a conflict or incoherent output), add it to this registry before ending the session.

### Cross-spec review gate before multi-spec wave

When a wave will implement tasks that touch two or more features simultaneously (i.e., tasks from different spec directories), a cross-spec review gate is required before dispatching.

**Gate checklist (main agent must confirm before dispatch):**

- [ ] All specs involved have passed the spec quality gate (see Rule 1)
- [ ] Shared domain types used by both features are defined in one canonical spec (no duplication)
- [ ] No acceptance criterion in Spec A contradicts an acceptance criterion in Spec B
- [ ] Invariants from both specs are compatible (e.g., both define "Queue" consistently)
- [ ] The Domain Vocabulary across both specs uses the same terms for the same concepts

**Why this matters:** Two specs written independently can define the same domain concept differently. When parallel subagents implement against conflicting specs, they produce incoherent domain logic that is expensive to reconcile after the fact.

**If a conflict is found:** Resolve it in the specs before dispatching. Do not dispatch the wave with the conflict unresolved and "let the subagents figure it out."

### Pre-wave dependency check + scope isolation

Before dispatching a wave, the main agent must perform a dependency check:

1. **List all files each proposed subagent will touch** (based on the role scope block).
2. **Check for overlaps** — if two subagents in the wave touch the same file, the wave is unsafe:
   - If the file is in the sequential-only registry → serialize those tasks.
   - If the file is not in the registry but shared → evaluate whether the overlap is additive (different sections) or conflicting (same section). If conflicting, serialize.
3. **Check for output/input dependencies** — if Subagent B depends on a type or interface that Subagent A will create, B must not start until A has committed.
4. **Confirm scope isolation** — each subagent in the wave must operate on a disjoint set of files. If disjoint sets cannot be established, reduce the wave size.

**Multi-agent scope conflict rule:** Two subagents must never be dispatched to modify the same file in the same wave. The one that commits second will overwrite or conflict with the first. There is no safe concurrent file write in this workflow.

**Document the check:** Record the file ownership map in the task-log before dispatching:
```
Wave N file ownership:
- Subagent A: [file1.cs, file2.cs]
- Subagent B: [file3.cs, file4.xaml]
- Overlap: none ✓
```

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

### Spec gap escalation — documentation requirement

When a subagent encounters a spec gap (an ambiguity, missing acceptance criterion, or contradictory requirement), it must document the gap with enough detail for Helder to make a decision.

**Required documentation format (in the task-log):**
```
### Spec gap: [short title]
**Location:** [spec file + section where the gap was found]
**Gap description:** [one sentence: what is missing or ambiguous]
**Options:**
- Option A: [description] — [consequence]
- Option B: [description] — [consequence]
**Recommendation:** Option [A/B] because [one sentence rationale]
**Blocking:** [Yes — cannot proceed without resolution / No — proceeding with Option A as documented assumption]
```

**Rules:**
- The subagent must NOT choose between options unilaterally (unless marking it as an assumption with `Blocking: No`).
- If `Blocking: Yes`, set task-log status to `blocked: spec gap` and stop.
- If `Blocking: No`, proceed with the documented assumption and flag it clearly. The assumption will be reviewed at the `To Review` stage.
- Never silently resolve a spec gap. Silence is not consensus.

### Subagent scope constraint — no unilateral redesign

Subagents implement what the spec says. They do not redesign, refactor beyond task scope, or make architectural decisions.

**Specifically, subagents must NOT:**
- Change an interface signature that is not part of their assigned task
- Introduce a new abstraction layer not described in `design.md`
- Move logic between layers (e.g., from Service to ViewModel) without spec authorization
- Add new repository methods beyond what the spec's interface section defines
- Rename entities, DTOs, or methods to names that differ from the spec
- "Improve" a design they disagree with — they must implement it and note the concern in the task-log

**If a subagent believes the spec is wrong or suboptimal:**
1. Note the concern in the task-log under a `### Design concern` section
2. Implement exactly what the spec says
3. Set status to `To Review` and let Helder evaluate the concern during review

**Why:** A subagent that redesigns while implementing introduces changes that were not reviewed, not approved, and not traced to any acceptance criterion. These changes are invisible until something breaks.

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

### Shared contracts — required before parallel implementation

Before dispatching a wave where two or more subagents will implement components that communicate (e.g., a service and its consumer, a repository and its interface, a ViewModel and its service), the main agent must:

1. **Write or verify the shared contracts** — the interfaces, DTOs, and method signatures that both sides depend on.
2. **Commit the contracts to the repository** before the wave starts. Subagents must implement against committed contracts, not against definitions that exist only in the briefing.
3. **Include the contract file paths** in every affected subagent's briefing so they read the same source.

**What counts as a shared contract:**
- Any `interface` in the Domain or Services project
- Any `record` DTO in the Contracts project
- Any navigation route name used by multiple pages
- Any DI registration key (`AddScoped<IFoo, Foo>`) depended on by two or more components

**Rule:** Parallel subagents that share a contract but implement against different assumptions will produce build failures or silent behavioral divergence. Commit the contract first — then parallelize.

### Wave handoff — inject actual contracts for new artifacts

When a wave produces a new type, interface, or DTO that a subsequent wave will consume, the main agent must extract and inject the actual contract into the next wave's briefing — not a file path alone.

**Why:** The next wave's subagents start with empty context. If they are only given a file path, they will read the file and infer the contract. But inference is lossy — they may miss nuances, misread a signature, or use a stale cached version.

**Protocol:**
1. After a wave completes, the main agent reads the output files for new public interfaces, DTOs, and service contracts.
2. For each new artifact that a subsequent wave depends on, extract the **exact signature** (interface definition, record declaration, or method signature).
3. Include these extracted signatures verbatim in the next wave's briefing under a `## Contracts from previous wave` section.

**Example briefing addendum:**
```markdown
## Contracts from previous wave

### IVenueRepository (new — committed in Wave 1)
Task<(IEnumerable<VenueListItemDto> items, int totalCount)> GetPagedAsync(
    int pageNumber, int pageSize, string? query, CancellationToken ct);
Task<bool> ExistsByNameAsync(string name, CancellationToken ct);

### VenueListItemDto (new — committed in Wave 1)
public record VenueListItemDto(int Id, string Name, int SingerCount);
```

This eliminates the "I assumed the interface looked like X" class of integration bugs between waves.

### How to brief a subagent
Give it: the spec file paths, the tasks to complete, the rules files to read (paths only), and the
constraint that it must build and fix errors before returning.

### Adversarial Critic pattern

For high-risk waves (new public interfaces, schema changes, significant business logic), the main agent should apply the **Adversarial Critic** pattern before dispatching:

**Protocol:**
1. After reading the spec and drafting the briefing, the main agent internally challenges its own plan by asking:
   - "What is the most likely way a subagent will misinterpret this briefing?"
   - "What spec ambiguity could cause the subagent to make a wrong choice?"
   - "Which acceptance criterion is vaguest and most likely to be implemented incorrectly?"
   - "What would break if the subagent implements the happy path only and ignores error handling?"

2. For each identified risk, the main agent either:
   - **Tightens the briefing** — add explicit instruction to address the ambiguity
   - **Tightens the spec** — update the spec with the missing detail before dispatching
   - **Flags it to Helder** — if the risk requires an architectural decision

3. A briefing that passes Adversarial Critic review should have no ambiguities that a subagent could resolve incorrectly without violating the spec.

**Why:** Subagents implement what they are told, not what was meant. The Adversarial Critic forces the orchestrator to find the gap between intent and instruction before it becomes a bug.

### Verifier subagent

After a wave completes and before the main agent proceeds to the next wave, a **Verifier subagent** may be dispatched to independently validate the wave's output.

**When to use a Verifier:**
- After any wave that touched more than 3 files
- After any wave that implemented a new public interface or DTO
- After any wave where a subagent reported `Build failure` or `blocked: spec gap`
- Before a wave that depends on correctness of the previous wave's output

**Verifier briefing template:**
```
Role: Verifier
Scope: Verify wave N output against spec and build
Spec source: [path to design.md + requirements.md]
Wave output files: [list of files committed in wave N]

Your tasks:
1. Read each committed file and confirm it matches the spec (interfaces, signatures, business rules)
2. Run dotnet build — confirm 0 errors
3. Run dotnet test — confirm 0 failures
4. Check that task-log entries for wave N are complete (status, changed files, build notes)
5. Write your findings to the task-log:
   - If all clear: status = "Verifier: wave N OK"
   - If issues found: status = "Verifier: wave N FAILED — [one-line summary]"
```

**The Verifier must not fix anything.** It reports findings only. The main agent decides whether to dispatch a fix subagent or escalate to Helder.

See `.claude/agents/verifier.md` for the full Verifier agent definition.

### Multi-session state handoff protocol

When a feature spans multiple sessions, the state at session end must be captured so the next session can resume without loss.

**Session-end handoff artifact (write to `Docs/superpowers/plans/<plan-name>-handoff.md`):**
```markdown
# Session Handoff — [Feature Name] — [YYYY-MM-DD]

## Last completed wave
Wave N — [brief description] — all tasks committed

## Current state
- Build: PASS / FAIL
- Tests: N passing, N failing
- Last committed task: [task title]

## Next wave
Wave N+1 — [tasks to dispatch]
- Task A: [scope, files owned]
- Task B: [scope, files owned]

## Open items
- [spec gaps, pending decisions, deferred concerns]

## Contracts in play
- [current interface signatures that the next wave will consume]

## Files modified this session
- [list of all files touched since session start]
```

**Rule:** The session-end handoff artifact must be committed before the session ends. It is the only reliable state source for the next session — in-context memory does not persist.

**Session resume rule:** At the start of a new session, read the handoff artifact first — before reading MASTER_PLAN.md or the spec. It tells you exactly where to resume.

### Context exhaustion warning signs

Context window exhaustion degrades output quality before the window is fully used. Recognize the early signs and act before the damage compounds.

**Warning signs in subagent output:**
- Subagent contradicts a decision it made earlier in the same session
- Subagent asks about information it was given in the briefing
- Subagent produces code that duplicates something it already wrote
- Subagent forgets a constraint it acknowledged earlier (e.g., uses `DisplayAlert` after being told not to)
- Build errors reference types or namespaces the subagent invented rather than read from the spec
- Subagent output becomes shorter and less specific with each iteration
- Subagent claims work is done but Changed files list is sparse relative to the task scope

**Warning signs in orchestrator context:**
- You are writing a briefing from memory without re-reading the spec
- You cannot recall what the previous wave committed without checking the task-log
- You are reasoning about code structure from cached impressions rather than reading the current file

**Response protocol:**
1. If the subagent shows warning signs: kill it (see Kill criteria), re-read the spec, produce a tighter briefing, dispatch a fresh subagent.
2. If the orchestrator shows warning signs: stop. Re-read MASTER_PLAN.md, the spec, and the task-log. Resume from verified ground truth.

### Context reset discipline for orchestrator

The orchestrator (main agent) accumulates context across waves. After many waves, earlier decisions may be compacted or lost. Treat each wave boundary as a potential context reset point.

**Context reset discipline:**

1. **Before dispatching each wave:** Re-read the spec (requirements.md + design.md) fresh — do not rely on in-context memory of earlier reads.
2. **Before briefing:** Confirm the spec paths in the briefing are current (not pointing to a stale version).
3. **After context compaction** (when Claude signals the context is being compressed): re-read MASTER_PLAN.md and the current tasks.md to re-establish ground truth. Do not continue from memory.
4. **Session resume:** Always start a new session by reading MASTER_PLAN.md → the current spec → the task-log. Never resume from memory alone.

**Warning sign:** If you find yourself writing a briefing without having just read the spec, stop. The spec drift you introduce in the briefing will become implementation drift.

**Rule:** The orchestrator's job is to hold the spec as the source of truth and inject it correctly into every wave. That job cannot be done from cached memory — it requires fresh reads at each wave boundary.

### Wave completion discovery briefs

After a wave completes and post-wave verification passes, the main agent must produce a **discovery brief** before dispatching the next wave. This brief documents what was actually built versus what was planned, so the next wave's briefing is grounded in reality.

**Discovery brief format (write to task-log or a `wave-N-discovery.md` note):**
```
## Wave N Discovery Brief

### What was built
- [Actual interfaces/types committed, with signatures]
- [Actual files created/modified]
- [Any deviations from the spec, with spec update references]

### Contracts for Wave N+1
- [Verbatim signatures of new interfaces/DTOs the next wave will consume]

### Open items
- [Any spec gaps found, with status (resolved/escalated/deferred)]
- [Any build warnings to watch]
- [Any test coverage gaps identified]
```

**Why:** The gap between "what was planned" and "what was built" grows with every wave. A discovery brief closes that gap before it propagates into the next briefing. Without it, the main agent briefs Wave 2 as if Wave 1 produced exactly what was designed — which it rarely does.

### Post-wave verification — main agent runs build independently

After every wave completes, the main agent must run the build and tests independently — not rely on the subagent's self-reported verification.

**Protocol (main agent, after each wave):**
1. Run `dotnet build` — confirm 0 errors. Do not proceed to the next wave if there are errors.
2. Run `dotnet test` — confirm 0 failures. Investigate any new failures before proceeding.
3. Review the task-log entries from the wave — confirm all entries have Verification evidence and Changed files.
4. If a subagent reported `Build failure` or `blocked: spec gap`, resolve before dispatching the next wave.

**Why independent verification?** Subagents report their own build results in the task-log. A subagent that hit context exhaustion may report `PASS` based on an earlier run that no longer reflects the committed state. The main agent's independent build is the authoritative gate.

**Rule:** The main agent never skips post-wave verification to "save time." A wave that passes post-wave verification is a stable foundation. A wave that is not verified is technical debt that compounds into the next wave.

### When to take back control
- After the subagent returns: run `dotnet build` and `dotnet test` as main agent
- If a shell command is needed mid-way (migrations, file moves): do it inline, then re-delegate

### Silent task completion — post-edit re-read requirement

A subagent that edits a file and immediately marks the task done without re-reading the result is practicing **silent completion** — it has not verified that the edit was applied correctly.

**Rule:** After every file edit, the subagent must re-read the affected section of the file and confirm:
1. The edit was applied at the correct location
2. The edit did not introduce a syntax error or structural inconsistency visible in the surrounding context
3. The edit matches what the spec required (not just "an edit was made")

**Specifically:**
- After editing a `.cs` file: re-read the method signature and its surrounding class context
- After editing a `.xaml` file: re-read the modified element and its parent container
- After editing a spec file: re-read the section updated to confirm the note was added cleanly

**Why:** LLM-based agents can produce edits that are syntactically correct in isolation but contextually wrong (e.g., wrong indentation level, mismatched braces, applied to the wrong overload). The re-read catches these before they become build errors.

**This is not optional.** A task-log entry that lacks a post-edit verification step is incomplete.

### Living spec protocol — write decisions back before stopping

When a subagent makes an implementation choice that is not fully specified (e.g., chose one of two valid approaches, discovered a constraint, resolved an ambiguity), it must write that decision back to the spec before stopping.

**Protocol:**
1. At the end of the task, review all decisions made that were not explicitly specified.
2. For each such decision, add a `> **Spec updated [YYYY-MM-DD]:** [decision summary]` note to the relevant spec file (`design.md` or `requirements.md`).
3. If the decision is a Key Decision (architecture-level), add it to the `Key Decisions` section of `design.md` using the standard format.
4. Commit the spec update as part of the same commit as the implementation (or as a separate commit immediately before stopping).

**Examples of decisions to write back:**
- "I added a `CreatedAt` timestamp to the entity because the spec didn't say not to"
- "I used `Task.WhenAll` for parallel validation — the spec didn't specify sync or async"
- "I discovered that SQLite requires the collation to be set per-query, not per-column — added to constraints-registry.md"

**Rule:** A subagent that makes undocumented decisions is leaving hidden state in the codebase. The next agent to touch that area will not know about those decisions and may override them.

### Kill criteria for stuck subagents

A stuck subagent is one that is looping, making no progress, or producing degrading output. The main agent must recognize the signs and terminate the subagent.

**Kill criteria — terminate and restart if ANY of these are true:**

| Signal | Action |
|--------|--------|
| 3 build failures with no diagnostic improvement | Kill — dispatch fresh subagent with tighter briefing |
| Subagent modifies the same file 4+ times in a row | Kill — context is exhausted; decompose the task |
| Subagent asks an open-ended "how should I approach this?" question | Kill — the briefing was insufficient; rewrite it |
| Subagent output contradicts the spec in a way it was already corrected on | Kill — context compaction has erased the correction |
| No commit after 45+ minutes of apparent work | Kill — something is wrong; investigate before re-dispatching |
| Subagent produces code that references files or types that don't exist | Kill — hallucination; context is stale |

**3-strike error recovery protocol (OPP-8-14):**
1. First strike: identify root cause, tighten briefing, re-dispatch
2. Second strike: decompose the task into smaller sub-tasks; re-dispatch the smallest unit
3. Third strike: escalate to Helder — do not dispatch a fourth attempt without human review

**Rule:** A stuck subagent consumes tokens without producing value. Killing and re-dispatching with a better briefing is always cheaper than letting a stuck agent continue.

### Build retry cap

If a build fails, the subagent may attempt to fix it. The retry cap is **3 attempts**.

**Protocol:**
- Attempt 1: Diagnose the error, apply a fix, rebuild.
- Attempt 2: If still failing, re-read the spec and the failing file from scratch — do not patch the previous patch.
- Attempt 3: If still failing, stop. Do NOT make a fourth attempt.

**On the third failure:**
1. Set task-log status to `Build failure`
2. Append a one-line diagnosis: what the error is and what was tried
3. Commit whatever state exists (even if broken — use a `wip:` prefix on the commit message)
4. Push and stop

**Why a cap?** A subagent that loops on build errors without a cap will exhaust its context window making increasingly desperate patches. After 3 attempts, the problem likely requires architectural guidance — not more patching.

### Subagent exit checklist (mandatory before returning)
Every subagent must complete ALL of these steps in order before stopping:

1. **Invoke `superpowers:verification-before-completion`** — catches non-negotiable violations (DevExpress-first, SafeAreaEdges, English-only, no DisplayAlert, etc.)
2. **Build:** Run `dotnet build` and confirm 0 errors. If build fails, apply the build retry cap (max 3 attempts). Document result in Verification evidence.
3. **Test:** If any `.cs` implementation file was changed, run `dotnet test` and confirm 0 failures. Document result in Verification evidence. Skip only if no code files were modified.
4. **Post-edit re-read:** Re-read the affected section of every edited file and confirm correctness (see Silent task completion rule).
5. **Living spec check:** Review decisions made during implementation — write back any undocumented decisions to the spec.
6. **Task-log:** Complete the task-log entry including Changed files, Verification evidence, and AC traceability matrix (if applicable).
7. **Commit:** Commit all changed files including any spec updates.
8. **Push:** `git push origin HEAD`

**The `Stop` hook warns if uncommitted changes remain. Treat it as a hard gate.**

A subagent that stops without completing all 8 steps has not finished the task.

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

### Proof of action — Changed files is mandatory

A task-log entry that claims `To Review` without a `### Changed files` section is **invalid**. The main agent must reject it and request a corrected entry.

**Rule:** Every task-log entry that represents completed implementation work must include an explicit list of every file that was created or modified. This is not optional documentation — it is the proof that the task was actually done.

**Format (mandatory):**
```
### Changed files:
- `relative/path/to/file.cs` — reason (e.g. "added GetPagedAsync method")
- `relative/path/to/test.cs` — reason (e.g. "added 3 test cases for GetPagedAsync")
```

**If no files were changed:** The task was not implemented. Do not set status to `To Review`. Either document why the task was a no-op (with spec reference) or complete the task.

**Why:** Subagents can falsely claim completion without having made any changes. The Changed files list is the minimum verifiable evidence that work was done. A `git diff` can independently confirm it.

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

For tasks that implement user-facing behavior, the task-log entry must include an **AC traceability matrix** — a table linking each acceptance criterion from `requirements.md` to the implementation evidence.

**Format (add to task-log entry when status is `To Review`):**
```
### AC traceability
| AC ref | Criterion (short) | Implementation evidence |
|--------|-------------------|------------------------|
| AC-1 | Singer added appears in queue | VenueService.AddSingerAsync returns (true, ...) |
| AC-2 | Duplicate name rejected | ValidateNameInput returns (false, "already exists") |
| AC-3 | Queue order preserved after add | GetQueueOrderedAsync tested in QueueRepositoryTests |
```

**Rules:**
- Every AC in the spec that is addressed by this task must appear in the matrix.
- "Implementation evidence" must be a specific code location, not a vague claim ("it works").
- If an AC was not implemented (out of scope for this task), mark it `deferred — task [X]`.
- ACs with no evidence entry will be flagged during review as unverified.

**When to skip:** Tasks with no user-facing acceptance criteria (e.g., pure refactors, config changes, documentation updates) do not require a traceability matrix.

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
