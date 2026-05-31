# Tasks — About Page

> **No prerequisite on What's New.** The About page ships with a `NullWhatsNewService` stub.
> The What's New section is hidden until that feature is implemented separately.

---

## Phase 1 — Contracts & Domain (no dependencies)

- [ ] **Define `ReleaseEntry` DTO and `IWhatsNewService` interface** [P]
  - **Produces:** `ReleaseEntry` record in `MyVocaList.Contracts/DTOs/`; `IWhatsNewService` interface in `MyVocaList.Domain/ServicesInterfaces/`
  - **Consumes:** nothing
  - **Risk:** Low — new types only, no existing code touched
  - **Files owned:** `MyVocaList.Contracts/DTOs/ReleaseEntry.cs`, `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs`
  - **Demo:** Both types compile; `ReleaseEntry` has Version, Date, Highlights, Fixes properties

- [ ] **Add `AppConstants.FoundedYear = 2025`** [P]
  - **Produces:** `FoundedYear` constant in `MyVocaList.Contracts`
  - **Consumes:** nothing
  - **Risk:** Low
  - **Files owned:** `MyVocaList.Contracts/AppConstants.cs`
  - **Demo:** Constant compiles and is accessible from MAUI project

---

## Phase 2 — Stub service + ViewModel (depends on: Phase 1 ✅)

- [ ] **Implement `NullWhatsNewService` stub and `AboutViewModel`** [SEQUENTIAL]
  - **Produces:** `NullWhatsNewService` (internal, in Services project); `AboutViewModel.cs`
  - **Consumes:** `IWhatsNewService`, `ReleaseEntry`, `AppConstants.FoundedYear`, `AppInfo`
  - **Risk:** Low
  - **Files owned:** `MyVocaList.Services/NullWhatsNewService.cs`, `MyVocaList/UI/ViewModels/AboutViewModel.cs`
  - **Demo:** `AboutViewModel.InitializeAsync()` returns with `CurrentRelease = null`, `Version = "v1.0.0"`, `Since = "Since 2025"`, `HasReleaseNotes = false`

---

## Phase 3 — Navigation wiring (depends on: Phase 1 ✅)

- [ ] **Add About route to navigation config** [SEQUENTIAL]
  - **Produces:** `Routes.About` constant; "About" menu entry in System group (before Exit); Shell route registration
  - **Consumes:** `Routes.cs`, `NavigationConfig.cs`, `AppShell.xaml`
  - **Risk:** Low — additive; order in System group must be: Preferences → Backup & Restore → About → Exit
  - **Files owned:** `MyVocaList/Navigation/Routes.cs`, `MyVocaList/Navigation/NavigationConfig.cs`, `MyVocaList/AppShell.xaml`
  - **Demo:** "About" item appears in flyout System group in correct position

---

## Phase 4 — Page XAML + DI (depends on: Phase 2 ✅, Phase 3 ✅)

- [ ] **Implement `AboutPage` XAML, code-behind, and DI registration** [SEQUENTIAL]
  - **Produces:** `AboutPage.xaml`, `AboutPage.xaml.cs`; DI entries in `MauiProgram.cs`
  - **Consumes:** `AboutViewModel`, design.md § Page Structure
  - **Risk:** Medium — XAML layout; `SafeAreaEdges="Container"` required; What's New section must be hidden when `HasReleaseNotes = false`
  - **Files owned:** `MyVocaList/UI/Pages/About/AboutPage.xaml`, `MyVocaList/UI/Pages/About/AboutPage.xaml.cs`, `MyVocaList/MauiProgram.cs`
  - **Demo:** About page opens from flyout; shows version in AppBar (`v1.0.0`), app logo, "MyVocaList", goal sentence, "Since 2025", License section with CC BY-NC-ND 4.0 and copyright; What's New section is absent (stub returns null)

---

## Phase 5 — .sln registration (after all files created)

- [ ] **Register all new files in `MyVocaList.sln`** [SEQUENTIAL]
  - **Files owned:** `MyVocaList.sln`
  - **Demo:** All new files visible in Visual Studio Solution Explorer under the correct solution folders

---

## Future (when What's New is implemented)

- Replace `NullWhatsNewService` registration with `WhatsNewService` in `MauiProgram.cs`
- Add `GetPendingReleaseAsync` to `IWhatsNewService` at that point
- `GetCurrentReleaseAsync` implementation moves to the real service
- About page: no changes required
