# Windows Version — Design

> **Status:** 💡 Pending — post-MVP
> **Decision date:** 2026-05-29

## Context

MyVocaList targets karaoke event admins. The goal is to make the app available to Windows users
(admin laptop, future kiosk) without a separate UI project.

**Critical baseline discovery:** `MyVocaList.csproj` already conditionally includes
`net10.0-windows10.0.19041.0` when building on Windows, and `Platforms/Windows/` already has
scaffolded files (App.xaml, App.xaml.cs, app.manifest, Package.appxmanifest).
Windows support exists at the skeleton level — no new project is required.

---

## Decision

**Polish the existing MAUI Windows target. No new project.**

Rationale confirmed with Helder:
- "Mobile app on Windows" UX is acceptable (admin tool, not consumer product)
- Post-MVP timeline — Windows ships after the 2026-06 mobile MVP
- Venue display mode and kiosk self-registration are post-MVP backlog items

---

## Feature Extension Impact

Because there is no second UI project, every feature added to the MAUI codebase is
**automatically available on Windows** with at most 0.1x additional effort for Windows-specific
testing and any platform-conditional tweaks. Services, Domain, and Infra are unchanged.

---

## Architecture Constraints (no changes needed to non-MAUI layers)

| Layer | Change required for Windows? |
|-------|------------------------------|
| Domain | None — net10.0, platform-agnostic |
| Contracts | None |
| Services | INextSingerAlertService may need a Windows no-op (check Plugin.LocalNotification Windows support) |
| Infra | SQLite path check only |
| MyVocaList (MAUI) | Windows-specific styles and layout tweaks in Platforms/Windows/ |

---

## Implementation Phases

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
