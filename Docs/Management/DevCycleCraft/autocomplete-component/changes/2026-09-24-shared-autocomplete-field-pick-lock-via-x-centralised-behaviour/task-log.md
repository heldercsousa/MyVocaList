# Task log: AutocompleteDXField (pick-lock via X)

> Plan: `plan.md` (this folder). One section per task, pre-created so parallel worktrees edit different
> hunks (plan § 0.4 item 0.3). Implementors write only their own section. The orchestrator owns
> `tasks.md`, `findings.md`, `MyVocaList.sln` and the LEDGER.
>
> **Approval:** the spec was approved by Helder 2026-09-25; the whole pipeline (merges and every Helder
> gate except the on-device stops T1b and T7) was **pre-authorised by Helder 2026-09-25**.

## Baseline

- 2026-09-25, `develop` @ `7561a0d6`: `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` →
  **Passed 635, Failed 0, Skipped 0** (Total 635). Used for the "+N tests" checks after each merge.
- Hook health: `.claude/settings.json` parses as valid JSON. `.worktrees/` is git-ignored.

### Checkpoint

- Step 0 in progress (orchestrator).

## T1 — Spike

**T1a: `Check build`** (implementor handback, 2026-09-25). Recorded on develop by the orchestrator per plan T1c
item 1. Full facts: `findings.md` § Environment.
- Branch `spike/acf-t1-lock-mechanism` @ `73184803` (local, not pushed), worktree `.worktrees/acf-t1-spike` (kept for T1b).
- `RequestDelay` type: **int**. Context7 confirmed every listed member (`IsReadOnly`, `IsDropDownOpen` and `SelectionChanged`
  were confirmed via assembly metadata; the last two are declared on `ItemsEditBase`). devexpress-maui MCP: returned empty.
- Build 0 errors. Emulator smoke not run (no device attached).
- Changed files (spike branch only): `MyVocaList/UI/Pages/Spike/AcfSpikePage.xaml(.cs)` (new), `MyVocaList/AppShell.xaml`,
  `MyVocaList/Navigation/NavigationConfig.cs`, `MyVocaList/UI/Pages/Songs/SongFormPage.xaml`.
- Next: **T1b, Helder on device (HARD STOP)**, run sheet in `findings.md`.

### Checkpoint

- 2026-09-25: T1a done; awaiting T1b (Helder). The spike worktree must NOT be deleted before T1c.

## T2

### Checkpoint

- (placeholder)

## T3

### Checkpoint

- (placeholder)

## T5

### Checkpoint

- (placeholder)

## T3-C

### Checkpoint

- (placeholder)

## T4

### Checkpoint

- (placeholder)

## T6

### Checkpoint

- (placeholder)

## T7 — Device gate

### Checkpoint

- (placeholder)
