# Spec Evolution — Nested folders + generated BACKLOG — Tasks

Plan: `plan.md` · Spec: `requirements.md`, `design.md` (approved 2026-07-22)

**Markers:** `[ ]` available · `[~]` claimed · `[x]` done · `[CANCELLED: reason]`

**All sequential — no `[P]`.** Every task after T2 consumes the previous task's contract or writes the same generated files; the file-overlap check forbids a wave.

**Lane split:** T1–T7 are code → **git worktree on a task branch** (HARD RULE). T8–T13 are docs/migration → **`develop`** (docs land on develop). Merge the T1–T7 worktree before starting T8, or the generator will not exist.

---

## Phase 1 — Generator (worktree, branch `feature/backlog-generator`)

- [ ] **T1 — Frontmatter parser**
  Produces: `parse(text) -> (dict, body)`, `FrontmatterError`. Files owned: `frontmatter.py`, `tests/test_frontmatter.py`. Risk: Low. Demo: 8 tests green.
- [ ] **T2 — Item model, validation, ordering**
  Consumes: T1. Produces: `Item`, `validate`, `order_items`, `notes_violations`, `STATUSES`, `TERMINAL`. Files owned: `model.py`, `tests/test_model.py`. Risk: Medium (ordering + the REQ-SEV-09 mechanical bound). Demo: 19 tests green.
- [ ] **T3 — Row/table rendering + fenced splice**
  Consumes: T2. Produces: `render_row`, `render_table`, `splice`, `render_backlog`, `RenderError`. Files owned: `render.py`, `tests/test_render.py`. Risk: Medium (byte-preservation outside fences). Demo: 11 tests green.
- [ ] **T4 — Monthly archive rendering**
  Consumes: T3. Produces: `bucket_by_month`, `render_archive`, `ARCHIVE_TEMPLATE`. Files owned: `render.py`, `tests/test_render.py`. Risk: Medium (independent sub-row archiving). Demo: 15 tests green.
- [ ] **T5 — CLI shell: `regen`, `--check`, `query`**
  Consumes: T1–T4. Produces: `walk`, `cmd_regen`, `query_lines`, `cmd_query`. Files owned: `backlog_gen.py`, `tests/test_backlog_gen.py`. Risk: High (idempotency is the core guarantee). Demo: `regen` twice → byte-identical.
- [ ] **T6 — `register` / `status` / `--renumber` + atomic `.sln` write**
  Consumes: T5. Produces: `next_bug_id`, `slugify`, `cmd_register`, `cmd_status`, `cmd_renumber`, `sln_add_entry`. Files owned: `backlog_gen.py`, `tests/test_backlog_gen.py`. Risk: High (ID allocation, atomicity). Demo: register a bug → folder + README + `.sln` line + regenerated row.
- [ ] **T7 — Hook wiring**
  Consumes: T5. Files owned: `orphan_check.py`, `.claude/githooks/pre-commit`, `tests/test_orphan_check_widening.py`. Risk: Medium (must not break the existing build gate or `orphan_check`'s fail-open posture). Demo: staged frontmatter with a stale BACKLOG → commit blocked.
- [ ] **T7b — Merge `feature/backlog-generator` → `develop`** (orchestrator; code review first)

## Phase 2 — Migration, additive (develop)

- [ ] **T8 — Freeze fixture + insert fences**
  Files owned: `BACKLOG.md` (fences only), `migration/BACKLOG-pre-migration.md`, `MyVocaList.sln`. Risk: Low. Demo: `git diff --stat` = 4 insertions, 0 deletions.
- [ ] **T9 — Feature READMEs (top-level rows)**
  Consumes: T8. Files owned: one `README.md` per top-level feature folder, `MyVocaList.sln`. Risk: Medium (verbatim transcription; Notes-bound overflow relocated, not rewritten). Demo: `regen --check` never exits 2.
- [ ] **T10 — READMEs for existing `bugs/`/`changes/` folders + the two separator rows**
  Consumes: T9. Files owned: item `README.md`s, `cross-cutting/README.md`, `MyVocaList.sln`. Risk: Medium. Demo: `regen --check` never exits 2.

> **⏸ HANDOFF SEAM after T10.** All work so far is additive — BACKLOG.md still reads exactly as before. Safe session end; resume at T11 from the task-log Checkpoint block.

## Phase 3 — Migration, destructive (develop)

- [ ] **T11 — Counter-example bugs get folders**
  Consumes: T10. Covers BUG-050/051/052, BUG-027/029/030/031/032 (back-link the prior task-log, delete nothing) and BUG-012 (`git mv` flat file → folder, `-01` day per REQ-SEV-00). Files owned: 9 new folders, `MyVocaList.sln`. Risk: Medium (`git mv` history must follow). Demo: `git log --follow` on BUG-012 shows pre-move commits.
- [ ] **T12 — Archives + equivalence gate**
  Consumes: T11. Files owned: 5 archive files, item folders for archived rows, `task-log.md`. Risk: **High — this is the gate.** Demo: every diff hunk vs the frozen fixture classified into REQ-SEV-25's four permitted classes; `regen --check` exit 0; `grep BUG-048` still hits an archive.

## Phase 4 — Rules

- [ ] **T13 — The `amend:` bundle**
  Consumes: T12. Files owned: `CLAUDE.md`, `.claude/rules/{workflow,bug-tracking}.md`, `.claude/library/{workflow-rule-1,workflow-rule-3,workflow-rules-6-7-8,bug-tracking-reference,spec-writing-guide,session-ops}.md`, `BACKLOG.md` header, `Docs/Changelog/changelog.md`. Risk: High (all must land together or the rules contradict each other). Demo: no file in `.claude/` still instructs reading BACKLOG.md.

---

## Gates

| Gate | When |
|------|------|
| Code review (fresh subagent) | after T7, before merging to develop |
| Equivalence gate — every diff hunk classified | T12 step 5 — blocking |
| `verification-before-completion` | before any completion claim |
| Helder | after T12 (confirm the regenerated BACKLOG reads correctly) and after T13 (authorship review of the amended rules files — CLAUDE.md § Authorship requires human review of rules files) |
