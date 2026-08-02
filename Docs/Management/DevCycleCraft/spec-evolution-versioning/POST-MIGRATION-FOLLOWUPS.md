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

> **Resolved 2026-08-02:** implemented Option 1, scoped to `task-log.md` files only, with fenced
> code blocks (` ``` `) stripped before the regex runs. `next_bug_id()` now additionally walks
> every feature/change `task-log.md` under `Docs/Management` and regexes its non-fenced content
> for `BUG-(\d{1,4})`, taking the max across all three sources (folders, archive files,
> task-logs). Regression tests added to `tests/test_backlog_gen.py`.
>
> A broader first attempt — scanning *every* `.md` under `Docs/Management`, not just
> `task-log.md` — was tried and rejected. Prose files (plans, write-ups, this very followups
> file) *discuss* bug ids without *recording* them, so scanning them creates a self-referential
> feedback loop: a resolution note that mentions a number becomes evidence the allocator reads
> back on its next run, and each retelling of the number ratchets the high-water mark upward
> again, indefinitely — a real defect (id-range abandonment), not conservatism. `task-log.md` is
> the only prose source where REQ-SEV-03 folder-less bugs are actually *recorded*, so it is the
> only one the allocator should read. Fenced code blocks are stripped for the same reason at
> finer grain: a task-log entry that quotes test/fixture output (an assertion against an
> illustrative example id) is not a record either.
>
> This paragraph intentionally names no BUG id above the real high-water mark, to avoid
> reintroducing the same feedback loop it describes.

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
