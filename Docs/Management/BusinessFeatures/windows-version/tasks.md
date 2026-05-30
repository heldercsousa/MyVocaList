# Windows Version — Tasks

> **Design:** `Docs/Management/BusinessFeatures/windows-version/design.md`
> **Status:** 💡 Pending — post-MVP (after 2026-06 mobile MVP)

---

## Phase 1 — Baseline (make it run without crashes)

- [ ] **Verify Windows entry point** [P]
  - **Produces:** Verified `Platforms/Windows/App.xaml.cs` entry point — no code changes expected
  - **Consumes:** nothing
  - **Risk:** Low — entry point is already scaffolded
  - **Files owned:** `MyVocaList/Platforms/Windows/App.xaml.cs` (read-only verify; edit only if broken)
  - **Demo:** `dotnet build MyVocaList/MyVocaList.csproj -f net10.0-windows10.0.19041.0` completes with 0 errors

- [ ] **Confirm SQLite path on Windows** [P]
  - **Produces:** Verified or fixed `FileSystem.AppDataDirectory` resolves to `AppData\Local` on Windows
  - **Consumes:** nothing
  - **Risk:** Low — `FileSystem.AppDataDirectory` is a MAUI abstraction; SQLite path should work out of the box
  - **Files owned:** `MyVocaList/Infra/` (conditional path fix only if needed)
  - **Demo:** App launches on Windows; database file is created under `%LOCALAPPDATA%\com.myvocalist\`

- [ ] **Confirm IOverlayService no-op on Windows** [P]
  - **Produces:** Verified or registered `NoOpOverlayService` for the Windows platform (same pattern as iOS)
  - **Consumes:** nothing
  - **Risk:** Low — no-op pattern already exists for iOS; Windows follows the same path
  - **Files owned:** `MyVocaList/MauiProgram.cs`, `MyVocaList/Platforms/Windows/` (only if registration is missing)
  - **Demo:** App launches on Windows without overlay-related exceptions

- [ ] **Confirm Plugin.LocalNotification Windows support** [SEQUENTIAL]
  - **Produces:** Either confirmed Windows support or a registered `NoOpNextSingerAlertService` for Windows
  - **Consumes:** IOverlayService no-op confirmation (preceding task)
  - **Risk:** Medium — Plugin.LocalNotification Windows support is uncertain per design.md; may require a no-op registration
  - **Files owned:** `MyVocaList/MauiProgram.cs`, `MyVocaList/Services/` (no-op only if needed)
  - **Demo:** App launches on Windows without notification plugin exceptions; no-op registered if unsupported

- [ ] **Walk every registered route — document crashes and layout breaks** [SEQUENTIAL]
  - **Produces:** `Docs/Management/BusinessFeatures/windows-version/findings.md` listing all routes, crash status, and layout issues
  - **Consumes:** All preceding Phase 1 tasks (app must launch without crashes first)
  - **Risk:** Medium — unknown layout breakage until all routes are navigated
  - **Files owned:** `Docs/Management/BusinessFeatures/windows-version/findings.md` (new file)
  - **Demo:** findings.md lists every AppShell route with pass/fail status and notes for each

- [ ] **Fix DevExpress control sizing for 1080p** [SEQUENTIAL]
  - **Produces:** Windows-specific styles in `Platforms/Windows/` that correct oversized 48dp touch targets at 1080p
  - **Consumes:** Route walk findings (must know which pages have sizing issues)
  - **Risk:** Medium — requires Windows-specific style overrides; must not affect mobile styles
  - **Files owned:** `MyVocaList/Platforms/Windows/` (new styles file or existing Resources/Styles/)
  - **Demo:** All pages display proportionally correct controls at 1080p resolution on Windows

---

## Phase 2 — UX Tuning (make it usable on desktop)

> **Prerequisite:** Phase 1 complete — app runs without crashes on Windows.

- [ ] **Keyboard navigation** [P]
  - **Produces:** Tab, Enter, and Escape keyboard navigation working in dialogs and lists
  - **Consumes:** Phase 1 complete
  - **Risk:** Medium — DevExpress controls may handle some keys natively; gaps require custom handlers
  - **Files owned:** `MyVocaList/Platforms/Windows/`, relevant page code-behind files
  - **Demo:** Admin can navigate the queue list with Tab, confirm with Enter, dismiss BottomSheet with Escape

- [ ] **Mouse hover states on interactive elements** [P]
  - **Produces:** Visible hover state on buttons, list items, and FAB on Windows
  - **Consumes:** Phase 1 complete
  - **Risk:** Low — DevExpress controls likely include hover states; verify and tweak if missing
  - **Files owned:** `MyVocaList/Platforms/Windows/` (Windows-specific style overrides only)
  - **Demo:** Hovering over any tappable element on Windows shows a visual state change

- [ ] **Flyout navigation width tuning for wide screens** [P]
  - **Produces:** AppShell flyout constrained to a sensible width (e.g. 280–320dp) on wide screens
  - **Consumes:** Phase 1 complete
  - **Risk:** Low — MAUI Shell flyout width is configurable
  - **Files owned:** `MyVocaList/AppShell.xaml`, `MyVocaList/Platforms/Windows/`
  - **Demo:** Flyout opens at a proportional width on a 1920×1080 window; does not stretch full-width

- [ ] **Window minimum size constraint** [P]
  - **Produces:** `Package.appxmanifest` updated with a minimum window size (e.g. 800×600)
  - **Consumes:** Phase 1 complete
  - **Risk:** Low — declarative manifest change; no code required
  - **Files owned:** `MyVocaList/Platforms/Windows/Package.appxmanifest`
  - **Demo:** Attempting to resize the app window below the minimum snaps back to the minimum size

- [ ] **Titlebar / system chrome integration check** [SEQUENTIAL]
  - **Produces:** Verified or fixed titlebar showing correct app name and icon; no visual overlap with content
  - **Consumes:** All other Phase 2 tasks (final visual pass)
  - **Risk:** Low — MAUI handles titlebar integration; verify `app.manifest` app name is correct
  - **Files owned:** `MyVocaList/Platforms/Windows/app.manifest`, `MyVocaList/Platforms/Windows/Package.appxmanifest`
  - **Demo:** App titlebar shows "MyVocaList" with the correct icon; content starts below the titlebar

---

## Phase 3 — Packaging

> **Prerequisite:** Phase 2 complete — app is usable on desktop.
> **Note:** Steps 2 and 3 require Helder's input before implementation.

- [ ] **MSIX packaging verification** [P]
  - **Produces:** Confirmed MSIX build via `Package.appxmanifest` (already scaffolded); build command documented
  - **Consumes:** Phase 2 complete
  - **Risk:** Low — scaffolding already exists; may need minor manifest fixes
  - **Files owned:** `MyVocaList/Platforms/Windows/Package.appxmanifest`
  - **Demo:** `dotnet publish -f net10.0-windows10.0.19041.0 -c Release` produces a valid `.msix` file

- [ ] **Certificate decision** [SEQUENTIAL — requires Helder input]
  - **Produces:** Decision recorded in design.md: self-signed or store-signed certificate
  - **Consumes:** MSIX packaging verification
  - **Risk:** Medium — store signing requires Microsoft account and review; self-signing limits distribution
  - **Files owned:** `Docs/Management/BusinessFeatures/windows-version/design.md`
  - **Demo:** design.md updated with certificate decision and rationale

- [ ] **Distribution decision** [SEQUENTIAL — requires Helder input]
  - **Produces:** Decision recorded in design.md: sideload vs Microsoft Store distribution
  - **Consumes:** Certificate decision
  - **Risk:** Medium — Microsoft Store submission has a lead time and review process
  - **Files owned:** `Docs/Management/BusinessFeatures/windows-version/design.md`
  - **Demo:** design.md updated with distribution decision and next steps
