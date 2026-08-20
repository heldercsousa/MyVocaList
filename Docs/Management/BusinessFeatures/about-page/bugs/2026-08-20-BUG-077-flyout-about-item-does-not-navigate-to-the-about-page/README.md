---
id: BUG-077
title: Flyout About item does not navigate to the About page
status: ✅ Fixed
severity: Major
target: 2026-08-20
section: BusinessFeatures
parent: about-page
goal: Tapping About in the flyout leaves the user on the current page; the About page has never been reachable in the shipped app.
gate: Closed. Helder confirmed the manual E2E passed on device 2026-08-20; flyout About navigates and the page renders.
closed: 2026-08
kind: bug
---

# Flyout About item does not navigate to the About page

Tapping About in the flyout leaves the user on the current page; the About page has never been reachable in the shipped app.


## Report

Reported by Helder 2026-08-20: tapping the **About** entry in the hamburger/flyout does not open the
About page — the user stays on the current page. Per the report the About page has **never** been
reachable in the shipped app, so the About feature's manual E2E was presumably never exercised end to end.

## Severity rationale

**Major** — a shipped feature is entirely unusable with no workaround (there is no other entry point to
the About page). No data loss or crash, so not Critical.

## Scope

Diagnosis and fix live in `AppShell` / flyout navigation wiring. The page content itself is unaffected;
the concurrent change `../../changes/2026-08-20-about-page-license-text-mit/` updates its license text.

## Regression test

Major → mandatory where testable. If the root cause sits in testable non-UI code, a failing test comes
first. If it is pure XAML/Shell wiring with no testable seam, manual E2E is recorded here instead
(`bug-tracking.md`).

## Root cause

`MyVocaList/Navigation/NavigationConfig.cs` — the `PageTypes` dictionary had **no entry for
`Routes.About`**. Everything else was wired correctly: `AboutPage` was registered in `AppShell.xaml`
(`FlyoutItem Route="about"`) and in DI (`MauiProgram.cs`), and `Routes.About` was present in
`NavigationConfig.BuildMenuGroups`, so the flyout item rendered and was tappable.

The defect is a **silent-failure design**: `AppShellViewModel.NavigateAsync` resolves the target via

```csharp
if (!NavigationConfig.PageTypes.TryGetValue(baseRoute, out var pageType)) return;
```

A missing key produces no exception, no log line, and no navigation — the tap is simply swallowed.
That is why the defect survived to production unnoticed.

## Fix

Added `[Routes.About] = typeof(AboutPage)` to `NavigationConfig.PageTypes` (one line).

## Regression test

`MyVocaList.Tests/Unit/Navigation/NavigationConfigTests.cs` (new) — **confirmed Red before the fix,
Green after** (2/2 failed → 2/2 passed):

1. `PageTypes_ContainsEntryForAboutRoute` — the specific defect.
2. `PageTypes_ContainsEntryForEveryMenuRoute_ExceptSpecialCasedRoutes` — enumerates
   `BuildMenuGroups`, excludes the two routes special-cased in `NavigateAsync` (`Queue`, `Exit`), and
   asserts every remaining menu route resolves. This guards the **whole failure class**, so any future
   menu item added without a `PageTypes` entry fails at test time instead of silently at runtime.

## Verification

- `dotnet build MyVocaList.sln -c Debug` → 8 projects, **0 errors** (verified by the orchestrator, not
  only reported by the implementor).
- `dotnet test MyVocaList.Tests/MyVocaList.Tests.csproj` → **592/592 passed**.
- Manual E2E — **PASSED (Helder, 2026-08-20)**: launch app → open hamburger flyout → tap "About" under the
  System group → About page opens and the License section reads "MIT License" /
  "Free to use, modify, and distribute." / "© 2025 Helder Sousa".

## Status

Fixed on `fix/about-page-license-and-nav` (`5b4d1954`), merged to develop as `12102115`.
Automated verification complete and Helder's manual E2E passed on device 2026-08-20. **Closed.**

## Follow-up observations (not fixed here)

1. **Silent navigation failure is the real hazard.** `NavigateAsync` swallowing an unknown route is
   what turned a one-line omission into a permanently unreachable page. The regression test now
   catches it at build time, but logging a warning in the `TryGetValue` miss branch would make the
   next occurrence self-diagnosing at runtime too. Not done here — out of scope for a Major bug fix,
   worth its own task.
2. **The About feature's manual E2E gate was evidently never exercised** — the page was unreachable
   from the only entry point that exists, yet the feature was closed ✅ Done. Worth reviewing how
   UI-only verification gates get signed off.
3. **`hamburger-nav-pattern` has no spec folder.** The briefing pointed the implementor at
   `Docs/Management/BusinessFeatures/hamburger-nav-pattern/`; it does not exist (the `.sln` carries a
   solution-folder GUID for that name, but no `Docs/` folder). The fix therefore followed the existing
   `Routes`/`NavigationConfig`/`AppShellViewModel` code pattern. Spec gap — unregistered.
