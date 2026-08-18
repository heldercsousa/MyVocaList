---
id: BUG-075
title: "Pre-commit hook inoperative in worktrees: dotnet test without --no-restore aborts on NETSDK1147"
status: 💡 Pending
severity: Major
target: 2026-08-18
section: DevCycleCraft
parent: hooks-redesign
goal: The pre-commit hook runs dotnet test without --no-restore; in a worktree the restore fails with NETSDK1147, so the hook exits before running a single test.
gate: Found in UOW Phase 2. The test gate silently does not run in any worktree, and every worktree commit needs --no-verify for an unrelated reason.
kind: bug
---

# Pre-commit hook inoperative in worktrees: dotnet test without --no-restore aborts on NETSDK1147

The pre-commit hook runs dotnet test without --no-restore; in a worktree the restore fails with NETSDK1147, so the hook exits before running a single test.

## Symptom

Inside a git worktree (e.g. `C:\Users\helde\source\repos\myvocalist-uow`), any commit that stages a
`.cs` or `.xaml` file triggers the gate, which then fails immediately with an implicit-restore error
rather than a test result:

```
error NETSDK1147: To build this project, the following workloads must be installed: maccatalyst
```

The hook's own `set -e` / non-zero branch then prints `pre-commit: BLOCKED — build or tests failed.`
The message is accurate about the exit code and **misleading about the cause**: no test was compiled,
let alone executed.

## Evidence

`.claude/githooks/pre-commit` invokes:

```sh
dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj -v q --nologo
```

There is no `--no-restore`. `dotnet test` therefore performs an implicit restore, which walks every
`TargetFramework` of the transitively referenced MAUI head — including the mobile TFMs — and demands
the `maccatalyst` workload manifest. In the main working tree that manifest resolution happens to be
already satisfied from prior restores; in a freshly created worktree it is not, so the restore fails.

Observed throughout UOW Phase 2 (2026-08-18): **every** commit on `feat/uow-pilot` required
`git commit --no-verify`, for this reason and not because any test was red. The suite on that branch
was green (`Failed 0, Passed 564, Skipped 0, Total 564`) at the same time the hook was reporting
"build or tests failed".

## Why it matters

This is the worse class of quality-gate defect: **the gate appears to run and does not.**

- `workflow.md § Rule 2` makes worktrees **mandatory for all implementation work**. So the
  environment in which the gate is broken is the only environment in which code is written. The gate
  is effectively dead across the entire project.
- The failure mode trains the agent (and Helder) to reach for `--no-verify` as routine. Once
  `--no-verify` is habitual, it also silently disables the constitutional guard and the
  `regen --check` frontmatter gate that share the same hook — guards that were not broken.
- It compounds **BUG-074**: that item authorises `--no-verify` narrowly, for deliberate RED-first
  commits. BUG-075 makes the bypass unconditional in practice, which destroys BUG-074's premise that
  a bypass is a rare, disclosed, auditable event.
- Because the bypass is unrelated to test state, `git log` gives no way to distinguish a commit whose
  tests were green from one whose tests were never run.

## Suspected cause and likely fix

One missing flag. Adding `--no-restore` (with an explicit, once-per-worktree
`dotnet restore`/`dotnet build` outside the hook, or `-p:TargetFramework=net10.0` to pin the gate to
the single TFM the gate actually needs) should confine the restore to what `dotnet test` really
builds. The hook's own comment already states the intended scope — *"this gate does NOT build the
Android head"* — so restoring mobile TFMs was never intended.

**Not fixed here, deliberately.** `.claude/githooks/pre-commit` is Helder's call and overlaps
BUG-074's proposed redesign of the same hook; the two should be resolved in one pass.

## Files to change when this is picked up

- `.claude/githooks/pre-commit` — the `dotnet test` invocation
- possibly `.claude/settings.json` — if the hook wiring changes
- coordinate with **BUG-074** (`DevCycleCraft/hooks-redesign/bugs/2026-08-04-BUG-074-…`)

## Verification when fixed

In a **freshly created** worktree (not the main tree — the main tree cannot reproduce this):

1. `git worktree add ../scratch-verify -b scratch/verify develop`
2. Stage a trivial `.cs` edit and commit **without** `--no-verify`.
3. The hook must print test counts and pass.
4. Then stage a deliberately failing test and confirm the hook still **blocks** — the fix must not
   turn the gate off, only make it run.
