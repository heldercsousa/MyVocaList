# DesignSystem Implementation Tasks

> **For:** Claude Code execution
> **Reference:** DesignSystem_Implementation_Guide.md (Parts 1-7, 9-13)
> **Base path:** `MyVocaList/`

---

## Phase 1: Foundation

### Task 1: Roboto Fonts
**Manual step** - Download from https://fonts.google.com/specimen/Roboto
- Place in `Resources/Fonts/`: Roboto-Regular.ttf, Roboto-Medium.ttf, Roboto-Bold.ttf
- Set Build Action: `MauiFont`

### Task 2: MauiProgram.cs
**File:** `MauiProgram.cs`
**Action:** Update ConfigureFonts + add DI registration
**Reference:** Guide Part 1 - MauiProgram.cs Configuration

### Task 3: App.xaml.cs
**File:** `App.xaml.cs`
**Action:** Add `MaterialDesignControls.InitializeComponents()` after InitializeComponent()
**Reference:** Guide Part 1 - App.xaml.cs Initialization

### Task 4: ThreadSafeViewModelBase
**File:** `UI/ViewModels/ThreadSafeViewModelBase.cs`
**Action:** Create new file
**Reference:** Guide Part 3 - ThreadSafeViewModelBase

### Task 5: ThreadSafeDialogService
**File:** `UI/Services/ThreadSafeDialogService.cs`
**Action:** Create new file (interface + implementation)
**Reference:** Guide Part 3 - ThreadSafeDialogService

### Task 6: MaterialStyles.xaml
**File:** `Resources/Styles/MaterialStyles.xaml`
**Action:** Add typography + list item + selection control styles
**Reference:** Guide Part 7

---

## Phase 2: Demo Pages

**Base path:** `UI/Pages/DesignSystem/`

### Task 7: ComponentsPage_Typography
**Files:** `ComponentsPage_Typography.xaml` + `.xaml.cs`
**Shows:** All 15 typography roles (Display, Headline, Title, Body, Label)
**Reference:** Guide Part 8 - Task 7

### Task 8: ComponentsPage_Buttons
**Files:** `ComponentsPage_Buttons.xaml` + `.xaml.cs`
**Shows:** 5 button variants + icon buttons
**Reference:** Guide Part 8 - Task 8

### Task 9: ComponentsPage_Cards
**Files:** `ComponentsPage_Cards.xaml` + `.xaml.cs`
**Shows:** MDC cards (Elevated, Filled, Outlined) + Frame fallbacks
**Reference:** Guide Part 8 - Task 9

### Task 10: ComponentsPage_Inputs
**Files:** `ComponentsPage_Inputs.xaml` + `.xaml.cs`
**Shows:** TextField, CheckBox, Switch, Slider, DatePicker, TimePicker
**Reference:** Guide Part 8 - Task 10

### Task 11: ComponentsPage_Lists
**Files:** `ComponentsPage_Lists.xaml` + `.xaml.cs`
**Shows:** 1/2/3-line items, SwipeView
**Reference:** Guide Part 8 - Task 11

### Task 12: ComponentsPage_Feedback
**Files:** `ComponentsPage_Feedback.xaml` + `.xaml.cs`
**Shows:** FAB sizes, Snackbar, Progress indicators
**Reference:** Guide Part 8 - Task 12

### Task 13: DesignSystemPage (Navigation Hub)
**File:** `DesignSystemPage.xaml` + `.xaml.cs`
**Action:** Update with navigation to all component pages
**Reference:** Guide Part 8 - Task 13

### Task 14: AppShell Route Registration
**File:** `AppShell.xaml.cs`
**Action:** Register routes for all new pages
**Reference:** Guide Part 8 - Task 14

---

## Validation

After each phase, verify:
- [ ] App builds without errors
- [ ] App launches on Android emulator
- [ ] No threading errors in debug output

Full criteria: Guide Part 12 - Success Criteria
