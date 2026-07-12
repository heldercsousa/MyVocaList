# CRUD Form Action Pattern — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move Save on `SongFormPage` from an in-body button into the native Shell top-app-bar trailing slot (`ToolbarItem`), and remove the now-redundant in-body Cancel button, per BACKLOG row 168.

**Architecture:** XAML-only change to a single page. Add `<ContentPage.ToolbarItems><ToolbarItem .../></ContentPage.ToolbarItems>` bound to the existing `SaveCommand`; delete the inline `HorizontalStackLayout` containing the Cancel/Save `dx:DXButton` pair. No ViewModel, Service, or Domain changes — `SaveCommand`/`CancelCommand` are reused as-is. Two rules-file updates document the new pattern as the general law for full-screen CRUD forms.

**Tech Stack:** .NET MAUI 10, XAML, CommunityToolkit.Mvvm (`IAsyncRelayCommand`).

## Global Constraints

- `SafeAreaEdges="Container"` already set on `SongFormPage` — do not change.
- English only in code, comments, docs (CLAUDE.md).
- XAML edits: one file → build → fix → next file (CLAUDE.md incremental-edits rule) — Task 1 is the only XAML task, so this applies within Task 1 only (no batching across files).
- No native dialogs (`DisplayAlert`/etc.) — not applicable here, no dialogs touched.
- Scope is `SongFormPage.xaml` only. `ArtistFormPage.xaml`, `PersonFormPage.xaml`, `VenueFormPage.xaml` must NOT be modified (AC-7) — this is a hard boundary from `requirements.md`.
- `SaveCommand`/`CancelCommand` are manually declared `IAsyncRelayCommand` properties (`SongFormViewModel.cs:152-153`, instantiated `SongFormViewModel.cs:123-124` as `new AsyncRelayCommand(SaveAsync)` / `new AsyncRelayCommand(CancelAsync)`) with **no `CanExecute` predicate** — both commands are always executable. This plan does not add one (AC-2's "matches SaveCommand.CanExecute" is satisfied trivially: both the old inline button and the new ToolbarItem have no disabling condition, so behavior is identical, not because a new predicate was added).

---

### Task 1: Move Save to ToolbarItem, remove inline Cancel/Save buttons on SongFormPage

**Files:**
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml:14-19` (add `ContentPage.ToolbarItems` after the `SafeAreaEdges="Container">` root tag, before `<!-- Root Grid so BottomSheets can overlay the ScrollView -->`)
- Modify: `MyVocaList/UI/Pages/Songs/SongFormPage.xaml:215-224` (delete the inline `HorizontalStackLayout` block)

**Interfaces:**
- Consumes: `SongFormViewModel.SaveCommand` (`IAsyncRelayCommand`, already exists, no change) — `MyVocaList/UI/ViewModels/SongFormViewModel.cs:152`
- Produces: nothing consumed by later tasks (Tasks 2-3 are documentation-only and don't reference this XAML).

- [ ] **Step 1: Add the ToolbarItem**

Current file header (`SongFormPage.xaml:1-18`):
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:dx="http://schemas.devexpress.com/maui"
    xmlns:dxe="clr-namespace:DevExpress.Maui.Editors;assembly=DevExpress.Maui.Editors"
    xmlns:vm="clr-namespace:MyVocaList.UI.ViewModels"
    xmlns:autocomplete="clr-namespace:MyVocaList.UI.Components.AutocompleteField"
    xmlns:lists="clr-namespace:MyVocaList.UI.Components.Lists"
    xmlns:dtoList="clr-namespace:MyVocaList.Contracts.DTOs.List;assembly=MyVocaList.Contracts"
    xmlns:resolution="clr-namespace:MyVocaList.Domain.Resolution;assembly=MyVocaList.Domain"
    x:Class="MyVocaList.UI.Pages.Songs.SongFormPage"
    x:DataType="vm:SongFormViewModel"
    Title="{Binding PageTitle}"
    BackgroundColor="{StaticResource Surface}"
    SafeAreaEdges="Container">

    <!-- Root Grid so BottomSheets can overlay the ScrollView -->
    <Grid>
```

Insert a `ContentPage.ToolbarItems` block immediately after the root `ContentPage` opening tag (after `SafeAreaEdges="Container">`) and before the `<!-- Root Grid -->` comment:

```xml
    <ContentPage.ToolbarItems>
        <ToolbarItem Text="Save" Command="{Binding SaveCommand}" />
    </ContentPage.ToolbarItems>

    <!-- Root Grid so BottomSheets can overlay the ScrollView -->
    <Grid>
```

`ToolbarItem` is in the default MAUI namespace already mapped as `xmlns="http://schemas.microsoft.com/dotnet/2021/maui"` — no new `xmlns` import needed. No `x:DataType` needed on `ToolbarItem` itself; it inherits the page's `BindingContext` for the `Command` binding.

- [ ] **Step 2: Remove the inline Cancel/Save button block**

Current block (`SongFormPage.xaml:215-224`):
```xml
                <HorizontalStackLayout HorizontalOptions="End" Spacing="8">
                    <dx:DXButton Content="Cancel"
                                 Style="{StaticResource OutlinedButton}"
                                 Padding="24,0"
                                 Command="{Binding CancelCommand}" />
                    <dx:DXButton Content="Save"
                                 Style="{StaticResource FilledButton}"
                                 Padding="24,0"
                                 Command="{Binding SaveCommand}" />
                </HorizontalStackLayout>
```

Delete this entire block (all 10 lines, including the opening/closing `HorizontalStackLayout` tags). Do not leave an empty `HorizontalStackLayout` or stray whitespace-only lines — remove cleanly so the preceding sibling element's closing tag is followed directly by the next element (or the parent's closing tag) at the correct indentation.

- [ ] **Step 3: Build**

Run: `dotnet build MyVocaList.sln`
Expected: 0 errors. If DevExpress `dx:` namespace import becomes unused after removing the only `dx:DXButton` usage on this page, that is NOT an error (XAML unused-namespace is not a build failure) — do not remove the `xmlns:dx` declaration; other `dx:` elements likely still exist elsewhere in the file (verify via build success, not by searching).

- [ ] **Step 4: Manual E2E smoke test (Level C — no automated test per testing.md)**

On emulator/device, verify and record in `task-log.md`:
1. SongFormPage top app bar shows a "Save" toolbar item (top-right).
2. No Cancel or Save button remains in the form body.
3. Tapping the toolbar "Save" persists the song and navigates away, identically to the old inline Save button.
4. Tapping the native back button discards unsaved changes, identically to the old inline Cancel button's behavior (verify `CancelAsync` logic — e.g. confirmation prompt if one existed — is unaffected, since `CancelCommand` itself is untouched by this change and simply has no XAML trigger anymore other than back-navigation, if the ViewModel already wires `CancelCommand` to back-navigation via `OnNavigatedFrom`/hardware-back-button handling — if it does NOT, note this as a finding rather than assuming).

- [ ] **Step 5: Commit**

```bash
git add MyVocaList/UI/Pages/Songs/SongFormPage.xaml
git commit -m "feat: SongFormPage — move Save to AppBar ToolbarItem, remove inline Cancel/Save buttons

Per BACKLOG row 168 (CRUD Form Action Pattern). Song is a pushed Shell
page, not a modal — native back button remains the sole dismiss action;
Save moves to the trailing ToolbarItem slot, bound to the existing
SaveCommand (no new binding infrastructure, no SmallAppBar/governed-
component change).

Spec: Docs/Management/DevCycleCraft/crud-form-action-pattern/"
```

---

### Task 2: Update crud-pages.md — ToolbarItem-Save as the general law for full-screen forms

**Files:**
- Modify: `.claude/library/crud-pages.md` (Form Page — Laws and Variants section, the button-placement variants currently documenting inline Cancel+Save)

**Interfaces:**
- Consumes: nothing from Task 1.
- Produces: nothing consumed by later tasks (Task 3 references this rule by name, not by binding contract).

- [ ] **Step 1: Replace the documented button-placement variants**

Locate the two currently-documented variants (inline `HorizontalStackLayout(End)` with Cancel+Save, and the sticky-bottom `Grid RowDefinitions="*,Auto"` variant) in the "Form Page — Laws and Variants" section. Replace both with:

```markdown
### Save/Cancel placement (full-screen forms)

**Law:** full-screen CRUD forms use a native Shell `ToolbarItem` for Save, in the top app bar's trailing slot — never an in-body button. The native Shell back button is the sole dismiss/discard action; no in-body Cancel button.

```xml
<ContentPage.ToolbarItems>
    <ToolbarItem Text="Save" Command="{Binding SaveCommand}" />
</ContentPage.ToolbarItems>
```

Rationale: Cancel is redundant with back-navigation once a form occupies the whole screen (it remains meaningful for bottom sheets/modals — see the sheet/modal form pattern, which keeps in-sheet Save/Cancel). Save reads better as a top-app-bar action per MD3's full-screen-dialog guidance. Full research + decision trail: `Docs/Management/DevCycleCraft/crud-form-action-pattern/design.md`.

**Currently non-compliant (as of 2026-07-12):** `ArtistFormPage`, `PersonFormPage`, `VenueFormPage` still use the old inline Cancel+Save pattern — they are pending a bottom-sheet/modal conversion decision (BACKLOG rows 43-45); only `SongFormPage` has been migrated to this law so far. Do not treat the other three as a bug — they are tracked separately. If a form's bottom-sheet conversion is later declined, migrate it to this ToolbarItem pattern as a follow-up task.
```

Remove the old inline-`HorizontalStackLayout` and sticky-bottom-`Grid` code samples entirely — they no longer represent the documented law. If either surrounding paragraph references "Cancel" as a required element elsewhere in the Form Page section, update that prose to match (grep the file for "Cancel" after this edit to confirm no stale references remain).

- [ ] **Step 2: Verify no stale references remain**

Run: `grep -n "Cancel" .claude/library/crud-pages.md`
Expected: no remaining references describing Cancel as a required/standard form element (references explaining sheet/modal forms keeping Cancel, or this task's own non-compliance note, are fine).

- [ ] **Step 3: Commit**

```bash
git add .claude/library/crud-pages.md
git commit -m "docs: crud-pages.md — ToolbarItem-Save is the general law for full-screen CRUD forms

Replaces the two inline-button variants. Notes Artist/Person/Venue as
currently non-compliant pending their bottom-sheet conversion decisions.

Spec: Docs/Management/DevCycleCraft/crud-form-action-pattern/"
```

---

### Task 3: Cross-reference the pattern in m3-components.md

**Files:**
- Modify: `.claude/library/m3-components.md` (near the existing `SmallAppBar`/trailing-action-slot guidance)

**Interfaces:**
- Consumes: nothing from Tasks 1-2 (documentation only).
- Produces: nothing.

- [ ] **Step 1: Add the cross-reference note**

Near the existing "Use SmallAppBar trailing Action1-3 slots when ≤ 3 actions suffice" guidance, add:

```markdown
**Full-screen CRUD forms (not list pages):** use a native Shell `ToolbarItem` for Save, not `SmallAppBar`. Full pattern + rationale: `crud-pages.md § Save/Cancel placement (full-screen forms)`.
```

- [ ] **Step 2: Commit**

```bash
git add .claude/library/m3-components.md
git commit -m "docs: m3-components.md — cross-reference ToolbarItem-Save pattern for full-screen forms

Spec: Docs/Management/DevCycleCraft/crud-form-action-pattern/"
```

---

### Task 4: BACKLOG.md status update

**Files:**
- Modify: `Docs/Management/BACKLOG.md` (row 168, row 46)

**Interfaces:**
- Consumes: completion of Tasks 1-3 (this task only runs after all three are committed).
- Produces: nothing.

- [ ] **Step 1: Update row 168 status to ✅ Done**

Change the status cell from `📋 Spec` to `✅ Done`. Append to the row's note: ` **Implemented 2026-07-12** — SongFormPage ToolbarItem-Save shipped; crud-pages.md/m3-components.md updated. Commits: [fill in the 3 commit SHAs from Tasks 1-3 once available].`

- [ ] **Step 2: Update row 46 status**

Change the status cell from `📋 Spec` to `✅ Done` (this row already carries the sequencing-override note from spec time — leave that note intact, just update the status marker and append `**Implemented 2026-07-12.**`).

- [ ] **Step 3: Commit**

```bash
git add Docs/Management/BACKLOG.md
git commit -m "docs: BACKLOG — mark CRUD Form Action Pattern (row 168) and Song AppBar-save (row 46) as Done"
```

---

## Self-Review Notes

- **Spec coverage:** AC-1/AC-3 → Task 1 Step 1 (ToolbarItem bound to SaveCommand). AC-2 → Global Constraints note (no CanExecute predicate exists on either the old button or the new ToolbarItem — behavior is identical by construction, not by new logic). AC-4/AC-5 → Task 1 Step 2. AC-6 → Task 1 Step 4 (manual verification; flagged as a finding-not-assumption if CancelCommand's back-button wiring is unclear). AC-7 → Global Constraints hard boundary (no task touches the other three pages). AC-8 → Task 2. AC-9 → Task 3.
- **Placeholder scan:** none found — all code blocks are complete, all file paths/line numbers are from verified Explore-agent reads.
- **Type consistency:** `SaveCommand`/`CancelCommand` referenced consistently as `IAsyncRelayCommand` across constraints and Task 1; no signature invented.
- **Open finding surfaced to implementer (Task 1 Step 4):** whether `CancelCommand`/back-navigation wiring already exists independent of the deleted Cancel button was not verified during spec/plan research (out of scope for XAML-only Explore reads) — the implementer must confirm this during manual E2E and report as a finding if back-button discard behavior turns out to differ from the old Cancel button's.
