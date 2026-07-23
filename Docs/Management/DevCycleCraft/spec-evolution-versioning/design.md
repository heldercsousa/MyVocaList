# Spec Evolution — Nested folders + generated BACKLOG — Design

Companion to `requirements.md`. Direction and rejected alternatives: `2026-07-21-nested-folders-and-generated-backlog-decision.md`. Prior research: `findings.md`.

## 1. Architecture in one line

**The folder tree is the database; BACKLOG.md and `backlog-archive/*.md` are generated views; agents query the database directly.**

```
Docs/Management/
  BACKLOG.md                          ← GENERATED view (active rows only)
  backlog-archive/BACKLOG-ARCHIVE-YYYY-MM.md   ← GENERATED views (closed rows, by closed month)
  cross-cutting/                      ← NEW: home for parent-less items (README.md + bugs/)
  BusinessFeatures/[feature]/
    README.md                         ← NEW: frontmatter + Goal/Gate for the feature row
    requirements.md · design.md · tasks.md · task-log.md
    changes/2026-07-21-inline-artist-create/README.md + spec files
    bugs/2026-07-21-BUG-050-suggestion-not-locked/README.md + spec files
  DevCycleCraft/[feature]/…           ← identical shape
```

`README.md` is the carrier because (a) the `.sln` gate wants a file, (b) GitHub/VS render it as the folder's front page, (c) it gives Goal/Gate prose a home that is not a table cell. A folder without `README.md` is invisible to the generator — and that is a validation error, not a silent skip (REQ-SEV-21).

## 2. Frontmatter schema

```yaml
---
id: BUG-050                       # BUG-NNN, or a kebab slug for non-bugs — unique tree-wide
title: "Song form — selecting an artist suggestion does not lock the field"
status: "💡 Pending"              # one of the 8 BACKLOG statuses
severity: Critical                # Critical | Major | Minor — bugs only
target: 2026-07-21                # registration date; YYYY-MM-DD | YYYY-MM | "—"
section: BusinessFeatures         # feature folders only: BusinessFeatures | DevCycleCraft
parent: artists-songs             # id of the parent folder's frontmatter; omitted at top level
goal: "Picking a suggestion must lock the Artist field."
gate: "Fix owned by the inline-artist-create change (T1)."   # optional
pointer: BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-050-suggestion-not-locked/
closed: 2026-07                   # required iff status is terminal
order: 20                         # optional tie-breaker within a parent
---
```

Design notes:
- **Declared refinement of the decision file.** The approved direction listed keys `id, title, status, severity, target, feature, parent`. This design splits `feature` into `section` (which BACKLOG table the row belongs to — the only thing the generator needs) and keeps `parent` for the logical row parent; the feature identity is then just the nearest ancestor's `id`, with no third redundant key to drift. Same information, one fewer place to get wrong — flagged here because the decision file is the approved input.
- **`parent` is declared, not inferred.** Path nesting gives the *render depth*; `parent` gives the *logical row parent*. They normally agree — the validator warns when they don't (catches a folder filed under the wrong feature).
- **`severity` drives nothing in the generator except validation**: a `severity: Minor` folder is a validation error (REQ-SEV-03 says Minor gets no folder). This makes the bug-tracking rule mechanical.
- **`closed` is a month, not a date** — it is exactly the archive key, so archiving cannot drift from the value that decides the file.
- **Parsing (NFR-1):** flat `key: value` only. Values are raw strings; quotes stripped if the whole value is quoted. Anything nested (a `-` list, an indented block) → validation error naming the key. No PyYAML dependency, no `yaml.safe_load` surface.

## 3. The generator

New modules inside the existing `.claude/scripts/backlog/` package.

> **Reuse assessment (decision-file constraint 4 — "assess existing machinery first").** Done, and the honest answer is that **no logic is reusable**: `backlog_lib.py` is a ~110-line pure classifier for *memory-note orphans* and never parses BACKLOG.md; `session_marker.py` records a session boundary. What is genuinely reused is the **package directory, the pure/shell split convention, and the existing `tests/` harness** — plus exactly one call site widened (§5). This is "extend rather than rewrite" in the sense of adding to the package without disturbing it, not in the sense of reusing its code. Helder should approve on that basis.

| File | Role |
|------|------|
| `frontmatter.py` | pure: parse a `README.md` → dict, or raise `FrontmatterError(path, reason)` |
| `model.py` | pure: `Item` record; `build_tree(items)` → ordered forest; `validate(items)` → `[error]` |
| `render.py` | pure: `render_backlog(tree, preserved_header)` / `render_archive(items, month, preserved_header)` → str |
| `backlog_gen.py` | I/O shell: walk, parse, validate, render, write; `--check` mode writes nothing |

Pure/shell split mirrors `lease_lib.py` and `backlog_lib.py` — the same reason: the interesting logic is unit-testable with no filesystem (NFR-4).

### CLI surface (three verbs, one script)

```
python .claude/scripts/backlog/backlog_gen.py register --section BusinessFeatures \
    --parent artists-songs --kind bug --severity Major \      # BUG-NNN auto-allocated, REQ-SEV-11a
    --title "..." --goal "..." [--gate "..."]      # REQ-SEV-11: creates folder + README + .sln entry, then regenerates
python .claude/scripts/backlog/backlog_gen.py status BUG-053 "🟡 In Progress"   # REQ-SEV-12
python .claude/scripts/backlog/backlog_gen.py status BUG-053 "✅ Fixed" --closed 2026-07
python .claude/scripts/backlog/backlog_gen.py regen [--check]                    # REQ-SEV-13
python .claude/scripts/backlog/backlog_gen.py query --status "🟡,🟢"             # REQ-SEV-22
```

`register` derives the folder name (`YYYY-MM-DD` from today + `BUG-NNN` + slugified title) rather than accepting a path — that is what keeps REQ-SEV-01 from depending on agent discipline. It also derives the **ID** (REQ-SEV-11a): `max(id)` over live folders ∪ all `backlog-archive/` months, + 1. Scanning the archives too is what prevents reuse of a retired number, which is exactly the fact BACKLOG.md used to carry and no longer can. `register` is atomic (REQ-SEV-21a): it stages folder + `README.md` + `.sln` edit in memory and writes them in one pass, rolling back on any failure, so the `.sln` HARD GATE can never be tripped by a half-registration.

> **Spec updated [2026-07-22]:** `--renumber` shipped as its own subcommand, `backlog_gen.py renumber BUG-053`, not as a flag on `register`. A flag was unreachable: `register`'s own arguments are `required=True`, so argparse can never accept a `register` invocation that supplies only `--renumber`. Behaviour is unchanged; only the CLI spelling differs. Caught at plan review, implemented in T6 (`51124a7`).

**Duplicate-ID resolution across worktrees:** `renumber BUG-053` rewrites the folder name and the `id:` key of the later-merged item, then regenerates. Renumbering is safe precisely because the ID appears in exactly two places (folder name, frontmatter) and every other reference is a path.

### Idempotency (REQ-SEV-13) — how it is guaranteed

Regeneration is a **total function of frontmatter + preserved regions**. There is no accumulation, no append path, no timestamp, no "last generated at" line. Writes happen only when the rendered bytes differ from the file on disk. `regen --check` returns non-zero on any difference — that is the CI/pre-commit gate and the test assertion.

### Preserved regions (REQ-SEV-14, REQ-SEV-20)

Generated content is fenced:

```markdown
<!-- BACKLOG:GENERATED:BEGIN business-features -->
| Target | Feature | Status | Notes |
...
<!-- BACKLOG:GENERATED:END business-features -->
```

Everything outside a fence — the header, Row rules, Status reference, archive-file prose headers — is read from the existing file and written back byte-identically. Absent fences (a brand-new archive month) are created from a template. This is why the 2026-03…2026-07 archives round-trip.

### Ordering (REQ-SEV-17)

Sort key per row: `(section_index, parent_chain, order ?? 500, target_sort, path)`, where `target_sort` normalizes `YYYY-MM` → `YYYY-MM-01` and `—` → sorts last. Today's file is not purely target-sorted (Helder has curated positions), so the migration assigns explicit `order:` values wherever the current order departs from the natural sort — that is precisely how REQ-SEV-25's row-for-row equivalence is achieved.

Two non-row artifacts survive as **frontmatter-declared separators**: `kind: milestone` (the `🏁 MVP release` line) and `kind: group` (the `Cross-cutting` row), rendered verbatim at their sorted position.

### Row rendering + mechanical template enforcement (REQ-SEV-09)

```
| {target} | {depth_arrows}{bold?}{title}{bold?} | {status} | Goal: {goal} [Gate: {gate}] Pointer: `{pointer}`. |
```

Before rendering, `validate()` runs the row-template checks the BACKLOG header currently states in prose: sentence count ≤ 3, word count ≤ ~50, exactly one backticked path, and a banned-token scan (7-hex-or-longer sha, `PASS`/`FAIL`, `AC-\d`, `\d+/\d+ green`, `\d+k tokens`, extra `.md`/`.cs` paths). A violation is an error with the folder path — the item's detail belongs in its own `README.md` body, which has no limit.

`depth_arrows` = `↳` × (path depth below the section root). Never authored.

### Archive split (REQ-SEV-18)

One pass, two sinks. For each item: terminal status → bucket by `closed`; else → live tree. Because bucketing is per-item, **a Done sub-row leaves while its active parent stays** — the archived row **drops its `↳` arrows entirely and carries a `(under: <parent title>)` suffix instead** — depth arrows are meaningless without the parent row present, and dropping them keeps archive rendering a pure function of the item alone, which is what makes REQ-SEV-13's byte-identical archive round-trip hold. The parent row in the live file simply loses that child. Grep-ability (REQ-SEV-18) is preserved because the full title including `BUG-NNN` is rendered unchanged into the archive.

An item whose parent is itself archived in a different month is rendered in its own month's file with the same suffix — months are independent views, never cross-referenced.

## 4. Replacing the Rule 7 read (REQ-SEV-23)

`query` walks the tree, filters, and prints one compact line per match:

```
🟡 2026-07-12  Inline Trivial Fix (ITF) lane  → DevCycleCraft/inline-trivial-fix/
🟢 2026-07-21  ↳ Song artist field — correctness fixes + inline create  → BusinessFeatures/artists-songs/changes/2026-07-21-inline-artist-create/
```

`workflow.md` Rule 7 step 1 becomes: *"run `backlog_gen.py query --status "🟡,🟢"` — do not read BACKLOG.md."* Today that is ~9 rows ≈ 12 lines against 136. Helder keeps the rendered file (REQ-SEV-24); agents stop paying for it. This also satisfies NFR-5: the agent runs one command, it does not glob.

## 5. Interaction with existing machinery

- `orphan_check.py` (Stop-hook advisory): its `backlog_changed_this_session()` — which lives in `orphan_check.py`, not `backlog_lib.py`, and detects change via `git diff --name-only HEAD` + `git ls-files --others` + an in-session commit diff, not a grep — currently watches only `Docs/Management/BACKLOG.md`. Since BACKLOG.md is now generated *from* folder writes, the check is widened to "BACKLOG.md **or** any `Docs/Management/**/README.md` changed" — otherwise registering a bug the new way would still trigger the "you didn't register it" advisory. Pure classifier logic in `backlog_lib.py` is unchanged.
- `session_marker.py`: unchanged.
- **Pre-commit hook** (`.claude/githooks/`): add `backlog_gen.py regen --check`. A commit touching any `README.md` frontmatter without a matching regenerated BACKLOG.md fails — this is what keeps the view from drifting when an agent hand-edits.
- **`.sln` gate:** `register` appends the `RelativePath = RelativePath` line for the new `README.md` to the matching Solution Folder, and creates the folder GUID entry when the Solution Folder is new (sequential counter, `constraints-reference.md § Visual Studio Solution`).

## 6. Migration (REQ-SEV-25 … 29)

**Five** sequential phases. **Phase 5 is the gate** and the only safe session-handoff point is the boundary between phase 3 and phase 4 (everything before it is additive; phase 4 rewrites the archive files).

1. **Feature READMEs** — one `README.md` per existing feature folder, frontmatter carrying that feature's current row verbatim (goal/gate/pointer split out of the Notes cell).
2. **Item folders for rows that already have one** — most `↳` rows already point at a `changes/` or `bugs/` folder; they only need frontmatter added.
3. **Counter-examples** — the eight bugs pointing at a parent `task-log.md` (BUG-027/029/030/031/032, BUG-050/051/052) get their own folder; each new `README.md` body opens with `> History before this folder existed: <parent>/task-log.md` (REQ-SEV-27, no deletion). BUG-012's flat file becomes a folder via `git mv` so blame follows. `cross-cutting-log.md` rows become `cross-cutting/` folders that link back to the log.
4. **Archives** — the 5 existing archive files are parsed once into item folders with `closed` set from the file name, then regenerated and diffed against the originals.
5. **Equivalence gate** — diff the generated BACKLOG.md against the frozen snapshot `migration/BACKLOG-pre-migration.md` (REQ-SEV-17). Every remaining diff line is enumerated in `task-log.md` with a reason, or the migration is not done (REQ-SEV-29).

Ordering rationale: phases 1 and 2 are purely additive; phase 3 is additive except for BUG-012's `git mv` (a rename, trivially reversible, and the only existing-file rewrite before the gate); phase 4 is the first destructive phase — it rewrites the 5 archive files — and runs only after the live tree already round-trips. Phase 5 gates the whole thing.

> **Spec updated [2026-07-23] — archive-regen gate redefined (Helder decisions F-2/F-3, T12a planning).** The T12a inventory enumerated **105 archived rows** (not the ~50 estimated in R-3) and established that a **byte-match of a regenerated archive against its committed original is impossible**: the renderer emits a canonical Notes format (`Goal: … Pointer: \`…\`.`), **drops the `↳` arrow** on archived rows (intentional `render_row` branch), and **appends `(under: <parent title>)`**, none of which the committed archive text carries. Therefore:
> - **Phase 4 canonically rewrites** all 5 archive files to the generator's format — this is now understood as the *intended* output, not a transcription that must match the old bytes.
> - **Phase 5 gate is redefined** to three checks: **(G1)** `regen --check` run twice yields zero diff (idempotency, REQ-SEV-13); **(G2)** every archived `BUG-NNN` from the frozen snapshot is still grep-reachable in some archive file (REQ-SEV-18/20); **(G3)** the `↳`-drop, `Goal:`-prefix, and `(under:)`-suffix reformattings are enumerated in `task-log.md` as a **named archive-migration diff class**, kept *separate* from REQ-SEV-25's four active-BACKLOG classes (those remain unwidened — REQ-SEV-25 governs the live BACKLOG.md gate, not the archives).
> - **Extended statuses (F-3):** `STATUSES` gains terminal `Superseded` and `Duplicate` states (`model.py` + `render.py`, phase-4 wave, with tests) so committed archive Status cells (`🔵 Superseded (closed …)`, `🔵 Duplicate (closed)`) reproduce faithfully instead of being normalized to `✅ Fixed`. `Closed — partially regressed` (BUG-019) stays reconciled to `✅ Fixed` (resolved decision 5).
> - **F-1 folder model for the 43 log-pointer rows:** the 21 `cross-cutting-log.md` rows follow §6.3's `cross-cutting/` model; the ~22 non-bug sub-rows sharing a feature `task-log.md` each get a `changes/<slug>/README.md` under their parent with `pointer:` kept on the shared log (REQ-SEV-27, nothing deleted). Their `id`/slug/`title`/`order` are agent-authored and flagged for the gate audit.

## 7. Risks

| Risk | Mitigation |
|------|-----------|
| Hand-edits to BACKLOG.md silently lost on next regen | pre-commit `--check` fails the commit; generated banner names the command |
| Migration loses curated row order | explicit `order:` values + the phase-5 equivalence diff |
| Frontmatter drifts from an item's own spec files | `pointer` defaults to the folder itself; the README body is the only prose home |
| Two worktrees register items concurrently | folder-per-item means no shared *line* to conflict on and regen is deterministic, so the file merges cleanly — but the **`BUG-NNN` collides** (both derive the same `max+1`). Caught by the duplicate-`id` validation at merge; resolved with `renumber` (REQ-SEV-11a). Depends on R-2 being blocking. |
| Restricted YAML parser rejects a legitimate value | errors name key + path and abort; values are prose in a table cell — nesting is never needed |

## 8. Decisions (approved)

> **Status: APPROVED by Helder 2026-07-22** — spec and all three recommendations below accepted as written. R-1/R-2/R-3 are now binding design decisions, not proposals; the "if rejected" notes are retained only as reversal conditions. Spec moves to 🗺️ Plan.

### R-1 — Frontmatter carrier: **`README.md`** (recommended)

The `.sln` cost is real but bounded: one `SolutionItems` line per item, appended automatically by `register` (§3), so it is never hand-maintained and never a gate an agent can forget. Against that, `README.md` is what VS Solution Explorer, GitHub, and any future web view render as the folder's front page — the Goal/Gate prose is visible exactly where someone lands. `item.md` would save nothing (the `.sln` line is required either way, per the HARD GATE) and buys only a filename.
*If rejected:* rename the carrier throughout §2/§3 and REQ-SEV-02/07. No logic changes.

### R-2 — Pre-commit `--check`: **blocking** (recommended)

This is the recommendation I hold most firmly, because advisory defeats the change's core premise. A generated view that *may* be stale is worse than a hand-written file — readers cannot tell which state they are looking at, and the whole argument for generation is that the view cannot drift from the source. Advisory also downgrades REQ-SEV-11a: duplicate `BUG-NNN`s would reach `develop` and be discovered later, when renumbering means touching an item someone has already started work against.

The blocking cost is low and self-clearing: the failure message is *"run `backlog_gen.py regen`"* — a one-command fix, not a debugging session — and it only fires on commits that touch frontmatter. This is the same posture the repo already takes for the `.sln` gate.

Note the deliberate asymmetry with `orphan_check.py`, which is advisory: that hook *guesses* (it classifies prose for new-work intent and can be wrong, so it must not block). `regen --check` is a byte comparison with no false positives. Blocking is only defensible because the check is exact.
*If rejected:* §7's top risk loses its mitigation; fallback is a `regen --check` step in the subagent exit checklist and `/sln-commit`, and REQ-SEV-11a's duplicate-ID guarantee weakens from "cannot reach develop" to "caught at next regen".

### R-3 — Migration span: **two sessions, handing off at the 3/4 boundary** (recommended)

~50 rows across 5 phases exceeds the Rule 2 task-sizing bound for one dispatch. The 3/4 boundary is the correct seam because it is the last point at which everything done so far is additive — phases 1–3 only add folders and frontmatter, leaving BACKLOG.md and the archives untouched, so an interrupted migration is a no-op for every other agent. Phase 4 is the first destructive step and phase 5 is the gate, so they belong together in one session with the equivalence diff.
*If rejected:* the planner must instead split within phase 4, which is worse — a half-rewritten archive set has no clean resume point.

### Consequence if all three are approved

Planning may begin (`writing-plans` → plan-reviewer). Nothing above authorizes implementation.
