# BACKLOG Generator & Spec-Folder Frontmatter

> Routed out of `CLAUDE.md § Docs/ Folder Layout` 2026-08-24 (doctor pass). The never-miss rules —
> shipped specs are immutable, Minor bugs get no folder, never hand-edit a fenced row, and the
> session-start `query` command — stay inline in `CLAUDE.md`. Read this file when registering an
> item, changing a status, or debugging a stale-BACKLOG pre-commit failure.

## Shipped specs are immutable; changes nest

A feature's `requirements.md`/`design.md` describe what shipped. Post-ship behavior changes do NOT
rewrite them — they get a dated `changes/YYYY-MM-DD-<slug>/` folder with its own spec files, which
cross-references the original. Critical/Major bugs get `bugs/YYYY-MM-DD-BUG-NNN-<slug>/`. Minor
bugs get **no folder** (the commit message is the artifact) — a `severity: Minor` folder is a
mechanical validation error (`bug-tracking.md`).

## Every item folder carries frontmatter; BACKLOG rows are generated

`README.md` opens with a flat `key: value` frontmatter block (`id, title, status, severity,
target, section, parent, goal, gate, pointer, closed, order` — schema in
`DevCycleCraft/spec-evolution-versioning/design.md § 2`). `Docs/Management/BACKLOG.md` and the
monthly `backlog-archive/*.md` files are **generated** from those blocks between
`<!-- BACKLOG:GENERATED:BEGIN … -->` fences. **Never hand-edit a fenced row** — it is silently
overwritten on the next regeneration, not merge-conflicted.

| To do this | Run |
|------------|-----|
| Register a new item | `python .claude/scripts/backlog/backlog_gen.py register --section … --parent … --kind bug --severity … --title "…" --goal "…"` (creates folder + `README.md` + `.sln` entry atomically, allocates `BUG-NNN`) |
| Change a status | `backlog_gen.py status <ID> "🟡 In Progress"` (terminal statuses also need `--closed YYYY-MM`) |
| Refresh the rendered file | `backlog_gen.py regen` (`--check` = verify only, writes nothing) |
| Find the active work set | `backlog_gen.py query --status "🟡,🟢"` |
| List post-MVP items | `backlog_gen.py query --horizon post-mvp` |

## Row and frontmatter rules

Moved here from the `BACKLOG.md` header on 2026-11-15.

- **Live rows are slim:** `| Target | Feature | Status |`. Goal, gate and the folder pointer are not rendered; they stay in the item `README.md` frontmatter and `query` prints the pointer. **Target** is the registration date (or the originally targeted month), never reinterpreted.
- **`goal:` and `gate:` stay short** (together at most 3 sentences, about 50 words): what the item delivers in business terms, and the single thing holding it (owner + what).
- **Banned from frontmatter text:** commit hashes, file paths, root-cause narrative, review verdicts, test counts, step trails, AC numbers. Branch and phase tracking belongs to `LEDGER.md`; execution history to the item's `task-log.md`.
- **Archive rotation:** an item reaching `✅ Done` / `✅ Fixed` / superseded-closed moves to `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-YYYY-MM.md`, keyed by its `closed:` month. A Done sub-row archives independently of a still-active parent. Archive rows keep the Notes cell (Goal, Gate, Pointer).
- **Lookups:** past `BUG-NNN` and feature history live in `backlog-archive/`; grep those files too.
- **Post-MVP items:** an item with `horizon: post-mvp` in its frontmatter (a child inherits it from its parent) is rendered in `Docs/Management/BACKLOG-POST-MVP.md`, not in `BACKLOG.md`. `query` leaves these items out unless `--horizon post-mvp` or `--horizon all` is passed. Register one with `register ... --horizon post-mvp`. Archiving is the same as for any item.
- **Fences:** the `<!-- BACKLOG:GENERATED:BEGIN/END -->` comment lines are the generator's markers. Removing one makes `regen` fail.

| Status | Meaning |
|--------|---------|
| `💡 Pending` | Captured, not yet evaluated |
| `📋 Spec` | Approved, spec being written |
| `🗺️ Plan` | Spec approved, plan being written |
| `🟢 Ready` | Plan approved, dispatchable |
| `🟡 In Progress` | Implementation underway |
| `🔵 Deferred` | Paused, reason documented |
| `🔴 Blocked` | Cannot proceed, dependency named |
| `✅ Done` | Shipped; the row moves to `backlog-archive/` |

A pre-commit gate runs `regen --check` on any commit touching a `Docs/Management/**/README.md`,
`BACKLOG.md`, or an archive file, and blocks the commit if the rendered files are stale.

Concurrency protocol for generated artifacts: `workflow-rule-2.md § Generated artifacts`.
