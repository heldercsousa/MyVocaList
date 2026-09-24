---
id: BUG-081
title: Song form edit mode — clear (X) icon disabled on locked artist, artist cannot be replaced
status: 🟡 In Progress
severity: Major
target: 2026-09-23
parent: artists-songs
goal: "Regression of REQ-ACREATE-15: in edit mode the locked Artist field renders its clear icon disabled, so the only unlock affordance is gone and a saved song's artist cannot be changed."
gate: Clear icon unlocks and clears the field in edit mode; VM-seam regression test Red then Green; Helder re-verifies on device.
kind: bug
---

# Song form edit mode — clear (X) icon disabled on locked artist, artist cannot be replaced

Regression of REQ-ACREATE-15: in edit mode the locked Artist field renders its clear icon disabled, so the only unlock affordance is gone and a saved song's artist cannot be changed.


## How this was found

Helder, on-device, 2026-09-23 (re-checking INLINE-AC's open BUG-069/BUG-070). Opening an
existing song in `SongFormPage`: the Artist field shows the persisted artist, but the clear (**X**)
icon is rendered **disabled**. Helder recalls it was enabled "some versions ago".

## Why Major

REQ-ACREATE-15 (`changes/2026-07-21-inline-artist-create/requirements.md`) makes the X icon the
**only** way to unlock a locked artist field. With it disabled, a saved song's artist cannot be
changed at all: no workaround. It also blocks REQ-ACREATE-16 (persisting a changed artist) on device,
and makes BUG-070 (misleading copy in edit mode) impossible to test.

## Leading hypothesis (unverified, for the fix task to confirm or refute)

`SongFormPage.xaml` binds `IsReadOnly="{Binding IsArtistLocked}"` on the DX `AutoCompleteEdit`, and
edit-load sets `IsArtistLocked = true` (BUG-052/BUG-024 hydration). A read-only DX editor plausibly
disables its own clear icon, so the lock suppresses the only unlock control. If so, the earlier
behaviour predates the switch to `IsReadOnly`, or predates edit-load locking. Confirm against the DX
25.2.x `AutoCompleteEdit` docs (Context7, version-pinned) before choosing a fix. Needs a
`git log -S IsReadOnly` on `SongFormPage.xaml` to date the change.

## Related, same session (not this bug)

- **BUG-069 still reproduces**, in two forms: (a) on edit-load the dropdown opens by itself, listing
  the persisted artist; (b) on a new song, right after picking an artist the dropdown re-opens,
  listing the artist just picked. BUG-069 was logged in the INLINE-AC task-log but never given a
  generated row/folder.
