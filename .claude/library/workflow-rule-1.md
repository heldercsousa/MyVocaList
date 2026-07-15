# Development Workflow — Reference — Rule 1 — Spec-First (full detail)

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Rule 1 — Spec-First

**Before writing any implementation code for a feature, read `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/design.md`.**

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

**BACKLOG.md is the source of truth for feature sequencing.** The main agent (not subagents) is responsible for updating `Docs/Management/BACKLOG.md` status at each milestone below.

0. **Identify** — read `Docs/Management/BACKLOG.md`; pick the highest-priority `🟢 Ready` item in the **Business Features** table, or the next `💡 Pending` item if none are Ready
1. **Brainstorm** — invoke `superpowers:brainstorming`; update BACKLOG.md status → `📋 Spec`
2. **Write spec** — write all three files; user reviews and approves; update status → `🗺️ Plan`
   - **2a. Constitution check** — verify the feature does not violate any Non-Negotiable rule in CLAUDE.md before writing the spec
3. **Write plan** — invoke `superpowers:writing-plans`; user approves; update status → `🟢 Ready`
4. **Implement** — delegate to a subagent (see Rule 2); update status → `🟡 In Progress`
5. **Phase-gate review** — invoke `/sln-review` after each phase before starting the next
   - On ship: update status → `✅ Done` in the **Business Features** table (or **Dev Cycle Craft** table for infrastructure/tooling items)

### Proactive BACKLOG triage — Untracked work

**Any work identified during a session that is not already in BACKLOG.md must get a brief entry before proceeding.**

This applies to:
- A new DevCycleCraft activity (tooling change, process rule, infrastructure work)
- A business feature idea mentioned in conversation (even informally)
- A significant constraint, investigation, or one-off fix that took material effort

**Format — add a row to the appropriate BACKLOG.md table:**

| Date | Activity/Feature | `💡 Pending` | One-line description |

- Use `💡 Pending` for ideas that arrived but aren't being acted on immediately
- Use `🟡 In Progress` if work is starting now
- Keep descriptions to one sentence — BACKLOG is a dashboard, not a spec

**Trigger questions** (ask at any point in a session):
- "Is what I'm about to do tracked in BACKLOG.md?"
- "Did Helder mention a feature or idea that has no BACKLOG row?"
- "Did I discover a process gap that warrants a DevCycleCraft entry?"

If the answer is "no" to the first, or "yes" to the others → add the entry, then proceed.

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
  - Artifact: `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/findings.md`
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
3. At the end of the spike, create `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/findings.md` (see `session-ops.md`).
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
