# Development Workflow — Reference — Rule 5 — Task Status Registration (full detail)

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Rule 5 — Task Status Registration

Agents record task outcomes manually in the task-log file. The `Stop` hook warns if uncommitted changes remain when a session ends.

### Proof of action — Changed files is mandatory

A task-log entry that claims `To Review` without a `### Changed files` section is **invalid**.

**Rule:** Every task-log entry that represents completed implementation work must include an explicit list of every file that was created or modified.

### Task-log file location

Task-log files live **beside the spec** at `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/task-log.md`.
Plan files live at `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/plan.md`.
Tasks without a feature association are logged to `Docs/DevEnv/plans/unassigned-task-log.md`.



### Task-log format (per task entry)
```
---
## Task: <title>
**Plan:** <plan file relative path>
**Status:** in progress | Check build | To Review | Build failure | blocked: spec gap | Spec updated — re-planning required | Early task done | Review task done
**Started:** MM/DD/YYYY
**Completed:** MM/DD/YYYY

### Changed files:
- `relative/path/to/file.cs` — reason (e.g. "added GetPagedAsync method")
- `relative/path/to/test.cs` — reason (e.g. "added 3 test cases")

### Build notes
[Only present if build was checked — records error summary and diagnosis]

### Verification evidence
- Build: [PASS / FAIL — error summary if FAIL]
- Tests: [PASS (N tests) / FAIL (N failures) / SKIPPED (no test files changed)]
- Post-edit re-read: [confirmed / N/A — no code files changed]
- Spec compliance: [confirmed — [spec file] section checked / divergence noted: [one line]]
```

### Acceptance criteria traceability matrix

For tasks that implement user-facing behavior, include an **AC traceability matrix** in the task-log entry:

```
### AC traceability
| AC ID | Criterion (short) | Implementation location | Test method |
|-------|-------------------|------------------------|-------------|
| AC-1 | Singer added appears in queue | VenueService.AddSingerAsync | AddSingerAsync_ValidInput_ReturnsSuccess |
```

Missing rows = missing tests = incomplete feature.

### Task statuses
| Status | Meaning |
|--------|---------|
| `in progress` | Task started, work underway |
| `Check build` | Code changed — build verification pending |
| `To Review` | Build passed — task ready for code review |
| `Build failure` | Build failed after 3 attempts — needs investigation |
| `blocked: spec gap` | Spec ambiguity found — question + options + recommendation documented |
| `Spec updated — re-planning required` | Implementation revealed a spec gap; spec updated; tasks.md may need re-ordering |
| `Early task done` | New asset/enhancement completed and committed |
| `Review task done` | Review task completed |

---
