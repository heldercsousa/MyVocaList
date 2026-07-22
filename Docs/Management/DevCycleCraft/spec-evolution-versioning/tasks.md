# Spec Evolution — Nested folders + generated BACKLOG — Tasks

Plan: `plan.md` · Spec: `requirements.md`, `design.md` (approved 2026-07-22)

**Markers:** `[ ]` available · `[~]` claimed · `[x]` done · `[CANCELLED: reason]`

**All sequential — no `[P]`.** Every task after T2 consumes the previous task's contract or writes the same generated files; the file-overlap check forbids a wave.

**Lane split:** T0–T7 are code → **git worktree on a task branch** (HARD RULE). T8–T13 are docs/migration → **`develop`** (docs land on develop). Merge the worktree (T7b) before starting T8, or the generator will not exist.

**Migration tasks are split by row group** so each stays inside the Rule 2 sizing bound (≤ 5 files / ≤ 2h). The row counts below come from the frozen fixture; re-count at T8 and re-split if a group exceeds ~12 rows.

---

## Phase 0 — Setup (worktree)

- [x] **T0 — Create the worktree**
  Files owned: none. Risk: Low. Review lane: Standard.
  ```bash
  git worktree add ../mvl-backlog-generator -b feature/backlog-generator develop
  cd ../mvl-backlog-generator && git merge-base --is-ancestor develop HEAD && echo "base OK"
  ```
  Demo: `base OK` printed (HARD RULE — the base branch must be `develop`, never `main`).
  Also at T0: check `MyVocaList.sln` for existing `.claude\scripts\backlog\*` entries and record the finding (Global Constraints — `.sln` scope for scripts is unresolved).

## Phase 1 — Generator (worktree `feature/backlog-generator`)

- [x] **T1 — Frontmatter parser**
  Consumes: nothing. Produces: `parse(text) -> (dict, body)`, `FrontmatterError(reason, path)`.
  Files owned: `frontmatter.py`, `tests/test_frontmatter.py`. Risk: Low (B). Review lane: Standard. Demo: 8 tests green.
- [x] **T2 — Item model, validation, ordering**
  Consumes: T1. Produces: `Item` (+ `is_terminal`, `is_separator`, `status_label`), `validate`, `order_items`, `target_sort`, `notes_violations`, `STATUSES`, `TERMINAL`, `SEVERITIES`.
  Files owned: `model.py`, `tests/test_model.py`. Risk: **High (A)** — validation is the mechanical enforcement of the row template. Review lane: Elevated. Demo: 22 tests green (19 + 3 separator/section tests added at plan re-review).
- [x] **T3 — Row/table rendering + fenced splice**
  Consumes: T2. Produces: `render_row`, `render_table`, `splice`, `render_backlog`, `RenderError`, `FENCE_BEGIN/END`, the three table heads.
  Files owned: `render.py`, `tests/test_render.py`. Risk: **High (A)** — byte-preservation outside fences is what protects the hand-written header. Review lane: Elevated. Demo: 13 tests green (11 + 2 milestone/group frozen-fixture tests added at plan re-review).
- [x] **T4 — Monthly archive rendering**
  Consumes: T3. Produces: `bucket_by_month`, `render_archive`, `ARCHIVE_TEMPLATE`.
  Files owned: `render.py`, `tests/test_render.py`. Risk: Medium (B). Review lane: Standard. Demo: 17 tests green (13 + 4 ArchiveTests); a Done child archives while its active parent stays.
- [x] **T5 — CLI shell: `regen`, `--check`, `query`**
  Consumes: T1–T4. Produces: `walk`, `cmd_regen`, `query_lines`, `cmd_query`, `_read`/`_write`/`_rel`.
  Files owned: `backlog_gen.py`, `tests/test_backlog_gen.py`. Risk: **High (A)** — idempotency is the core guarantee. Review lane: Elevated. Demo: `regen` twice → byte-identical; `--check` writes nothing.
- [x] **T6 — `register` / `status` / `renumber` + atomic `.sln` write**
  Consumes: T5. Produces: `next_bug_id`, `slugify`, `_folder_for`, `_readme_text`, `sln_add_entry`, `cmd_register`, `cmd_status`, `cmd_renumber`.
  Files owned: `backlog_gen.py`, `tests/test_backlog_gen.py`. Risk: **High (A)** — ID allocation and atomicity. Review lane: Elevated. Demo: register a bug → folder + README + `.sln` line + regenerated row; `renumber` renames folder and id.
- [x] **T7 — Widen `orphan_check`'s watch set**
  Consumes: T5. Produces: `WATCHED_PATHS`, `is_watched`; `backlog_changed_this_session` rewritten.
  Files owned: `orphan_check.py`, `tests/test_orphan_check_widening.py`. Risk: Medium (B) — must preserve the fail-open posture (INV-1). Review lane: Standard. Demo: 4 tests green; full suite still green.
  > The **blocking pre-commit gate is NOT here** — it is T12b. Installing it now would block T8–T11's own commits.
- [x] **T7b — Code review + merge `feature/backlog-generator` → `develop`**
  Consumes: T1–T7. Produces: the generator available on develop. Files owned: none (merge only). Risk: Medium. Review lane: **Elevated — fresh code-review subagent before the merge.** Demo: full suite green on develop.

## Phase 2 — Migration, additive (develop)

- [x] **T8 — Freeze fixture + insert fences**
  Consumes: T7b. Produces: `migration/BACKLOG-pre-migration.md`, the four fence markers.
  Files owned: `BACKLOG.md` (fences only), `migration/BACKLOG-pre-migration.md`, `MyVocaList.sln`. Risk: Low. Review lane: Standard.
  Demo: `git diff --stat` = 4 insertions, 0 deletions; `regen --check` exit code recorded in the task-log (the cheapest early signal that no pre-existing README breaks the walk).
- [x] **T9a — Feature READMEs: Business Features top-level rows** (3 written; 10 rows deferred — see task-log T9a)
  Consumes: T8. Produces: one `README.md` per top-level Business Feature + their `order:` values.
  Files owned: those READMEs, `MyVocaList.sln`. Risk: Medium. Review lane: Standard. Demo: `regen --check` never exits 2.
- [x] **T9b — Feature READMEs: Dev Cycle Craft top-level rows** (9 written of 28 top-level rows; 18 folder-less rows → T9c-2, 1 blocked — see task-log T9b)
  Consumes: T8 (independent of T9a in content, but serialized — both write `MyVocaList.sln`).
  Files owned: those READMEs, `MyVocaList.sln`. Risk: Medium. Review lane: Standard. Demo: same.
> **Sizing correction [2026-07-22]:** T9a's "~12 rows" estimate was wrong. Only **3 of 13**
> top-level Business Feature rows have an existing spec folder; the other 10 route to T9c, T10b
> or T11c. The row-group split was estimated from the BACKLOG row count without checking which
> rows had folders. T9c is correspondingly larger and is split below.

- [x] **T9c-1 — Folder-less Business Features rows → `cross-cutting/` folders** (6 of 7 written; Windows version blocked — see task-log T9c-1)
  Consumes: T9a/T9b. Covers: Form & Autocomplete UX Overhaul, User Tutorial/Learning, Website,
  Singer self-registration, Social features, Dead-code cleanup QueueService (needs its OWN folder —
  its pointer sits inside `queue-management/`, already owned by another row), Windows version
  (BLOCKED — see below).
  Files owned: `Docs/Management/cross-cutting/**`, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [x] **T9c-2a — Folder-less Dev Cycle Craft rows, first half** (9 of 9 written — see task-log T9c-2a)
  Consumes: T9c-1. T9b reported **18** folder-less top-level Dev Cycle Craft rows — over the Rule 2
  bound, so split in two. Take the first 9 in table order.
  Files owned: `Docs/Management/cross-cutting/**`, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [ ] **T9c-2b — Folder-less Dev Cycle Craft rows, second half** (~9 rows + the Autocomplete Mobile UX Pattern row)
  Consumes: T9c-2a. Includes **① Autocomplete Mobile UX Pattern** (pos 24), which needs its OWN
  folder — its pointer is a file inside `autocomplete-component/`, whose folder is owned by sub-rows
  that this row does not parent.
  Files owned: `Docs/Management/cross-cutting/**`, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.

> **Queued for T13 (`amend:` process, not to be done mid-migration):** `constraints-registry.md`
> records the sequential `.sln` Solution-Folder GUID counter as last-used `0041`; the highest
> actually in use before T9c-1 was `0056`. Found by the T9c-1 implementor.

> **⛔ SYSTEMIC — needs Helder's decision before T12.** REQ-SEV-09's banned-content rule
> (`model._BANNED`) rejects file references (`\S+\.(cs|xaml|py|md)`) and review verdicts
> (`PASS`, `AC-\d+`). For **governance rows the file name IS the subject** — e.g. "BACKLOG-first
> Registration Enforcement", whose goal is *"work items must be registered in BACKLOG.md before
> memory writes"*. Such a row cannot be transcribed faithfully AND satisfy the rule.
> Impact so far: **1 row blocked** (BACKLOG-first Registration Enforcement) and **5 rows trimmed**
> (orders 20, 100, 110, 150, 520) by relocating overflow verbatim into the README body. No text was
> reworded, but those rows will render SHORTER in the regenerated BACKLOG than they read today.
> Options: (A) Helder supplies compliant one-line goals for the affected rows; (B) exempt
> `Dev Cycle Craft` governance rows from the file-reference pattern; (C) accept the trimming and
> record it as permitted diff class (d) at T12. **This is why T12 will not be a clean byte-match.**

> **⛔ BLOCKED — needs Helder before T12.** The **Windows version** row has no Goal in BACKLOG
> (Gate + Pointer only), but `model.REQUIRED` makes `goal` mandatory. Inventing one is content
> fabrication; omitting it makes `regen` exit 2. Options: (A) Helder supplies a one-line goal —
> recommended; (B) relax `REQUIRED`, which weakens REQ-SEV-09 for every future row. Until resolved,
> the row cannot be migrated and T12's equivalence gate will show it as missing.
- [ ] **T10a — READMEs for existing `bugs/` folders**
  Consumes: T9c. Files owned: those READMEs, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [ ] **T10b — READMEs for existing `changes/` folders + the two separator rows**
  Consumes: T10a. Produces: item READMEs, `cross-cutting/README.md` (`kind: group`), `milestones/2026-06-mvp-release/README.md` (`kind: milestone`).
  Files owned: those files, `MyVocaList.sln`. Risk: Medium. Review lane: Standard. Demo: `regen --check` never exits 2.

> **⏸ HANDOFF SEAM after T10b.** All work so far is additive — BACKLOG.md's rendered rows are untouched (the fences still wrap the original hand-written table; nothing regenerates until T12). Safe session end; resume at T11a from the task-log Checkpoint block.

## Phase 3 — Migration, destructive (develop)

- [ ] **T11a — BUG-050/051/052 get folders**
  Consumes: T10b. Each back-links `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md`; nothing is deleted from it (REQ-SEV-27).
  Files owned: 3 folders, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [ ] **T11b — BUG-027/029/030/031/032 get folders**
  Consumes: T11a. Each back-links `BusinessFeatures/artists-songs/task-log.md`; preserve each row's `🔵 Deferred` status and its deferral reason as `gate:`.
  Files owned: 5 folders, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [ ] **T11c — BUG-012 flat file → folder**
  Consumes: T11b. `git mv` so history follows; `-01` day per REQ-SEV-00.
  Files owned: 1 folder, `MyVocaList.sln`. Risk: Medium (`git mv` history). Review lane: Standard. Demo: `git log --follow` shows pre-move commits.
- [ ] **T12a — Archived rows → item folders**
  Consumes: T11c. One folder per row in the 5 archive files, `closed:` from the file name's month. Split per archive month if any single month exceeds ~12 rows.
  Files owned: those folders, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [ ] **T12 — Archive fences + the equivalence gate**
  Consumes: T12a. Files owned: 5 archive files, `task-log.md`. Risk: **High — this is the gate.** Review lane: **Architectural (Helder).**
  Demo: every diff hunk vs the frozen fixture classified into REQ-SEV-25's four permitted classes; `regen --check` exit 0; `grep BUG-048` still hits an archive; query ≤ 20 lines.
- [ ] **T12b — Install the blocking pre-commit gate**
  Consumes: T12 (precondition: `regen --check` exits 0). Produces: the R-2 gate.
  Files owned: `.claude/githooks/pre-commit`. Risk: Medium. Review lane: Standard. Demo: a deliberately stale BACKLOG is rejected; a clean tree commits.

## Phase 4 — Rules

- [ ] **T13a — Amend the routing tables**
  Consumes: T12b. Files owned: `CLAUDE.md`, `.claude/rules/workflow.md`, `.claude/rules/bug-tracking.md`, `BACKLOG.md` header banner. Risk: High. Review lane: **Architectural (Helder).**
- [ ] **T13b — Amend the library section files**
  Consumes: T13a. Files owned: `.claude/library/{workflow-rule-1,workflow-rule-3,workflow-rules-6-7-8,bug-tracking-reference,spec-writing-guide,session-ops}.md`. Risk: High. Review lane: **Architectural (Helder).**
  > T13a and T13b must land in **one `amend:` commit** — split for sizing/review only, committed together, or the routing tables contradict the library for the duration.
- [ ] **T13c — Changelog + contradiction sweep**
  Consumes: T13b. Files owned: `Docs/Changelog/changelog.md`. Risk: Low. Review lane: Standard.
  Demo: `grep -rn "BACKLOG.md" .claude/ CLAUDE.md` returns no instruction to read the file.

---

## Gates

| Gate | When |
|------|------|
| Fresh code-review subagent | T7b, before merging the generator to develop |
| Equivalence gate — every diff hunk classified | T12 — blocking |
| `regen --check` exit 0 | precondition of T12b; do not install the gate otherwise |
| `verification-before-completion` | before any completion claim |
| Helder | after T12 (confirm the regenerated BACKLOG reads correctly) and after T13b (authorship review — `CLAUDE.md § Authorship` requires human review of rules files) |
