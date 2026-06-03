# Session Planning — What to Tackle & Parallel Worktree Strategy

## Context

Three spec-complete features are sitting at `📋 Spec` (draft, pending review), all unblocked. App Versioning is ✅ Done, which is the only external dependency. Meanwhile, Artists & Songs Phase 16C requires a manual emulator smoke test from Helder — subagents cannot do this.

This plan identifies what subagents can execute now, and how to split the work across 2–3 git worktrees to maximize parallelism.

---

## Current State Snapshot

| Feature | Status | Subagent-ready? | Blocker |
|---------|--------|-----------------|---------|
| Artists & Songs — Phase 16C | 🟡 [~] | ❌ Manual | Helder emulator smoke test |
| **What's New / Release Notes** | 📋 Spec | ✅ After plan | Helder spec approval → plan writing |
| **User Suggestions** | 📋 Spec | ✅ After plan | Helder spec approval → plan writing |
| **App Update Check** | 📋 Spec | ✅ After plan | Helder spec approval → plan writing |

---

## Recommended Work Allocation

### Main session (this terminal)

1. **Artists & Songs Phase 16C** — Helder runs the emulator smoke test; subagent does build/review/commit steps 16C.2–16C.5 afterwards
2. **Spec review** — Helder reads and approves the 3 draft specs (they are already written; this is a human gate)
3. **Plan writing** — Invoke `superpowers:writing-plans` for each approved spec → produces `plan.md` in each feature folder
4. **Orchestration** — Dispatch worktree subagents, monitor waves, merge, verify

### Terminal 2 — git worktree `feature/user-suggestions`

Fully independent from the other two features:
- No shared MAUI pages with What's New or App Update Check
- `MauiProgram.cs` is the only hotspot — DI registration is its final task step
- `AppShell.xaml` route entry (`feedback`) is its own route, no overlap
- No `AppShellViewModel.cs` touch needed (navigated to from Settings page, not startup trigger)

**Eligible for parallel execution with Terminals 3 once plans exist.**

### Terminal 3 — git worktree `feature/whats-new`

- Implements What's New / Release Notes end-to-end
- **Hotspot coordination note:** touches `AppShellViewModel.cs` (startup trigger via `WeakReferenceMessenger`)
- `MauiProgram.cs` (DI) and `AppShell.xaml` (no route needed — triggered via message) are shared hotspots
- **Cannot run in parallel with App Update Check** — both touch `AppShellViewModel.cs` at startup

### App Update Check — sequenced after What's New merges

- Also touches `AppShellViewModel.cs`
- Run in Terminal 3 (or a new worktree) **after** What's New is merged back to `develop`
- OR: brief both subagents to implement all layers except `AppShellViewModel` wiring, then do the wiring integration on main as a final step

---

## Parallelism Map

```
Main terminal:          Phase 16C (manual) → spec reviews → orchestration
Terminal 2 (worktree):  User Suggestions  [fully parallel]
Terminal 3 (worktree):  What's New        [parallel with T2, sequential before App Update Check]
                        → merge → App Update Check [sequential after What's New]
```

**Maximum safe parallelism: 2 worktrees at a time** (T2 + T3 simultaneously), due to AppShellViewModel being shared between What's New and App Update Check.

---

## File Conflict Matrix

| File | User Suggestions | What's New | App Update Check |
|------|-----------------|------------|-----------------|
| `MauiProgram.cs` | ✅ touches | ✅ touches | ✅ touches |
| `AppShell.xaml` | ✅ route `feedback` | ❌ | ❌ |
| `AppShellViewModel.cs` | ❌ | ✅ startup trigger | ✅ startup trigger |
| `AppDbContext.cs` | ❌ | ❌ | ❌ |
| `GlobalUsings.cs` | maybe | maybe | maybe |

**Conflict:** `AppShellViewModel.cs` — What's New and App Update Check both need it. Solution: run them sequentially on Terminal 3.

**No conflict** between User Suggestions (T2) and either What's New or App Update Check (T3).

---

## Step-by-Step Execution Order

### Step 0 — Right now (pre-flight)
- [ ] Helder runs emulator smoke test for Phase 16C
- [ ] Helder reviews `whats-new/requirements.md` + `design.md`
- [ ] Helder reviews `user-suggestions/requirements.md` + `design.md`
- [ ] Helder reviews `app-update-check/requirements.md` + `design.md`

### Step 1 — Write plans (main terminal, after spec approvals)
- `superpowers:writing-plans` for What's New → `whats-new/plan.md`
- `superpowers:writing-plans` for User Suggestions → `user-suggestions/plan.md`
- `superpowers:writing-plans` for App Update Check → `app-update-check/plan.md`

### Step 2 — Dispatch parallel wave (Terminals 2 + 3)
- Terminal 2: `git worktree add .worktrees/user-suggestions feature/user-suggestions` → subagent implements all tasks from `user-suggestions/plan.md`
- Terminal 3: `git worktree add .worktrees/whats-new feature/whats-new` → subagent implements all tasks from `whats-new/plan.md`

### Step 3 — After Terminal 2 & 3 complete
- Merge `feature/user-suggestions` → `develop`
- Merge `feature/whats-new` → `develop`
- Run build + tests on `develop`

### Step 4 — Terminal 3 (reused or new worktree): App Update Check
- `git worktree add .worktrees/app-update-check feature/app-update-check`
- Subagent implements App Update Check from `app-update-check/plan.md`
- After complete: merge → `develop`, build + test

---

## Questions for Helder Before Proceeding

1. **Spec review preference** — Do you want to review all 3 specs now in this session (quick read + approve), or one at a time as each feature starts?
2. **Phase 16C priority** — Should the emulator smoke test happen before any new feature work starts, or can new feature planning proceed in parallel while you're smoke-testing?
3. **App Update Check AppShellViewModel strategy** — Preferred approach:
   a. Sequential on Terminal 3 (simpler, safe)
   b. Parallel but split: subagents skip AppShellViewModel wiring; main does final integration

---

## Verification

After all 3 features are merged to `develop`:
- `dotnet build` — 0 errors
- `dotnet test` — 0 failures
- Emulator smoke test for each new feature (Helder)
- `/project:review` per feature
- `/project:commit` + version tag when ready for release
