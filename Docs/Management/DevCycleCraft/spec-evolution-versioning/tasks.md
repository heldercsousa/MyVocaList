# Spec Evolution — Nested folders + generated BACKLOG — Tasks

Plan: `plan.md` · Spec: `requirements.md`, `design.md` (approved 2026-07-22)

**Markers:** `[ ]` available · `[~]` claimed · `[x]` done · `[CANCELLED: reason]`

---

## ✅ MIGRATION RESUMED — 6 decisions answered by Helder (2026-07-22)

| # | Blocker | Decision |
|---|---------|----------|
| 1 | Windows version row has no Goal | **Agent authors a one-line goal**, derived strictly from the row's existing Gate + Pointer text. `model.REQUIRED` is NOT relaxed. Marked *agent-authored, pending review* in the task-log. |
| 2 | Banned-content vs governance rows | **Agent authors compliant one-line goals** for the 1 blocked + 5 trimmed rows (orders 20, 100, 110, 150, 520). `model._BANNED` is NOT relaxed; no Craft exemption. Each goal marked *agent-authored, pending review*. |
| 3 | BUG-022 is `Minor` but has a folder | **(A) Reclassify to Major.** Folder stays; the severity value changes in the rendered row → declare as a T12 diff hunk. |
| 4 | BUG-026 / bug-043 have no valid parent | **(A) Give both parents frontmatter** — `cross-cutting/README.md` and `autocomplete-component/README.md`. The bugs are not re-homed; nesting is preserved. |
| 5 | BUG-019 free-text status + folder shared with live BUG-028 | **(A) Pick a valid `STATUSES` status and split the folder** so each row owns one. The rows are NOT merged. |
| 6 | Archive files have two tables, `ARCHIVE_TEMPLATE` has one flat region | **(A) Split into two regions** (`archive-business` / `archive-craft`). Needs a `render.py` change in a worktree + re-fencing the 5 archive files → new task **T9e**, which must land before T10a (T10a's bug READMEs route through `render_archive`). |

> **Blanket authorization (Helder, 2026-07-22):** proceed autonomously on anything that would
> otherwise need approval, using the recommended approach. **Two carve-outs it does not cover:**
> (a) **T13** — `CLAUDE.md § Authorship` requires a human to read any rules file before commit;
> prepare the `amend:` bundle and stop before committing. (b) Every **agent-authored goal** under
> decisions 1–2 is flagged for audit as a set, since no option existed that avoided authoring.

## ✅ T12a PLANNING — archive-regen decisions answered by Helder (2026-07-23)

Surfaced by the T12a inventory (Plan subagent, 105 archived rows enumerated). These **redefine the T12 gate** — recorded before any T12a folder work, per the SDD Invariant (spec before code).

| # | Blocker | Decision |
|---|---------|----------|
| F-2 | The renderer emits a **canonical** archive format (`Goal: … Pointer: \`…\`.`, drops the `↳` arrow, appends `(under: <parent>)`) that differs from the committed archive text on nearly every row → a clean byte-match against today's files is impossible. | **Canonical rewrite + idempotency gate.** T12 rewrites all 5 archive files ONCE to the generator's canonical format. The gate is redefined: **(1)** `regen --check` run twice yields zero diff (idempotency, REQ-SEV-13); **(2)** every archived `BUG-NNN` still grep-reachable (REQ-SEV-20/18); **(3)** the `↳`-drop, `Goal:`-prefix, and `(under:)`-suffix reformattings are **enumerated as a named archive-migration diff class** — NOT folded into REQ-SEV-25's four active-BACKLOG classes, which stay unwidened. |
| F-3 | Committed archive Status cells contain terminal states not in the 8 `STATUSES`: `🔵 Superseded (closed …)`, `🔵 Duplicate (closed)`, `Closed — partially regressed`. A README carrying them verbatim fails `validate()`. | **Extend `STATUSES`** with terminal `Superseded` and `Duplicate` states (`model.py` + `render.py` change, in the T12 wave, with tests). Preserves the real distinction (e.g. BUG-008 superseded by the DX AutoCompleteEdit replacement) rather than normalizing history to `✅ Fixed`. `Closed — partially regressed` (BUG-019) stays reconciled to `✅ Fixed` per resolved decision 5. |
| D | BUG-022, archived as "(Minor)", was given `severity: Major` in frontmatter to dodge REQ-SEV-03. | **Keep the Major re-classification** (confirmed). Only archived Minor bug; title still reads "(Minor)" so the rendered label matches. No broad precedent set. |

### Still open — proposed resolutions (agent-authored; flagged for the T12 gate audit)

> Under the blanket authorization these proceed on the recommended approach, but each is **flagged** because it authors content or shape not present in the archive text (the agent-authored-content carve-out).

- **F-1 (43 rows point at a LOG file, not a folder).** Two model families: **(a)** 21 `cross-cutting-log.md` rows → `Docs/Management/cross-cutting/<slug>/README.md` (REQ-SEV-28 already covers these; log retained + linked; `section: DevCycleCraft` set directly since `cross-cutting/` is not a section root). **(b)** ~22 non-bug sub-rows pointing at a shared feature `task-log.md` (Search-Picker sub-rows, form-validation 01–06, crud-dedup Steps 1–7e, rules-refactoring sub-tasks) have **no design model.** **Proposed:** each gets a `changes/<slug>/README.md` under its parent feature, `pointer:` kept on the shared `task-log.md` (nothing deleted, REQ-SEV-27); `id`/slug/`title` are agent-authored from the row label. **Flagged: 22 invented slugs + titles.**
- **F-4 (`(under:)` suffix).** Archived rows carry `parent:` → the suffix is emitted for every child row. **Proposed:** keep `parent:` (preserves the logical tree and matches the canonical-rewrite target); the suffix is part of the F-2 archive-migration diff class.
- **F-5 (pointer/ordering collisions).** 2026-03 "Venues CRUD" + "Venues MD3 rebuild" both point at `venues/`; Search-Picker sub-rows share one `task-log.md`. **Proposed:** author distinct `id` + `order` so rows neither merge nor reorder; reading order pinned to the frozen snapshot. **Flagged: `order` values are agent-assigned.**

### Superseded — the original blocker table (kept for the record)

## ⛔ MIGRATION PAUSED — 6 decisions needed from Helder (2026-07-22)

Phase 1 (generator) is **complete and merged**; 113 tests green. The additive migration ran through
T9d. **T10a is blocked** and everything after it depends on these. None is a generator bug — each is
a place where the existing BACKLOG predates a rule this feature encodes. This is what a migration is
supposed to surface.

| # | Blocker | Found by | Options (recommendation first) |
|---|---------|----------|-------------------------------|
| 1 | **Windows version** row has no Goal; `model.REQUIRED` mandates one | T9a | (A) supply a one-line goal · (B) relax `REQUIRED` — weakens REQ-SEV-09 for every future row |
| 2 | **Banned-content vs governance rows.** `model._BANNED` rejects file references, but for rows like *BACKLOG-first Registration Enforcement* the filename IS the subject. 1 row blocked, 5 trimmed (overflow moved verbatim to README bodies — those rows render SHORTER than today) | T9b | (A) compliant one-line goals · (B) exempt Dev Cycle Craft governance rows from the file-reference pattern · (C) accept trimming as permitted diff class (d) |
| 3 | **BUG-022 is `Minor` but has a folder**; `validate` errors *"severity 'Minor' must not have a folder (REQ-SEV-03)"*. The folder predates the rule | T10a | (A) reclassify to Major · (B) dissolve the folder · (C) exempt pre-existing folders |
| 4 | **Two bugs have no valid parent.** `BusinessFeatures/cross-cutting/` has no README; `autocomplete-component/README.md` exists but has NO frontmatter so `walk()` skips it. Falling back to `section:` would render them top-level — a structural change, not a transcription | T10a | (A) give both parents frontmatter · (B) re-home the bugs |
| 5 | **BUG-019** archive status is free text *"Closed — partially regressed"* (not in `model.STATUSES`), and live **BUG-028** points at the same folder — one folder cannot back two rows | T10a | (A) pick a valid status + split the folder · (B) merge the two rows |
| 6 | **Archive files have TWO tables** (`## Business Features`, `## Dev Cycle Craft`) but `ARCHIVE_TEMPLATE` defines ONE flat `archive` region. T9d's fences therefore enclose the `## Dev Cycle Craft` heading, which T12 would consume. Fencing only one table would make T12 silently DROP the other's rows | T9d | (A) split into two regions (`archive-business` / `archive-craft`) — a `render.py` change needing its own task before T12 · (B) accept a single merged archive table |

> **Consequence for T12:** the equivalence gate will NOT be a clean byte-match. Decisions 2 and 6
> change rendered content by design. That is a decision to take knowingly, not a diff class to wave
> through.

---

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
- [x] **T9c-2b — Folder-less Dev Cycle Craft rows, second half** (9 of 9 written, incl. the Autocomplete Mobile UX Pattern row)
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
- [x] **T9d — `archive` generated-region fences in the 5 monthly archive files** *(split out of T12 to fix the T10a sequencing defect recorded below)*
  Consumes: nothing. Produces: the `archive` fence pair in each of the 5 `Docs/Management/backlog-archive/` files, so `render_archive` → `splice` resolves instead of raising `RenderError`.
  Files owned: the 5 archive files, `tasks.md`, `task-log.md`. Risk: Low (purely additive — exactly +2/-0 lines per file, verified via `git diff --numstat` plus a per-file sha256 byte-preservation proof). Review lane: Standard.
  > Placement follows the T8 precedent in `BACKLOG.md`: BEGIN immediately above the table head, END immediately after the last row; the hand-written prose header stays outside. **Implementation decision:** each archive file has *two* tables (Business Features / Dev Cycle Craft) while `ARCHIVE_TEMPLATE` defines a single flat `archive` region, so the single fence pair spans from the first table head to the last row — the intervening `## Dev Cycle Craft` heading therefore sits inside the region and will be consumed by T12's regeneration.
- [x] **T9e — Split the flat `archive` region into `archive-business` / `archive-craft`** *(decision 6A; code → worktree on a task branch, HARD RULE)*
  Consumes: T9d. Produces: `render.py` renders two archive regions and `ARCHIVE_TEMPLATE` declares both; the 5 archive files carry two fence pairs instead of one, with the `## Dev Cycle Craft` heading OUTSIDE both regions (it is hand-written prose, and T12 must not consume it).
  Files owned: `.claude/scripts/backlog/render.py`, `.claude/scripts/backlog/tests/test_render.py`, the 5 archive files. Risk: **High (A)** — this is the only change to merged, green generator code. Review lane: **Elevated — fresh code-review subagent before merge.**
  Demo: full suite green (was 113); per-file sha256 byte-preservation proof that re-fencing changed only fence lines; `splice` resolves both regions in all 5 files; `regen --check` exits 0 or 1, never a `RenderError`.
  > Ordering: **before T10a**, not before T12. T10a's 6 archived bug READMEs route through `render_archive` → `splice`, so they must be verified against the final region names, or T10a's evidence is written against a layout that T9e then invalidates.

> **✅ T9e MERGED [2026-07-22] — Elevated review verdict: PASS WITH FINDINGS.** 125 tests green on
> develop. Independently re-verified by the reviewer: test count, **zero removed test lines**,
> byte preservation on all 5 archive files, headings outside every fence, idempotency sound
> (distinct region names ⇒ re-splice is a fixed point; a missing fence raises rather than appends).
> **All three findings are about claims made AROUND T9e, not the code in it.** Work owed:
>
> - **[ ] F1 (blocking T12) — the `regen --check` demo statements are invalid; correct them.**
>   `cmd_regen` does `if errors: return 2` **before** `outputs = _render_all(...)`. Exit 2
>   confirmed pre-existing at develop HEAD (banned-content on this feature's own folder), so T9e
>   did not break it — but it means **`regen --check` has never once executed `render_archive`**
>   during this migration and cannot until the validation error clears. *"`regen --check` exits 0"
>   is a validation gate that never reaches the renderer — it is NOT an archive equivalence gate.*
>   The in-process splice proofs are the ONLY evidence archive rendering is correct. T12 must either
>   clear the validation error first or invoke `_render_all`/`render_archive` directly. Leaving the
>   statement as written would let a green-looking gate certify equivalence it never tested.
> - **[x] F2 (done 2026-07-22, branch `feature/generator-fixes`) — `_render_all` now passes `all_items=items`.**
>   It currently calls `render_archive(existing, month_items, month, titles)`, so parent resolution
>   sees only that month's bucket. A bug whose parent closed in a different month (or is still open)
>   falls through to the folder prefix; `BusinessFeatures/`/`DevCycleCraft/` survive, but
>   **`cross-cutting/` is not in `ARCHIVE_SECTIONS` and raises** — and `Docs/Management/cross-cutting/`
>   is a real, populated directory. The first archived `cross-cutting` bug without an explicit
>   `section:` hard-fails `regen`. Currently masked by F1. Fail-loud itself is **upheld as correct**
>   (REQ-SEV-18: mis-filing shades into dropping, and a mis-filed row *looks* successful). Passing
>   the full pool costs nothing — `_render_all` already holds `items` — and leaves the raise for
>   genuinely unplaceable rows. Not doing it in T9e was procedurally correct (`backlog_gen.py` was
>   outside its `Files owned`; touching it would have been Rule 2 bundling).
> - **[x] F3 (done 2026-07-22, branch `feature/generator-fixes`) — `.gitattributes` pinned; NOT renormalized (deliberate — see task-log).**
>   All 5 archive files are CRLF in the worktree, LF in the blob; `core.autocrlf=true` and
>   `.gitattributes` pins only `*.sh`, `pre-commit`, `.claude/scripts/**/*.py`. (T9d's log recorded
>   LF because it measured the blob, not the working tree — the two logs measure different things
>   and should say which.) `_read`/`_write` round-trip working-tree bytes and `cmd_regen` decides
>   staleness with `current != text`, so on a differently-configured checkout `regen` can rewrite
>   line endings across whole files and report them stale for reasons unrelated to content —
>   **a whole-file diff that is 100% line endings is indistinguishable from one that is not**, which
>   makes T12's gate impossible to classify honestly. Pin `Docs/Management/backlog-archive/*.md`
>   and `BACKLOG.md` to `text eol=lf`.
>
> - **[RETRACTED 2026-07-22] F4 was WRONG — `_section_from_path` was never dead code.** `walk()`
>   sets `rel_dir = _rel(root, dirpath)`, and `_rel` is `os.path.relpath(path, join(root,
>   MANAGEMENT))` — i.e. relative *to* `Docs/Management`, so those segments are already stripped and
>   `parts[0]` **is** the section name. Live `walk()` output: `'BusinessFeatures/artists-songs/'` →
>   `BusinessFeatures`. The fallback has always fired. The `cross-cutting/` hard-fail F4 describes is
>   real but its sole cause is **F2**; the chain was two-deep-plus-a-raise, not one-deep.
>   **How the error was made:** the claim was reasoned from the function's apparent intent rather
>   than from running it, then repeated as established fact in three briefs (T10b, T11a, T11b) —
>   each of which wrote defensive `section:` keys partly on a false premise. Those keys are still
>   correct (F2 was real), so nothing shipped wrong, but the reasoning was not.
>   **Guard added:** `test_walk_produces_paths_the_folder_prefix_fallback_can_read` asserts the real
>   `walk()` output shape and `_section_from_path`'s result together, so a future change to `_rel`
>   fails loudly instead of silently deleting the third resolution step.
>   **⚠️ Audit the rest of this block the same way — by execution, not by reading.** F1's
>   `regen --check` claim is the next most load-bearing and has never been run by me directly.
>
> - **[superseded — original text of F4, kept for the record]** `render._section_from_path` is dead code in
>   production. `walk()` builds `rel_path` as `Docs/Management/…`, so the function tests
>   `parts[0] == "Docs"` and **always returns `None`**. The folder-prefix fallback therefore never
>   fires outside tests. Combined with F2 (`_render_all` omits `all_items`, so parent resolution
>   sees only the month's bucket, and archived bugs' parents are non-terminal ⇒ absent), an archived
>   item without an explicit `section:` hits `RenderError` **regardless of its folder**. T10a is
>   immune only because it wrote `section:` on every item — that is load-bearing, not belt-and-braces.
>   Fix `_section_from_path` and the `all_items` call site in the same task, or the fallback chain is
>   two-thirds fictional.
>
>   **F4 correction [2026-07-22]:** `walk()` builds `rel_path` via `_rel`, which is relative to
>   `Docs/Management`, so the first segment IS the section name (`BusinessFeatures/artists-songs/`).
>   `parts[0] == "Docs"` never happens and the fallback fires normally on real paths — verified
>   against live `walk()` output. No code change; a regression test now locks the path shape so a
>   future change to `_rel` fails loudly instead of silently deleting the third resolution step.
>   The `cross-cutting/` hard-fail F4 describes is real, but its cause is F2 alone, and F2 fixes it.
>
> - **[x] F6 (found by T11a; DONE in T11c 2026-07-22) — T10a's `BUG-028-artistspage-trailing-catalog-button-noop/` folder had no date prefix**, leaving two naming conventions in one `bugs/` directory. Renamed with `git mv` to `2026-07-03-BUG-028-artistspage-trailing-catalog-button-noop/` (date = the row's own `target: 2026-07-03`, not invented); its `pointer:` and both `.sln` entries were updated in the same commit. **Not in F6's scope and untouched:** the six *legacy* folders (BUG-017/018/019/021/023/024) still carry no date prefix.
>
> - **[ ] F5 (found by T10b; own task, T13-adjacent) — separators bypass every validation check.**
>   `model.validate` does `if it.is_separator: continue` **before** any field check, so a
>   `kind: milestone` / `kind: group` row can carry an invalid `target`, a bogus `severity` or a
>   stray `closed` and still validate clean. Separators are the one row class with **no mechanical
>   guard** — the exact inverse of REQ-SEV-09's intent, which is that the row template be
>   mechanically enforced rather than prose-enforced. Not acted on in T10b (`model.py` was outside
>   its `Files owned`; changing it would have been Rule 2 bundling).
>
> - **[x] F6 (found by T11a; DONE in T11c 2026-07-22 — see the resolved bullet above) — T10a's `BUG-028` folder violates the REQ-SEV-01 naming pattern.**
>   REQ-SEV-01 and `design.md` §2's own worked example mandate `YYYY-MM-DD-BUG-NNN-<slug>`;
>   T10a created `BUG-028-artistspage-trailing-catalog-button-noop/` with **no date prefix**, so one
>   `bugs/` directory now contains both spellings. T11a followed the spec for its own three folders
>   and flagged rather than touching T10a's file. **Fix before T12** — `register` derives the folder
>   name mechanically (REQ-SEV-11), so a hand-made folder that departs from the pattern is exactly
>   the drift the generator exists to prevent. A `git mv` (history follows) + `id:`/pointer update.
>
> ### ⚠️ T12 GATE HAZARDS — found by execution-audit 2026-07-22, NOT previously recorded
>
> **H1 (biggest classification hazard) — mid-migration, `regen` DELETES most archive rows.** With the
> validation error cleared, rendered output keeps **3 of ~30** rows for `BACKLOG-ARCHIVE-2026-06.md`,
> **3 of 7** for `2026-07`, and drops **18** from `BACKLOG.md`. Expected — those legacy rows have no
> backing item folder until **T12a** completes — but it means **T12 cannot use "regen produces no
> diff" as its gate** until the migration is finished. Running the gate before T12a would read as
> catastrophic data loss.
>
> **H2 — archived rows drop their `↳` by design, and every committed archive row has one.**
> `render_row` has an explicit `if archived: label = item.title` branch (docstring: preserving arrows
> "would break the byte-identical round-trip (REQ-SEV-13)"), yet all current archive rows are spelled
> `| … | ↳ BUG-015: … |`. Regen will therefore strip a `↳` from **every archived row** — a
> systematic, expected content diff that **must be named as a permitted class** alongside item (v),
> or it will read as data loss at the gate.
>
> **H3 — F3's pin is INERT for existing working trees.** `eol=lf` applies on **checkout**, so it does
> nothing for files already on disk. `core.autocrlf=true` is set locally and the working tree is
> **100% CRLF today** while the blobs are pure LF. Until a post-migration renormalization *or* a
> forced re-checkout (`git rm --cached -r Docs/Management && git checkout -- Docs/Management`) runs,
> **every diff T12 measures is contaminated by line endings** — the exact condition F3 exists to
> eliminate. The F2/F3/F4 commit message's framing ("bites on a *differently-configured* checkout")
> is wrong: it bites here, now.
>
> **H4 (found T12a Wave A, 2026-07-23) — `regen` may SKIP a file with committed rows but zero backing items.** After Wave A, `regen --check` flagged 2026-03/06/07 as stale but **NOT 2026-04/05** — the two months with committed rows but zero migrated folders. A month rendering to empty apparently leaves its file untouched rather than emptying the fenced region, so its old rows survive spuriously and the gate would falsely pass. **Self-resolves once every row has a folder** (04/05 get theirs in Waves B/C). But T12 MUST assert every archive file was actually rewritten — e.g. confirm each of the 5 files appears in a `regen` write pass, not just that `regen --check` is clean — or a month accidentally left un-migrated would pass silently. Verify at the gate.
>
> ### Corrections to this block, from the same audit
>
> - **F5 — conclusion CONFIRMED, mechanism WRONG.** A maximally malformed separator (`target:
>   BANANA-99`, `severity: Catastrophic`, `closed: NOT-A-MONTH`, `status: not-a-status`) validates
>   clean, so the hole is real. But `continue` does **not** come "before any field check": three
>   checks run above it (required-keys incl. `target` presence, `section in SECTIONS`, and the
>   resolves-to-no-section check). **A fix written against F5's stated mechanism would target the
>   wrong line** — the guard point is the `continue`'s position relative to the *format* checks.
> - **Item (vi) — UNDERCOUNTED: 10 unprefixed bug folders, not 6.** `git ls-files` on develop shows
>   **0 of 10 comply** with REQ-SEV-01. Missed: `BUG-026`, `BUG-022`, and **`bug-043`** — the last a
>   second, distinct violation (lowercase `bug-`, no date, no slug) that no finding mentions. The
>   REQ-SEV-01 debt is ~67% larger than recorded.
> - **Item (iv) — the audit called it false; the audit was wrong, on a branch artifact.** It
>   enumerated on `develop`, where the migration branch has not merged. Verified directly:
>   develop still has `BUG-028-…`, `feature/backlog-migration` has `2026-07-03-BUG-028-…`. **F6 did
>   land.** (Lesson recorded because it is the same class of error as F4: enumerate on the branch
>   that holds the work.)
> - **F1 — CONFIRMED in full by execution**, including its cause and its corollary: exit 2 precedes
>   `_render_all`; the cause is the banned-content error on this feature's own folder; clearing only
>   that error lets `render_archive` run (`months: ['2026-06','2026-07']`, exit 1). T12's gate design
>   rests on solid ground.
> - **Item (v) — CONFIRMED.** `_depth` counts `bugs`/`changes` segments only; `parent` changes
>   nothing. BUG-012 necessarily gains a `↳`.
>
> > **Methodological pattern worth keeping:** of five claims re-verified by execution, three were
> > fully correct (F1, F2, item v), one correct-in-conclusion but wrong-in-mechanism (F5), one
> > materially undercounted (item vi), and F4 was flatly wrong. **The conclusions have held; the
> > stated mechanisms and enumerations are where the errors live.** Verify mechanism claims by
> > running the code, and enumerations with `git ls-files` on the correct branch.
>
> ### Open for Helder at T12 (accumulating — audit as a set, not one at a time)
>
> **(i) Agent-authored `Goal:` sentences** (decision-1/2 class; no option existed that avoided
> authoring). So far: **Windows version** row; **BUG-029, BUG-030, BUG-031/032** (their rows carry
> no Goal at all — Notes open `Deferred:` / `Answered by Helder 2026-07-10:` — and
> `model.REQUIRED` mandates one). Plus the decision-2 rows (orders 20, 100, 110, 150, 520 + the
> blocked BACKLOG-first Registration Enforcement row). **Every one is text Helder would normally
> write.**
>
> **(ii) Forced respellings — four so far, all from ONE pattern.** `model._BANNED`'s test-count
> regex `\b\d+\s*/\s*\d+\b` has forced: `Mask="00/00"` (T10a), `BUG-050/051/052` (T10b),
> `after 050/051` (T11a), and would have hit `BUG-031/032` except that `notes_violations` scans
> only `goal`+`gate`, not `title`. **Four incidents from one rule is a signal the pattern is too
> broad, not that the content was wrong** — decide at T12 whether to narrow it (e.g. require a
> `green`/`tests`/`passed` context word) rather than keep rewording real content around it.
>
> **(iii) Two bugs carry no `severity:`** — BUG-030 and BUG-031/032 are tagged `(spec gap)`, not
> Critical/Major/Minor. Left unset rather than invented. This is a literal edge of REQ-SEV-01
> ("every Critical or Major bug … lives at …"): they are neither, yet own folders because every
> live row needs one. Observation, not a blocker.
>
> **(iv) BUG-028 folder name** — fixed by F6 (T11c).
>
> **(v) BUG-012 gains a `↳` — recommended as permitted diff class (d).** The row is top-level today
> because no *Venues* row exists, but `model._depth` derives the arrow from the **path**, and
> REQ-SEV-01 forces the bug into `venues/bugs/`. **No frontmatter value suppresses it** — `parent`
> is unset and irrelevant. Every alternative is larger than T11c: add a Venues row (forbidden by
> REQ-SEV-25), stay flat (defeats T11c), or make `_depth` respect `parent` (generator change).
> Table position is unchanged; only the indent differs.
>
> **(vi) Six legacy bug folders still violate REQ-SEV-01 naming.** F6 fixed BUG-028, but
> **BUG-017/018/019/021/023/024 also have no date prefix** (enumerated via `git ls-files`; each is
> `pointer:`-referenced by an archived row). The F6 brief's premise — that renaming BUG-028 would
> leave one convention in that directory — was wrong. Out of scope for T11c, untouched, recorded
> as remaining REQ-SEV-01 debt for T12/T13.
>
> **(vii) `severity` unset on BUG-012** — its legacy file says "Medium", which is not in
> `model.SEVERITIES`, and its row states none. Guessing `Minor` would trip REQ-SEV-03 and block a
> task the spec schedules; guessing `Major` would be fabrication. Left unset, like (iii).
>
> **Environment hazard found during the review — do not repeat:** `grep` is rewritten by the `rtk`
> proxy into a search tool, which silently corrupted the reviewer's first fence-stripping run and
> produced bogus "DIFFERS" output. **Never use `grep` for byte-exact work in this repo** — use
> Python.

> **⏸ SESSION HALT [2026-07-22] — superseded by the merge above.** T9e was **committed (`407aa3d`) and
> pushed** to `feature/archive-regions` (worktree `../mvl-archive-regions`), status **To Review**,
> **NOT merged**. Its Elevated code review was dispatched and **died mid-run on an API session
> limit (resets 11:40am America/Sao_Paulo) having produced zero findings** — so T9e is unreviewed,
> not reviewed-clean. **Resume by re-dispatching the T9e review**, then merge, then T10a.
>
> Three T9e items awaiting that review's adjudication:
> 1. **The 5 archive files are CRLF in the working tree, not LF.** `.gitattributes` pins only
>    `*.sh`, `pre-commit`, `.claude/scripts/**/*.py`; `.md` is unpinned and `core.autocrlf=true`.
>    T9d's log recorded LF for the same files — the two worktrees checked out differently. Hazard
>    for a generator whose guarantee is byte-identity. **→ queued for T13:** pin
>    `Docs/Management/backlog-archive/*.md` (or `*.md`) in `.gitattributes`.
> 2. **`regen --check` exits 2, claimed pre-existing** (proven at develop HEAD by stashing —
>    unverified independently). Corollary: **exit 2 aborts before `_render_all`, so `regen --check`
>    never exercises `render_archive`** — the in-process splice proofs are load-bearing.
>    **→ T9e's and T12's demo statements asking for `regen --check` exit 0/1 as the archive gate are
>    wrong and must be corrected** once the pre-existing claim is confirmed. Exit 2 is the
>    decision-2 banned-content class and may clear when those goals are authored.
> 3. **`render_archive` gained an `all_items` param** (default `items`) resolving section via own
>    `section:` → parent chain → folder prefix → **`RenderError`**. Beyond a pure region split.
>    Failing loud is argued from REQ-SEV-18 (mis-filing shades into dropping, losing a `BUG-NNN`
>    from the only grep-reachable file). Review must decide (a) fail-loud vs default-region, (b)
>    whether `_render_all` can hit the raise for a `cross-cutting/` item with no `section:`,
>    (c) whether `_render_all` should pass the full pool instead.

- [x] **T10a — READMEs for existing `bugs/` folders** — **9 of 9 written**, plus `BUG-028`'s new folder (decision 5A), `BusinessFeatures/cross-cutting/README.md` (decision 4A) and frontmatter prepended to `autocomplete-component/README.md` (decision 4A) — 12 items total; see task-log T10a (the superseding entry at the end of the file)
  > **Correction to the count below:** the split is **8 archived / 1 live**, not 6 archived — BUG-022 and bug-043 are archived too, in the **Dev Cycle Craft** archive table, and both carry an explicit `section: DevCycleCraft` for that reason. All 8 verified routing to the correct T9e region in-process.
  > **Owed to T10b:** do NOT create a second `cross-cutting` group README — T10a already wrote `Docs/Management/BusinessFeatures/cross-cutting/README.md` (`kind: group`, id `cross-cutting`); a duplicate id is a validation error. `Docs/Management/cross-cutting/` is a different thing and needs no group README.
  > **Owed to T12:** two declared diff hunks — BUG-022's severity reclassification (`Minor` → `Major`, decision 3A) and the rewording of its Notes (`Mask="00/00"` trips `model._BANNED`'s test-count pattern; the literal is preserved in the README body). Helder confirms the rewording at T12.
  Consumes: T9c. Files owned: those READMEs, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
  > **Sequencing defect [2026-07-22]:** 9 bug folders exist (not ~12; `ls` under `bugs/` misreports —
  > enumerate with `git ls-files`). **6 back archived (`✅ Fixed`) rows**, so their READMEs route
  > through `bucket_by_month` → `render_archive` → `splice`, and the 5 archive files have **no
  > `archive` fence** — those fences are **T12's** owned work. Proven: writing them makes
  > `regen --check` raise an uncaught `RenderError`, not exit 2. **T10a must run after the archive
  > fences are inserted.** Recommend splitting fence-insertion out of T12 into a small predecessor
  > (it also unblocks T12a). The other 3 folders are blocked independently: BUG-022 is `Minor`
  > (`model.py` forbids a folder), BUG-026 and bug-043 have no parent item, BUG-019 has a free-text
  > status and is claimed by both the archive row and live BUG-028.
- [x] **T10b — READMEs for existing `changes/` folders + the two separator rows** (3 change READMEs + the `🏁 MVP release` milestone; the `Cross-cutting` group README was NOT re-created — T10a already owns it, see task-log T10b)
  Consumes: T10a. Produces: item READMEs, `cross-cutting/README.md` (`kind: group`), `milestones/2026-06-mvp-release/README.md` (`kind: milestone`).
  Files owned: those files, `MyVocaList.sln`. Risk: Medium. Review lane: Standard. Demo: `regen --check` never exits 2.

> **⏸ HANDOFF SEAM after T10b.** All work so far is additive — BACKLOG.md's rendered rows are untouched (the fences still wrap the original hand-written table; nothing regenerates until T12). Safe session end; resume at T11a from the task-log Checkpoint block.

## Phase 3 — Migration, destructive (develop)

- [x] **T11a — BUG-050/051/052 get folders** (3 folders + READMEs written; pointer relocation + BUG-052's `050/051` respelling declared as T12 hunks — see task-log T11a)
  Consumes: T10b. Each back-links `DevCycleCraft/autocomplete-component/changes/2026-07-19-dx-autocompleteedit-replacement/task-log.md`; nothing is deleted from it (REQ-SEV-27).
  Files owned: 3 folders, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [x] **T11b — BUG-027/029/030/031/032 get folders** (4 folders + READMEs written — **four rows, not five**: BUG-031/032 is a single BACKLOG row; pointer relocation, three agent-authored `Goal:` sentences and BUG-029's `Deferred:`→`Gate:` relabelling declared as T12 hunks — see task-log T11b)
  Consumes: T11a. Each back-links `BusinessFeatures/artists-songs/task-log.md`; preserve each row's `🔵 Deferred` status and its deferral reason as `gate:`.
  Files owned: 5 folders, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [x] **T11c — BUG-012 flat file → folder** (moved to `venues/bugs/2026-03-01-BUG-012-venuesviewmodel-fetch-slow/`; **F6 done in the same task** — T10a's `BUG-028-…` folder renamed to `2026-07-03-BUG-028-…`; two T12 hunks declared — see task-log T11c)
  Consumes: T11b. `git mv` so history follows; `-01` day per REQ-SEV-00.
  Files owned: 1 folder, `MyVocaList.sln`. Risk: Medium (`git mv` history). Review lane: Standard. Demo: `git log --follow` shows pre-move commits.
- [x] **T12-pre — Extend STATUSES for terminal Superseded / Duplicate** *(added 2026-07-23, F-3; code change, must precede T12a's superseded-bug folders)* — DONE `e7b29a5`, 143 tests green (+14); TERMINAL is full-string, suffix reconstructed archive-only from `closed:`.
  Consumes: T11c. Produces: `model.STATUSES`/`TERMINAL` gain `🔵 Superseded`, `🔵 Duplicate` (terminal, keyed on full string not emoji — `🔵 Deferred` stays active); `render.py` reconstructs the archive Status cell's `(closed <month>)` suffix from the `closed:` key. Unit tests for: Superseded/Duplicate validate + archive-route, Deferred stays live, suffix reconstruction, and the emoji-collision guard.
  Files owned: `.claude/scripts/backlog/model.py`, `.claude/scripts/backlog/render.py`, `.claude/scripts/backlog/tests/`. Risk: Medium (generator core). Review lane: Standard. Demo: a README `status: 🔵 Superseded` + `closed: 2026-07` renders `🔵 Superseded (closed 2026-07)` and routes to the archive; a `🔵 Deferred` row stays in live BACKLOG.
- [x] **T12a — Archived rows → item folders** — **COMPLETE** (all month-waves A–U landed; final Wave U = BUG-045/047, see `task-log.md` § "Wave U (final) — T12a COMPLETE"; merged into develop 2026-07-26 with T12/T12b). *(Checkbox was left at `[~]` with a stale "Waves B–G pending" note; corrected 2026-07-30 after verifying the task-log completion entry — T12 and T12b, which consume T12a, were already `[x]` and merged.)*
  Consumes: T12-pre. One folder per row in the 5 archive files, `closed:` from the file name's month. **105 rows enumerated** (see T12a PLANNING); pin the 43 log-pointer partition exactly (REQ-SEV-28/28a) + grep-verify no stray bug sub-row. Split into per-month sub-waves of ≤5 folders; `MyVocaList.sln` is sequential-only so serialize the `.sln` commit at each wave boundary. Feature/activity rows → new README in the existing folder; archived bugs → new `bugs/YYYY-MM-DD-BUG-NNN-slug/` (git mv where a flat file exists); log-pointer rows → REQ-SEV-28/28a model. Agent-authored goals/slugs/titles/order flagged in task-log per the carve-out.
  Files owned: those folders, `MyVocaList.sln`. Risk: Medium. Review lane: Standard.
- [x] **T12 — Archive regeneration + the equivalence gate** *(fence **insertion** is no longer T12's — it moved to T9d, done 2026-07-22; T12 now only regenerates the already-fenced regions and runs the gate)* — DONE `75a6dc9` (addendum `64ce6da`), G1/G2/G3 all PASS.
  Consumes: T12a. Files owned: 5 archive files, `task-log.md`. Risk: **High — this is the gate.** Review lane: **Architectural (Helder).**
  Demo (redefined gate, F-2): **G1** `regen --check` run twice → exit 0 (idempotency, after validation clears — T9e-F1 precondition); **G2** every frozen-snapshot `BUG-NNN` still grep-reachable in an archive; **G3** every archive diff line vs the pre-rewrite files matches one of the three transforms (↳-drop / `Goal:`-canonicalize / `(under:)`-suffix) — any unmatched line is a defect. Live-BACKLOG diff still uses REQ-SEV-25's four classes; archives do NOT.
  > **Spec updated 2026-07-25:** REQ-SEV-32 amended before this run — the 3 external-pointer archived rows (folder-less per the earlier reading) got minimal `cross-cutting/<slug>/README.md` folders first (addendum commit `64ce6da`), since a folder-less row is silently dropped by the canonical rewrite (hazard H1; REQ-SEV-27 wins). G3's third transform is broadened in practice to include archive-migration row insertion (the 3 restored rows) and the Superseded/Duplicate `(closed …)` suffix — both already covered by T12-pre's status model — no diff line fell outside a named class.
- [x] **T12b — Install the blocking pre-commit gate**
  Consumes: T12 (precondition: `regen --check` exits 0). Produces: the R-2 gate.
  Files owned: `.claude/githooks/pre-commit`. Risk: Medium. Review lane: Standard. Demo: a deliberately stale BACKLOG is rejected; a clean tree commits.

## Phase 4 — Rules

- [x] **T13a — Amend the routing tables**
  Consumes: T12b. Files owned: `CLAUDE.md`, `.claude/rules/workflow.md`, `.claude/rules/bug-tracking.md`, `BACKLOG.md` header banner. Risk: High. Review lane: **Architectural (Helder).**
- [x] **T13b — Amend the library section files**
  Consumes: T13a. Files owned: `.claude/library/{workflow-rule-1,workflow-rule-3,workflow-rules-6-7-8,bug-tracking-reference,spec-writing-guide,session-ops}.md`. Risk: High. Review lane: **Architectural (Helder).**
  > T13a and T13b must land in **one `amend:` commit** — split for sizing/review only, committed together, or the routing tables contradict the library for the duration.
- [x] **T13d — Write-ownership & concurrency protocol for generated artifacts** *(authorized by Helder 2026-07-22; retires the exception-registry row of the same date)*
  Consumes: T13b. Files owned: `.claude/rules/workflow.md`, `.claude/library/workflow-rule-2.md`, `.claude/exception-registry.md`. Risk: High. Review lane: **Architectural (Helder) — rules file, `CLAUDE.md § Authorship` applies.**
  > **Why:** "Docs land on develop" was written when every doc was hand-edited by one agent at a time. It cannot cover a **generated** artifact, where a concurrent edit is not a mergeable line conflict but a silent overwrite on the next `regen` — nor two live sessions in one repo, which is now the normal case (this migration + INLINE-AC, 2026-07-22). Detection exists (T12b's pre-commit gate rejects a stale BACKLOG) but **detection is not a protocol**: nothing tells an agent what to do when the gate fires, or how two sessions coordinate ownership of a generated region.
  > **Must answer, at minimum:**
  > 1. Which artifacts are *generated* (single-writer, regenerate-don't-edit) vs *hand-written* (mergeable, land on develop). The split this exception used — generated → worktree, `task-log`/`tasks`/`LEDGER`/changelog → develop — is the starting proposal, not the conclusion.
  > 2. What an agent does when the pre-commit gate rejects a stale BACKLOG: regenerate and retry, or stop and escalate? (Regenerating blindly can discard another session's un-regenerated edit.)
  > 3. How a second live session learns a generated region is owned — LEDGER row, lease (`.claude/scripts/lease/`), or the `.itf-active`-style marker already used by the ITF lane.
  > 4. Whether `register`/`status` (which write folder + README + `.sln` + regenerate) are safe to run from two sessions at all, given T7b's finding that `register` is **not** atomic (that claim was retracted; the test that "proved" it was tautological).
  > Demo: the exception-registry row dated 2026-07-22 is **deleted**, not renewed, and the protocol covers the case it described.

- [x] **T13c — Changelog + contradiction sweep**
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
