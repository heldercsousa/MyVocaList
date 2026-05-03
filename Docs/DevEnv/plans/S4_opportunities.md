# S4 — Context & Memory: Enhancement Opportunities

> Analyzed against: `Docs/DevEnv/plans/_current_state_summary.md` and `~/.claude/projects/.../memory/MEMORY.md`
> SDD source files: S4_Context_and_Memory.md, S4_1_Memory_Bank_Context_Files.md, S4_2_Context_Engineering.md, S4_3_External_Integrations.md, S4_1_1_Cross_Session_Context_Loss.md

---

### OPP-4-1: Add session-end constraint capture ritual to workflow
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S4.1.1 — Cross-Session Context Loss
**Rationale:** The SDD research identifies "just-learned constraints" as the #1 category of cross-session context loss. MyVocaList already has examples of discovered constraints that ended up in code-principles.md (ObservableRangeCollection ANR rule, SafeAreaEdges default) — but only after being discovered the hard way. There is no workflow trigger to capture these at session end. Adding a named ritual prevents recurring re-discovery on this project where DevExpress and MAUI have many non-obvious behavioral constraints.
**Suggested content/change:** Add a Rule 7 or append to Rule 3 in workflow.md:

```
## Rule 7 — Session-End Constraint Capture

When a session discovers a new constraint — a DevExpress behavior, an EF Core migration limit, a MAUI
platform quirk, a SQLite performance requirement — capture it before ending the session:

1. If it's permanent project architecture: add to CLAUDE.md or the relevant .claude/rules/ file
2. If it's feature-scoped: add to the spec's design.md under a "Discovered Constraints" section
3. Commit the update as part of the task commit (Rule 3)

Signs a constraint was discovered: "I found that...", "turns out...", "this fails because...",
"we need to avoid..." appearing in the session before a build fix.

Do not end the session leaving a discovered constraint only in conversation history.
```

---

### OPP-4-2: Add CLAUDE.md size monitoring guidance
**Target:** `CLAUDE.md`
**Action:** Add
**Source topic:** S4.2 — Context Engineering (CLAUDE.md bloat failure mode)
**Rationale:** The current CLAUDE.md is ~550 lines, which the SDD research identifies as at the threshold where it becomes a context tax. The "Continuous Enhancement" section currently only says to add/update/delete rules — it doesn't mention size governance. Since MyVocaList is actively growing (Artists, Songs, Queue features planned), the file will grow without a size gate. The research cites 600 lines as the refactoring threshold for context quality.
**Suggested content/change:** Append to the "Continuous Enhancement" section of CLAUDE.md:

```
**Context size governance:** CLAUDE.md must stay under 600 lines. When it approaches this limit:
- Move stable, detailed patterns to .claude/library/ or .claude/rules/ files
- Replace inline examples with "See .claude/rules/X.md" references
- Keep only routing tables, non-negotiables, and architectural constraints inline
Do not add rules that a linter or type-checker already enforces.
```

---

### OPP-4-3: Add context window exhaustion guidance for long tasks
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S4.2 — Context Engineering (isolate strategy) + S4.1.1 — memory coherence degrades at turn 73
**Rationale:** The current state summary lists "No rule for context window exhaustion management in long tasks" as a gap. The SDD research identifies coherence degradation starting around turn 73 in a 200K-token context. MyVocaList tasks like implementing a full CRUD feature (spec + domain + infra + services + VM + page) involve many file reads and can easily reach this point. Without guidance, agents contradict earlier decisions or forget constraints mid-task.
**Suggested content/change:** Add to Rule 2 (Subagent Delegation) in workflow.md:

```
### Context exhaustion warning signs
A subagent may be approaching context exhaustion when it:
- Contradicts a decision made earlier in the same session
- Re-proposes an approach that was already ruled out
- Ignores a non-negotiable that was in the briefing

**When this happens:** The main agent should end the current subagent, commit whatever is done, then
spawn a fresh subagent with a new briefing that re-states the constraints and picks up from
the last committed state. Never try to "remind" a context-exhausted agent — start fresh.
```

---

### OPP-4-4: Explicit "decisions made in conversation" capture in spec design.md template
**Target:** `.claude/rules/workflow.md`
**Action:** Update
**Source topic:** S4.1.1 — Cross-Session Context Loss (architectural decisions made in conversation)
**Rationale:** The current workflow requires writing spec files (requirements.md, design.md, tasks.md) before coding. But it says nothing about capturing trade-off decisions made during brainstorming or design review conversations — the "we chose X over Y because Z" reasoning that is frequently lost. For MyVocaList, examples include: why we use round-based queue progression instead of time-based, why we don't use MediatR yet, why we use composition over inheritance in VMs. These are invisible to a new agent.
**Suggested content/change:** Add a "Key Decisions" section to the design.md spec template described in Rule 1:

```
### Spec structure update (design.md)
Add a "## Key Decisions" section to every design.md:

| Decision | Alternatives considered | Why chosen | Revisit condition |
|----------|------------------------|------------|-------------------|
| Round-based queue progression | Time-based, random | Karaoke convention; singer expectation | If async events needed |

This section is updated by the developer (not the agent) as trade-offs are settled during brainstorming
or implementation. It is the persistent record of reasoning that conversation history does not preserve.
```

---

### OPP-4-5: Add spec-drift detection check to review.md
**Target:** `.claude/commands/review.md`
**Action:** Update
**Source topic:** S4.1.1 — Cross-Session Context Loss (spec staleness and implementation intent)
**Rationale:** The current review.md checklist covers code quality, architecture, DevExpress patterns — but the current state summary explicitly notes "review.md doesn't cover spec-drift detection or spec vs code consistency checks." The SDD research identifies spec staleness as the third major category of cross-session context loss. For MyVocaList, where specs are written before implementation and features span multiple sessions, specs can drift significantly from what was actually built. The review step is the natural place to catch this before committing.
**Suggested content/change:** Add a spec-drift check to the review.md checklist:

```
## Spec-Drift Check (run after implementation tasks)
- Does the implemented behavior match the acceptance criteria in requirements.md?
- Were any validation rules changed during implementation? Update requirements.md if so.
- Were any design decisions reversed or altered? Update design.md Key Decisions if so.
- Are all tasks.md checkboxes accurate (checked = done, unchecked = not done)?

If spec drift is found: update the spec files in the same commit as the code. Never leave specs
out of sync with working code.
```

---

### OPP-4-6: Document subagent state handoff protocol for multi-session features
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S4.1.1 — Cross-Session Context Loss (agent hand-offs require structured state)
**Rationale:** The current workflow describes subagent delegation but not what state a subagent must write when a feature spans multiple sessions or multiple subagent waves. The SDD research is explicit: "conversation history alone is not sufficient" for agent hand-offs. MyVocaList features already span multiple sessions (Venues CRUD took multiple sessions; Artists + Songs will be larger). Without a state handoff protocol, wave 2 subagents have no reliable record of what wave 1 decided or discovered.
**Suggested content/change:** Add to Rule 2 (Subagent Delegation) in workflow.md:

```
### Multi-session state handoff

When a feature spans more than one development session or more than one wave of subagents:

**Before ending a wave:** The main agent reads the task-log and verifies:
1. All completed tasks have their Changed files documented in the task-log
2. Any constraint discovered during the wave is captured in a rules/spec file
3. The tasks.md checkboxes accurately reflect what is done vs. what remains

**At the start of the next wave:** Briefings must include:
- Which tasks.md entries are already checked
- Any constraint discoveries from the previous wave (file paths, not inline content)
- The current git HEAD (so the subagent can diff to understand what changed)

This prevents re-work and contradictory decisions across waves.
```

---

### OPP-4-7: Add "lost-in-the-middle" mitigation to CLAUDE.md structure guidance
**Target:** `CLAUDE.md`
**Action:** Update
**Source topic:** S4.2 — Context Engineering ("lost-in-the-middle" effect and attention budgets)
**Rationale:** The SDD research documents that model correctness drops significantly around 32K tokens, with attention concentrated at the beginning and end of context. The current CLAUDE.md has the "Non-Negotiables" section near the bottom (~line 120+), after lengthy sections on architecture, MCP/skills, rules files, commands, and coding rules. These non-negotiables are the most critical constraints for Claude Code on this project — they should appear earlier so they receive stronger attention during inference.
**Suggested content/change:** Restructure CLAUDE.md so "Non-Negotiables" appears immediately after the "App" and "Stack" sections, before Architecture. This ensures the highest-priority constraints are in the first 30–40 lines where model attention is strongest. No content changes — positional reorder only.

Note: Do not move this mechanically — evaluate whether the current position is already effective given that CLAUDE.md is loaded as system context (position 1 in the context stack). If testing shows no regression, deprioritize this change.

---

### OPP-4-8: Add verification-before-completion reference to workflow
**Target:** `.claude/rules/workflow.md`
**Action:** Add
**Source topic:** S4.1.1 — Cross-Session Context Loss (memory reconciliation loops / Pattern 4)
**Rationale:** The current state summary notes "No rule for hallucination detection or verification before completion (skill exists but not in CLAUDE.md)." The `superpowers:verification-before-completion` skill exists but is not referenced in the workflow rules. For MyVocaList, where subagents implement across domain/infra/services/UI layers, a verification pass catches constraint violations (e.g., SafeAreaEdges, DisplayAlert usage, hardcoded colors) before commit — exactly the reconciliation loop the SDD research describes.
**Suggested content/change:** Add to the "Subagent exit checklist" in Rule 2:

```
### Subagent exit checklist (mandatory before returning)
Every subagent must, in this order:
1. Invoke `superpowers:verification-before-completion` — catches non-negotiable violations
2. Build (0 errors)
3. Commit changed files
4. Push (`git push origin HEAD`)
```

(Replace the current 3-step checklist with this 4-step version.)
