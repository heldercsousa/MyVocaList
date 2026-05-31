# Tasks — About Page

> **Prerequisite:** All What's New tasks must be committed before Phase 1 begins.
> `IWhatsNewService`, `ReleaseEntry`, and `WhatsNewService` are consumed here but owned by that feature.

---

## Phase 1 — Service extension (depends on: What's New ✅)

- [ ] **Add `GetCurrentReleaseAsync` to `IWhatsNewService` + implement in `WhatsNewService`** [SEQUENTIAL]
  - **Produces:** new method on `IWhatsNewService`; implementation in `WhatsNewService`; unit tests for the new method
  - **Consumes:** `IWhatsNewService.cs`, `WhatsNewService.cs`, `ReleaseEntry` DTO
  - **Risk:** Low — additive change; no existing method is modified
  - **Files owned:** `MyVocaList.Domain/ServicesInterfaces/IWhatsNewService.cs`, `MyVocaList.Services/WhatsNewService.cs`, `MyVocaList.Tests/Unit/Services/WhatsNewServiceTests.cs`
  - **Demo:** `dotnet test` passes including new tests for `GetCurrentReleaseAsync` (returns entry for matching version, returns null for unmatched version, returns null on malformed JSON)

---

## Phase 2 — Contracts constant (no dependencies)

- [ ] **Add `AppConstants.FoundedYear = 2025`** [P]
  - **Produces:** `FoundedYear` constant in `MyVocaList.Contracts`
  - **Consumes:** nothing
  - **Risk:** Low
  - **Files owned:** `MyVocaList.Contracts/AppConstants.cs`
  - **Demo:** Constant compiles and is accessible from the MAUI project

---

## Phase 3 — Navigation wiring (depends on: Phase 1 ✅)

- [ ] **Add About route to navigation config** [SEQUENTIAL]
  - **Produces:** `Routes.About`, menu entry in System group, Shell route registration
  - **Consumes:** `Routes.cs`, `NavigationConfig.cs`, `AppShell.xaml`
  - **Risk:** Low — additive; System group order must place About before Exit
  - **Files owned:** `MyVocaList/Navigation/Routes.cs`, `MyVocaList/Navigation/NavigationConfig.cs`, `MyVocaList/AppShell.xaml`
  - **Demo:** "About" item appears in the flyout System group; tapping it does not crash (page may be empty at this stage)

---

## Phase 4 — ViewModel + Page (depends on: Phase 1 ✅, Phase 2 ✅, Phase 3 ✅)

- [ ] **Implement `AboutViewModel`** [SEQUENTIAL]
  - **Produces:** `AboutViewModel.cs`
  - **Consumes:** `IWhatsNewService`, `AppInfo`, `AppConstants.FoundedYear`
  - **Risk:** Low
  - **Files owned:** `MyVocaList/UI/ViewModels/AboutViewModel.cs`
  - **Demo:** `AboutViewModel.InitializeAsync()` populates `Version` and `CurrentRelease` (or null)

- [ ] **Implement `AboutPage` XAML + code-behind + DI** [SEQUENTIAL — after ViewModel]
  - **Produces:** `AboutPage.xaml`, `AboutPage.xaml.cs`; DI registration in `MauiProgram.cs`
  - **Consumes:** `AboutViewModel`, all decisions in `design.md § Page Structure`
  - **Risk:** Medium — XAML layout correctness; `SafeAreaEdges="Container"` must be present
  - **Files owned:** `MyVocaList/UI/Pages/About/AboutPage.xaml`, `MyVocaList/UI/Pages/About/AboutPage.xaml.cs`, `MyVocaList/MauiProgram.cs`
  - **Demo:** About page opens from flyout, shows version in AppBar, logo, name, "Since 2025", license section, and (if `releases.json` has current version entry) What's New section. If no entry: What's New section is hidden.

---

## Phase 5 — .sln registration (after all files created)

- [ ] **Register all new files in `MyVocaList.sln`** [SEQUENTIAL]
  - **Files owned:** `MyVocaList.sln`
  - **Demo:** All new files visible in Visual Studio Solution Explorer under the appropriate solution folders
