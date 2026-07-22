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
- [ ] **T9a — Feature READMEs: Business Features top-level rows** (~12 rows)
  Consumes: T8. Produces: one `README.md` per top-level Business Feature + their `order:` values.
  Files owned: those READMEs, `MyVocaList.sln`. Risk: Medium. Review lane: Standard. Demo: `regen --check` never exits 2.
- [ ] **T9b — Feature READMEs: Dev Cycle Craft top-level rows** (~18 rows)
  Consumes: T8 (independent of T9a in content, but serialized — both write `MyVocaList.sln`).
  Files owned: those READMEs, `MyVocaList.sln`. Risk: Medium. Review lane: Standard. Demo: same.
- [ ] **T9c — Folder-less rows → `cross-cutting/` folders**
  Consumes: T9a/T9b. Every row whose pointer is `cross-cutting-log.md` gets a folder that links back to the log (retained, never deleted — REQ-SEV-28).
  Files owned: `Docs/Management/cross-cutting/**`, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
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
