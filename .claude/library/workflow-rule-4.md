# Development Workflow — Reference — Rule 4 — Tasks.md Source of Truth (full detail + DRY Onion phases)

> Section file split from `workflow-reference.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `workflow-reference.md`.

## Rule 4 — Tasks.md Is the Source of Truth

Check off each task in `Docs/Management/[BusinessFeatures|DevCycleCraft]/[feature]/tasks.md` as it completes.

**Sequential constraint:** Never start a task that depends on the output of an incomplete task.

**Parallel exception:** Tasks marked `[P]` may be dispatched simultaneously as a wave per Rule 2.

### In-progress marker — [~] for claimed tasks

```markdown
- [~] **Implement ISingerService** [SEQUENTIAL]  ← claimed — do not reassign
- [ ] **Implement SingersViewModel** [P]          ← available
- [x] **Define SingerEntry entity**               ← done
```

| Marker | Meaning |
|--------|---------|
| `[ ]` | Available — not started |
| `[~]` | In progress — claimed by a dispatched subagent |
| `[x]` | Done — committed |
| `[CANCELLED: reason]` | Removed from scope |

**Rule:** Never dispatch a task marked `[~]`. If a subagent was killed without completing a `[~]` task, reset it to `[ ]` before re-dispatching.

#### Lease-aware `[~]` reclaim (Session Continuity)

A `[~]` claim is a **lease, not a lock** — it is only binding while its owner is *fresh*.
Before treating a `[~]` task as owned-and-blocked, classify its claim with the lease
helper rather than assuming the owner is still alive:

1. Identify the owner session id from the claim file under `.claude/leases/` (the claim
   whose `resume_pointer` matches the work, or the only live claim on this host).
2. Run `python .claude/scripts/lease/reclaim.py <my_session_id> <owner_session_id>` and act
   on the single printed word:
   - `fresh`     → the owner is alive; **leave the `[~]` task** and select the next `[ ]`
     task (this is AC-1.3 — do not wait).
   - `reclaimed` → the claim was stale; you now own it. Run
     `python .claude/scripts/lease/resume.py <owner_session_id>` to read the resume pointer,
     then continue the exact next step (AC-2.3 / AC-4.2). Leave the marker `[~]` (it is now
     yours) — do not reset to `[ ]`.
   - `lost`      → a concurrent session reclaimed first; re-evaluate and select the next
     `[ ]` task (AC-2.4 / INV-3).

> Only reset a `[~]` to `[ ]` when the claim classifies as **stale** AND you choose not to
> reclaim it. Never reset a `fresh` claim.

### Task atomization checklist

A task is **atomic** if it passes this checklist:

- [ ] The task produces a single, clearly named artifact (one method, one ViewModel, one page, one migration)
- [ ] The task does not require knowledge of the output of another in-progress task
- [ ] The task can be described in one sentence without using "and" more than once
- [ ] The task fits within the sizing limits (see Rule 2)
- [ ] The task has a `Demo:` statement or a clear acceptance criterion it satisfies
- [ ] A new developer could implement this task correctly using only the spec + `Files owned` declaration

### DRY Onion task ordering rule

Tasks must be ordered from the inside of the architecture outward — Domain first, then Infra, then Services, then UI.

```
Wave 1 (innermost):  Domain entities + repository interfaces
Wave 2:              EF Core migrations + repository implementations
Wave 3:              Service methods
Wave 4 (outermost):  ViewModels + pages
```

**Rule:** Do NOT dispatch a task in Wave N+1 until all tasks in Wave N that produce types consumed by Wave N+1 have been committed.

### Task entry format — structured fields

```markdown
- [ ] **Task title** [P | SEQUENTIAL]
  - **Produces:** [list of new files, interfaces, or types this task creates]
  - **Consumes:** [list of files, interfaces, or types this task depends on being committed first]
  - **Risk:** [Low | Medium | High — one-line reason]
  - **Files owned:** [exact file paths this subagent may create or edit]
  - **Demo:** [one sentence — what a human observer sees when this is done]
  - **Review lane:** [Standard | Elevated | Architectural]
```

### Dependency ordering example — phases template

```markdown
## Phase 1 — Domain (no dependencies)
- [ ] **Define entity** [P]
  - Produces: `MyVocaList.Domain/Entities/SingerEntry.cs`
  - Consumes: nothing
  - Files owned: `MyVocaList.Domain/Entities/SingerEntry.cs`

- [ ] **Define repository interface** [P]
  - Produces: `MyVocaList.Domain/Interfaces/ISingerRepository.cs`
  - Consumes: `SingerEntry.cs`
  - Files owned: `MyVocaList.Domain/Interfaces/ISingerRepository.cs`

## Phase 2 — Infra [SEQUENTIAL — waits for Phase 1]
- [ ] **Add EF Core migration** [SEQUENTIAL]
  - Produces: `*_AddSingerEntry.cs` migration
  - Consumes: `SingerEntry.cs`
  - Files owned: `MyVocaList.Infra/Migrations/*.cs`, `AppDbContext.cs`

- [ ] **Implement repository** [SEQUENTIAL — waits for interface]
  - Produces: `MyVocaList.Infra/Repositories/SingerRepository.cs`
  - Consumes: `ISingerRepository.cs`
  - Files owned: `SingerRepository.cs`

## Phase 3 — Services [SEQUENTIAL — waits for Phase 2]
- [ ] **Implement ISingerService + SingerService** [SEQUENTIAL]
  - Produces: `ISingerService.cs`, `SingerService.cs`
  - Consumes: `ISingerRepository.cs`
  - Files owned: both service files

## Phase 4 — UI [SEQUENTIAL — waits for Phase 3]
- [ ] **ViewModel** [P]
  - Produces: `SingersViewModel.cs`
  - Consumes: `ISingerService.cs`
  - Files owned: `SingersViewModel.cs`

- [ ] **Page + XAML** [P]
  - Produces: `SingersPage.xaml`, `SingersPage.xaml.cs`
  - Consumes: `SingersViewModel.cs`
  - Files owned: both page files
```

---
