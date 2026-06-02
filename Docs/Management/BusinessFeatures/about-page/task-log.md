# Task Log — About Page

---
## Task: All phases (1–5)
**Plan:** `Docs/Management/BusinessFeatures/about-page/plan.md`
**Status:** To Review
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
