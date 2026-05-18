# Plan: Compress workflow.md Below 40k Chars

## Context

`workflow.md` is 118.5k chars — nearly 3× the 40k Claude Code performance threshold. Every session loads it in full, consuming tokens that reduce available context for reasoning and code generation. The file grew because it combines two concerns: **routing rules** (what to do, when) and **reference protocols** (how to do it in detail, with templates and examples). Only the routing layer needs to load every session.

The `.claude/` folder already has the right structure for the fix:
- `agents/orchestrator.md` — thin today (89 lines); explicitly delegates detail to `workflow.md`
- `agents/implementor.md` — thin today (108 lines); has basics but misses many workflow.md sections
- `library/` — on-demand reference files (loaded via skills, not auto-loaded)

The fix: move detailed reference protocols to `agents/` and `library/`, replace them in `workflow.md` with one-line "See X" pointers.

---

## Extraction Map

### 1. Expand `agents/orchestrator.md` (~+30k chars moved from Rule 2)

Move these Rule 2 sub-sections verbatim, appended after the existing content:

- `Pre-dispatch validation checklist` (full checklist block)
- `Pre-wave dependency check + scope isolation`
- `Briefing protocol — paths only, never paste content` + Role scope declaration
- `Thick-slice task format for briefings`
- `Adversarial Critic pattern`
- `Wave handoff — inject actual contracts for new artifacts`
- `Wave completion discovery briefs`
- `Multi-wave checkpoint pattern`
- `Dependency-first merge sequencing`
- `Git worktrees as isolation primitive`
- `Shared contracts — required before parallel implementation`
- `Pre-parallel interface contracts — commit before dispatch`
- `Cross-spec review gate before multi-spec wave`
- `Fresh-context iteration pattern`
- `Kill criteria for stuck subagents` (3-dispatch escalation included)
- `Approval Authority Matrix`
- `Review SLA and Risk-Tiered Review Lanes` (full lane table + enforcement)
- `DGI complexity classification`

Update the opening reference line in `orchestrator.md` to say:
> "For commit discipline, task-log format, and spec quality gates, see `.claude/rules/workflow.md`."

### 2. Expand `agents/implementor.md` (~+12k chars moved from Rule 2)

Move these Rule 2 sub-sections, appended after the existing content:

- `Pre-task context gate — verify spec + test exist` (full checklist)
- `Intent verification before To Review` (full checklist)
- `E2E emulator gate — mandatory before To Review`
- `Bounded autonomy rule — irreversible actions need confirmation`
- `Spec gap escalation — documentation requirement` (full format block)
- `Subagent return protocol — status signal only`
- `Subagent scope constraint — no unilateral redesign`
- `Living spec protocol — write decisions back before stopping`
- `Silent task completion — post-edit re-read requirement`
- `Subagent MCP isolation per task` (MCP assignment table)

Update the `Before Writing Any Code` section to remove the `workflow.md` read and replace with:
> "3. `.claude/agents/implementor.md` (this file) — exit checklist and scope constraints"

### 3. Create `library/spec-writing-guide.md` (~+30k chars moved from Rule 1)

New file containing Rule 1's detailed reference content:

- `Spec language — determinism` (prohibited terms table + replacements)
- `Acceptance criteria format` (Given/When/Then + EARS with full examples)
- `requirements.md — mandatory sections` (with section list)
- `design.md — mandatory sections` + optional sections (state machine, integration contracts)
- `Architecture reversibility documentation` (table + rule)
- `Spec format portability rule` (portability requirements + diagram guidance)
- `Failure-mode analysis` (3-question protocol)
- `Demo statement requirement` (format + examples + purpose)
- `Spec ownership constraint` (allowed/not-allowed table)
- `Tacit knowledge capture` (4-question protocol + LLM extraction technique)
- `Over-specification guard` (thin spec standard + length guideline)
- `Decision log — fourth optional spec file` (format + when to use)
- `Spec versioning discipline` (change note format + rules)
- `Spec-update gate — after implementation`
- `Rebuild test — feature close-out spec quality check` (protocol + interpretation table)
- `Functional vs technical separation` (table)

Add file header:
```
# Spec-Writing Guide — MyVocaList
> Loaded on-demand. Invoke when writing requirements.md, design.md, or tasks.md.
```

### 4. Create `library/session-ops.md` (~+14k chars moved from Rule 7)

New file containing Rule 7's detailed templates:

- `ACTIVE-CONSIDERATIONS.md — session priority stack` (format + rules + relationship to handoff)
- `findings.md — session artifact for exploratory work` (when to create + full format)
- `Multi-session state handoff protocol` (full artifact format)
- `Context exhaustion warning signs` (subagent signs + orchestrator signs + response protocol)
- `Session start constraint capture` (3-bullet protocol)
- `Tiered memory governance` (full table + 5 governance rules + promotion rule)

Add file header:
```
# Session Operations Guide — MyVocaList
> Loaded on-demand. Reference when starting sessions, managing multi-wave state, or writing handoff artifacts.
```

### 5. Compress `workflow.md`

For each extracted section, replace the full content with a one-line pointer:

```markdown
> See `agents/orchestrator.md` for the full pre-dispatch checklist, briefing protocol,
> wave handoff, adversarial critic, kill criteria, and review lane details.

> See `agents/implementor.md` for pre-task context gate, intent verification, E2E gate,
> spec gap escalation format, and subagent scope constraints.

> See `.claude/library/spec-writing-guide.md` for AC format, spec language rules,
> design.md sections, reversibility documentation, and rebuild test protocol.

> See `.claude/library/session-ops.md` for ACTIVE-CONSIDERATIONS format, findings.md
> format, handoff artifact format, and context exhaustion warning signs.
```

**Sections that STAY in workflow.md (never moved):**
- Hook Enforcement Notes (summary table only — 20 lines)
- SDD Invariant (5 lines — constitutional)
- Rule 1: Spec-First core + spec decision table (the big table stays — it's a routing table, not a reference)
- Rule 2: Hard caps (4-subagent max, sequential-only file registry, wave-based structure)
- Rule 3: Commit After Every Task (complete — short)
- Rule 4: Tasks.md source of truth + in-progress marker + atomization checklist + DRY Onion ordering + task entry format + dependency ordering template
- Rule 5: Task Status Registration + task-log format + status table + proof of action rule
- Rule 6: Research Tool Gate (complete — short)
- Rule 7: Session start reading order (the 7-step list stays) + anti-glob rule
- Rule 8: GitHub MCP Pre-Task Collision Check (complete — short)

---

## Estimated Result

| File | Before | After |
|------|--------|-------|
| `workflow.md` | ~118k chars | ~38k chars (under 40k threshold) |
| `agents/orchestrator.md` | ~3k chars | ~33k chars |
| `agents/implementor.md` | ~4k chars | ~16k chars |
| `library/spec-writing-guide.md` | (new) | ~30k chars |
| `library/session-ops.md` | (new) | ~14k chars |

`workflow.md` goes from 118k → ~38k. Passes the performance threshold.

---

## Implementation Notes

- **Amend: process applies** — each commit must use `amend:` prefix; changelog entry required per CLAUDE.md § Amending These Rules
- **Content integrity** — sections are moved verbatim; no rewriting, no condensing of the moved content
- **Cross-references** — after moving, scan `implementor.md` and `orchestrator.md` for any existing "see workflow.md §X" links that now point to moved content; update to point to the new file
- **One file at a time, build between** — not applicable here (no code changes); but commit each file independently for clean git history

---

## Commit Strategy

1. `amend: create library/spec-writing-guide.md — extract Rule 1 reference detail from workflow.md`
2. `amend: create library/session-ops.md — extract Rule 7 reference detail from workflow.md`
3. `amend: expand agents/orchestrator.md — extract Rule 2 orchestrator protocols from workflow.md`
4. `amend: expand agents/implementor.md — extract Rule 2 implementor protocols from workflow.md`
5. `amend: compress workflow.md — replace extracted sections with See-X pointers; target <40k chars`
6. `amend: changelog — record workflow.md compression`

---

## Verification

After all commits:
1. Confirm `workflow.md` char count is under 40k: `(Get-Content workflow.md -Raw).Length`
2. Confirm no "§" section references in workflow.md point to sections that were removed
3. Confirm `orchestrator.md` and `implementor.md` contain the expected sections
4. Confirm both new library files exist and have headers
5. Check that no session guidance was accidentally lost (spot-read 5 random sub-sections in the new files)
