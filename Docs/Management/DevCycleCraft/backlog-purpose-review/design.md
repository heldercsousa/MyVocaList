# BACKLOG.md Purpose Review — Design

> **Status:** Approved design (brainstorming session 2026-07-15 with Helder).
> **BACKLOG row:** 2026-07-14 *BACKLOG.md purpose review — restore it as a PO-level business artifact*.
> **Type:** Docs-only task — no code. Runs on `develop` directly (no worktree needed).

## Problem

BACKLOG.md was created as a SCRUM Product-Owner backlog: business-level tracking of each item's goal and high-level status. It has drifted into a technical ledger — rows carry root causes, commit hashes, file paths, review verdicts, test counts, token measurements, and per-step trails. Measured cost: ~56k tokens / 251 lines, exceeding the 25k single-Read cap (the file can no longer be read in one call).

## Decisions (Helder, 2026-07-15)

1. **Done rows → monthly archive files** (not slimmed-in-place, not a single archive).
2. **Displaced technical detail → feature docs, created if missing** (`task-log.md`/`findings.md` in the feature folder; create + `.sln`-register when absent). Never deleted — always moved.
3. **Row-template rule lives in BACKLOG.md's own header** + one pointer line in `workflow.md` Rule 1 (via `amend:` commit + changelog entry).
4. **Archive rows are slimmed too** — same template as active rows; their narratives also move to feature docs, so no file is ever token-fat.
5. **Monthly rotation (sprint-style):** one archive file per completion month, treating a month as an imaginary sprint.

## Row Template (applies to BACKLOG.md AND archive files)

```
| Target | Feature/Item | Status | Notes |
```

**Target** = the date the item was registered (or the month originally targeted, for the early 2026-MM rows) — existing values carry over unchanged during the slim pass; do not reinterpret them.

**Notes column: ≤ 3 sentences / ~50 words**, containing only:
- **Goal** — what the item delivers and why it matters, in business terms (one sentence; business value is folded into Goal — 3-field template approved by Helder 2026-07-15).
- **Gate/blocker** — the single thing holding it (owner + what), if any. For archived rows: one-sentence outcome instead.
- **Pointer** — one `Docs/Management/.../[feature]/` path where all technical detail lives.

**Banned from rows:** commit hashes, file paths beyond the single pointer, root-cause narrative, review verdicts, test counts, per-step status trails, token measurements, AC numbers. Branch/phase tracking belongs to `LEDGER.md`; execution history to the feature's `task-log.md`.

## Archive

- Location: `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-YYYY-MM.md` (folder + each file `.sln`-registered).
- A row moves to the archive file matching **the month it reached ✅ Done / ✅ Fixed / superseded-closed**. Existing Done rows are back-filed by their completion dates (2026-03 … 2026-07).
- At each month boundary a new file starts — each stays small and cheap to read.
- Statuses remaining in BACKLOG.md: 💡 📋 🗺️ 🟢 🟡 🔵 🔴 (🔵 Deferred stays active — it is future work, not history; ✅ appears only in archive files).
- **Nested `↳` sub-rows:** a Done sub-row archives independently of a still-active parent — the parent row keeps its pointer to the feature folder where the sub-item's history lives. A closed parent whose sub-rows are all closed archives together with them (same monthly file).
- **Cross-cutting bugs without a feature folder:** displaced detail goes to a single shared `Docs/Management/cross-cutting-bugs-log.md` (dated headings per BUG-NNN, `.sln`-registered) — do NOT create a folder per bug.
- Discoverability note in both BACKLOG.md and archive headers: past BUG-NNN / feature lookups must grep `backlog-archive/` too.

## Execution Steps

1. **Header rewrite:** BACKLOG.md preamble becomes the rule block — template, ≤3-sentence limit, banned-content list, archive rotation rule, "agents must not re-fatten" instruction.
2. **Detail relocation:** for every row (active + to-be-archived) exceeding the template, move the narrative verbatim into the feature's `task-log.md`/`findings.md` under a dated `## Moved from BACKLOG.md (2026-07-15)` heading; create file/folder + `.sln` registration where missing.
3. **Archive split:** move Done/closed rows (slimmed) into their monthly archive files.
4. **Slim active rows** to the template (e.g. the *Artist & Song Form UX Redesign* row becomes ~3 sentences + pointer to `artists-songs/changes/2026-07-10-form-ux-redesign/`).
5. **workflow.md Rule 1 pointer** (`amend:` commit + `Docs/Changelog/changelog.md` entry).
6. **Measurement:** record BACKLOG.md size before (~56k tokens per the Read tool's truncation report / 251 lines) and after in `findings.md`. **Primary mechanical check:** a single `Read` of the slimmed BACKLOG.md succeeds without truncation (< 25k-token cap). Secondary: line/word count via `wc` for a comparable before/after figure.
7. **Helder review gate:** the slimmed BACKLOG.md is presented for review before commit (per the task row: "propose before any mass edit" — template approved 2026-07-15; the mass-edit result still gets a review pass).

## Interactions

- *Spec Evolution, Versioning & Feature-Folder Organization* (💡): archive files are append-only history — consistent with its immutability direction; the status vocabulary stays the current 8 emojis until that feature defines a richer one.
- *Richer task-status vocabulary* (💡): not addressed here — align later, don't duplicate.
- `LEDGER.md` / `/sln-ledger`: unchanged; the header rule names it as the home for branch/phase tracking.

## Risks

- **Lookup friction:** past-item searches now span archive files — mitigated by header notes in both places.
- **History fidelity:** relocation is verbatim move-not-rewrite; the dated heading preserves provenance.
- **Re-fattening:** mitigated by the header rule living exactly where edits happen; if drift recurs, escalate to a hook (out of scope now).
