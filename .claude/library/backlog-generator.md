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

A pre-commit gate runs `regen --check` on any commit touching a `Docs/Management/**/README.md`,
`BACKLOG.md`, or an archive file, and blocks the commit if the rendered files are stale.

Concurrency protocol for generated artifacts: `workflow-rule-2.md § Generated artifacts`.
