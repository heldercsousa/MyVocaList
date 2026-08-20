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

---

## 2026-08-20 — Relicensing follow-through: MIT license text + BUG-077

**Trigger:** Helder relicensed the repository from CC BY-NC-ND 4.0 to MIT (root `LICENSE`/`README.md`,
develop `d3c52f7a`). The shipped About page still rendered the retired licence, and while scoping that
fix Helder reported a second, unrelated defect: the flyout **About** item had never navigated anywhere.

Handled as two separate tracked items, not one bundle:
- Change item `changes/2026-08-20-about-page-license-text-mit/` — AC-AB-05a / AC-AB-05b supersede
  AC-AB-05. The shipped `requirements.md`/`design.md` were **not** edited (immutable history).
- `bugs/2026-08-20-BUG-077-flyout-about-item-does-not-navigate-to-the-about-page/` — Major.

**Lane:** implementor subagent in worktree `../MyVocaList-about-license`, branch
`fix/about-page-license-and-nav` (based on develop). Not ITF — `.xaml` target is excluded by C3.

### Changed files

| File | Why |
|------|-----|
| `MyVocaList/UI/Pages/About/AboutPage.xaml` | License block literals → "MIT License" / "Free to use, modify, and distribute." (AC-AB-05a) |
| `MyVocaList/Navigation/NavigationConfig.cs` | BUG-077 fix — added `[Routes.About] = typeof(AboutPage)` to `PageTypes` |
| `MyVocaList.Tests/Unit/Navigation/NavigationConfigTests.cs` *(new)* | BUG-077 regression test — Red before, Green after; guards the whole failure class |

### Root cause (BUG-077)

`NavigationConfig.PageTypes` was missing the `Routes.About` key. `AppShellViewModel.NavigateAsync`
does `if (!PageTypes.TryGetValue(baseRoute, out var pageType)) return;` — an unknown route produces no
exception, no log, no navigation. Full analysis + follow-ups: the BUG-077 `README.md`.

### AC traceability

| AC | Criterion | Implementation | Test |
|----|-----------|----------------|------|
| AC-AB-05a | License section shows "MIT License" + "Free to use, modify, and distribute." | `AboutPage.xaml` License block | Manual E2E (XAML literal, no testable seam — `testing.md` Level C) |
| AC-AB-05b | No retired licence text anywhere on the page | `AboutPage.xaml` | `grep -rn "CC BY-NC-ND\|NonCommercial" --include=*.xaml --include=*.cs` → no matches |
| BUG-077 | Flyout About item navigates to the About page | `NavigationConfig.cs` `PageTypes` | `NavigationConfigTests` (2 tests, Red→Green) |

### Verification evidence

Re-run by the orchestrator on the branch before merging — not taken on the implementor's report alone:
- `dotnet build MyVocaList.sln -c Debug` → **8 projects, 0 errors**, 7 warnings.
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj --no-restore` → **592/592 passed**, 0 failed.
- Merged to develop as `12102115` (`--no-ff`, both parents verified).

**Outstanding gate — Helder, manual E2E:** flyout → **About** → page opens; License section reads
"MIT License" / "Free to use, modify, and distribute." / "© 2025 Helder Sousa".

### Process note

The 2026-06-25 review verdict above records navigation as passing for this feature, and the feature was
closed ✅ Done — yet the page was unreachable from its only entry point. The gap was structural, not a
reviewer slip: the review checked that `AppShell.xaml` and DI registered the page, but nothing checked
the third table (`NavigationConfig.PageTypes`) that `NavigateAsync` actually reads, and the miss branch
is silent. The new class-wide test closes that specific hole.
