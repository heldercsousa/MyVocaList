# Queue Management — Dead-Code Cleanup

> **Status:** 🟡 Item 1 DONE (2026-08-18, merge `d73db7ab`) · Item 2 still pending — discovered 2026-06-19 during the BUG-011 fix investigation (branch `fix/bug-011-queue-bottomsheet`).
> **Type:** Dev Cycle Craft / cleanup. Two independent, low-risk deletions. Each is its own small task; neither blocks the other.
> **Out of scope of BUG-011** — registered here so the findings are not lost.

The Queue Management feature shipped with two superseded artifacts still present in `develop`. Both are unused at runtime but add confusion and token weight, and a future agent could wire the wrong one.

---

## Item 1 — Remove the superseded `QueueService` / `IQueueService` ✅ DONE 2026-08-18

There are **two** service implementations and interfaces for the queue:

| Artifact | State |
|----------|-------|
| `Services/QueueServiceNew.cs` (`IQueueServiceNew`, `Domain/ServicesInterfaces/IQueueServiceNew.cs`) | **Active.** Registered `AddScoped<IQueueServiceNew, QueueServiceNew>()` (`MauiProgram.cs:129`); consumed by `QueueManagementViewModel` (`:15`, `:27`). |
| `Services/QueueService.cs` (`IQueueService`, `Domain/ServicesInterfaces/IQueueService.cs`) | **Dead.** Not registered in DI; no known consumer. Superseded by the `*New` variant. |

**Task:**
1. Grep the whole solution for `IQueueService` / `QueueService` (word-boundary; exclude `QueueServiceNew`/`IQueueServiceNew`) to confirm **zero** consumers and **zero** DI registration.
2. If confirmed clean: delete `Services/QueueService.cs` and `Domain/ServicesInterfaces/IQueueService.cs`.
3. Consider renaming `QueueServiceNew`/`IQueueServiceNew` → `QueueService`/`IQueueService` once the old pair is gone (the "New" suffix is a smell now that it is the only implementation). This rename touches `MauiProgram.cs`, `QueueManagementViewModel.cs`, and the two service/interface files — treat as a separate follow-up step so the deletion can land first.
4. Build + run the queue tests; confirm green.

**Risk:** Low (deletion of unreferenced code). Verify-before-delete is the only safeguard needed.

---

## Item 2 — Remove the `QueuePage` placeholder

| Artifact | State |
|----------|-------|
| `MyVocaList/UI/Pages/Queue/QueueManagementPage.xaml(.cs)` | **Active.** Registered as a `ShellContent` root view (`AppShell.xaml:109`) and in DI (`MauiProgram.cs:169/188`). The real Queue UI. |
| `MyVocaList/UI/Pages/Queue/QueuePage.xaml` + `QueuePage.xaml.cs` | **Dead.** 712-byte placeholder (identical across every branch); only an `exitConfirmSheet` shell with no real queue logic. The BUG-011 report mistakenly named this page. |

**Task:**
1. Grep for `QueuePage` across `*.xaml`, `*.cs`, `AppShell.xaml(.cs)`, `MauiProgram.cs`, and route registrations to confirm **no** navigation route, DI registration, or `x:Reference` points at it.
2. If confirmed clean: delete `QueuePage.xaml` and `QueuePage.xaml.cs`.
3. Build; confirm 0 errors (XamlC will fail if any compiled binding still references it).

**Risk:** Low. The only known reference was the BUG-011 report text, already corrected.

---

## Verification (both items)
- `dotnet build` → 0 errors.
- `dotnet test` → queue tests green (note the pre-existing flaky parallel-SQLite `QueueRepositoryTests` race; run in isolation to confirm).
- Solution Explorer: deleted files removed from `MyVocaList.sln` where applicable (source `.cs`/`.xaml` are project-globbed; no `.sln` edit needed for those — only `Docs/`/`.claude/` files require `.sln` registration).

## Commit
Bug-fix/cleanup ceremony: commit message as spec. Suggested subject:
`chore: Queue — remove dead QueueService/IQueueService and QueuePage placeholder`

---

## Item 1 — completion record (2026-08-18)

Done as the first increment of UOW Phase 3.5 (`cross-cutting/read-model-notracking-guidelines/changes/2026-08-04-merge-duplicate-repository-families-into-one-infra-repository-infra-repositories/`),
which needed the dead code gone before its own scope could be decided.

**Branch** `chore/queue-deadcode-cleanup` (commit `dd3b01df`) → merged to develop as **`d73db7ab`**.

**Deleted:** `Services/QueueService.cs` (166 lines), `Domain/ServicesInterfaces/IQueueService.cs` (22
lines). **Also removed** (added to scope — same dead-dependency class): `VenueService`'s unused
`IEventRepository _eventRepository` field, ctor parameter and assignment.

**Verification:** zero consumers and zero DI registration confirmed by two independent methods before
deleting (the rtk proxy silently dropped matches on one pass — caught and cross-checked with a direct
file read). Suite **590 passed / 0 failed / 0 skipped**, exactly the pre-merge baseline. Independent
verifier verdict **PASS**, including confirmation that the one test edit
(`MyVocaList.Tests/Unit/Services/VenueServiceTests.cs`) was mechanical — dropping a mock argument for a
constructor parameter that no longer exists — with no assertion, case or behaviour altered
(`testing.md § Builder Must Not Modify Tests`).

**`TODO [BUG-071 / UOW]` markers: 26 → 20**, none migrated — they were deleted along with their dead host.

### The finding this unlocked

`Domain/RepositoryInterface/IEventRepository` + `Infra/Repository/EventRepository` — the OLD,
singular-namespace pair — now have **zero consumers**. `VenueService` was the last one and its
dependency was dead. This materially shrinks UOW Phase 3.5: the old family's `Event` half is
unreferenced code, not a competing implementation to weigh against the new one.

**Left deliberately undone (next increment, not this task):** that pair is still DI-registered at
`MyVocaList/Extensions/ServiceCollectionExtensions.cs:28`, now binding an interface nothing injects.
Harmless at runtime, but it makes the old family look live to a future reader.

### Item 1 step 3 — NOT done, still open

Renaming `QueueServiceNew`/`IQueueServiceNew` → `QueueService`/`IQueueService` (now that the name is
free) was explicitly excluded so the deletion could land alone. Still worth doing; the `New` suffix is
a smell now that it is the only implementation.

## Item 2 — still pending

The `QueuePage` placeholder deletion was out of scope for this session and is untouched.
