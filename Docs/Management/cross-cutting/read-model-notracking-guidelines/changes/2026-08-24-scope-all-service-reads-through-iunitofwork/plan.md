# Execution Plan — Scope all service reads through `IUnitOfWork`

Item `READ-SCOPE` · Spec `./requirements.md` (REQ-UOW-36…52) · Design `./design.md` · Tasks `./tasks.md`
Parent: `../2026-08-03-dbcontext-lifetime-unit-of-work-pattern-maui-has-no-per-page-scope/`
Defect: `../../bugs/2026-08-24-BUG-078-service-read-paths-still-use-the-captive-appdbcontext-one-of-them-tracks/` (Major)

**Status at plan time:** spec approved by Helder 2026-08-25 with one amendment (REQ-UOW-51/52,
search-length thresholds; `design.md` D8 + § 2c). No code written, no branch, no worktree.

---

## 1. What this plan is executing, in one paragraph

Every service **write** already goes through `IUnitOfWork.ExecuteAsync`; every service **read** still
runs on the app-lifetime captive `AppDbContext`, because `AppDbContext` is Scoped and MAUI creates one
DI scope per *Window*, not per page. That produces BUG-078 (a stale delete-confirmation), keeps
`DbLoadGate` justified, and blocks parent Phase 4.7. This plan wraps every service read in
`ExecuteReadAsync`, removes the single `.AsTracking()` read, makes the rule permanent with an
architecture test, and only then removes `DbLoadGate`. **The rationale is `DbContext` lifetime — not
tracking, not transactions** (`design.md § 2`); do not re-derive it.

## 2. Setup — before the first dispatch

| # | Step | Notes |
|---|---|---|
| S1 | `git worktree add <path> -b <task-branch> develop` | Rule 2 HARD RULE — all code work is in a worktree |
| S2 | Verify `git merge-base --is-ancestor develop HEAD` | The native `EnterWorktree` may default to `main`. If develop is not an ancestor, **recreate** |
| S3 | Confirm baseline green: `dotnet build` (0 errors) + `dotnet test` | A pre-existing red test must be known *before* Wave 1, or the Red evidence is unreadable |
| S4 | `backlog_gen.py status READ-SCOPE "🟡"` + LEDGER row update | Never hand-edit a generated row |
| S5 | Open the `## Checkpoint` block in `task-log.md` | Rule 5 — write-ahead pings, ≤ ~10 min apart |

## 3. Dispatch sequence

Wave cap 4 (Rule 2). Ten dispatch rounds, twelve tasks. Each subagent is fresh, briefed with its task entry from `tasks.md` plus the
named spec sections; its context is discarded after it completes.

| Order | Dispatch | Parallel? | Gate that must be green before starting |
|---|---|---|---|
| 1 | **0.1** constants | single | S3 baseline green |
| 2 | **1.1** BUG-078 Red | single | 0.1 committed; `.AsTracking()` **still present** |
| 3 | **2.1** BUG-078 Green | single | **1.1's FAIL output pasted into `task-log.md`** |
| 4 | **3.1** remove `.AsTracking()` + **2** comments | single | 2.1's PASS output pasted |
| 5 | **4.1–4.4** | **4 parallel** | 3.1 committed |
| 6 | **4.5–4.6** | 2 parallel | sub-wave 4a (4.1–4.4) complete; 4.6 additionally needs 2.1 committed |
| 7 | **5.1** `ArtistSuggestionService` · **5.2** `SongSuggestionService` | 2 parallel | Wave 4 complete |
| 8 | **6.1** concurrency · **7.1** architecture test | 2 parallel | Wave 5 committed (7.1 fails before it) |
| 9 | **7.2** census-wide walk | single | 6.1 + 7.1 committed |
| 10 | **8.1** `DbLoadGate` removal | single | **everything above**, and **7.2's walk clean** |

**Between every wave:** verify build + full test suite, check the boxes in `tasks.md`, commit
(`/sln-commit`), and update the Checkpoint block. Discard the subagent instance.

## 4. The three gates that can invalidate this work

1. **The Red gate (Waves 1→2→3).** BUG-078's staleness exists *only* while
   `ArtistRepository.cs:79-80` calls `.AsTracking()`. Removing it first makes the regression test pass
   against unfixed code — a test that has never failed proves nothing, and `bug-tracking.md` makes
   fail-before/pass-after **mandatory** for a Major fix. The wave order therefore inverts DRY Onion
   **on purpose** (D6). It is safe because Waves 1–2 consume no new Infra type and Wave 3 only deletes
   a call. **Any reordering here invalidates REQ-UOW-45's evidence and the task-log entry is rejected.**
2. **The lambda gate (R1 — the highest-value review item).** An implementor writing `_repository.X()`
   *inside* an `ExecuteReadAsync` lambda produces code that compiles, passes every test, and leaves the
   defect fully intact while looking fixed. **Three** defences, all required: a per-file Python walk in
   each Wave 4/5 task, **task 7.2's census-wide walk over all of `Services/*.cs`** (this is REQ-UOW-36/37's
   mandated evidence and the "limb (a)" artifact Wave 8 depends on — nothing else produces it), and
   REQ-UOW-50's architecture test as the permanent gate for every service added later.
3. **The gate-removal gate (R3/R4).** `DbLoadGate` may only be removed once *every* read is scoped, and
   the `Task.Run` offloads in `LoadFirstPageAsync`/`LoadMoreAsync` must survive it — they share a
   comment block with the gate, and deleting them regresses `page-load-frozen`. Two `Assert.NotSame`
   off-context assertions (`CrudListViewModelBaseTests.cs:37`, `:75`) cover this — **two**, not three.

## 5. Evidence discipline

- **`grep`/`rg` results are not admissible.** The wrapper in this environment returned false zeroes
  twice during spec authoring, and a shell `diff` reported a confident false difference after silently
  failing on a path error. Every exhaustive, "zero occurrences", or file-comparison claim is produced
  by a **direct Python file walk**, pasted into `task-log.md`.
- Every Red and every Green is a **separate recorded run** with its output pasted. Waves 1 and 2 are
  never collapsed into one task.
- Wave 7's architecture test must be **seen to fail** on a scratch `_field` reintroduction, then
  reverted, then landed green — both outputs pasted.
- **Every test this change adds goes in a NEW file.** REQ-UOW-49 caps edits to pre-existing files under
  `MyVocaList.Tests/` at the closed four-row carve-out; the carve-out files are `CreateSut`/comment
  edits only. Its evidence — `git diff --stat` over `MyVocaList.Tests/` plus a reviewed diff of the four
  files showing no `Assert`/`Verify`/`Setup` change — is produced in task 8.1.
- REQ-UOW-42's concurrency test is **authored in Wave 6** but its mandated condition is "with
  `DbLoadGate` removed", so it is **re-run gate-free in Wave 8** and both runs are pasted.

## 6. Standing constraints (do not "fix" these)

| Constraint | Why |
|---|---|
| **`MauiProgram.cs` appears in no wave** | The two suggestion services are **pre-built for the `ArtistFormPage`/`SongFormPage` autocomplete feature, not dead code**. DI registration belongs to that feature. Also a sequential-only file. Never "clean up" the unregistered services. |
| **Wave order 1→2→3 inverts DRY Onion** | See gate 1 above. D6, `design.md § 1` and `§ 7`'s warning block. |
| **The rationale is `DbContext` lifetime** | Not tracking (already globally `NoTracking`), not transactions (`ExecuteReadAsync` opens none). `design.md § 2`. |
| **Debounce is out of scope** | REQ-UOW-52 records ~200–250 ms as a forward constraint on the consuming autocomplete feature; `CrudListViewModelBase.cs:254` stays untouched. |
| **No transaction / ambient scope on the read path** | R8, `design.md § 2b`. REQ-UOW-34 unchanged. |
| **Test edits limited to the four-row REQ-UOW-49 carve-out** | `testing.md § Builder Must Not Modify Tests`. Anything else under `MyVocaList.Tests/` is a violation, not a judgement call. |

## 7. Blocked / escalate — do not self-adjudicate

- An existing test goes red after Wave 3 ⇒ log `blocked: spec gap` and escalate. **Never** restore
  `.AsTracking()`, never edit the test (R6).
- The Wave 1 test passes on its first run ⇒ the reproduction is wrong. Stop and fix the test, do not
  proceed to Wave 2.
- A new data service lands before Wave 7 ⇒ REQ-UOW-50's governed-field list is a **fact claim** as of
  2026-08-25 and must be re-verified.

## 8. Definition of done

- [ ] Every task in `tasks.md` checked, each committed separately
- [ ] BUG-078 fail-before/pass-after pair recorded; bug folder status updated
- [ ] Task 7.2's census-wide walk pasted and **clean** (REQ-UOW-36/37 limb (a) evidence)
- [ ] Architecture test green and demonstrated failing on a scratch violation
- [ ] Concurrency test run **twice** — gate present (Wave 6) and gate removed (Wave 8)
- [ ] REQ-UOW-49 evidence: `git diff --stat` over `MyVocaList.Tests/` + reviewed diff of the four
      carve-out files showing no `Assert`/`Verify`/`Setup` change
- [ ] `DbLoadGate` gone tree-wide (Python walk); `Task.Run` offloads intact
- [ ] AC traceability matrix (REQ-UOW-36…52) in `task-log.md`
- [ ] Parent spec's Phase 4.7 unblocked; its DO-NOT-PROCEED verdict lifted
- [ ] `backlog_gen.py status READ-SCOPE "✅"`, LEDGER row closed, docs synced to `develop`
