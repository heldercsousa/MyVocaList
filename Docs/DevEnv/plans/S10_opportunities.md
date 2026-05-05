# S10 — Applicability: Enhancement Opportunities
> Analyzed against current .claude state (see _current_state_summary.md)

---

### OPP-10-01: Spec gate for small vs. large tasks — explicit bypass rule
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1 — Problem-Size Suitability / S10.2 — Trade-offs and Limitations
**Rationale:** The current workflow.md mandates spec-first for everything. SDD research is unambiguous: full spec overhead is net-negative on isolated bug fixes, single-file refactors, and one-off scripts. Giving Claude Code an explicit bypass condition prevents wasted ceremony and builds realistic expectations for what spec-first actually covers.
**Suggested content/change:** Add a "Spec bypass rule" block under Rule 1:

```
### When to skip the full spec workflow
Full spec discipline (requirements.md + design.md + tasks.md) is overhead when ALL of the following are true:
- Single developer, single session, scope fits in one file
- Root cause is known (bug fix) or requirement is one sentence (minor UI adjustment)
- Code will never be touched again (one-off migration, script)
- No coordination with other features or agents required

In these cases: document intent in the commit message; skip spec files; proceed with implementation.
For everything else — multi-session, multi-file, multi-agent, or ambiguous requirements — spec-first is mandatory.
```

---

### OPP-10-02: Exploration-first pattern — vibe-then-spec handoff
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1.2 — When SDD Is Overhead (Exploratory Context) / S10.2.2 — Cultural Resistance
**Rationale:** The current workflow has no guidance for the common scenario where requirements are genuinely unknown. Forcing a spec before exploring produces specs that are premature or wrong. The SDD research recommends an explicit "explore first, formalize once direction is clear" pattern. Without this, Claude Code will either write a bad spec upfront or stall waiting for clarity that only emerges from exploration.
**Suggested content/change:** Add a "Discovery mode" section under Rule 1:

```
### Discovery mode (when requirements are unknown)
When the goal is to discover *what* to build rather than implement a known design:
1. Use vibe coding to explore 2–3 approaches (time-box: one session)
2. When one approach is chosen, STOP and write the spec for that approach
3. Then proceed with spec-first workflow as normal

Never write a spec for an approach you have not validated through exploration. A premature spec locks in a wrong design.
```

---

### OPP-10-03: Spec drift detection — review gate for spec vs. code consistency
**Target:** `.claude/commands/review.md`
**Action:** Add
**Source topic:** S10.2 — Specification Maintenance and Spec Drift
**Rationale:** The current review.md checklist covers build quality, architecture, and DevExpress usage, but has no check for spec-code divergence. SDD research identifies spec drift as a primary ROI killer: specs written at feature start become actively misleading by month 2 if not maintained. Adding a drift check to the review command creates a systematic gate to catch this before it accumulates.
**Suggested content/change:** Add to the review checklist:

```
## Spec-Code Consistency
- [ ] Does the implementation match the feature's `design.md`? Check key interfaces, data flow, and validation rules.
- [ ] If implementation diverged from the spec (e.g., different approach chosen, new constraint discovered), has `design.md` been updated to reflect the actual design?
- [ ] If a new edge case was handled that the spec did not cover, has `requirements.md` been updated?
- [ ] Tasks that were completed — are they checked off in `tasks.md`?

Note: Internal refactors that do not change external behavior do NOT require spec updates. Only update specs when behavior or design intent changes.
```

---

### OPP-10-04: Over-specification guard — thin spec standard
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — The Waterfall Trap: Over-Specification
**Rationale:** The current workflow has no guidance on spec length or detail level. SDD research warns that AI-assisted spec generators produce exhaustive 800+ line specs for simple features, which recreates Waterfall: long planning phases, limited feedback, scope creep from "we might need X." Explicit brevity guidance prevents this anti-pattern in Claude Code's spec writing.
**Suggested content/change:** Add a "Spec length guideline" note to the spec structure table in Rule 1:

```
### Spec length guideline
- **requirements.md**: 1 page. User stories + acceptance criteria + explicit out-of-scope list.
- **design.md**: 1–2 pages. Architecture, interfaces, key decisions. Not pseudo-code.
- **tasks.md**: One task per ~4–8 hours of work. Not one per line of code.

Guard rail: if writing the spec takes longer than the feature would take to implement, you have over-specified. Stop and trim.
Danger signal: "we might need X in the future" appearing in a spec — remove it. Specs describe what is being built now.
```

---

### OPP-10-05: Spec-Anchored maintenance rule — when to update specs
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — Specification Maintenance and Spec Drift
**Rationale:** No current rule covers when specs should be updated vs. left as-is after implementation. Without guidance, either all spec updates are skipped (specs become stale) or every tiny change triggers a spec update (overhead). The SDD "Spec-Anchored with loose maintenance" pattern draws a clear line: update specs when external behavior changes; skip updates for internal-only refactors.
**Suggested content/change:** Add under Rule 4 (Tasks.md is the source of truth):

```
### When to update specs after implementation
Update design.md or requirements.md when:
- A chosen approach differs from what was documented in design.md
- A new business rule or constraint was discovered during implementation
- An edge case was handled that requirements.md did not mention
- The public interface (method signatures, DTO shape) changed from the spec

Do NOT update specs for:
- Internal refactors that do not change external behavior
- Performance optimizations that preserve the same contract
- Code style or naming changes within a module

Rule: if the spec would mislead the *next* agent reading it, update it. If the spec still accurately describes *what* the feature does (not how), leave it.
```

---

### OPP-10-06: Brownfield rule — spec-first for new code only, not retroactive
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1.1 — Brownfield Retrofit Difficulty / S10.1.5 — Brownfield Retrofit: The Hard Case
**Rationale:** The current state summary lists "No rule for brownfield retrofit strategy" as a gap. MyVocaList already has Venue CRUD implemented without formal specs. When future features touch existing code, there is no guidance on whether to retroactively spec the touched code. SDD research is unambiguous: spec only the *new feature or change*, not the entire module. This prevents wasted effort and prevents agents from attempting to reverse-engineer specs from existing code.
**Suggested content/change:** Add a note to Rule 1:

```
### Brownfield rule: spec what you are building, not what already exists
When a new feature touches existing code that has no spec:
- Write the spec for the NEW FEATURE only — not for the existing code it integrates with
- Describe the change: what new behavior is added, how it integrates at the boundary
- Do NOT retroactively spec the existing code (VenueService, VenueRepository, etc.) unless you are explicitly changing its behavior
- When you DO change existing behavior: add a spec section describing the change and update the relevant `design.md`

This is the Spec-First-for-New-Features pattern. Coverage grows organically as features are added; legacy code remains unspecified until actively changed.
```

---

### OPP-10-07: Spec skip for bug fixes — commit message as the spec
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.1.2 — When SDD Is Overhead / S10.2 — Overhead on Small Tasks
**Rationale:** The current workflow has no explicit guidance for bug fixes. SDD research shows that spec overhead on bug fixes (where root cause is known and scope is clear) is 50–100% of implementation time — clearly net-negative. Giving Claude Code a "commit message is the spec" pattern for bug fixes prevents ceremony waste while preserving traceability.
**Suggested content/change:** Add to Rule 3 (Commit after every task):

```
### Bug fixes: commit message is the spec
For bug fixes where root cause is documented and scope is contained:
- No spec file required
- Document in the commit message: (1) root cause, (2) fix applied, (3) how to verify
- Format: "fix: [symptom] — root cause: [cause] — fix: [approach] — verified by: [test or manual step]"
- If the bug reveals a missing requirement, ADD that requirement to the relevant `requirements.md` — the bug fix itself still does not need a spec file
```

---

### OPP-10-08: Subagent spec delegation constraint — specs must be pre-approved
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S10.2 — Specification Writing Skill Floor / S10.2.2 — Cultural Resistance
**Rationale:** SDD research identifies specification writing as requiring higher skill than code writing — it requires anticipating edge cases, understanding architectural constraints, and abstracting from implementation. The current workflow delegates implementation to subagents but has no explicit rule about who writes the spec. Subagents writing their own specs is a risk: they may over-specify, under-specify, or encode wrong architectural choices. The spec must be written and approved by the main agent (or Helder) before subagents are dispatched.
**Suggested content/change:** Add a constraint to Rule 2 (Subagent Delegation):

```
### Spec ownership constraint
Subagents execute against specs — they do not write specs.
- Spec writing (requirements.md, design.md, tasks.md) is always the main agent's responsibility, reviewed by Helder before any subagent is dispatched.
- A subagent that discovers a spec gap (missing requirement, ambiguous design decision) must set status to `blocked: spec gap` and stop. It does not write or modify the spec unilaterally.
- The main agent resolves the gap, updates the spec, gets Helder's approval, then re-dispatches.
```
