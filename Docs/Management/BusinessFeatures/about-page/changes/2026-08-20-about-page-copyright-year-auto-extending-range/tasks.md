# About Page Copyright Year — auto-extending range · Tasks

- [ ] **T1 — Copyright range formatter + binding**
  - **Produces:** auto-extending copyright line (AC-AB-05c … AC-AB-05f)
  - **Consumes:** `AppConstants.FoundedYear` (already exists, drives `Since`)
  - **Risk:** Low — one display string, pure formatting logic
  - **Files owned:** `MyVocaList/UI/ViewModels/AboutViewModel.cs`,
    `MyVocaList/UI/Pages/About/AboutPage.xaml`,
    `MyVocaList.Tests/Unit/ViewModels/AboutViewModelTests.cs`
  - **Demo:** About page shows `© 2025–2026 Helder Sousa`; `Since 2025` unchanged
  - **Review lane:** unit tests (Level B, TDD — tests first) + manual E2E for the rendered page
