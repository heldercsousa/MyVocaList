# Spike Findings — Shell-native `//route` vs. animation preservation

**Date:** 2026-07-11 · **Branch (throwaway):** `worktree-spike+hamburger-nav-animation` (commit `9b07d8d`, not merged)
**Question:** Can adopting MAUI Shell's built-in section navigation (`GoToAsync("//route")`) fix the leading-icon logic **while preserving** the mandatory page-slide animations?

## Hypothesis

Migrating flyout navigation from `PushAsync` to absolute `//route` would reset the navigation stack for every top-level destination, making each menu page a section root. Shell would then render the hamburger natively (root + `FlyoutBehavior=Flyout`) and the `CrudListPageBase` `NavigationStack.Count <= 1` heuristic would evaluate correctly — all for "free," with no custom code.

## Method

Single-file change to `AppShellViewModel.NavigateAsync`: every non-Exit destination routed through `await Shell.Current.GoToAsync("//" + route, animate: true)`. Built clean (`net10.0-android`, 0 errors). Deployed to the Android emulator; Helder observed the transitions (only a real device reveals them).

## Result — FAIL

| Behavior | Current (`PushAsync`, develop) | Spike (`//route`) |
|---|---|---|
| Menu tap → forward page slide | ✅ animates | ❌ **no slide** (section switch does not animate) |
| Android hardware back from a CRUD list | ✅ animates, pops correctly | ❌ **exits the app** (section root has an empty back-stack) |
| Leading icon correctness | ❌ misfires (the bug) | ✅ correct |
| AppBar hamburger tap | n/a | ❌ **dead** (nothing happens) |

Shell-native section switching fixes the **one** thing (icon) by breaking the **two** hard requirements: the forward slide **and** working hardware back. Because Helder's constraint is that **animations must be OS/framework-driven and never hand-written** (UI-thread concurrency risk — see BACKLOG "Shell navigation swallows button tap animations"), the only way to restore the slide under `//route` — a custom Shell transition — is itself prohibited.

## Decision

**Option A / `//route` is rejected.** Proceed with **B′**: keep the framework-animated `PushAsync` (forward slide + OS-animated hardware back already work, untouched) and fix **only** the leading-icon determination. See `design.md`.

Spike code is throwaway and remains isolated on its branch; `develop` is untouched.
