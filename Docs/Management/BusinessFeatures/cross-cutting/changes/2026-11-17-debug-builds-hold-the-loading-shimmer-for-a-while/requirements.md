# Requirements: Debug builds hold the loading shimmer for a while

> **Status: APPROVED 2026-11-17.** Helder: "shimmer must be ready to appear. Currently crud lists load
> instantly, but later we can do changes to them making them become slow. You can add a shimmer show
> for a while in debug builds for testing purposes, but must run only in debug builds."

## Goal

Local loads finish in milliseconds, so the loading shimmer of the lists and of the song form is never
seen and cannot be checked. Debug builds keep it on screen for a short time; release builds are
unchanged.

## Acceptance criteria

- **REQ-DLH-01: Debug builds hold the first-load shimmer.** WHEN the app is a debug build, the first
  load of a CRUD list (Venues, Songs, Artists, Singers, and every list built on the shared list base)
  and the edit-mode load of the song form SHALL take at least the hold time of 1,500 ms before the
  loaded content replaces the shimmer. The wait happens off the UI thread: the screen stays usable
  and Back works during it.
- **REQ-DLH-02: Release builds are unchanged.** WHEN the app is a release build, no hold exists: the
  code path is compiled out, not switched off at run time.
- **REQ-DLH-03: Tests are not slowed.** The hold is switched on only by the app's start-up code, so
  unit tests that build view models directly run with no hold, in any build configuration.
- **REQ-DLH-04: One place.** The hold time is one named value in one place.

- **REQ-DLH-05: Every slow list load shows the shimmer, in every build.** (Helder, 2026-11-17: "any
  slow list load, excluding those already built-in from the DX component, is welcome") WHILE a list
  replaces its rows — first load, a later visit, a search, or a filter reload (Artists role filter,
  a singer's or artist's catalog) — the list SHALL show the shimmer in place of the rows until the
  new rows are applied. Pull-to-refresh keeps the DevExpress refresh circle and loading more on
  scroll keeps the DevExpress load-more indicator: neither shows the shimmer. In debug builds the
  hold of REQ-DLH-01 applies to every load that shows the shimmer.
  - A load that is superseded (a newer search started) does not end the shimmer; the newest load does.

## Out of scope

- Pull-to-refresh and load-more (DevExpress shows its own indicators).
- A minimum shimmer time in release builds (Helder: lists load instantly today; revisit when a list
  becomes slow).
- Field search indicators (separate item `../../../artists-songs/changes/2026-11-17-remote-and-local-field-searches-show-a-loading-indicator/`).

## Design

- A static holder in the UI project, e.g. `LoadingHold` with `TimeSpan Duration` (default
  `TimeSpan.Zero`) and `Task WaitAsync(CancellationToken)` that returns at once when the duration is zero.
- `MauiProgram.CreateMauiApp` sets `LoadingHold.Duration = TimeSpan.FromMilliseconds(1500)` inside
  `#if DEBUG` (REQ-DLH-02, 03, 04). `MauiProgram.cs` is a sequential-only file: one writer.
- `CrudListViewModelBase.InitializeAsync`, first load only: the fetch and the hold run together inside
  the existing `Task.Run` (`Task.WhenAll`), so the shimmer lasts max(fetch, hold).
- `SongFormViewModel` edit-mode load: the same, inside its off-UI-thread load (after the load fix of
  2026-11-17 is merged).
- `CrudListView` (governed) is not edited. `CrudListViewModelBase` is a shared base class, not a
  governed UI component; its consumers are the four list view models (Venues, Songs, Artists, Persons):
  risk per consumer is the same — the first load waits 1.5 s in debug only; check: each list shows the
  shimmer once on a debug device.

## Testing

Level B. Unit: `LoadingHold.WaitAsync` returns at once with a zero duration; with a set duration the
base's first load does not finish before it (set and reset the duration in the test). Device check
(Helder): in a debug build each list and the song form show the shimmer for about 1.5 s.
