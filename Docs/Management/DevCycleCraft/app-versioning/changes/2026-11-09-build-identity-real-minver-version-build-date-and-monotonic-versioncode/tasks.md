# Build identity — Tasks

> Source of truth for task state (Rule 4). Details for each task: `plan.md`. The tasks run strictly
> in sequence: both edit the same csproj target.

Both tasks use one branch and worktree, `task/bid-build-identity` (`.worktrees/bid`), in sequence.
After T2, one Verifier checks the whole branch. Helder asked for low token use on 2026-11-09.
T1 runs on the implementor's Sonnet override, which Helder OK'd for T1 only; T2 uses the default
Haiku.

- [x] **T1** — Version wiring, identity line, git build details (pieces 1–7; REQ-BID-01…10).
- [x] **T2** — Status snapshot (pieces 8–9; REQ-BID-11…13). Depends on T1. It touches 6 files,
  over the 5-file guideline; accepted because it is sequential with one writer, and two of the
  files are tests.
- [x] **G1** — Helder confirms `/sln-release minor alpha`, which creates tag `v0.2.0-alpha.0` on
  `develop` (D11, REQ-BID-14). This must happen before any merge.
- [x] **G2** — Merge into `develop`, then check the version and that no update prompt appears
  (orchestrator).
- [x] **G3** — Helder checks the About page and the Android version name on the S23. This happens
  only after G1 and G2: a build from before the tag reads `0.1.0-alpha` and hits the
  forced-update gate.
