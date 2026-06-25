# Task Log — About Page

---
## Task: All phases (1–5)
**Plan:** `Docs/Management/BusinessFeatures/about-page/plan.md`
**Status:** Reviewed — PASS-WITH-MINOR
**Started:** 2026-06-01
**Completed:** 2026-06-01

### Changed files:
- `Contracts/DTOs/ReleaseEntry.cs` — new DTO
- `Contracts/AppConstants.cs` — added FoundedYear = 2025
- `Domain/ServicesInterfaces/IWhatsNewService.cs` — new interface
- `Services/NullWhatsNewService.cs` — stub implementation
- `MyVocaList/UI/ViewModels/AboutViewModel.cs` — ViewModel
- `MyVocaList/UI/Pages/About/AboutPage.xaml` — page layout
- `MyVocaList/UI/Pages/About/AboutPage.xaml.cs` — code-behind
- `MyVocaList/Navigation/Routes.cs` — added About route
- `MyVocaList/Navigation/NavigationConfig.cs` — added About menu entry
- `MyVocaList/AppShell.xaml` — registered About shell route
- `MyVocaList/MauiProgram.cs` — DI: IWhatsNewService, AboutViewModel, AboutPage
- `MyVocaList.Tests/Unit/Services/NullWhatsNewServiceTests.cs` — unit tests
- `MyVocaList.sln` — doc files registered

### Verification evidence
- Build: PASS (0 errors)
- Tests: PASS (207 tests)
- Post-edit re-read: confirmed
- Spec compliance: confirmed — design.md page structure, ViewModel interface, navigation, DI all match

### Review verdict (2026-06-25, per-task review loop)
**PASS-WITH-MINOR.** Code, DI, navigation, SafeAreaEdges, English-only, and the .sln gate all pass; business logic stays out of the ViewModel.
- **Open item before ship (verify on emulator):** `AboutPage.xaml:18` uses `Source="appicon.png"`, but `appicon` is registered as `<MauiIcon>` (launcher icon), not a runtime `<MauiImage>` under `Resources/Images/`. The logo will compile and pass unit tests but likely renders **blank** at runtime → risks failing AC-AB-02. If blank, add a PNG logo to `Resources/Images/` and point `Source` at it.
- **Minor:** `MauiProgram.cs:117` registers the real `WhatsNewService` (correct — the feature shipped), leaving `Services/NullWhatsNewService.cs` as dead code still described as active in design.md. Reconcile the spec note; consider removing the orphaned stub. (Real path is covered by `WhatsNewServiceTests`.)
