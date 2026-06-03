# Session Plan — Work Queue & Parallel Worktree Strategy

## Context

Phase 16C smoke test revealed 2 bugs (logged as BUG-001, BUG-002 in `artists-songs/bugs/`).
Phase 16C will close with bugs as follow-up items. Three spec-complete features are ready for plan-writing and implementation.

---

## Decisions Made

| Decision | Choice |
|----------|--------|
| Phase 16C gate | Close as-is; bugs become follow-up tasks |
| Spec review flow | Review all 3 specs now, batch-approve |
| AppShellViewModel conflict | Sequential on same worktree |

---

## Execution Order

### Step 1 — Close Phase 16C (main terminal)
- Run build + tests: `dotnet build` + `dotnet test`
- Run `/project:review`
- Update `Docs/Changelog/changelog.md`
- Run `/project:commit`
- Update BACKLOG.md: Artists & Songs → ✅ Done

### Step 2 — Spec review (Helder reads, this session)
Read and approve these 3 spec folders (requirements.md + design.md each):
- `Docs/Management/BusinessFeatures/whats-new/`
- `Docs/Management/BusinessFeatures/user-suggestions/`
- `Docs/Management/BusinessFeatures/app-update-check/`

Promote each to `🗺️ Plan` in BACKLOG.md after approval.

### Step 3 — Write plans (main terminal, after approvals)
For each approved spec, invoke `superpowers:writing-plans`:
- → `whats-new/plan.md`
- → `user-suggestions/plan.md`
- → `app-update-check/plan.md`

Promote each to `🟢 Ready` in BACKLOG.md after plan approval.

### Step 4 — Parallel dispatch (2 worktrees)

**Terminal 2 — worktree `feature/user-suggestions`**
```
git worktree add .worktrees/user-suggestions feature/user-suggestions
```
Subagent implements all tasks from `user-suggestions/plan.md`.  
Fully independent — no shared hotspot files with the other two features.

**Terminal 3 — worktree `feature/whats-new`**
```
git worktree add .worktrees/whats-new feature/whats-new
```
Subagent implements all tasks from `whats-new/plan.md`.  
Runs in parallel with Terminal 2.

### Step 5 — Merge wave 1
After T2 and T3 complete:
- Merge `feature/user-suggestions` → `develop`
- Merge `feature/whats-new` → `develop`
- `dotnet build` + `dotnet test` on `develop` — 0 errors/failures

### Step 6 — App Update Check (Terminal 3, sequential)
```
git worktree add .worktrees/app-update-check feature/app-update-check
```
Subagent implements all tasks from `app-update-check/plan.md`.  
Runs after What's New is merged (avoids AppShellViewModel conflict).

### Step 7 — Merge wave 2 + final gate
- Merge `feature/app-update-check` → `develop`
- Final build + test
- Emulator smoke test per feature (Helder)
- `/project:review` per feature
- `/project:commit`

---

## File Conflict Matrix

| File | User Suggestions | What's New | App Update Check |
|------|:---:|:---:|:---:|
| `MauiProgram.cs` | ✅ | ✅ | ✅ |
| `AppShell.xaml` | ✅ route `feedback` | — | — |
| `AppShellViewModel.cs` | — | ✅ startup | ✅ startup |
| `AppDbContext.cs` | — | — | — |

**Conflict:** `AppShellViewModel.cs` → resolved by sequencing What's New before App Update Check.  
**No conflict** between User Suggestions and either of the other two.

---

## Bug Follow-up (separate session or additional terminal)

| Bug | File | Priority |
|-----|------|----------|
| BUG-001: Back button + trailing toggle label | `ArtistsPage.xaml` | High |
| BUG-002: Form search strip MD3 compliance | `ArtistFormPage.xaml`, `SongFormPage.xaml` | Medium — investigate first |

BUG-002 requires Helder to review m3.material.io/components/search/overview and decide Option A vs B before implementation starts. This can be a third worktree task or a follow-up session.

---

## Verification Checklist (end of session)

- [ ] Artists & Songs Phase 16C committed and BACKLOG updated to ✅ Done
- [ ] All 3 specs approved by Helder
- [ ] 3 plan files written and approved
- [ ] User Suggestions merged to develop — build + tests green
- [ ] What's New merged to develop — build + tests green
- [ ] App Update Check merged to develop — build + tests green
- [ ] BACKLOG updated for all 3 features → ✅ Done
- [ ] BUG-001 and BUG-002 registered in `artists-songs/bugs/` ✅ (done)
