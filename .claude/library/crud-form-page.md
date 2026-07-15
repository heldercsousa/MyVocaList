# CRUD Page Design Laws — Form Page — laws and variants

> Section file split from `crud-pages.md` on 2026-07-14 (token-scoped reads). Index + never-miss rules: `crud-pages.md`.

## Form Page — Laws and Variants

### Law
Add/Edit forms are always separate Shell navigation pages. Never use a `BottomSheet` for a form that accepts keyboard input — the keyboard covers the sheet on Android.

### Standard layout
```xml
<ContentPage SafeAreaEdges="All" ...>
    <ScrollView>
        <VerticalStackLayout Padding="24" Spacing="16">
            <!-- fields -->
            <!-- action buttons -->
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

`SafeAreaEdges="All"` + `ScrollView` handles keyboard avoidance automatically.

### Save/Cancel placement (full-screen forms)

**Law:** full-screen CRUD forms use a native Shell `ToolbarItem` for Save, in the top app bar's trailing slot — never an in-body button. The native Shell back button is the sole dismiss/discard action; no in-body Cancel button.

```xml
<ContentPage.ToolbarItems>
    <ToolbarItem Text="Save" Command="{Binding SaveCommand}" />
</ContentPage.ToolbarItems>
```

Rationale: Cancel is redundant with back-navigation once a form occupies the whole screen (it remains meaningful for bottom sheets/modals — see the sheet/modal form pattern, which keeps in-sheet Save/Cancel). Save reads better as a top-app-bar action per MD3's full-screen-dialog guidance. Full research + decision trail: `Docs/Management/DevCycleCraft/crud-form-action-pattern/design.md`.

**Currently non-compliant (as of 2026-07-12):** `ArtistFormPage`, `PersonFormPage`, `VenueFormPage` still use the old inline Cancel+Save pattern — they are pending a bottom-sheet/modal conversion decision (BACKLOG rows 43-45); only `SongFormPage` has been migrated to this law so far. Do not treat the other three as a bug — they are tracked separately. If a form's bottom-sheet conversion is later declined, migrate it to this ToolbarItem pattern as a follow-up task.

### Validation (law)
All form fields validate per the **Form Validation Standard** in `dialogs-validation.md` — validate on blur
(dirty fields), switch to keystroke-on-error so the error clears the moment it is fixed, and use Save as the
safety net for cross-field / uniqueness / DB checks. Errors are inline per field via `HasError`/`ErrorText`
only — never a summary, dialog, or snackbar. Validation rules live in `Validate<Field>Input` service methods
(business logic in Services), not in ViewModels or pages. This is a pointer; the standard is single-sourced in
`dialogs-validation.md`.

### Never
- Do not use `DisplayAlert`, `DisplayActionSheet`, or `DisplayPromptAsync` for validation or confirmation. See `dialogs-validation.md`.
- Do not validate on Save only (the "Wall of Red" anti-pattern) — see the Form Validation Standard in `dialogs-validation.md`.
- Do not use `FloatingToolbar` on a form page — it is hidden behind the keyboard on Android.

---
