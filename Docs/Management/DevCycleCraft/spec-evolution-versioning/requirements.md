# Spec Evolution — Nested `bugs/`/`changes/` folders + generated BACKLOG — Requirements

**Feature:** Spec Evolution, Versioning & Feature-Folder Organization (BACKLOG: Dev Cycle Craft / 2026-07-09, `📋 Spec`)
**Approved direction:** `2026-07-21-nested-folders-and-generated-backlog-decision.md` — its decisions and rejected alternatives are inputs, not open questions.
**Scope:** Part A (nested folder pattern) + Part B (generated BACKLOG) delivered as ONE change. The folder structure is what makes the generator possible; neither ships alone.

## Out of scope (this change)

- Spec **semver** version headers, `decision-log.md` adoption, and the bug-fix→spec-version binding line (findings.md points 1/4, `sdd/spec-s9-2-1`). They ride on the folder pattern; they are a follow-up row once folders exist.
- Merging delta specs into current-truth files (OpenSpec "archive+merge"). Current-truth files keep only forward links.
- `LEDGER.md`, `cross-cutting-log.md`, and `tasks.md` marker vocabulary — untouched.
- Richer task-status vocabulary (its own BACKLOG sub-row).

## User stories

- **US-1 — As the orchestrator**, when Helder says "register this bug/opportunity", I create a folder with frontmatter and regenerate BACKLOG, rather than hand-editing a table row.
- **US-2 — As any agent at session start**, I learn what is active from a ~15-line query instead of reading a 136-line file.
- **US-3 — As Helder**, I still open one BACKLOG.md and see the whole product picture, in the order I expect.
- **US-4 — As anyone doing history lookup**, `grep -r BUG-0NN Docs/` finds the item's folder and its archived row in one hop.
- **US-5 — As a future reader of a feature**, everything that ever happened to that feature is nested under its folder, chronologically ordered by folder name.

---

## Acceptance criteria

### A. Folder pattern

- **REQ-SEV-00** The date prefix is always a full `YYYY-MM-DD`. When an item's registration day is unknown (a migrated row whose `target` is a bare `YYYY-MM`), the day is `-01` — a fixed rule, never per-item judgement. `target` keeps its original `YYYY-MM` value; only the folder name is padded.
- **REQ-SEV-01** Every Critical or Major bug belonging to a feature lives at `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/bugs/YYYY-MM-DD-BUG-NNN-<slug>/`; every change lives at `…/[feature]/changes/YYYY-MM-DD-<slug>/`. Folder names are lowercase `bugs/` and `changes/`; the date is the registration date in ANSI form; `BUG-NNN` follows the date for bugs.
- **REQ-SEV-02** An item folder contains `README.md` (frontmatter + the row's Goal/Gate prose) and, as needed, `requirements.md`, `design.md`, `plan.md`, `tasks.md`, `task-log.md`. Only `README.md` is mandatory.
- **REQ-SEV-03** Minor bugs get NO folder — a line in the parent's `task-log.md` and the commit message, per `bug-tracking.md`. A Minor bug therefore never appears in BACKLOG.
- **REQ-SEV-04** A bug with no single parent business feature is registered under `Docs/Management/cross-cutting/bugs/…` and renders under the existing `### Cross-cutting` grouping.
- **REQ-SEV-05** Nesting depth is unbounded: a change folder may itself contain `bugs/`/`changes/`. Render depth is derived from path depth (`↳`, `↳↳`, `↳↳↳`), never hand-typed.
- **REQ-SEV-06** No item folder may be created without a matching `.sln` `SolutionItems` entry in the same commit (existing HARD GATE, `constraints-registry.md`).

### B. Frontmatter

- **REQ-SEV-07** Every feature folder and every item folder carries YAML frontmatter at the top of its `README.md` with required keys: `id`, `title`, `status`, `target`, `section` (feature folders only), `goal`. Optional: `severity`, `gate`, `pointer`, `parent`, `closed` (YYYY-MM), `order`.
- **REQ-SEV-08** `status` is one of the valid BACKLOG statuses; `severity` one of `Critical|Major|Minor`; `target` an ANSI date or `YYYY-MM` or `—`. Any other value is a validation error (REQ-SEV-19). *(Spec updated 2026-07-23, F-3: the set is extended beyond the original eight with terminal `Superseded` and `Duplicate` states so archived rows carrying `🔵 Superseded (closed …)` / `🔵 Duplicate (closed)` validate and reproduce faithfully. `model.STATUSES`/`render.py` change in the archive-rewrite wave, with tests.)*
- **REQ-SEV-09** `goal` + `gate` together must satisfy the existing PO-level row template: ≤ 3 sentences and ≤ **55 words** total (the exact mechanical bound replacing the header's prose "~50 words"; whitespace-split tokens, the pointer excluded), exactly one `pointer`, and none of the banned content (commit hashes, extra file paths, root-cause narrative, review verdicts, test counts, per-step status trails, token measurements, AC numbers). A row violating this fails validation — the prose rule becomes mechanical.
- **REQ-SEV-10** `pointer` defaults to the item's own folder when omitted; an explicit value is used verbatim.

### C. Registration flow (agent-driven)

- **REQ-SEV-11** A single command registers a new item: it creates the folder, writes `README.md` with valid frontmatter, adds the `.sln` entry, and regenerates BACKLOG.md — no table row is ever appended by hand.
- **REQ-SEV-11a** **BUG-NNN allocation replaces the "read BACKLOG.md for the highest ID" rule** (`bug-tracking.md`), which REQ-SEV-23 removes. `register --kind bug` derives the next ID itself by scanning every `id:` in the tree (live folders **and** all `backlog-archive/` months, so retired IDs are never reused) and taking `max + 1`; `--id` may be passed only to assert an expected value and errors if it disagrees. Concurrency: two worktrees registering simultaneously both derive the same number and neither sees the other, so the ID is provisional until merge — the **pre-commit `--check` (REQ-SEV-21) fails on a duplicate `id` at merge time**, and the resolution is to run `renumber <id>` on the later item *(spelling updated 2026-07-22 — its own subcommand, not a `register` flag; see design.md §3)*, which rewrites the folder name and frontmatter. Duplicate IDs must never reach `develop`.
- **REQ-SEV-12** A single command sets an item's `status` (and `closed` when terminal) and regenerates. Milestone status updates cost one call.
- **REQ-SEV-13** Regeneration is **idempotent**: running it twice with no frontmatter change produces a byte-identical BACKLOG.md and byte-identical archive files (verifiable as `git diff --exit-code`). It is therefore safe to run at every workflow milestone.
- **REQ-SEV-14** Regeneration never destroys unknown content: everything outside the generated regions of BACKLOG.md (header, row rules, status reference) is preserved verbatim.
- **REQ-SEV-15** The generator is deterministic and offline — stdlib-only Python, no network, no LLM call — so two agents on two worktrees produce identical output from identical frontmatter.

### D. Generated BACKLOG + archives

- **REQ-SEV-16** BACKLOG.md's two tables (Business Features, Dev Cycle Craft) are generated from frontmatter. Only rows with an **active** status (💡 📋 🗺️ 🟢 🟡 🔵 🔴) appear.
- **REQ-SEV-17** Row order is stable and reproduces today's reading order — verified against a frozen snapshot of the pre-migration file committed as `spec-evolution-versioning/migration/BACKLOG-pre-migration.md`, which is the fixture the equivalence test (REQ-SEV-29) diffs against: `section` → explicit `order` when present → `target` → path. Non-row separators that exist today (the `🏁 MVP release` marker, the `Cross-cutting` grouping row) survive regeneration.
- **REQ-SEV-18** Rows with a terminal status (`✅ Done`/`✅ Fixed`/superseded-closed) are emitted instead into `Docs/Management/backlog-archive/BACKLOG-ARCHIVE-YYYY-MM.md`, keyed by the `closed` month — never by the target date. A Done sub-row archives while its still-active parent stays in the live file; the archived row keeps its full title including `BUG-NNN`, so `grep -r BUG-048 Docs/Management/backlog-archive/` still hits.
- **REQ-SEV-19** A terminal item with no `closed` month is a validation error, not a silent drop.
- **REQ-SEV-20** Archive files are generated with the same template and preserve their existing hand-written header prose (2026-03 … 2026-07 must round-trip without content loss). *(Spec updated 2026-07-23, F-2: "round-trip without content loss" means no archived `BUG-NNN`/row is dropped — NOT a byte-match of the old Notes text. The archive **rows** are canonically rewritten (Notes → `Goal: … Pointer: …`, `↳` dropped, `(under: <parent>)` appended); only the header prose outside the fences is byte-preserved. The gate is idempotency + grep-reachability, defined in design.md §6.)*
- **REQ-SEV-21** Validation failures are reported with folder path + reason and **abort** the write — BACKLOG.md is never left partially generated. The error set is: bad `status`/`severity`/`target` value · missing required key · missing `closed` on a terminal item · Notes over the REQ-SEV-09 bound · banned content · duplicate `id` · `parent` naming no existing item · `parent` disagreeing with the folder's path parent · **a folder whose `severity` is `Minor`** (REQ-SEV-03) · nested/unsupported YAML (NFR-1).
- **REQ-SEV-21a** Error-path behaviour of the other verbs: `register` is **atomic** — folder, `README.md`, and the `.sln` entry are written together or not at all, so REQ-SEV-06 can never fail from a half-completed registration; `status` on an unknown `id` exits non-zero and changes nothing; `query` (REQ-SEV-22) **never hard-fails** — a malformed `README.md` is skipped with a one-line warning to stderr, because session start must not be blocked by an unrelated bad file (only `regen` aborts).

### E. Query replacing the Rule 7 read

- **REQ-SEV-22** A query command returns the active work set — filterable at minimum by status — as compact lines (`status · target · title · pointer`), reading frontmatter directly, not BACKLOG.md.
- **REQ-SEV-23** `workflow.md` Rule 7 step 1's "read `Docs/Management/BACKLOG.md`" is replaced by that query scoped to `🟡,🟢`. Measured target: ≤ 20 output lines vs the current 136-line / ~4.5k-token read.
- **REQ-SEV-24** BACKLOG.md remains committed and human-readable for Helder (US-3); the query is the agent path, not a replacement for the file.

### F. Migration of existing rows

- **REQ-SEV-25** All ~50 live rows plus the 5 existing archive files are migrated: each becomes a folder with frontmatter, and regeneration reproduces the current BACKLOG.md content — **row-for-row equivalent** (same rows, same order, same Goal/Gate/Pointer text). The **only** permitted diff classes, each of which must still be enumerated line-by-line in the task-log, are: (a) trailing-whitespace removal, (b) single-space normalization inside a cell, (c) a `pointer` path changed by REQ-SEV-26's counter-example moves, (d) Notes text shortened to meet the REQ-SEV-09 bound — with the removed sentence relocated verbatim into the item's `README.md` body. Any other diff is a defect, not a normalization. *(Spec updated 2026-07-23, F-2: these four classes govern the **live BACKLOG.md** gate only. The **archive files** are canonically rewritten and cannot byte-match their originals — they carry a separate **archive-migration diff class** (the `↳`-drop, `Goal:`-prefix, and `(under: <parent>)`-suffix reformattings), enumerated in the task-log and gated by idempotency + grep-reachability per REQ-SEV-20 and design.md §6. REQ-SEV-25's four classes are NOT widened to cover the archives.)*
- **REQ-SEV-26** Counter-examples get a real folder, and their `pointer` moves off the parent `task-log.md` onto their own folder:
  - BUG-050/051/052 → under `BusinessFeatures/artists-songs/bugs/2026-07-21-BUG-05N-<slug>/` (currently point at the DX-AC change task-log).
  - BUG-027/029/030/031/032 → under `BusinessFeatures/artists-songs/bugs/2026-07-03-BUG-0NN-<slug>/` (currently point at the parent `artists-songs/task-log.md`).
  - BUG-012 → the flat file `BusinessFeatures/venues/bugs/BUG-012-venuesviewmodel-fetch-slow.md` becomes `bugs/2026-03-01-BUG-012-venuesviewmodel-fetch-slow/README.md`, content preserved via `git mv` so history follows.
- **REQ-SEV-27** Migration preserves the narrative already living in the old pointer target: the parent `task-log.md` entries are **not** deleted; the new folder's `README.md` links back to them. No history is destroyed by migration.
- **REQ-SEV-28** Rows whose pointer is `cross-cutting-log.md` (folder-less items) migrate to `Docs/Management/cross-cutting/…` folders; `cross-cutting-log.md` itself is retained and linked, not deleted.
- **REQ-SEV-28a** *(added 2026-07-23, F-1)* Archived **non-bug sub-rows** whose pointer is a shared feature `task-log.md` (e.g. Search-Picker page sub-rows, form-validation 01–06, crud-dedup Steps 1–7e, rules-refactoring sub-tasks) each become a `changes/<slug>/README.md` under their parent feature; the `pointer:` stays on the shared `task-log.md` (REQ-SEV-27, nothing deleted). Their `id`/slug/`title`/`order` are agent-authored from the row label and flagged for the gate audit — no design model existed for these before this decision.
- **REQ-SEV-29** Migration is verified by a before/after comparison committed to the task-log: pre-migration BACKLOG.md vs generated BACKLOG.md, with every diff line accounted for. *(Spec updated 2026-07-23, F-2: the **archive** half of the gate is verified differently — not a before/after byte-diff (impossible after canonical rewrite) but the three checks G1 `regen --check` idempotency, G2 every frozen-snapshot `BUG-NNN` still grep-reachable, G3 the archive-migration diff class enumerated. See design.md §6.)*

### G. Rule amendments (must land in the same change)

- **REQ-SEV-30** These files are amended **together**, in one `amend:` commit with a changelog entry. Since the routing-table refactor the authoritative prose lives in the library section files, so amending the routing tables alone would leave the library contradicting them on day one — a live SDD-Invariant violation. The complete list:

  | File | What changes |
  |------|--------------|
  | `CLAUDE.md § Docs/ Folder Layout` | nested `bugs/`/`changes/` shape + `README.md` carrier |
  | `CLAUDE.md § Development Methodology` | the in-place spec-update wording |
  | `.claude/rules/workflow.md` | SDD Invariant, Rule 1, Rule 3 ritual, Rule 7 step 1 |
  | `.claude/library/workflow-rule-1.md` | spec-folder routing + proactive-triage format |
  | `.claude/library/workflow-rule-3.md` | Session-End Spec Update Ritual |
  | `.claude/library/workflow-rules-6-7-8.md` | Rule 7 step 1's literal "read `Docs/Management/BACKLOG.md`" → the query |
  | `.claude/rules/bug-tracking.md` | folder placement Critical/Major; no folder for Minor; ID allocation per REQ-SEV-11a |
  | `.claude/library/bug-tracking-reference.md` | same, in full detail (named in `findings.md § Constraints`) |
  | `.claude/library/spec-writing-guide.md` | item-folder file set + frontmatter block |
  | `.claude/library/session-ops.md` | session-start read set drops the BACKLOG read |
  | `Docs/Management/BACKLOG.md` header | row rules become "generated — do not hand-edit" (REQ-SEV-31) |

  A grep for `BACKLOG.md` across `.claude/` at implementation time is mandatory — any hit not in this table is added to it before the amend commit lands.
- **REQ-SEV-31** BACKLOG.md gains a generated-file banner naming the regeneration command; the defensive "agents: do NOT re-fatten this file" rule is replaced by mechanical validation (REQ-SEV-09).

---

## Non-functional

- **NFR-1** Stdlib-only Python (no PyYAML): frontmatter parsing is a restricted subset — flat `key: value`, no nested structures, no anchors.
- **NFR-2** Full regeneration over the whole `Docs/Management/` tree completes in < 2 s, measured as the wall-clock of `regen --check` on a warm filesystem on Helder's Windows dev machine, reported once in the task-log. It is a budget, not a per-run assertion — no test fails on timing.
- **NFR-3** Cross-platform: Windows dev + Linux CI, forward-slash-normalized paths, LF endings preserved.
- **NFR-4** The generator has unit tests alongside `backlog_lib.py`'s existing tests (`.claude/scripts/backlog/tests/`), including idempotency (REQ-SEV-13) and the archive-split case (REQ-SEV-18).
- **NFR-5** No open-ended `Glob("Docs/**")` is introduced into any agent's session-start path — the query reads the tree itself, agents do not.
