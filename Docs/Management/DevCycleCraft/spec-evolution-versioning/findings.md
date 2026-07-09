# Spec Evolution, Versioning & Feature-Folder Organization — Registration + Research Findings

> **Status:** 💡 Registered 2026-07-09 (Helder) — NOT yet a spec. This file captures the problem statement and pre-research so the future brainstorming session starts warm. No CLAUDE.md / workflow change may be made from this file alone; `CLAUDE.md § Development Methodology` explicitly stays untouched until this feature defines the target organization.

## Problem statement (Helder, 2026-07-09)

1. **History destruction:** `CLAUDE.md § Development Methodology` currently instructs updating specs in place when behavior changes. That destroys the record of what was previously specified and implemented. Desired: prior specs/plans/task-logs are **preserved immutable** once implemented.
2. **Evolution as new artifacts:** when a shipped feature changes, create a **new spec describing the change** (delta), reference it from the original spec (forward link) and reference the original from the delta (back link) — easy evolution tracking in both directions.
3. **Proposed shape:** new BACKLOG task **nested under the original parent feature row** + new **dedicated subfolder nested inside the original feature folder**, with its own spec/plan/logs, carrying a **creation date (ANSI/ISO `YYYY-MM-DD`) in the task label, folder name, and file labels** so both humans and agents can find artifacts and read the evolution timeline at a glance.
4. **Bug-fix version binding:** every bug fix must record **which spec version it fixes against** — otherwise, with many fixes over time, it becomes impossible to tell which spec state each fix refers to.
5. **Folder hygiene:** feature folders degrade into flat piles of files with no way to hook them to versions over time (see `artists-songs/` today). A smarter nested-content pattern is needed — paramount for gathering the app's detailed info reliably.
6. **Timeline tracking gap:** beyond BACKLOG.md entries there is no way to track work along time, which makes BACKLOG the only version-information source. This whole topic is equally about **versioning**.

## Internal research (SDD corpus)

- **`sdd/spec-s9-2-1-spec-versioning-n-rollback.md`** — directly applicable, largely ready to adopt:
  - Spec **semver** (MAJOR = breaking AC change / MINOR = additive AC / PATCH = spec-text fix) with a **version header block** in every spec (`Version / Status / Date / Reason for change / Breaking changes`).
  - **"Regenerated from this version"** binding — the exact mechanism point 4 needs: bind each implementation (and each bug fix) to an explicit spec version. Commit convention `spec: vX.Y.Z — <reason>`, one commit per spec version, optional tags `spec-<feature>-vX.Y.Z`.
  - **Immutability rule:** "Once a spec version is committed, it is never modified. Changes become new versions, preserving history." — the doctrinal basis for point 1.
  - **Decision log** (`decision-log.md` beside the spec, `DEC-YYYY-MM-DD-<topic>` entries with options/trade-offs/reversal conditions) — the "why" layer.
- **`sdd/spec-s9-2-2-spec-rot-under-evolution.md`** — why in-place mutation without discipline is dangerous (agents treat specs as oracles; stale/incoherent specs mislead every future session); endorses **spec changelog blocks**, **session-end update ritual** (already in workflow.md Rule 3), and cites **OpenSpec change isolation / spec delta mode** as the reference architecture.

## External research (community patterns, 2026)

- **OpenSpec** ([openspec.pro](https://openspec.pro/), [Fission-AI/OpenSpec concepts](https://github.com/Fission-AI/OpenSpec/blob/main/docs/concepts.md), [directory structure](https://deepwiki.com/Fission-AI/OpenSpec/4.1-directory-structure), [deep dive](https://redreamality.com/garden/notes/openspec-guide/)) — **closest match to Helder's proposal, nearly 1:1:**
  - Two spaces: `specs/` = current-truth of system behavior; `changes/` = each proposed change is a **self-contained subfolder** with its own `proposal.md`, `design.md`, `tasks.md`, and **delta specs** (only the requirements that change: ADDED/MODIFIED/REMOVED).
  - On completion, `openspec archive` moves the change folder to a **date-prefixed archive folder** (e.g. `2025-01-15-add-oauth/`) and merges the deltas into the current-truth spec. History preserved as immutable dated packages; current truth stays readable in one place.
  - Loop: Propose → Apply → Archive. [Discussion #737](https://github.com/Fission-AI/OpenSpec/discussions/737) covers merging multiple changes into one domain spec.
- **ADR supersession pattern** (Nygard; already sourced in S9.2.1) — records are numbered, dated, immutable; a new record marks the old one `Superseded by ADR-NNN`. The forward/back-link mechanic of point 2.
- **GitHub spec-kit** — sequential-numbered feature folders (`specs/001-…`); confirms number/date-prefixed folder naming as the mainstream discoverability device.

## Candidate direction (to be validated by brainstorming — NOT decided)

Adapt OpenSpec's shape to the existing `Docs/Management/.../[feature]/` layout rather than adopting the tool:

```
BusinessFeatures/[feature]/
  requirements.md · design.md · tasks.md · plan.md · task-log.md   ← current truth, version headers (S9.2.1)
  spec-changelog.md                                                ← already canonical; per-version row + link to change folder
  changes/
    2026-07-15-add-song-filters/      ← delta spec/plan/tasks/task-log; back-links original; original forward-links it
  bugs/
    BUG-0NN-…/                        ← existing pattern; ADD "fixes against spec vX.Y.Z" binding line
```

Open questions for the spec session: version-header adoption scope (all features vs new ones); whether current-truth files merge deltas (OpenSpec style) or only link them; BACKLOG nested-row label format (`↳ YYYY-MM-DD change: <name>`); migration/retrofit for messy existing folders (`artists-songs/` as pilot); timeline view beyond BACKLOG (generated index? `git log` conventions per S9.2.1?); decision-log adoption.

## Constraints already known

- `.sln` registration HARD GATE applies to every new `Docs/` file/folder.
- `CLAUDE.md § Development Methodology`, `workflow.md` SDD Invariant + Rules 1/3 (Session-End Spec Update Ritual — transitional immutable-history wording applied to the SDD Invariant 2026-07-09, approved provisionally), `bug-tracking.md` + `library/bug-tracking-reference.md` (approved provisionally 2026-07-09 — must gain the bug-fix→spec-version binding), `spec-writing-guide.md`, and the `Docs/ Folder Layout` section must all be amended **together** when the design lands (`amend:` process).
- Must stay compatible with Spec-Anchored SDD Level 2: agents still need ONE authoritative current-truth spec per feature to read at session start — history must not force agents to reconstruct truth from deltas.
