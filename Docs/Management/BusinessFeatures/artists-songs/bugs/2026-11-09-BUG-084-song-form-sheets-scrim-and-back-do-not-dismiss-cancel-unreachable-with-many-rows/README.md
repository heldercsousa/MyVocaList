---
id: BUG-084
title: "Song form sheets: scrim and Back do not dismiss; Cancel unreachable with many rows"
status: 🟡 In Progress
severity: Major
target: 2026-11-09
section: BusinessFeatures
parent: artists-songs
goal: Song form sheets dismiss on scrim and system Back saving nothing, and Cancel stays reachable by scrolling when the list is long.
gate: "Device retest: a long duplicate list scrolls to Cancel; Back and scrim dismiss and save nothing."
kind: bug
---

# Song form sheets: scrim and Back do not dismiss; Cancel unreachable with many rows

Song form sheets dismiss on scrim and system Back saving nothing, and Cancel stays reachable by scrolling when the list is long.

**Found:** device run 2026-11-09 (Samsung S23). Scrim tap and the system Back key did not dismiss the Song form sheets (only Cancel did); with several candidate rows the Cancel button was clipped and unreachable.

**Root cause:** both sheets declared `AllowDismiss="False"`, `AllowedState="HalfExpanded"`, a 0.55 height ratio and one unbounded `VerticalStackLayout` with no scrolling, Cancel last. `OnDisappearing` also unsubscribed the ViewModel handler with no resubscribe.

**Fix (merged `aa1af4da`, 2026-11-09; Verifier conditional pass, no blockers):** `AllowDismiss="True"`, `AllowedState="All"`, ratio 0.7, content `Grid *,Auto` with a ScrollView in row 0 and Cancel pinned in row 1; Hidden events route through the same Dismiss commands as Cancel; `OnAppearing` resubscribes (detach first).

**Device retest still open (status stays In Progress until it passes):** Back key and scrim dismiss both sheets and save nothing; swipe-down too; long Possible duplicates list scrolls and Cancel stays visible; the sheet opens again after being dismissed (a second Save re-opens it); leave the page and return, then Save, and the sheet still opens; sheet size looks right; no sheet closes by itself right after opening.
