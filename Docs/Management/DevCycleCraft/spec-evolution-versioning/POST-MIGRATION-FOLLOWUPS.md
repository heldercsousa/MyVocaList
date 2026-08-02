# Post-Migration Follow-ups (SPEC-EVO)

Durable reminders for issues deliberately deferred during the T12a folder migration
(Helder 2026-07-25: "leave the folder-collision issue, record a reminder, tackle later").
None of these block T12; they are cleanups to schedule after the migration branch merges.

## FUP-1 — Folder-collision semantic inversion (deferred by Helder 2026-07-25)

**What:** Several archived *Done* rows targeted a feature folder whose `README.md` was already
owned by a **different, still-live** item (`🟡 In Progress` / Pending). To avoid overwriting the
live item's README, the archived Done row was filed as a disambiguated `changes/<slug>/` sub-item
with `pointer:` kept on the existing `task-log.md` (REQ-SEV-27, nothing deleted).

**Cases (all committed on `feature/backlog-migration`):**
- `queue-management/` — live README = "Queue Entry Point Redesign"; archived core "Queue Management"
  filed as `changes/…queue-management-core-product/` (Wave J).
- `backup-restore/` — live README = "Backup Tier 2"; archived "Data Backup & Restore Tier 1+3"
  filed as `changes/…backup-restore-tier1-3/` (Wave J).
- `ui-form-validation-guide/` — live README = in-progress guide; archived "Form validation guide
  shipped" filed as `changes/2026-06-30-form-validation-guide-shipped/` (Wave N).

**Why it's a smell:** the *core/original* feature ends up nested as a `changes/` sub-item **under**
a README describing a later *redesign* of it — semantically inverted (the sub-item is arguably the
parent). It validates cleanly and loses no data, but the hierarchy reads backwards.

**Options to weigh later (not decided):**
1. Leave as-is (accept the inversion; it's only a doc-tree shape, validates fine).
2. Swap: make the archived core the folder `README.md`, refile the live redesign as the `changes/`
   sub-item (touches a live item's frontmatter — do under that item's own task, not here).
3. Introduce a distinct `history/` or `origin` marker so core-vs-redesign is explicit.

**Trigger:** revisit when the three live items above reach closeout, or during the
Spec-Evolution feature's own retrospective — whichever comes first.

## FUP-2 — bug-043 REQ-SEV-01 naming debt (pre-existing, tracked in tasks.md item vi)

`autocomplete-component/bugs/bug-043/` is lowercase, undated, unslugged (pre-scheme). Out of scope
for T12a; only a future `git mv` fixes the name. Recorded here so it isn't lost.

## FUP-3 — `.sln` GUID counter drift in constraints-registry.md (fix under T13)

`constraints-registry.md` still says last-used `.sln` GUID = `0041`; actual high-water after T12a
is `00D0`. Correct it in the T13 rules bundle.

## FUP-4 — `next_bug_id()` can reissue an already-used BUG id (found 2026-07-30, resolved 2026-08-02)

> **Resolved 2026-08-02:** implemented Option 1. `next_bug_id()` now additionally walks every
> `.md` file under `Docs/Management` (not just item folders and archive files) and regexes its
> content for `BUG-(\d{1,4})`, taking the max across all three sources. Folder-less bug ids
> recorded only in a `task-log.md` (e.g. BUG-065/066/067) now raise the high-water mark correctly.
> Regression tests added to `tests/test_backlog_gen.py`. Note: because the scan reads file
> *content* rather than only structured ids, it also picks up illustrative `BUG-NNN` ids inside
> code examples/fixtures in spec prose (e.g. `BUG-999` in this feature's own `plan.md`), which
> pushes the real-tree high-water mark higher than the lowest safe value (`BUG-1000` instead of a
> tighter `BUG-068`). This is a false positive but a safe-direction one — it never causes reissue,
> only over-caution — so it was left as-is rather than narrowing the regex.

**What:** `backlog_gen.py next_bug_id()` computes the next id from **item folders + archive files
only**. Bugs that were tracked in a feature/change `task-log.md` without ever getting a folder are
invisible to it. Today BUG-053…BUG-064 are exactly that (all found during the INLINE-AC cycle), so
the generator reports the next id as **BUG-053** — an id already in use.

**How it surfaced:** attempting `backlog_gen.py register --id BUG-067 …` for a defect from the
INLINE-AC on-device re-run #4; the assertion refused with *"expected id BUG-067 but the tree says
BUG-053"*. The `--id` assertion did its job — but without it the command would have **silently
created a colliding `BUG-053` folder**. The three new defects were therefore recorded in
`artists-songs/changes/2026-07-21-inline-artist-create/task-log.md` instead (BUG-065/066/067),
matching the precedent of the ids around them.

**Why it matters:** ids are the join key between commit subjects, BACKLOG/archive rows and
task-logs. A reissued id makes `git log --grep BUG-053` and every archive lookup ambiguous, and it
fails *silently* whenever a caller omits `--id`.

**Options to weigh (not decided):**
1. Widen `next_bug_id()` to also scan `task-log.md` files (and any `.md` under `Docs/Management`)
   for `BUG-(\d{3})` — cheap, makes the allocator authoritative over everything grep-reachable.
2. Keep a single explicit high-water file (e.g. `Docs/Management/.bug-id-high-water`) that
   `register` bumps — deterministic, but drifts if anyone numbers a bug by hand.
3. Backfill folders for BUG-053…064 — largest change, and REQ-SEV-03 deliberately keeps Minor bugs
   folder-less, so the underlying gap (folder-less ids exist by design) would remain.

Option 1 addresses the root cause: as long as folder-less bug ids are legitimate, the allocator
must not derive the high-water mark from folders alone.

**Trigger:** before the next `register` call for a bug, or with the T13 bundle — whichever first.
