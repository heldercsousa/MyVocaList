# Inline Trivial Fix (ITF) Lane — Task Log

---
## Task: Implement Guard 3 (ITF bounds) in `constitutional-guard.py` + test suite
**Plan:** `Docs/Management/DevCycleCraft/inline-trivial-fix/design.md` § Guard 3 — control flow
**Status:** To Review
**Started:** 2026-07-21
**Completed:** 2026-07-21
**Branch / worktree:** `worktree-agent-a20848108c35ae4b3` @ `.claude/worktrees/agent-a20848108c35ae4b3`

### Changed files
- `.claude/scripts/constitutional-guard.py` — added Guard 3: `_worktree_root`, `_count_lines`, `_changed_lines`, `_itf_block`, `_itf_guard`; module constants `_ITF_MARKER`, `_ITF_MAX_LINES`, `_ITF_EXPIRY`, `_ITF_BLOCKED_SUFFIXES`, `_ITF_GOVERNED_COMPONENTS` (keep in sync with `component-safety-gate.md`), `_ITF_SEQUENTIAL_FILES` (keep in sync with `workflow.md` Rule 2). Guards 1 and 2 are unmodified; the only change to pre-existing code is `main()`'s final `return 0` becoming `return _itf_guard(file_path, tool_input)`.
- `.claude/scripts/tests/test_constitutional_guard.py` — new, 33 tests.
- `Docs/Management/DevCycleCraft/inline-trivial-fix/task-log.md` — this file.

Not `.sln`-registered: `.claude/scripts/**` is outside the `.sln` registration gate (`constraints-registry.md` § Visual Studio Solution). This task-log **is** a `Docs/` file and needs `.sln` registration when synced to develop.

### Worktree base repair (pre-work)
On arrival `git merge-base --is-ancestor develop HEAD` returned rc=1: the worktree was pinned at `0e6449f0`, an ancestor of develop, and did not contain the ITF rule amendments (`.claude/scripts/` was effectively empty). HEAD had zero unique commits, so this was resolved with a non-destructive `git merge --ff-only develop` (`0e6449f0..6e0323bb`); `is-ancestor` returns 0 afterwards. Flagged for the orchestrator: any worktree created before `6e0323b` has the same defect.

### Verification evidence

**Test suite — 33/33 passing** (`python .claude/scripts/tests/test_constitutional_guard.py`; block-message stderr lines filtered out of the listing for readability):

```
test_c1_file_message (__main__.TestBlockMessages) ... ok
test_c1_lines_message (__main__.TestBlockMessages) ... ok
test_c3_message (__main__.TestBlockMessages) ... ok
test_c4_message_points_at_governance (__main__.TestBlockMessages) ... ok
test_c5_message (__main__.TestBlockMessages) ... ok
test_edit_takes_max_of_old_and_new (__main__.TestChangedLineCounting) ... ok
test_expected_lines_literal_absent_from_guard_source (__main__.TestChangedLineCounting) ... ok
test_multiedit_sums_edits (__main__.TestChangedLineCounting) ... ok
test_write_counts_content_lines (__main__.TestChangedLineCounting) ... ok
test_lane_itf_trailer_is_greppable (__main__.TestCommitTrailerAudit) ... ok
test_cross_worktree_declaration_is_inert (__main__.TestDeclarationScope) ... ok
test_expired_declaration_is_inert (__main__.TestDeclarationScope) ... ok
test_malformed_marker_is_inert (__main__.TestDeclarationScope) ... ok
test_marker_absent_is_inert (__main__.TestDeclarationScope) ... ok
test_marker_that_is_not_an_object_is_inert (__main__.TestDeclarationScope) ... ok
test_marker_with_invalid_utf8_is_inert (__main__.TestDeclarationScope) ... ok
test_marker_with_non_string_declared_at_is_inert (__main__.TestDeclarationScope) ... ok
test_marker_with_non_string_file_is_inert (__main__.TestDeclarationScope) ... ok
test_marker_with_null_file_is_inert (__main__.TestDeclarationScope) ... ok
test_marker_with_unparseable_declared_at_is_inert (__main__.TestDeclarationScope) ... ok
test_absent_expected_lines_still_permits_a_valid_edit (__main__.TestExpectedLinesIsAuditOnly) ... ok
test_guard1_blocks_on_develop_even_with_declaration (__main__.TestGuardOrdering) ... ok
test_main_is_inert_without_a_declaration (__main__.TestGuardOrdering) ... ok
test_non_governed_lookalike_is_permitted (__main__.TestItfBounds) ... ok
test_valid_small_cs_edit_is_permitted (__main__.TestItfBounds) ... ok
Ran 33 tests in 7.143s
OK
```

**Inert-on-normal-edit probe** — real worktree, real `Services/SongService.cs`, 40-line edit, no marker, end-to-end through `main()`. This is the regression that would break every agent's ability to edit code, so it is verified against the live checkout, not a fixture:

```
marker present: False
target exists: True
exit code: 0
stderr: ''
```

**Live positive counterpart** — a real marker written into this worktree, then deleted:

```
Services/SongService.cs (2 lines)   -> exit 0
Services/SongService.cs (40 lines)  -> exit 2   C1 (<= 5 changed lines)
Services/ArtistService.cs (2 lines) -> exit 2   C1 (exactly 1 file per declaration)
MyVocaList/MauiProgram.cs (2 lines) -> exit 2   C1 (exactly 1 file per declaration)
marker removed: True
```

(The `MauiProgram.cs` row reports C1 rather than C5 because the active declaration named a different file; per `design.md § Guard 3 — control flow` the file-identity check precedes the path-list checks. Correct, though the ACs read as if each condition reports itself.)

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|-------|-----------|-------------------------|-------------|
| AC-ITF-01 | Valid declaration + ≤ 5 lines + C2–C8 met → permitted (exit 0) | `_itf_guard` final `return 0` | `TestItfBounds.test_valid_small_cs_edit_is_permitted`; `TestExpectedLinesIsAuditOnly.test_large_expected_lines_does_not_loosen_the_cap` |
| AC-ITF-02 | Edit to a file other than the declared one → block naming C1 | `_itf_guard` declared-vs-target comparison | `TestItfBounds.test_different_file_blocks`; `TestBlockMessages.test_c1_file_message` |
| AC-ITF-03 | Changed-line count > 5 → block naming C1 | `_changed_lines` + `_ITF_MAX_LINES` check | `TestItfBounds.test_nine_line_edit_blocks`; `TestBlockMessages.test_c1_lines_message`; `TestChangedLineCounting.*` (Write / Edit / MultiEdit arithmetic) |
| AC-ITF-04 | `.xaml` / `.xaml.cs` target → block naming C3 | `_ITF_BLOCKED_SUFFIXES` check | `TestItfBounds.test_xaml_target_blocks`; `TestBlockMessages.test_c3_message` |
| AC-ITF-05 | Governed component → block naming C4, pointing at `component-change-governance.md` | `_ITF_GOVERNED_COMPONENTS` stem match | `TestItfBounds.test_governed_component_blocks`; `TestItfBounds.test_non_governed_lookalike_is_permitted` (negative); `TestBlockMessages.test_c4_message_points_at_governance` |
| AC-ITF-06 | Sequential-only registry file → block naming C5 | `_ITF_SEQUENTIAL_FILES` + `Migration.cs` / `/Migrations/` match | `TestItfBounds.test_sequential_only_file_blocks`; `TestItfBounds.test_migration_file_blocks`; `TestBlockMessages.test_c5_message` |
| AC-ITF-07 | On develop/main, Guard 1 fires first (ITF grants no worktree exemption) | `main()` calls `_branch_guard` before `_itf_guard` | `TestGuardOrdering.test_guard1_blocks_on_develop_even_with_declaration` |
| AC-ITF-08 | ITF commit discoverable via `git log --grep "Lane: ITF"` | No guard code — commit-message convention (`workflow.md` Rule 3) | `TestCommitTrailerAudit.test_lane_itf_trailer_is_greppable` |
| AC-ITF-09 | Internal error / malformed / unreadable declaration → exit 0 (fail-open) | `_itf_guard` blanket `except Exception: return 0` + typed-field early returns | `TestDeclarationScope.test_malformed_marker_is_inert`, `…_not_an_object_…`, `…_invalid_utf8_…`, `…_null_file_…`, `…_non_string_file_…`, `…_non_string_declared_at_…`, `…_unparseable_declared_at_…`; `TestExpectedLinesIsAuditOnly.test_absent_expected_lines_still_permits_a_valid_edit` |
| AC-ITF-10 | No declaration in the editing worktree → exit 0, no ITF bound on any agent | `_itf_guard` marker-absent early return; marker resolved from `_worktree_root`, not repo root | `TestDeclarationScope.test_marker_absent_is_inert`; `…_cross_worktree_declaration_is_inert`; `TestGuardOrdering.test_main_is_inert_without_a_declaration`; plus the live inert probe above |
| AC-ITF-11 | Declaration older than 30 min treated as absent | `_ITF_EXPIRY` check | `TestDeclarationScope.test_expired_declaration_is_inert` |
| AC-ITF-12 | The `Lane: ITF (N files, N lines)` trailer is orchestrator-self-reported and not machine-verified | N/A — no implementation | **N/A by design.** AC-ITF-12 states an *interpretation* of the trailer's evidential weight, not a behaviour of any component. There is nothing to execute: asserting "N is not verified" would require testing the absence of a verifier. It is discharged by the design decision (D5) and by `workflow.md` Rule 3 documenting the trailer as an audit index rather than evidence. |

**Note on AC-ITF-08's test.** `test_lane_itf_trailer_is_greppable` builds a fixture repo, commits a Bug-Fix-Pattern message carrying the trailer, and asserts `git log --grep "Lane: ITF"` finds it. It therefore exercises **git**, not Guard 3 — the guard has no role in commit messages. This is acceptable because the AC is itself about git's discoverability of the convention: it pins the exact trailer string (`Lane: ITF`) against typo drift in the rules file and confirms the grep an auditor will run actually matches a real commit. It is a convention test, not an enforcement test, and is labelled as such.

**`expected_lines` is audit-only (verifier W1).** Originally covered only by a source-text assertion, which would pass even if the field were read dynamically. Now proven behaviourally in both directions by `TestExpectedLinesIsAuditOnly`: `expected_lines: 999` + a 9-line edit still blocks (does not loosen the cap), and `expected_lines: 1` + a 3-line edit still permits (does not tighten it). The source-text assertion is retained as belt-and-braces and renamed to say so.

### Design concerns / spec gaps (non-blocking — recorded for review)

1. **C5 migration matching.** The registry says "any `*Migration.cs`", but real files are `20260407190608_PersonConfigFixes.cs`. Implemented as `endswith("Migration.cs")` **or** path contains `/Migrations/`; the directory rule is what actually fires.
2. **C4 matching strategy unstated.** The authoritative list holds component *names*, not paths. Implemented as a file-name-stem match, so `ListItem.cs/.xaml/.xaml.cs` match while `ListItemLeadingImage.xaml.cs` does not (negative test included).
3. **Unreachable C5 entries.** `Directory.Build.props` and `tasks.md` can never reach Guard 3 — `main()` returns early for anything outside `_CODE_SUFFIXES`. Kept in the constant for fidelity with the registry, but they are dead entries. (Verifier W3; being corrected in `requirements.md` on develop by the orchestrator.)
4. **Condition precedence when several fail.** `design.md`'s flow fixes the order file → C3 → C4 → C5 → lines, so only the first failing condition is reported. Intended, but the ACs read as if each condition reports itself.
5. **Case-insensitive path comparison.** Correct on Windows, not filesystem-agnostic: on a case-sensitive FS a declaration for `Foo.cs` would authorize an edit to `foo.cs`. Behaviour left unchanged per verifier W4; the assumption is now recorded in a comment at the comparison site.
6. **Lifecycle state 3 has no mechanical backstop.** `requirements.md` accepts this explicitly, so Guard 3 implements nothing for marker deletion.

### Build notes
No `.cs` / `.xaml` files were touched, so `dotnet build` / `dotnet test` do not apply (the pre-commit hook confirmed: *"no .cs/.xaml changes staged — skipping build+test gate"*). Python suite: 33 passed, 0 failed.
Commits: `64cbef8` (Guard 3 + initial suite), follow-up commit below (verifier W1/W2/W4 + this task-log).
Files written and re-read after edit: `.claude/scripts/constitutional-guard.py`, `.claude/scripts/tests/test_constitutional_guard.py`.
Not pushed, not merged, per briefing.
