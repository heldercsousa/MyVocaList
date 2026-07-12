# Tasks — Hamburger on CRUD list pages (B′)

Scope: 4 CRUD list pages. Single-file production change + manual E2E verification. Light ceremony (single file, < 1 hour) per `workflow.md` Rule 1.

- [ ] **Task 1 — Always-hamburger in `CrudListPageBase.OnNavigatedTo`**
  - **Produces:** correct leading icon (hamburger) on all 4 CRUD list pages.
  - **Consumes:** existing `ICrudListViewModel.AppBarNavigationIcon` / `AppBarNavigationCommand`; existing `"menu"` glyph.
  - **Change:** replace the `NavigationStack.Count <= 1` conditional with unconditional hamburger assignment (icon `"menu"` + open-flyout command); remove the dead `arrow_back_outlined`/`GoToAsync("..")` branch; add the classification/assumption code comment (see `design.md`).
  - **Files owned:** `MyVocaList/UI/Pages/Base/CrudListPageBase.cs`.
  - **Risk:** None functional — the removed branch was unreachable for these pages. Confirm no other CrudListPageBase-derived page exists outside the 4 (grep before edit).
  - **Verification (manual E2E, Helder on emulator):**
    - REQ-HNAV-01 — open each of Venues/Singers/Artists/Songs from the flyout → leading icon is the hamburger, not a back arrow.
    - REQ-HNAV-02 — tap the hamburger → drawer opens.
    - REQ-HNAV-03 — Android hardware back → unchanged (confirm-sheet/search handling, else pops with animation; app does not exit unexpectedly from a list).
    - REQ-HNAV-04 — forward navigation still slides (framework default); no visual regression.
  - **Demo:** "Open Venues from the menu; the AppBar shows the hamburger; tapping it reopens the drawer."
  - **Review lane:** code review (subagent) + Helder emulator observation.
  - **Exit:** build 0 errors → post-edit re-read → `.sln` already registers spec docs → living-spec check → `task-log.md` entry with AC traceability matrix + E2E evidence → `/sln-commit`.

- [ ] **Task 2 — Session-end BACKLOG + spec close-out**
  - Update BACKLOG row (2026-07-11 "Hamburger menu on all hamburger-loaded pages") status; note Shell-native pages deferred to AppBar/SearchAppBar Interaction Redesign (BACKLOG 2026-07-10).
  - Confirm out-of-scope items (Shell-native pages, AppBar-back animation, SearchAppBar swap) are captured in BACKLOG/requirements.
