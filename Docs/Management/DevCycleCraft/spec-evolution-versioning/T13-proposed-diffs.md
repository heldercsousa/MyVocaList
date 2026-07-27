# T13 — Rules amendment bundle (PROPOSED DIFFS — NOT APPLIED)

> **Status: awaiting Helder's authorship review.** `CLAUDE.md § Continuous Enhancement — Authorship`
> makes human review a HARD gate for any `.claude/rules/*` or `CLAUDE.md` change. Nothing in this
> document has been written to the target files. Every block below is `OLD` → `NEW` on exact
> existing text, so applying it after approval is mechanical.
>
> **Landing rule (REQ-SEV-30):** T13a + T13b + T13d land in **ONE `amend:` commit**. Splitting them
> leaves the routing tables contradicting the library for the duration — a live SDD-Invariant
> violation. T13c (changelog) rides in the same commit.
>
> Prepared 2026-07-26, after the migration merged to develop (`2a6a1f8`).

---

## 0. Mandatory `BACKLOG.md` sweep (REQ-SEV-30 requires this before the commit lands)

`Grep "BACKLOG\.md" .claude/ --glob *.md` — every hit, and whether REQ-SEV-30's table already covers it:

| File:line | In REQ-SEV-30 table? | Action |
|-----------|----------------------|--------|
| `.claude/rules/workflow.md:51, 102, 186` | yes | §3 below |
| `.claude/rules/bug-tracking.md:10` | yes | §6 |
| `.claude/library/workflow-rule-1.md:57, 59, 60, 70, 77, 86` | yes | §4 |
| `.claude/library/workflow-rules-6-7-8.md:28` | yes | §5 |
| `.claude/library/bug-tracking-reference.md:12` | yes | §7 |
| `.claude/library/session-ops.md:27` | yes | §9 |
| **`.claude/agents/orchestrator.md:32, 378`** | **NO — added by this sweep** | §10 |
| **`.claude/exception-registry.md:16`** | **NO — added by this sweep** | §15 (T13d deletes the row) |

Both additions are recorded here per REQ-SEV-30's "any hit not in this table is added to it before
the amend commit lands". `.claude/library/workflow-rule-3.md` and `.claude/library/spec-writing-guide.md`
carry no literal `BACKLOG.md` hit but are in the REQ-SEV-30 table for other content (§8, §12).

---

# T13a — Routing tables

## 1. `CLAUDE.md § Docs/ Folder Layout (canonical)`

**OLD** (the fenced tree plus the routing rule that follows it):

```
Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/
  requirements.md       ← acceptance criteria, user stories, validation rules
  design.md             ← architecture, interfaces, interaction flows (user-preference override for brainstorming skill default)
  tasks.md              ← ordered checkboxed implementation tasks
  plan.md               ← execution plan
  task-log.md           ← activity log
  findings.md           ← spike results (optional)
  spec-changelog.md     ← spec revision history (required for features with ≥1 post-approval change)

```

**NEW:**

```
Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/
  README.md             ← frontmatter carrier — the item's BACKLOG row is GENERATED from it
  requirements.md       ← acceptance criteria, user stories, validation rules
  design.md             ← architecture, interfaces, interaction flows (user-preference override for brainstorming skill default)
  tasks.md              ← ordered checkboxed implementation tasks
  plan.md               ← execution plan
  task-log.md           ← activity log
  findings.md           ← spike results (optional)
  spec-changelog.md     ← spec revision history (required for features with ≥1 post-approval change)

  bugs/YYYY-MM-DD-BUG-NNN-<slug>/README.md      ← one folder per Critical/Major bug
  changes/YYYY-MM-DD-<slug>/README.md           ← one folder per post-ship change to this feature
```

**Add immediately below the tree (new subsection):**

> ### Shipped specs are immutable; changes nest
>
> A feature's `requirements.md`/`design.md` describe what shipped. Post-ship behavior changes do NOT
> rewrite them — they get a dated `changes/YYYY-MM-DD-<slug>/` folder with its own spec files, which
> cross-references the original. Critical/Major bugs get `bugs/YYYY-MM-DD-BUG-NNN-<slug>/`. Minor
> bugs get **no folder** (the commit message is the artifact) — a `severity: Minor` folder is a
> mechanical validation error (`bug-tracking.md`).
>
> ### Every item folder carries frontmatter; BACKLOG rows are generated
>
> `README.md` opens with a flat `key: value` frontmatter block (`id, title, status, severity,
> target, section, parent, goal, gate, pointer, closed, order` — schema in
> `DevCycleCraft/spec-evolution-versioning/design.md § 2`). `Docs/Management/BACKLOG.md` and the
> monthly `backlog-archive/*.md` files are **generated** from those blocks between
> `<!-- BACKLOG:GENERATED:BEGIN … -->` fences. **Never hand-edit a fenced row** — it is silently
> overwritten on the next regeneration, not merge-conflicted.
>
> | To do this | Run |
> |------------|-----|
> | Register a new item | `python .claude/scripts/backlog/backlog_gen.py register --section … --parent … --kind bug --severity … --title "…" --goal "…"` (creates folder + `README.md` + `.sln` entry atomically, allocates `BUG-NNN`) |
> | Change a status | `backlog_gen.py status <ID> "🟡 In Progress"` (terminal statuses also need `--closed YYYY-MM`) |
> | Refresh the rendered file | `backlog_gen.py regen` (`--check` = verify only, writes nothing) |
> | Find the active work set | `backlog_gen.py query --status "🟡,🟢"` |
>
> A pre-commit gate runs `regen --check` on any commit touching a `Docs/Management/**/README.md`,
> `BACKLOG.md`, or an archive file, and blocks the commit if the rendered files are stale.

## 2. `CLAUDE.md § Development Methodology` — the in-place spec-update wording

**OLD:**

> Code changes without a corresponding spec update are out of scope unless the change is a bug fix affecting no spec-described behavior.

**NEW:**

> Code changes without a corresponding spec update are out of scope unless the change is a bug fix affecting no spec-described behavior. **A shipped spec is immutable history — it is never rewritten in place.** A change to shipped behavior is recorded in a dated `changes/YYYY-MM-DD-<slug>/` folder beside it (`§ Docs/ Folder Layout`); only a feature that has not yet shipped is edited in place.

## 3. `.claude/rules/workflow.md`

### 3.1 SDD Invariant

**OLD:**

> - New requirement mid-implementation → record the spec change first, then the code. For a feature **not yet shipped**, update its spec in place. For **shipped/implemented** behavior, do NOT rewrite the existing spec (it is immutable history) — append a new dated change spec that cross-references the original. *(Target pattern being defined: BACKLOG "Spec Evolution, Versioning & Feature-Folder Organization"; until it lands, at minimum add a dated `> **Spec updated [YYYY-MM-DD]:**` note instead of silently rewriting.)*

**NEW:**

> - New requirement mid-implementation → record the spec change first, then the code. For a feature **not yet shipped**, update its spec in place. For **shipped/implemented** behavior, do NOT rewrite the existing spec (it is immutable history) — create `changes/YYYY-MM-DD-<slug>/` beside it, with its own `requirements.md`/`design.md`/`README.md` cross-referencing the original (`CLAUDE.md § Docs/ Folder Layout`). Register it with `backlog_gen.py register` so it gets a BACKLOG row.

### 3.2 Rule 1 — the BACKLOG bullet

**OLD:**

> - **BACKLOG.md is the source of truth for feature sequencing** — the main agent updates status at each milestone (💡→📋→🗺️→🟢→🟡→✅). Untracked work discovered mid-session gets a brief BACKLOG row *before* proceeding. BACKLOG rows follow the PO-level template defined in BACKLOG.md's own header (Goal + Gate + one Pointer, ≤3 sentences); technical detail goes to the feature docs, never into the row.

**NEW:**

> - **Item frontmatter is the source of truth for feature sequencing; BACKLOG.md is its generated view** — the main agent updates status at each milestone (💡→📋→🗺️→🟢→🟡→✅) with `backlog_gen.py status <ID> "<status>"`, never by editing a row. Untracked work discovered mid-session is registered with `backlog_gen.py register` *before* proceeding. The row template (Goal + Gate + one Pointer, ≤3 sentences, no commit hashes / file paths / verdicts / test counts) is **mechanically enforced** by the generator's validation — a violating `goal:`/`gate:` aborts the write and names the folder. Technical detail goes in the item's own `README.md` body, which has no limit.

### 3.3 Rule 2 — "Docs land on develop" (this is the T13d edit; see §13)

### 3.4 Rule 3 — Session-End Spec Update Ritual

**OLD:**

> - **Session-End Spec Update Ritual:** review every spec file touched; if it no longer describes what was built, add a `> **Spec updated [YYYY-MM-DD]:**` note; check off completed tasks / mark `[CANCELLED: reason]`; commit spec updates in the final commit.

**NEW:**

> - **Session-End Spec Update Ritual:** review every spec file touched. **Not-yet-shipped feature** → if the spec no longer describes what was built, add a `> **Spec updated [YYYY-MM-DD]:**` note in place. **Shipped feature** → do not edit the shipped spec; open a `changes/YYYY-MM-DD-<slug>/` folder instead. Check off completed tasks / mark `[CANCELLED: reason]`; update the item's `README.md` frontmatter (`status:`, `gate:`) and run `backlog_gen.py regen`; commit spec updates in the final commit.

### 3.5 Rule 7 step 1

**OLD:**

> 1. **Active handoff file** `…/[feature]/handoff.md` if present — else read `Docs/Management/BACKLOG.md` for the current `🟡 In Progress` / highest `🟢 Ready` item.

**NEW:**

> 1. **Active handoff file** `…/[feature]/handoff.md` if present — else run `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"` for the current `🟡 In Progress` / highest `🟢 Ready` item. **Do not read `Docs/Management/BACKLOG.md`** — the query returns the same work set in ~12 lines instead of ~136 (REQ-SEV-23). The rendered file is for Helder.

---

# T13b — Library section files

## 4. `.claude/library/workflow-rule-1.md`

### 4.1 New-feature workflow, lead-in + step 0

**OLD:**

> **BACKLOG.md is the source of truth for feature sequencing.** The main agent (not subagents) is responsible for updating `Docs/Management/BACKLOG.md` status at each milestone below.
>
> 0. **Identify** — read `Docs/Management/BACKLOG.md`; pick the highest-priority `🟢 Ready` item in the **Business Features** table, or the next `💡 Pending` item if none are Ready

**NEW:**

> **Item `README.md` frontmatter is the source of truth for feature sequencing; `BACKLOG.md` is its generated view.** The main agent (not subagents) updates status at each milestone below with `backlog_gen.py status <ID> "<status>"` — never by editing a row.
>
> 0. **Identify** — run `backlog_gen.py query --status "🟢,💡"`; pick the highest-priority `🟢 Ready` Business Features item, or the next `💡 Pending` item if none are Ready

Steps 1–5: replace each `update BACKLOG.md status → X` with `backlog_gen.py status <ID> "X"`.
Step 5's ship line gains: `✅ Done` requires `--closed YYYY-MM`; the row then renders into
`backlog-archive/BACKLOG-ARCHIVE-YYYY-MM.md` automatically — **do not move a row by hand.**

### 4.2 Proactive BACKLOG triage

**OLD:**

> **Any work identified during a session that is not already in BACKLOG.md must get a brief entry before proceeding.**
> …
> **Format — add a row to the appropriate BACKLOG.md table:**
>
> | Date | Activity/Feature | `💡 Pending` | One-line description |

**NEW:**

> **Any work identified during a session that is not already registered must get an item folder before proceeding.**
> …
> **Format — register it, never hand-write a row:**
>
> ```bash
> python .claude/scripts/backlog/backlog_gen.py register \
>     --section DevCycleCraft --kind activity \
>     --title "…" --goal "…" [--gate "…"] [--parent <parent-id>]
> ```
>
> The command creates the folder, its `README.md` frontmatter, the `.sln` entry, and regenerates the
> row. `status:` defaults to `💡 Pending`; pass `🟡 In Progress` if work starts now.

Trigger question "Is what I'm about to do tracked in BACKLOG.md?" → "**Does what I'm about to do have
an item folder?** (`backlog_gen.py query` to check)".

## 5. `.claude/library/workflow-rules-6-7-8.md § Rule 7`, step 1

**OLD:**

>    - **If no handoff file exists:** read `Docs/Management/BACKLOG.md` to identify the current `🟡 In Progress` item or the highest-priority `🟢 Ready` item — that is the current work context

**NEW:**

>    - **If no handoff file exists:** run `python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"` to identify the current `🟡 In Progress` item or the highest-priority `🟢 Ready` item — that is the current work context. **Do not read `Docs/Management/BACKLOG.md`**: it renders the same set at ~10× the token cost (REQ-SEV-23), and reading it is not a substitute for the query, which reads frontmatter directly.

## 6. `.claude/rules/bug-tracking.md` — Non-negotiables block

**OLD:**

> - **ID:** sequential `BUG-NNN`, continue from the highest in BACKLOG.md, never reuse. Used in commit subject + BACKLOG row + task-log.
> - **Placement:** register BEFORE fixing (proactive triage); nest under the parent feature (or `### Cross-cutting`), never free-floating.

**NEW:**

> - **ID:** never hand-allocated. `backlog_gen.py register --kind bug` derives the next `BUG-NNN` from `max(id)` over live folders **∪ all `backlog-archive/` months**, so a retired number can never be reused. Used in commit subject + generated row + task-log. A collision after a cross-worktree merge is fixed with `backlog_gen.py renumber BUG-NNN`, never by hand.
> - **Placement:** register BEFORE fixing (proactive triage). **Critical/Major** → its own folder `<parent-feature>/bugs/YYYY-MM-DD-BUG-NNN-<slug>/README.md`, created by `register` (the folder name is derived, never typed). **Minor** → **no folder**; the commit message is the artifact. A `severity: Minor` folder is a validation error that aborts regeneration. Cross-cutting bugs nest under `BusinessFeatures/cross-cutting/`, never free-floating.

## 7. `.claude/library/bug-tracking-reference.md`

**Line 12 OLD:**

> - Bugs use a sequential ID: `BUG-001`, `BUG-002`, … (continue from the highest existing ID in BACKLOG.md — never reuse).

**NEW:**

> - Bugs use a sequential ID: `BUG-001`, `BUG-002`, … allocated by `backlog_gen.py register` from `max(id)` over live item folders ∪ every `backlog-archive/` month — never hand-picked, never reused. (BACKLOG.md no longer carries the retired numbers, so reading it is not a valid way to pick the next ID.)

**Workflow step 1 (line 76) OLD:**

> 1. **Register** — assign `BUG-NNN`, classify severity, add nested BACKLOG row under the parent feature.

**NEW:**

> 1. **Register** — classify severity, then `backlog_gen.py register --kind bug --severity <S> --parent <feature-id> --title "…" --goal "…"`. It assigns `BUG-NNN`, creates the dated folder (Critical/Major only), writes frontmatter, registers the `.sln` entry, and regenerates the nested row. Minor bugs get no folder — register is not run; the commit message is the record.

## 8. `.claude/library/workflow-rule-3.md § Session-End Spec Update Ritual`

**OLD step 3:**

> 3. If the answer is "no" or "partially": add a `> **Spec updated [YYYY-MM-DD]:**` note; update ACs, signatures, or invariants to reflect delivered behavior

**NEW step 3 (split in two):**

> 3. If the answer is "no" or "partially", branch on whether the feature has shipped:
>    - **Not shipped** — add a `> **Spec updated [YYYY-MM-DD]:**` note in place; update ACs, signatures, or invariants to reflect delivered behavior.
>    - **Shipped** — the spec is immutable history. Create `changes/YYYY-MM-DD-<slug>/` beside it with its own spec files cross-referencing the original, and `backlog_gen.py register` it. Do not edit the shipped spec.

**New step 6:**

> 6. If any item's `status:`/`gate:` changed, update its `README.md` frontmatter and run `backlog_gen.py regen` — the pre-commit gate rejects a commit that leaves the rendered files stale.

## 9. `.claude/library/session-ops.md`

**Line 27 OLD fragment:**

> Recording a work item here does NOT register it: `BACKLOG.md` is the only registration surface.

**NEW:**

> Recording a work item here does NOT register it: **an item folder with frontmatter** (created by `backlog_gen.py register`) is the only registration surface — `BACKLOG.md` is a generated view of that surface, not the surface itself.

**Add to the session-start read set subsection:**

> **BACKLOG.md is not in the session-start read set.** Use `backlog_gen.py query --status "🟡,🟢"`
> (workflow.md Rule 7 step 1). Reading the rendered file costs ~4.5k tokens for the same information
> and is a Rule 7 violation, not a fallback.

## 10. `.claude/agents/orchestrator.md` *(added by the §0 sweep — not in REQ-SEV-30's original table)*

**Read-scope allow-list, OLD line 32:**

> - `Docs/Management/BACKLOG.md`

**NEW:**

> - `backlog_gen.py query` output (the agent path to the work set). `Docs/Management/BACKLOG.md` itself is readable but **should not be read at session start** — Rule 7 step 1 replaced that read with the query.

Line 378's occurrence: same substitution, in whatever briefing/checklist sentence it appears in —
verify the exact text at apply time (it was `[Omitted long matching line]` in the sweep).

## 12. `.claude/library/spec-writing-guide.md`

**Add a new section after the spec-anatomy section:**

> ## Item folder file set + frontmatter
>
> Every registered item (feature, activity, change, Critical/Major bug) is a folder whose `README.md`
> begins with a flat `key: value` frontmatter block. `register` writes it; agents edit values, never
> the rendered row.
>
> ```yaml
> ---
> id: BUG-050                       # BUG-NNN, or a kebab slug for non-bugs — unique tree-wide
> title: "Song form — selecting an artist suggestion does not lock the field"
> status: "💡 Pending"              # one of the 8 BACKLOG statuses
> severity: Critical                # Critical | Major | Minor — bugs only; Minor must have NO folder
> target: 2026-07-21                # registration date; YYYY-MM-DD | YYYY-MM | "—"
> section: BusinessFeatures         # BusinessFeatures | DevCycleCraft
> parent: artists-songs             # id of the logical parent item; omitted at top level
> goal: "…"                         # required — the Goal sentence of the rendered row
> gate: "…"                         # optional — the single blocker
> pointer: BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-050-suggestion-not-locked/
> closed: 2026-07                   # required iff status is terminal (✅ / Superseded / Duplicate)
> order: 20                         # optional tie-breaker within a parent
> ---
> ```
>
> Parsing is stdlib-only and deliberately restricted (NFR-1): flat `key: value`, no lists, no nested
> blocks, no anchors. Anything nested is a validation error naming the key.
>
> `goal`/`gate` are the row's Notes and are **mechanically bounded**: ≤ 3 sentences, ≤ ~50 words,
> exactly one backticked path, and no commit hashes, review verdicts, `AC-N`, `N/M green`, token
> counts, or extra file paths. Everything that does not fit goes in the `README.md` body below the
> frontmatter, which has no limit. The bound is enforced by `validate()` — a violation aborts
> regeneration and names the folder; it is not a style suggestion.

---

# T13d — Write-ownership & concurrency protocol for generated artifacts

## 13. `.claude/rules/workflow.md § Rule 2` — "Docs land on develop"

**OLD:**

> - **Docs land on develop `[HARD RULE]`:** spec files, `task-log.md`, BACKLOG.md, LEDGER.md, and changelog updates are committed to `develop` (by the main agent), never left stranded on a worktree branch. If a subagent edited docs inside a worktree, sync them to develop before/at merge (`/sln-docs-sync`).

**NEW:**

> - **Docs land on develop `[HARD RULE]`:** spec files, `task-log.md`, `tasks.md`, LEDGER.md, and changelog updates are committed to `develop` (by the main agent), never left stranded on a worktree branch. If a subagent edited docs inside a worktree, sync them to develop before/at merge (`/sln-docs-sync`).
> - **Generated artifacts are single-writer, not mergeable `[HARD RULE]`:** `Docs/Management/BACKLOG.md`, `Docs/Management/backlog-archive/*.md`, and every `Docs/Management/**/README.md` frontmatter block are **generated**. A concurrent edit to a generated region is not a line conflict — it is a silent overwrite on the next `regen`. Full protocol: `workflow-rule-2.md § Generated artifacts`.

## 14. `.claude/library/workflow-rule-2.md` — new section

> ## Generated artifacts — write-ownership protocol
>
> **1. Which artifacts are generated.**
>
> | Artifact | Class | Rule |
> |----------|-------|------|
> | `Docs/Management/BACKLOG.md` (inside `BACKLOG:GENERATED` fences) | generated | regenerate, never hand-edit |
> | `Docs/Management/backlog-archive/*.md` (inside fences) | generated | regenerate, never hand-edit |
> | `Docs/Management/**/README.md` frontmatter block | **source** | edit the value, then `regen` |
> | `README.md` body, `task-log.md`, `tasks.md`, `plan.md`, spec files, `LEDGER.md`, changelog | hand-written | ordinary merge; land on develop |
>
> Everything **outside** a fence in a generated file (headers, Row rules, prose) is hand-written and
> byte-preserved by the generator — editing there is always safe.
>
> **2. When the pre-commit gate rejects a stale BACKLOG: regenerate, then re-inspect — never
> `--no-verify`.** Run `backlog_gen.py regen`, then `git diff` the regenerated files **before**
> committing. If the diff touches only rows whose frontmatter this session changed → commit. If it
> touches rows this session never touched, another session's frontmatter change is unregenerated:
> **stop and escalate** — do not commit, and do not revert their rows. Blind regeneration is safe
> for the *rendered* file (it is a total function of frontmatter) but the diff is the only signal
> that a second writer is active, so it must be read, not skipped.
>
> **3. How a second session learns a region is owned.** A session doing a **bulk** operation over
> generated artifacts (a migration, a mass status sweep) announces it in its `LEDGER.md` row —
> owner, branch, since-date — and retires the announcement when the branch merges. Ordinary
> single-item work (`register` / `status` on one item) needs **no** announcement: it touches one
> frontmatter block plus a deterministic re-render, which merges normally. The rule is proportional
> to blast radius, not applied to every write.
>
> **4. Concurrent `register` / `status` are safe within one repo; the hazard is cross-worktree ID
> allocation.** Both verbs are atomic (folder + `README.md` + `.sln` + re-render written in one pass,
> rolled back on failure), so a half-registration cannot trip the `.sln` HARD GATE. What they cannot
> see is a `BUG-NNN` allocated in a *different worktree* that has not merged yet — two worktrees can
> allocate the same ID. That is detected at merge (duplicate `id` is a validation error, so the
> merge commit cannot pass the gate) and fixed with `backlog_gen.py renumber BUG-NNN`, which rewrites
> the folder name and the `id:` key. Renumbering is safe because the ID lives in exactly two places;
> every other reference is a path.

## 15. `.claude/exception-registry.md` — DELETE the 2026-07-22 row

The row expires "when the concurrency/write-ownership task lands in the T13 rules bundle" and says
"this row must then be removed, not renewed". §13–14 are that task. **Delete the whole
`| 2026-07-22 | … |` row** — do not mark it expired, do not renew it. (The 2026-07-11 DevExpress row
is unrelated and stays.)

---

# FUP-3 — stale `.sln` GUID counter *(rides in the same commit)*

## 16. `.claude/rules/constraints-registry.md § Visual Studio Solution (.sln)`

**OLD fragment:**

> **New-folder pattern, `NestedProjects` parent GUIDs, and the sequential GUID counter (last used `0041`)** are in `library/constraints-reference.md § Visual Studio Solution (.sln)`.

**NEW:**

> **New-folder pattern, `NestedProjects` parent GUIDs, and the sequential GUID counter (last used `00D3`)** are in `library/constraints-reference.md § Visual Studio Solution (.sln)`.

Apply the same `0041` → `00D3` correction to the counter's authoritative statement in
`.claude/library/constraints-reference.md § Visual Studio Solution (.sln)` — verify the exact
spelling there at apply time. Note that `backlog_gen.py register` now allocates GUIDs itself, so the
counter is documentation for hand-made folders only.

---

# T13c — Changelog entry

## 17. `Docs/Changelog/changelog.md` — prepend

> ### 2026-07-26 — `amend:` Spec Evolution, Versioning & Feature-Folder Organization
>
> **Old rule:** a shipped spec was updated in place with a dated `> **Spec updated:**` note; bugs and
> post-ship changes had no standard home; `Docs/Management/BACKLOG.md` was hand-maintained and read
> at every session start; `BUG-NNN` was hand-allocated from the highest number visible in that file.
>
> **New rule:** shipped specs are immutable — post-ship changes get `changes/YYYY-MM-DD-<slug>/` and
> Critical/Major bugs get `bugs/YYYY-MM-DD-BUG-NNN-<slug>/`, each with a `README.md` frontmatter
> block. `BACKLOG.md` and the monthly archives are **generated** from those blocks and must never be
> hand-edited inside their fences. Registration, status changes, ID allocation, and `.sln`
> registration go through `.claude/scripts/backlog/backlog_gen.py`; a pre-commit gate blocks a commit
> that leaves the rendered files stale. Session start uses `query --status "🟡,🟢"` instead of reading
> the file (~12 lines vs ~136). Minor bugs get no folder — mechanically enforced.
>
> **Why:** the row template was prose-enforced and drifted; BACKLOG.md was a 4.5k-token read on every
> session start; retired `BUG-NNN`s were invisible once archived, so IDs could be reused; and
> in-place spec rewriting destroyed the history SDD depends on.
>
> **Effective:** 2026-07-26. Spec: `Docs/Management/DevCycleCraft/spec-evolution-versioning/`.
> Files amended: `CLAUDE.md`, `.claude/rules/{workflow,bug-tracking,constraints-registry}.md`,
> `.claude/library/{workflow-rule-1,workflow-rule-2,workflow-rule-3,workflow-rules-6-7-8,bug-tracking-reference,spec-writing-guide,session-ops,constraints-reference}.md`,
> `.claude/agents/orchestrator.md`, `.claude/exception-registry.md`, `Docs/Management/BACKLOG.md` header.

---

## Post-apply verification (run before the `amend:` commit)

1. `Grep "BACKLOG\.md" .claude/ CLAUDE.md --glob *.md` → **no hit instructs an agent to read the
   file** (T13c's demo statement). Hits that describe it as generated, or name it as Helder's view,
   are expected and fine.
2. `python -m unittest discover -s .claude/scripts/backlog/tests -p "test_*.py"` → 144 green.
3. `python .claude/scripts/backlog/backlog_gen.py regen --check` → exit 0.
4. Commit subject prefix `amend:` with the rationale in the body (`CLAUDE.md § Amending These Rules`).
5. Check off T13a/T13b/T13c/T13d in `tasks.md`; set the feature's `README.md` `status:` to
   `✅ Done` with `--closed 2026-07`, then `regen`.

## Open for Helder in this bundle

- **Everything above is agent-drafted rules prose.** `CLAUDE.md § Authorship` requires a human read
  before any of it commits — that is the gate this document exists to serve, not a formality.
- **§10 and §11 are outside REQ-SEV-30's approved file list** (found by the mandatory sweep). They
  are in scope by REQ-SEV-30's own "add it to the table" instruction, but they widen the bundle.
- **§14 point 3** ("bulk operations announce in LEDGER; single-item writes do not") is the one
  genuinely new policy choice here rather than a transcription — the alternative is announcing every
  write, which would make `register` unusable in practice.
- The **gate-audit set** from the migration (≈38–41 agent-authored slugs/titles, all `order` values,
  reworded goals, dropped pre-scheme severities) is still open and is **not** part of this bundle —
  it is logged per-wave in `task-log.md`. Deferred follow-ups: `POST-MIGRATION-FOLLOWUPS.md`.
