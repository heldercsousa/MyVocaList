# Inline Trivial Fix Command (`/sln-itf`)

Apply a fully diagnosed 1-file / ≤ 5-line fix **as the orchestrator, without dispatching a subagent**.
Rules: `workflow.md § Rule 2 — ITF lane` (summary) · `library/workflow-rule-2.md § Inline Trivial Fix (ITF) lane` (C0–C8 table).
Guard 3 (`constitutional-guard.py`) enforces C1/C3/C4/C5 once the marker exists; it cannot be exceeded.

**Invocation:** `/sln-itf <ID> <repo-relative file> "<root cause>"` — ID is the BUG-NNN, task ID, or spike slug.

## 1. Qualify — every line must be YES, or stop and dispatch an implementor

| # | Check |
|---|-------|
| C1 | Exactly 1 file, ≤ 5 changed lines |
| C2 | Root cause + exact file + exact line known (stack trace, subagent report, or read lane: grep + ≤ 2 source files) |
| C3 | Not `.xaml` / `.xaml.cs` |
| C4 | Not a governed component (`library/component-safety-gate.md`) |
| C5 | Not `AppShell.xaml(.cs)` / `AppDbContext.cs` / `tasks.md` / `Migrations/*`. `MauiProgram.cs`, `GlobalUsings.cs`, `Directory.Build.props` → step 2 |
| C6 | Severity ≤ Major and no mandatory regression test (`bug-tracking.md`) |
| C7 | Inside a worktree on a task or spike branch based on develop — never `develop` / `main` |

## 2. Sole-writer check (soft-set files only)

Run from the repo root; replace `<file>` with the repo-relative path:

```bash
for b in $(git branch --no-merged develop --format='%(refname:short)'); do
  git diff --name-only develop..."$b" | grep -qxF "<file>" && echo "LIVE WRITER: $b"
done
```

Then check `Docs/Management/LEDGER.md` Active rows for a dispatched subagent owning the file.
Output from the loop on any branch **other than the current one**, or a LEDGER owner → not a sole writer → dispatch.
Otherwise set `"sole_writer": true` in step 3.

## 3. Declare (before opening the file for edit)

Write `<worktree root>/.itf-active` (gitignored; never at the repo root):

```json
{"id": "<ID>", "file": "<repo-relative path, forward slashes>", "expected_lines": <n>,
 "declared_at": "<UTC ISO-8601, e.g. 2026-11-03T18:40:00Z>", "sole_writer": true}
```

Omit `sole_writer` for non-soft-set files. The marker expires after 30 minutes.
Append one line to the feature's `task-log.md` (or `Docs/DevEnv/plans/unassigned-task-log.md`):
`ITF: <ID> — <file> — root cause: <one sentence> — expected <n> line(s).`

## 4. Edit → build → test

Make the single edit. Then `dotnet build` (0 errors) and the affected tests (C8). If the guard blocks,
the build fails twice, or a second file turns out to be needed: delete the marker, log
`ITF aborted: <reason>` under the `ITF:` line, and dispatch an implementor.

## 5. Commit and close

Delete `.itf-active`, then commit with the Bug Fix Pattern plus the trailer:

```
fix: [component] — [symptom]

Root cause: [one sentence]
Fix: [one sentence]
Regression risk: [None | Low | Medium — reason]
Lane: ITF (1 files, <n> lines)
```

The task-log line is docs: it lands on develop (`/sln-docs-sync` at merge). Audit trail: `git log --grep "Lane: ITF"`.
