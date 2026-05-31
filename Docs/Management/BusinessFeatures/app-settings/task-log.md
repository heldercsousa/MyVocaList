---
## Task: Stale HasYouTubeApiKey Fix
**Plan:** Docs/Management/BusinessFeatures/app-settings/plan.md
**Status:** To Review
**Started:** 05/31/2026
**Completed:** 05/31/2026

### Changed files:
- `MyVocaList/UI/ViewModels/SongFormViewModel.cs` — added `RefreshApiKeyFlagAsync()` public method
- `MyVocaList/UI/Pages/Songs/SongFormPage.xaml.cs` — extended `OnAppearing` to call `RefreshApiKeyFlagAsync()`

### Verification evidence
- Build: PASS (pre-existing errors on develop unrelated to this change; confirmed by stash test)
- Tests: PASS (195 tests, 0 failures)
- Post-edit re-read: confirmed
- Spec compliance: confirmed — design.md § Stale HasYouTubeApiKey checked
