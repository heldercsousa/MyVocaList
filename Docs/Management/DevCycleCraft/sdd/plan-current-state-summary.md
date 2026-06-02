# .claude Current State — Analysis Summary
> Generated for SDD→.claude enhancement analysis. Reference this instead of reading all rules files individually.

## What Already Exists

### CLAUDE.md
- Stack: .NET MAUI 10, C# 13, DevExpress MAUI v25.2.4, CommunityToolkit.Mvvm, EF Core 10, SQLite
- Architecture: Domain → Contracts → Infra → Services → MAUI (strict layering, no reverse deps)
- Workflow rule: spec-first (requirements.md + design.md + tasks.md) before any code
- Subagent delegation rule: main agent runs shell, subagents write code; max 4 parallel
- Commit rule: `/project:commit` after every task
- Continuous enhancement rule: after every task, ask what should improve CLAUDE.md/rules/library
- Language: English only (code, comments, UI)
- Non-negotiables: no DisplayAlert/ActionSheet/PromptAsync; DevExpress first; MD3 terminology; SafeAreaEdges="Container"
- MCP gate: Context7 → Exa → WebSearch hierarchy before any research

### .claude/rules/code-principles.md
- Nullable reference types: enabled but lenient (specific warnings suppressed)
- Service return patterns: tuples (success, message, entity?)
- Exception handling: GlobalExceptionHandler catches unexpected; only specific try-catch patterns allowed
- ObservableRangeCollection: never call ReplaceRange more than once per RunOnUiThread block (ANR risk)
- Global usings: 2+ types in project → GlobalUsings.cs; 2+ projects → Directory.Build.props
- Pagination: AppPagination.DefaultPageSize = 20 (single source of truth)
- DI: AddSingleton for shared state, AddScoped for repos/services, AddTransient for pages/VMs
- XML docs: interface owns docs, implementors use <inheritdoc />
- Architecture constraints enforced: business logic in Services only

### .claude/rules/testing.md
- Test project: MyVocaList.Tests targeting net10.0 (not net10.0-android)
- Test types: Unit (Services, ViewModels) + Integration (Repositories with real SQLite)
- TDD workflow (Red→Green→Refactor) mandatory from Step 4 onward
- Anti-patterns: mock DbContext in repo tests, assert on private state, Thread.Sleep
- Naming: {Method}_{Context}_{Expected}
- Test structure: Unit/Services/, Unit/ViewModels/, Integration/Repositories/
- TestDbContextFactory: unique temp SQLite file per test (not in-memory EF provider)

### .claude/rules/workflow.md
- Rule 1: Spec-first (read design.md before any code)
- Rule 2: Subagent delegation (max 4 parallel, wave-based, paths-only briefings, status signal via task-log)
  - Subagent return statuses: `To Review` (build passed), `Build failure`, `blocked: spec gap`
  - Subagent exit checklist: verification-before-completion → build → commit → push
- Rule 3: Commit after every task
- Rule 4: tasks.md is source of truth
- Rule 5: Task status in task-log beside plan file at `Docs/superpowers/plans/` (manually recorded — no auto hooks for this)
  - `Docs/DevEnv/plans/` is SDD research only — task-logs do NOT go there
  - Statuses: `in progress`, `Check build`, `To Review`, `Build failure`, `blocked: spec gap`, `Spec updated — re-planning required`, `Early task done`, `Review task done`
- Rule 6: Research tool gate (Context7 → Exa → WebSearch)
- Hooks active: `UserPromptSubmit` (workflow gate on action keywords), `Stop` (uncommitted changes warning), `PreToolUse` (rtk token filtering via global settings)

### .claude/rules/mediatr-patterns.md
- Status: MediatR planned but NOT yet registered. Reference patterns only.
- Command, Query, Notification patterns documented
- Current architecture: direct service interfaces (IVenueService etc.)

### .claude/library/crud-pages.md
- Three laws: MD3 always; use existing components; DevExpress first
- Spec-first workflow: Brainstorm → Spec → Plan → Implement → Review
- AppBar: SmallAppBar + SearchAppBar in Shell.TitleView; never custom Grid
- List: DXCollectionView + ListItem always; never cards
- FloatingToolbar: always visible, slot CanExecute gates; FAB in shared HorizontalStackLayout
- Form: dedicated Shell nav page (never BottomSheet for keyboard input)
- ViewModel checklist: ~20 required properties/commands for list pages
- Code-behind checklist: OnAppearing, OnCollectionViewScrolled, OnSelectionChanged, OnBackButtonPressed
- ConfirmDelete BottomSheet: HalfExpandedRatio=0.28
- Shimmer skeleton: 6 bones, await Task.Yield() before first fetch

### .claude/library/database-indexing.md
- Every WHERE/ORDER BY/JOIN field must have explicit index
- Index types: standard, unique, nullable unique, filtered/partial, composite
- Naming: IX_{TableName}_{FieldName(s)} always explicit via HasDatabaseName
- Collation: EF.Functions.Like + EF.Functions.Collate (never .StartsWith/.Contains)
- Required vs optional: always explicit (.IsRequired() or .IsRequired(false)) — no EF defaults

### .claude/library/devexpress-patterns.md
- Namespaces: dx, dxe, dxcv, dxg, dxc
- DXButton: 5 named styles in MaterialStyles.xaml
- DXCollectionView: SelectedItems requires IList wrapper; AllowCascadeUpdate=False always
- Multi-select: long press enters, DXCollectionView handles natively in Multiple mode
- ShimmerView: await Task.Yield() before data load
- TextEdit: BoxCornerRadius removed in 25.1.3+
- BottomSheet: full patterns for confirm/edit sheets
- SwipeContainerItem: use Tap event, not Command binding
- Compiled bindings: typed ViewModel property on page, not BindingContext.X
- SmallAppBar + SearchAppBar: custom ContentView components in AppBars/

### .claude/library/dialogs-validation.md
- Never: DisplayAlert, DisplayActionSheet, DisplayPromptAsync
- Use BottomSheet for confirmations; Shell nav page for forms with keyboard
- ConfirmSheet component exists but has ANR bug (avoid until DX fixes it)
- TextEdit validation: HasError + ErrorText properties
- Snackbar: ISnackbarService for non-blocking feedback

### .claude/library/m3-components.md
- MD3 terminology: Headline, SupportingText (not Body), Illustration, LeadingContent, TrailingContent
- Type scale: 10 StyleClass entries in MaterialStyles.xaml
- Small Top App Bar: 64dp, Surface→SurfaceContainer on scroll
- FloatingToolbar: M3 Expressive component, 64dp, SecondaryContainer bg, max 5 slots
- Empty State: Illustration + Headline + SupportingText slots
- List item variants: 1-line (56dp), 2-line (72dp), 3-line (88dp, top-align)

### .claude/library/theme-locale.md
- Dark mode only (light planned for v2.0)
- Option B theme: full token override in MaterialColors.xaml (no ThemeManager seed)
- Indigo palette, seed ~#4858AB
- StaticResource for layout properties; dx:ThemeColor for DX-specific properties
- Never raw hex, Color.FromArgb, or Colors.X in XAML
- No localization active (useLocalization: false)

### .claude/library/ux-patterns.md
- Touch targets: 48×48dp minimum (WCAG 2.5.5 / MD3)
- Contextual action bar (5-column Grid): RETIRED in favor of SmallAppBar.Title + FloatingToolbar
- Multi-select: long press enters, exit on Cancel or count→0
- Empty state: vertically centered (never Start)
- Badge/chip: DXBorder + Label with count

### .claude/commands/review.md
- Checklist: build → code quality → MAUI specifics → architecture → DevExpress
- Mandatory enhancement check after every review
- Specific antipatterns to catch: DisplayAlert, hardcoded colors, hardcoded strings, etc.

## What Does NOT Exist (gaps relative to SDD concepts)
- No rule for cross-session context loss recovery strategy
- No rule for spec versioning or rollback when requirements change
- No rule for hallucination detection or verification before completion (skill exists but not in CLAUDE.md)
- No rule about adversarial/adversarial review patterns
- No rule for context window exhaustion management in long tasks
- No rule for dependency ordering between parallel agents
- No rule for silent task completion detection
- No rule for brownfield retrofit strategy
- No rule about task atomization guidance beyond "commit after every task"
- review.md doesn't cover spec-drift detection or spec vs code consistency checks
