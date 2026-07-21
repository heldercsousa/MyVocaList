# Decision — Nested `bugs/`/`changes/` folders + generated BACKLOG

**Date:** 2026-07-21 · **Approved by:** Helder ("I agree with your idea for the backlog since now")
**Parent BACKLOG row:** Dev Cycle Craft / 2026-07-09 — *Spec Evolution, Versioning & Feature-Folder Organization*
**Status:** 💡 direction approved, **not yet specced**. This file records the decision and its constraints so a fresh session can write the spec without re-deriving anything.

---

## Part A — Nested folder pattern (approved in principle, rule not yet written)

Every bug or change that belongs to a feature lives in a dated folder nested under that feature:

```
Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/
  requirements.md · design.md · tasks.md · task-log.md      ← the feature's own spec
  changes/2026-07-21-inline-artist-create/                  ← one folder per change
      requirements.md · design.md · plan.md · tasks.md · task-log.md
  bugs/2026-07-20-BUG-050-suggestion-not-locked/            ← one folder per bug
      (same file set, as needed)
```

**Rules agreed:**
- Folder names **lowercase** `bugs/` and `changes/` (Helder wrote "Bugs"; lowercased for case-sensitivity safety on CI/Linux and in grep patterns).
- Each item folder is prefixed with the **ANSI date** (`YYYY-MM-DD`) followed by a title slug.
- For bugs, keep **`BUG-NNN` in the folder name after the date** — date-first sorts chronologically, the ID keeps `grep -r BUG-050 Docs/` a one-hop lookup.
- **Minor bugs do NOT get a folder.** `bug-tracking.md` already says Minor = commit message only; a folder-per-Minor-bug is ceremony with no reader. Critical/Major get a folder; Minor gets a line in the parent `task-log.md`.

**Current state — the rule does not exist, so practice is inconsistent.** Existing examples that already follow it: `artists-songs/changes/2026-07-21-inline-artist-create/`, `artists-songs/changes/2026-07-10-form-ux-redesign/`, `autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/`. Counter-examples needing migration: BUG-050/051/052 and BUG-027/029/030/031/032 point at a *parent* `task-log.md`; BUG-012 is a flat `bugs/BUG-012-….md` file.

## Part B — BACKLOG becomes generated, not hand-maintained

**Problem measured:** BACKLOG.md is 136 lines / ~4.5k tokens and is read at session start (workflow.md Rule 7) by every agent regardless of task. An agent fixing one bug loads Play Store compliance, the Windows version, and DbContext architecture review. The file also needs a defensive rule at its own head ("agents: do NOT re-fatten this file") — a rule that exists only because the file is hand-written.

**Decision:** keep BACKLOG.md, stop writing it by hand.

1. Each `bugs/`/`changes/` folder (and each feature folder) carries YAML frontmatter — `id`, `title`, `status`, `severity`, `target`, `feature`, `parent`.
2. A generator walks `Docs/Management/**/{bugs,changes}/*/` plus feature folders and **regenerates** BACKLOG.md's tables. Source of truth = the folder. BACKLOG.md = a generated view.
3. Agents stop reading the whole file. Rule 7 step 1 becomes a query (`--status "🟡,🟢"` → ~15 lines) instead of a 136-line read.

### Constraints the generator MUST satisfy (from Helder, 2026-07-21)

| Constraint | Detail |
|-----------|--------|
| **Orchestrator writes, not just Helder** | The orchestrator updates rows at workflow milestones; Helder edits manually only occasionally, usually asking an agent to register a newly-found bug or opportunity. So the generator must be **agent-driven and idempotent** — safe to run at every milestone, not a nightly batch. A "register this new bug" flow must create the folder + frontmatter, not append a row. |
| **Monthly archive rotation must survive** | `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-YYYY-MM.md` exists for 2026-03 … 2026-07. Rows reaching ✅/superseded rotate out, keyed by completion month, and a Done sub-row archives independently of a still-active parent. The generator must emit both the live file and the monthly archives from the same frontmatter — archives stay greppable (BACKLOG header § Lookups depends on this). |
| **Row template preserved** | The PO-level template (Goal + Gate + one Pointer, ≤3 sentences, banned content list) is defined in BACKLOG.md's own header. Generated rows must conform, which also *mechanically enforces* what is currently a prose rule. |
| **Existing machinery reused** | `.claude/scripts/backlog/` already has `backlog_lib.py`, `orphan_check.py`, `session_marker.py`, plus tests. Extend rather than rewrite — assess these first. |

### Explicitly rejected

- **Sequential numeric folder prefixes** (`001-`, `002-`, GitHub Spec Kit style). BUG-NNN already exists for bugs and ANSI dates sort correctly; a second sequence is a merge-conflict generator when two worktrees both claim `053-`.
- **Deleting BACKLOG.md** in favour of folder-only tracking. Helder's stated objection is correct: it makes general progress tracking manual, folder by folder. Generation solves both concerns at once.

## Effort estimate

The generator is the small part. **The backfill is the real work** — ~50 existing rows need frontmatter and, for the counter-examples above, a folder each. Mechanical; suits one implementor wave.

## Next step

Write the spec (requirements + design) for Parts A and B **as one change** — the folder structure is what makes the generator possible. Route it under the parent row *Spec Evolution, Versioning & Feature-Folder Organization*, which already describes exactly this scope. Then spec-reviewer, then Helder approval, then plan.
