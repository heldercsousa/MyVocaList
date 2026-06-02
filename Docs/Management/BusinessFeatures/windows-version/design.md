# Windows Version — Design

> **Status:** 🔴 Blocked — DevExpress MAUI has no Windows support
> **Decision date:** 2026-05-29
> **Updated:** 2026-05-30

## Context

MyVocaList targets karaoke event admins. The goal is to make the app available to Windows users
(admin laptop, future kiosk) without a separate UI project.

**Critical baseline discovery:** `MyVocaList.csproj` already conditionally includes
`net10.0-windows10.0.19041.0` when building on Windows, and `Platforms/Windows/` already has
scaffolded files (App.xaml, App.xaml.cs, app.manifest, Package.appxmanifest).
Windows support exists at the skeleton level — the project compiles.

---

## Blocker — DevExpress MAUI Does Not Support Windows

**DevExpress MAUI controls have no Windows renderer.** This applies to every control the app uses:
DXCollectionView, BottomSheet, TextEdit, FilterChipGroup, AppBar, and all others.

The MAUI project compiles for `net10.0-windows10.0.19041.0`, but at runtime every DevExpress control
either throws or renders nothing. The app is not functional on Windows in its current form.

Because DevExpress is a **constitutional constraint** in this project (UI Component Priority rule —
unamendable), replacing DX controls is not an option without an architecture review.

**This feature is blocked until DevExpress announces Windows support for their MAUI controls.**
No implementation work should begin before that announcement.

---

## Effort Analysis — Alternative Paths

All three paths below were evaluated and rejected for now.

### Option A — Wait for DevExpress MAUI Windows support (preferred path)
- **Effort:** Near-zero — existing skeleton + phases below remain valid
- **Risk:** Timeline unknown; DevExpress has not committed to a Windows target date
- **Outcome:** Once DX ships Windows renderers, Phase 1 becomes a real 1–2 day effort

### Option B — Replace DevExpress with stock MAUI controls for Windows
- **Effort:** Extremely high — every page, every dialog, every list must be duplicated or conditionally compiled
- **Risk:** Violates the DevExpress-first constitutional constraint; creates a parallel UI codebase to maintain indefinitely
- **Outcome:** Rejected

### Option C — Add a separate Windows WinUI/WPF project
- **Effort:** Very high — entirely new UI layer; all pages rebuilt from scratch in a different framework
- **Risk:** No code sharing at the UI layer; Services/Domain/Infra are reusable but the UI is the majority of the work
- **Outcome:** Rejected

### Option D — Add a web front-end (Blazor or minimal API + SPA)
- **Effort:** Very high — new frontend project + Web API project + middleware layer (auth, routing, error handling) to follow modern .NET patterns; no MAUI code reused at the UI layer
- **Risk:** Introduces a separate tech stack, deployment model, and security surface; Services/Domain/Infra are reusable but the scaffolding cost is high
- **Outcome:** Rejected for now; could be revisited if the app expands to a multi-user web product

---

## Decision (updated 2026-05-30)

**No implementation work until DevExpress MAUI announces Windows support.**

The lowest-effort path to a real Windows version is Option A. All other options introduce
more effort than the Windows target justifies for an admin-only tool.

Re-evaluate when:
- DevExpress publishes a Windows-compatible MAUI release (monitor devexpress.com/maui release notes)
- Or the product scope changes to require a web interface for multi-user access

---

## Original Plan (valid once blocker is resolved)

The phases below were designed for polishing the existing MAUI Windows target. They remain
the implementation plan once DevExpress Windows support ships.

### Phase 1 — Baseline (make it run without crashes)

| Step | Status | Notes |
|------|--------|-------|
| Verify `Platforms/Windows/App.xaml.cs` entry point | `[ ]` | |
| Confirm SQLite path on Windows (`FileSystem.AppDataDirectory` → AppData\Local) | `[ ]` | |
| Confirm `IOverlayService` → `NoOpOverlayService` on Windows | `[ ]` | Same as iOS path |
| Confirm `Plugin.LocalNotification` Windows support (or register no-op) | `[ ]` | |
| Walk every registered route — document crashes and layout breaks | `[ ]` | |
| Fix DevExpress control sizing for 1080p (48dp touch targets look large) | `[ ]` | Windows-specific styles in Platforms/Windows/ |

### Phase 2 — UX Tuning (make it usable on desktop)

| Step | Status | Notes |
|------|--------|-------|
| Keyboard navigation (Tab, Enter, Escape in dialogs and lists) | `[ ]` | |
| Mouse hover states on interactive elements | `[ ]` | |
| Flyout navigation width tuning for wide screens | `[ ]` | |
| Window minimum size constraint in Package.appxmanifest | `[ ]` | |
| Titlebar / system chrome integration check | `[ ]` | |

### Phase 3 — Packaging

| Step | Status | Notes |
|------|--------|-------|
| MSIX packaging via Package.appxmanifest (already scaffolded) | `[ ]` | |
| Self-signed or store-signed certificate decision | `[ ]` | Requires Helder input |
| Sideload vs Microsoft Store distribution decision | `[ ]` | Requires Helder input |

---

## Post-MVP Windows Features (separate BACKLOG entries)

1. **Venue Display Mode** — full-screen view showing current singer, upcoming queue, song info.
   Shared MAUI page triggered from queue management. Works on Windows (second monitor) and mobile.

2. **Kiosk / Singer Self-Registration** — locked-down page for singer self-registration.
   Windows or tablet. Depends on "Singer self-registration" business feature.

---

## Verification (when Phase 1 begins)

1. `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-windows10.0.19041.0` — 0 errors
2. Run on Windows dev machine — walk every registered route
3. `dotnet test` — 0 failures (no test changes expected)
