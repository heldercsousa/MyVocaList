# Task Log — Hamburger on CRUD list pages

## Task 1 — Always-hamburger in CrudListPageBase.OnNavigatedTo

### Changed files
- `MyVocaList/UI/Pages/Base/CrudListPageBase.cs` — OnNavigatedTo: removed NavigationStack.Count<=1 conditional and dead back-arrow branch; unconditional hamburger assignment.

### AC traceability matrix

| AC ID | Criterion | Implementation location | Test method |
|---|---|---|---|
| REQ-HNAV-01 | Leading icon is hamburger, never back arrow, on all 4 CRUD list pages | `CrudListPageBase.cs` OnNavigatedTo (unconditional `vm.AppBarNavigationIcon = "menu"`) | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-02 | Tapping hamburger opens the drawer | `CrudListPageBase.cs` OnNavigatedTo (`AppBarNavigationCommand` → `Shell.Current.FlyoutIsPresented = true`) | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-03 | Hardware back behavior unchanged | Untouched: `OnBackButtonPressed` + `Shell.BackButtonBehavior` on each page | Manual E2E — Android emulator, Helder observation |
| REQ-HNAV-04 | No hand-written animation; forward slide unchanged | Untouched: `AppShellViewModel.NavigateAsync` PushAsync path | Manual E2E — Android emulator, Helder observation |

### Verification evidence
[Fill in after Step 5: pass/fail per page per check, any screenshots/notes from Helder's emulator observation.]
